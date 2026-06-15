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
    private readonly IDataPersistenceService _dataPersistence;

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
    /// <param name="dataPersistence">データ永続化サービス。</param>
    public MainViewModel(
        IRandomService randomService,
        IItemRepository itemRepository,
        IDataPersistenceService dataPersistence)
    {
        _randomService = randomService;
        _itemRepository = itemRepository;
        _dataPersistence = dataPersistence;
        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        SpinCommand = new RelayCommand(Spin, CanSpin);
    }

    /// <summary>
    /// グループデータを初期化し、SQLite から Items を読み込みます。
    /// JSON は使用せず、デフォルトの9グループを作成して SQLite から Items を充填します。
    /// 前回起動時に選択されたグループを復元します。
    /// </summary>
    private async Task InitializeAsync()
    {
        // デフォルトグループを作成（Roulette1～9）
        var groups = new List<RouletteGroup>(RouletteGroup.GROUP_COUNT);
        for (var i = 1; i <= RouletteGroup.GROUP_COUNT; i++)
        {
            groups.Add(new RouletteGroup(i, $"Roulette{i}"));
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

        // 前回起動時に選択されたグループを復元
        var lastSelectedGroupId = await _dataPersistence.GetLastSelectedGroupIdAsync();
        var selectedGroup = groups.FirstOrDefault(g => g.Id == lastSelectedGroupId) 
            ?? (groups.Count > 0 ? groups[0] : null);
        SelectedGroup = selectedGroup;
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
            _ = OnItemsTextChangedAsync().ContinueWith(
                _ => { },
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
    /// 選択されたグループIDを永続化します。
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

        // グループIDを永続化（Fire-and-forget）
        _ = _dataPersistence.SaveLastSelectedGroupIdAsync(value.Id)
            .ContinueWith(
                _ => { },
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
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
                }
            }

            // DB に存在するが MemoryItems に無いアイテムを削除
            var memoryLabels = new HashSet<string>(group.Items.Select(i => i.Name));
            foreach (var item in dbItems)
            {
                if (!memoryLabels.Contains(item.Label))
                {
                    await _itemRepository.DeleteItemAsync(item.Id);
                }
            }
        }
        catch (Exception ex)
        {            // エラーはサイレントで処理（ユーザー入力中のため通知は不要）
            _ = ex;
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
