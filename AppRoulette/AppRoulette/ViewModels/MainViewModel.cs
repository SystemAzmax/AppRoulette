using AppRoulette.Models;
using AppRoulette.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AppRoulette.ViewModels;

/// <summary>
/// メイン画面の ViewModel。
/// グループ管理・アイテム管理・ルーレット選択ロジックを担います。
/// </summary>
public class MainViewModel : ObservableObject
{
    private readonly IRandomService _randomService;
    private readonly IItemRepository _itemRepository;

    /// <summary>直前の <see cref="ItemsText"/> の値（改行増加検出に使用）。</summary>
    private string _previousItemsText = string.Empty;

    private IReadOnlyList<RouletteGroup> _groupList =
        Array.Empty<RouletteGroup>();

    private RouletteGroup? _selectedGroup;

    private string _itemsText = string.Empty;

    private int _itemCount;

    private bool _isSpinning;

    private int _selectedItemIndex = -1;

    /// <summary>
    /// ComboBox に表示するグループ一覧を取得します。
    /// </summary>
    public IReadOnlyList<RouletteGroup> GroupList
    {
        get => _groupList;
        private set => SetProperty(ref _groupList, value);
    }

    /// <summary>
    /// 現在選択中のグループを取得または設定します。
    /// </summary>
    public RouletteGroup? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                SpinCommand.NotifyCanExecuteChanged();
                OnSelectedGroupChanged(value);
            }
        }
    }

    /// <summary>
    /// テキストエリアに表示するアイテムテキスト（1行1アイテム）を取得または設定します。
    /// </summary>
    public string ItemsText
    {
        get => _itemsText;
        set
        {
            if (SetProperty(ref _itemsText, value))
            {
                OnItemsTextChanged(value);
            }
        }
    }

    /// <summary>
    /// 現在のグループのアイテム数を取得します。
    /// ComboBox 右隣に表示します。
    /// </summary>
    public int ItemCount
    {
        get => _itemCount;
        private set
        {
            if (SetProperty(ref _itemCount, value))
            {
                SpinCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// ルーレットが回転中かどうかを取得または設定します。
    /// フェーズ6のアニメーションから変更されます。
    /// </summary>
    public bool IsSpinning
    {
        get => _isSpinning;
        set
        {
            if (SetProperty(ref _isSpinning, value))
            {
                SpinCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// ルーレットの出目アイテムインデックス（0 始まり）を取得します。
    /// -1 は未選択を示します。
    /// </summary>
    public int SelectedItemIndex
    {
        get => _selectedItemIndex;
        private set => SetProperty(ref _selectedItemIndex, value);
    }

    /// <summary>グループデータを読み込み初期状態に設定するコマンド。</summary>
    public IAsyncRelayCommand InitializeCommand { get; }

    /// <summary>
    /// ルーレットを回してアイテムをランダムに選択するコマンド。
    /// アイテムが 1 件以上存在し、かつ回転中でない場合に実行可能。
    /// </summary>
    public IRelayCommand SpinCommand { get; }

    /// <summary>
    /// <see cref="MainViewModel"/> を初期化します。
    /// </summary>
    /// <param name="randomService">ランダム生成サービス。</param>
    /// <param name="itemRepository">SQLite Item リポジトリ。</param>
    public MainViewModel(
        IRandomService randomService,
        IItemRepository itemRepository)
    {
        _randomService = randomService;
        _itemRepository = itemRepository;
        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        SpinCommand = new RelayCommand(Spin, CanSpin);
    }

    /// <summary>
    /// グループデータを初期化し、SQLite から Items を読み込みます。
    /// JSON は使用せず、デフォルトの3グループを作成して SQLite から Items を充填します。
    /// </summary>
    private async Task InitializeAsync()
    {
        // デフォルトグループを作成（グループ1～3）
        var groups = new List<RouletteGroup>(RouletteGroup.GROUP_COUNT);
        for (var i = 1; i <= RouletteGroup.GROUP_COUNT; i++)
        {
            groups.Add(new RouletteGroup(i, $"グループ{i}"));
        }

        // SQLite から各グループのアイテムを読み込み、グループに充填
        foreach (var group in groups)
        {
            var dbItems = await _itemRepository.GetItemsByGroupAsync(group.Id);
            group.Items = dbItems
                .Select(item => new RouletteItem(item.Label))
                .ToList();
        }

        GroupList = groups;
        SelectedGroup = groups.Count > 0 ? groups[0] : null;
    }

    /// <summary>
    /// ルーレットを回してアイテムをランダムに選択します。
    /// 各アイテムの Weight 値に基づいて確率的に選択されます。
    /// </summary>
    private void Spin()
    {
        if (SelectedGroup is null || SelectedGroup.Items.Count == 0)
        {
            return;
        }

        var selectedItem = _randomService.SelectByWeight(SelectedGroup.Items);
        if (selectedItem is not null)
        {
            SelectedItemIndex = SelectedGroup.Items.IndexOf(selectedItem);
        }
    }

    /// <summary>
    /// ルーレットを開始できるかどうかを返します。
    /// アイテムが 1 件以上存在し、かつ回転中でない場合に <c>true</c>。
    /// </summary>
    private bool CanSpin() => ItemCount > 0 && !IsSpinning;

    /// <summary>
    /// <see cref="ItemsText"/> 変更時にアイテムリストとアイテム数を更新し、
    /// 改行が増えた場合は非同期で SQLite に保存します。
    /// </summary>
    /// <param name="value">変更後のテキスト。</param>
    private void OnItemsTextChanged(string value)
    {
        if (SelectedGroup is null)
        {
            return;
        }

        var items = ParseItems(value);
        SelectedGroup.Items = items;
        ItemCount = items.Count;

        var previousLineCount = CountLines(_previousItemsText);
        var currentLineCount = CountLines(value);

        if (currentLineCount != previousLineCount)
        {
            // Fire-and-forget: テキスト変更時に非同期で SQLite に保存
            // （例外はデバッグ出力）
            _ = OnItemsTextChangedAsync().ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception is not null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[AppRoulette] 同期エラー: {t.Exception.InnerException}");
                    }
                },
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        _previousItemsText = value;
    }

    /// <summary>
    /// テキスト変更時に SQLite と JSON 両方に同期する処理
    /// </summary>
    private async Task OnItemsTextChangedAsync()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        // SQLite に同期のみ（JSON は不使用）
        await SyncItemsToSqliteAsync(SelectedGroup);
    }

    /// <summary>
    /// <see cref="SelectedGroup"/> 変更時にテキストとアイテム数を更新します。
    /// グループ切り替え前に、現在のグループの Items を SQLite に同期します。
    /// </summary>
    /// <param name="value">変更後のグループ。</param>
    private void OnSelectedGroupChanged(RouletteGroup? value)
    {
        if (value is null)
        {
            _previousItemsText = string.Empty;
            ItemsText = string.Empty;
            ItemCount = 0;
            return;
        }

        // グループを切り替える前に現在のグループの変更を SQLite と JSON に同期
        if (SelectedGroup is not null && SelectedGroup != value)
        {
            _ = OnSelectedGroupChangedAsync(SelectedGroup);
        }

        var text = FormatItems(value.Items);
        _previousItemsText = text;
        ItemsText = text;
        ItemCount = value.Items.Count;
        SelectedItemIndex = -1;
    }

    /// <summary>
    /// グループ変更時に SQLite に同期する処理
    /// </summary>
    private async Task OnSelectedGroupChangedAsync(RouletteGroup group)
    {
        // SQLite に同期のみ（JSON は不使用）
        await SyncItemsToSqliteAsync(group);
    }

    /// <summary>
    /// (このメソッドは使用されていません。SQLite 完全移行のため廃止予定)
    /// </summary>
    private async Task SaveAsync()
    {
        if (GroupList.Count == 0)
        {
            return;
        }

        // JSON は使用しない（SQLite のみ）
        await Task.CompletedTask;
    }

    /// <summary>
    /// グループの Items テキストを解析し、SQLite に同期します。
    /// （追加アイテムを挿入、削除アイテムを削除）
    /// </summary>
    private async Task SyncItemsToSqliteAsync(RouletteGroup group)
    {
        try
        {
            // SQLite から現在グループのアイテムを取得
            var dbItems = await _itemRepository.GetItemsByGroupAsync(group.Id);
            var dbLabels = new HashSet<string>(dbItems.Select(i => i.Label));

            // MemoryItems から新規追加されたアイテムを DB に追加
            foreach (var item in group.Items)
            {
                if (!dbLabels.Contains(item.Name))
                {
                    var newItem = new Item(item.Name, group.Id);
                    await _itemRepository.AddItemAsync(newItem);
                    System.Diagnostics.Debug.WriteLine(
                        $"[AppRoulette] Added item to SQLite: {item.Name} (GroupId={group.Id})");
                }
            }

            // DB に存在するが MemoryItems に無いアイテムを削除
            var memoryLabels = new HashSet<string>(group.Items.Select(i => i.Name));
            foreach (var item in dbItems)
            {
                if (!memoryLabels.Contains(item.Label))
                {
                    await _itemRepository.DeleteItemAsync(item.Id);
                    System.Diagnostics.Debug.WriteLine(
                        $"[AppRoulette] Deleted item from SQLite: {item.Label} (Id={item.Id})");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[AppRoulette] SyncItemsToSqliteAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// 改行区切りのテキストをアイテムリストに変換します。
    /// CSV形式（アイテム名,Weight）に対応しており、Weightが省略された場合はデフォルト値1を使用します。
    /// 
    /// 形式例：
    /// - "アイテムA" → Weight=1
    /// - "アイテムA,5" → Weight=5
    /// - "アイテムA,3\nアイテムB,1" → 複数アイテム
    /// </summary>
    /// <param name="text">変換元テキスト。</param>
    /// <returns>アイテムリスト。</returns>
    public static List<RouletteItem> ParseItems(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r').Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                // CSV形式（名前,Weight）に解析
                var parts = line.Split(',');
                var name = parts[0].Trim();
                var weight = 1; // デフォルト値

                if (parts.Length > 1)
                {
                    if (int.TryParse(parts[1].Trim(), out var parsedWeight))
                    {
                        // Weight値を1～5の範囲に制限
                        weight = Math.Max(1, Math.Min(5, parsedWeight));
                    }
                }

                return new RouletteItem(name) { Weight = weight };
            })
            .ToList();

    /// <summary>
    /// アイテムリストを改行区切りのCSV形式テキストに変換します。
    /// 各行は「アイテム名,Weight」の形式となります。
    /// </summary>
    /// <param name="items">変換元アイテムリスト。</param>
    /// <returns>改行区切りのCSV形式テキスト。</returns>
    public static string FormatItems(IEnumerable<RouletteItem> items) =>
        string.Join('\n', items.Select(i => $"{i.Name},{i.Weight}"));

    /// <summary>
    /// テキストの行数を返します。空文字は 0 を返します。
    /// </summary>
    /// <param name="text">対象テキスト。</param>
    /// <returns>行数。</returns>
    public static int CountLines(string text) =>
        string.IsNullOrEmpty(text)
            ? 0
            : text.Split('\n').Length;
}

/// <summary>
/// IItemRepository の利用例と XAML ViewModel 統合用のユーティリティクラス。
/// データベースの Items テーブルとの CRUD 操作を行い、
/// ObservableCollection により XAML にバインド可能な形で提供します。
/// </summary>
public class ItemRepositoryViewModel : ObservableObject
{
    private readonly IItemRepository _repository;
    private ObservableCollection<Item> _items = new();

    /// <summary>
    /// データベースから取得したアイテムのコレクション。
    /// XAML の ListBox や DataGrid にバインド可能です。
    /// </summary>
    public ObservableCollection<Item> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    private Item? _selectedItem;

    /// <summary>
    /// 選択中のアイテム。
    /// </summary>
    public Item? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private string _newItemLabel = string.Empty;

    /// <summary>
    /// 新規追加予定のアイテムラベル。
    /// </summary>
    public string NewItemLabel
    {
        get => _newItemLabel;
        set => SetProperty(ref _newItemLabel, value);
    }

    // Note: Weight は現在未実装のため、常に 1 に固定されます。
    // 将来的に Weight 設定機能が追加される場合はここで実装します。
    // private int _newItemWeight = 1;
    // public int NewItemWeight { ... }

    private int _currentGroupId = 1;

    /// <summary>
    /// 操作対象のグループ ID。
    /// </summary>
    public int CurrentGroupId
    {
        get => _currentGroupId;
        set => SetProperty(ref _currentGroupId, value);
    }

    /// <summary>
    /// ItemRepositoryViewModel を初期化します。
    /// </summary>
    public ItemRepositoryViewModel()
    {
        _repository = new SqliteItemRepository();
    }

    /// <summary>
    /// データベースからアイテムを読み込み、ObservableCollection に展開します。
    /// XAML ViewModel の初期化時に呼び出します。
    /// </summary>
    public async Task LoadItemsAsync()
    {
        try
        {
            var items = await _repository.GetItemsByGroupAsync(CurrentGroupId);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] Loaded {items.Count} items from group {CurrentGroupId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] LoadItemsAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// 新しいアイテムをデータベースに追加します。
    /// </summary>
    public async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemLabel))
        {
            System.Diagnostics.Debug.WriteLine(
                "[ItemRepositoryViewModel] Cannot add item with empty label");
            return;
        }

        try
        {
            // Weight は常に 1 に固定
            var newItem = new Item(NewItemLabel, CurrentGroupId);
            int insertedId = await _repository.AddItemAsync(newItem);

            newItem.Id = insertedId;
            Items.Add(newItem);

            NewItemLabel = string.Empty;

            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] Added item: {newItem}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] AddItemAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// 選択中のアイテムを削除します。
    /// </summary>
    public async Task DeleteSelectedItemAsync()
    {
        if (SelectedItem == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "[ItemRepositoryViewModel] No item selected for deletion");
            return;
        }

        try
        {
            int deletedCount = await _repository.DeleteItemAsync(SelectedItem.Id);
            if (deletedCount > 0)
            {
                Items.Remove(SelectedItem);
                System.Diagnostics.Debug.WriteLine(
                    $"[ItemRepositoryViewModel] Deleted item: {SelectedItem}");
            }

            SelectedItem = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] DeleteSelectedItemAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// 選択中のアイテムを更新します。
    /// </summary>
    public async Task UpdateSelectedItemAsync()
    {
        if (SelectedItem == null)
        {
            System.Diagnostics.Debug.WriteLine(
                "[ItemRepositoryViewModel] No item selected for update");
            return;
        }

        try
        {
            int updatedCount = await _repository.UpdateItemAsync(SelectedItem);
            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] Updated {updatedCount} item(s): {SelectedItem}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] UpdateSelectedItemAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// グループ内のすべてのアイテムを削除します。
    /// </summary>
    public async Task DeleteAllItemsInGroupAsync()
    {
        try
        {
            int deletedCount = await _repository.DeleteItemsByGroupAsync(CurrentGroupId);
            Items.Clear();

            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] Deleted {deletedCount} items from group {CurrentGroupId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ItemRepositoryViewModel] DeleteAllItemsInGroupAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用例を示すコメント。
    /// XAML ViewModel への組み込み例：
    /// 
    /// public sealed partial class MainWindow : Window
    /// {
    ///     private ItemRepositoryViewModel _itemViewModel = new();
    /// 
    ///     public MainWindow()
    ///     {
    ///         InitializeComponent();
    ///         Loaded += async (s, e) => await _itemViewModel.LoadItemsAsync();
    ///     }
    /// }
    /// 
    /// XAML:
    /// &lt;ListBox ItemsSource="{Binding _itemViewModel.Items}" /&gt;
    /// &lt;Button Content="Add" Click="AddButton_Click" /&gt;
    /// 
    /// コードビハインド:
    /// private async void AddButton_Click(object sender, RoutedEventArgs e)
    /// {
    ///     await _itemViewModel.AddItemAsync();
    /// }
    /// </summary>
    private static void UsageExample()
    {
        // 使用例をここに記載
    }
}