using AppRoulette.Models;
using AppRoulette.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AppRoulette.ViewModels;

/// <summary>
/// メイン画面の ViewModel。
/// グループ管理・アイテム管理・ルーレット選択ロジックを担います。
/// </summary>
public class MainViewModel : ObservableObject
{
    private readonly IDataPersistenceService _persistenceService;
    private readonly IRandomService _randomService;

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
    /// <param name="persistenceService">データ永続化サービス。</param>
    /// <param name="randomService">ランダム生成サービス。</param>
    public MainViewModel(
        IDataPersistenceService persistenceService,
        IRandomService randomService)
    {
        _persistenceService = persistenceService;
        _randomService = randomService;
        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        SpinCommand = new RelayCommand(Spin, CanSpin);
    }

    /// <summary>
    /// グループデータを読み込み、初期状態に設定します。
    /// </summary>
    private async Task InitializeAsync()
    {
        var groups = await _persistenceService.LoadGroupsAsync()
            .ConfigureAwait(false);

        GroupList = groups;
        SelectedGroup = groups.Count > 0 ? groups[0] : null;
    }

    /// <summary>
    /// ルーレットを回してアイテムをランダムに選択します。
    /// </summary>
    private void Spin()
    {
        if (SelectedGroup is null || SelectedGroup.Items.Count == 0)
        {
            return;
        }

        SelectedItemIndex = _randomService.Next(SelectedGroup.Items.Count);
    }

    /// <summary>
    /// ルーレットを開始できるかどうかを返します。
    /// アイテムが 1 件以上存在し、かつ回転中でない場合に <c>true</c>。
    /// </summary>
    private bool CanSpin() => ItemCount > 0 && !IsSpinning;

    /// <summary>
    /// <see cref="ItemsText"/> 変更時にアイテムリストとアイテム数を更新し、
    /// 改行が増えた場合は非同期で保存します。
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

        if (currentLineCount > previousLineCount)
        {
            // Fire-and-forget: 改行入力時に非同期保存（例外はデバッグ出力）
            _ = SaveAsync().ContinueWith(
                t => System.Diagnostics.Debug.WriteLine(
                    $"[AppRoulette] 保存エラー: {t.Exception}"),
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        _previousItemsText = value;
    }

    /// <summary>
    /// <see cref="SelectedGroup"/> 変更時にテキストとアイテム数を更新します。
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

        var text = FormatItems(value.Items);
        _previousItemsText = text;
        ItemsText = text;
        ItemCount = value.Items.Count;
        SelectedItemIndex = -1;
    }

    /// <summary>
    /// 全グループデータを非同期で保存します。
    /// </summary>
    private async Task SaveAsync()
    {
        if (GroupList.Count == 0)
        {
            return;
        }

        await _persistenceService.SaveGroupsAsync(GroupList)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 改行区切りのテキストをアイテムリストに変換します。空行は除去します。
    /// </summary>
    /// <param name="text">変換元テキスト。</param>
    /// <returns>アイテムリスト。</returns>
    public static List<RouletteItem> ParseItems(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r').Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => new RouletteItem(name))
            .ToList();

    /// <summary>
    /// アイテムリストを改行区切りのテキストに変換します。
    /// </summary>
    /// <param name="items">変換元アイテムリスト。</param>
    /// <returns>改行区切りのテキスト。</returns>
    public static string FormatItems(IEnumerable<RouletteItem> items) =>
        string.Join('\n', items.Select(i => i.Name));

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
