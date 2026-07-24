using AppRoulette.Models;
using AppRoulette.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace AppRoulette.ViewModels;

/// <summary>
/// メイン画面の ViewModel。
/// グループ管理・アイテム管理・ルーレット選択ロジックを担います。
/// </summary>
public class MainViewModel : ObservableObject
{
    private const int SAVE_DEBOUNCE_MS = 50;

    private const string DEFAULT_NEW_GROUP_DISPLAY_NAME = "New Roulette";

    private const string SAVE_STATUS_SAVED = "保存済み";

    private const string SAVE_STATUS_SAVING = "保存中";

    private const string SAVE_STATUS_FAILED = "保存失敗";

    private const string DEFAULT_NEW_ITEM_NAME = "新しいアイテム";

    private const string SYNC_STATUS_READY = "同期待機";

    private const string SYNC_STATUS_SYNCING = "同期中";

    private const string SYNC_STATUS_SYNCED = "同期済み";

    private const string SYNC_STATUS_FAILED = "同期失敗";

    private readonly IRandomService _randomService;
    private readonly IItemRepository _itemRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly ICloudSyncService? _cloudSyncService;
    private readonly SyncTimestampStore? _syncTimestampStore;

    private readonly Dictionary<int, CancellationTokenSource> _saveDebounceTokens =
        new();

    /// <summary>ルーレットアニメーション状態を管理する ViewModel。</summary>
    private SpinAnimationViewModel _spinAnimationViewModel = new();

    /// <summary>直前の <see cref="ItemsText"/> の値（改行増加検出に使用）。</summary>
    private string _previousItemsText = string.Empty;

    private ObservableCollection<RouletteGroup> _groupList =
        new();

    private ObservableCollection<RouletteItem> _editableItems =
        new();

    private RouletteGroup? _selectedGroup;

    private RouletteItem? _selectedEditableItem;

    private string _itemsText = string.Empty;

    private string _groupNameText = string.Empty;

    private int _itemCount;

    private bool _isSpinning;

    private int _selectedItemIndex = -1;

    private string _saveStatusText = SAVE_STATUS_SAVED;

    private string _itemEditStatusText = string.Empty;

    private bool _isGroupNameUnsaved;

    private bool _isSyncingItemsText;

    private bool _isSyncingEditableItems;

    private string _syncStatusText = SYNC_STATUS_READY;

    /// <summary>直近のスピンで当選したアイテム。</summary>
    private RouletteItem? _lastWinner;

    /// <summary>
    /// ComboBox に表示するグループ一覧を取得します。
    /// </summary>
    public ObservableCollection<RouletteGroup> GroupList
    {
        get => _groupList;
        private set
        {
            if (SetProperty(ref _groupList, value))
            {
                OnPropertyChanged(nameof(SelectedGroupPositionText));
            }
        }
    }

    /// <summary>
    /// 表形式編集 UI に表示するアイテム一覧を取得します。
    /// </summary>
    public ObservableCollection<RouletteItem> EditableItems
    {
        get => _editableItems;
        private set => SetProperty(ref _editableItems, value);
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
                RenameGroupCommand.NotifyCanExecuteChanged();
                ResetGroupNameCommand.NotifyCanExecuteChanged();
                DuplicateGroupCommand.NotifyCanExecuteChanged();
                DeleteGroupCommand.NotifyCanExecuteChanged();
                MoveGroupUpCommand.NotifyCanExecuteChanged();
                MoveGroupDownCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(SelectedGroupPositionText));
                OnSelectedGroupChanged(value);
            }
        }
    }

    /// <summary>
    /// 選択中グループがグループ一覧内の何番目かを取得します。
    /// </summary>
    public string SelectedGroupPositionText
    {
        get
        {
            if (SelectedGroup is null)
            {
                return string.Empty;
            }

            var index = GroupList.IndexOf(SelectedGroup);
            return index >= 0 ? $"{index + 1}/{GroupList.Count}" : string.Empty;
        }
    }

    /// <summary>
    /// 表形式編集 UI で選択中のアイテムを取得または設定します。
    /// </summary>
    public RouletteItem? SelectedEditableItem
    {
        get => _selectedEditableItem;
        set
        {
            if (SetProperty(ref _selectedEditableItem, value))
            {
                UpdateItemEditCommandStates();
            }
        }
    }

    /// <summary>
    /// 選択中グループの編集用表示名を取得または設定します。
    /// </summary>
    public string GroupNameText
    {
        get => _groupNameText;
        set
        {
            if (SetProperty(ref _groupNameText, value))
            {
                RenameGroupCommand.NotifyCanExecuteChanged();
                UpdateGroupNameUnsavedState();
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
                OnPropertyChanged(nameof(CanSpinNow));
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
                OnPropertyChanged(nameof(CanSpinNow));
            }
        }
    }

    /// <summary>
    /// ルーレットを開始できるかどうかを取得します。
    /// UI 側のボタン等の活性状態バインディングに使用します。
    /// </summary>
    public bool CanSpinNow => SpinCommand.CanExecute(null);

    /// <summary>
    /// ルーレット盤面・抽選の対象となる有効なアイテム一覧を取得します。
    /// IsEnabled が false のアイテムは除外されます。
    /// </summary>
    public IReadOnlyList<RouletteItem> WheelItems =>
        SelectedGroup?.Items.Where(item => item.IsEnabled).ToList()
        ?? (IReadOnlyList<RouletteItem>)Array.Empty<RouletteItem>();

    /// <summary>
    /// 選択中グループの「当たったら除外」モードを取得または設定します。
    /// 設定変更時は SQLite に永続化します。
    /// </summary>
    public bool IsExcludeOnWinEnabled
    {
        get => SelectedGroup?.ExcludeOnWin ?? false;
        set
        {
            if (SelectedGroup is null || SelectedGroup.ExcludeOnWin == value)
            {
                return;
            }

            SelectedGroup.ExcludeOnWin = value;
            OnPropertyChanged();
            _ = SaveExcludeOnWinSafelyAsync(SelectedGroup.Id, value);
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

    /// <summary>
    /// 現在の保存状態の表示テキストを取得します。
    /// </summary>
    public string SaveStatusText
    {
        get => _saveStatusText;
        private set => SetProperty(ref _saveStatusText, value);
    }

    /// <summary>
    /// 表形式編集 UI の入力状態や警告メッセージを取得します。
    /// </summary>
    public string ItemEditStatusText
    {
        get => _itemEditStatusText;
        private set => SetProperty(ref _itemEditStatusText, value);
    }

    /// <summary>
    /// 選択中グループが未保存の変更があるかどうかを取得します。
    /// </summary>
    public bool IsGroupNameUnsaved
    {
        get => _isGroupNameUnsaved;
        private set => SetProperty(ref _isGroupNameUnsaved, value);
    }

    /// <summary>
    /// ルーレットアニメーション状態を管理する ViewModel を取得します。
    /// </summary>
    public SpinAnimationViewModel SpinAnimation
    {
        get => _spinAnimationViewModel;
    }

    /// <summary>グループデータを読み込み初期状態に設定するコマンド。</summary>
    public IAsyncRelayCommand InitializeCommand { get; }

    /// <summary>
    /// ルーレットを回してアイテムをランダムに選択するコマンド。
    /// アイテムが 1 件以上存在し、かつ回転中でない場合に実行可能。
    /// </summary>
    public IRelayCommand SpinCommand { get; }

    /// <summary>
    /// 選択中のグループの全アイテムをクリアするコマンド。
    /// </summary>
    public IRelayCommand ClearItemsCommand { get; }

    /// <summary>
    /// 選択中のグループ名を変更するコマンド。
    /// </summary>
    public IAsyncRelayCommand RenameGroupCommand { get; }

    /// <summary>
    /// 選択中のグループ名を既定形式でリセットするコマンド。
    /// </summary>
    public IAsyncRelayCommand ResetGroupNameCommand { get; }

    /// <summary>
    /// 新しいグループを追加するコマンド。
    /// </summary>
    public IAsyncRelayCommand AddGroupCommand { get; }

    /// <summary>
    /// 選択中のグループを複製するコマンド。
    /// </summary>
    public IAsyncRelayCommand DuplicateGroupCommand { get; }

    /// <summary>
    /// 選択中のグループを削除するコマンド。
    /// </summary>
    public IAsyncRelayCommand DeleteGroupCommand { get; }

    /// <summary>
    /// 選択中のグループを1つ上へ移動するコマンド。
    /// </summary>
    public IAsyncRelayCommand MoveGroupUpCommand { get; }

    /// <summary>
    /// 選択中のグループを1つ下へ移動するコマンド。
    /// </summary>
    public IAsyncRelayCommand MoveGroupDownCommand { get; }

    /// <summary>表形式編集 UI に新しいアイテム行を追加するコマンド。</summary>
    public IRelayCommand AddItemRowCommand { get; }

    /// <summary>表形式編集 UI の選択行を削除するコマンド。</summary>
    public IRelayCommand DeleteSelectedItemRowCommand { get; }

    /// <summary>表形式編集 UI の選択行を1つ上へ移動するコマンド。</summary>
    public IRelayCommand MoveItemRowUpCommand { get; }

    /// <summary>表形式編集 UI の選択行を1つ下へ移動するコマンド。</summary>
    public IRelayCommand MoveItemRowDownCommand { get; }

    /// <summary>表形式編集 UI のアイテムを名前順に並び替えるコマンド。</summary>
    public IRelayCommand SortItemsCommand { get; }

    /// <summary>表形式編集 UI のアイテムをシャッフルするコマンド。</summary>
    public IRelayCommand ShuffleItemsCommand { get; }

    /// <summary>全グループを OneDrive フォルダーと手動同期するコマンド。</summary>
    public IAsyncRelayCommand SyncAllCommand { get; }

    /// <summary>
    /// 同期機能が利用可能（OneDrive フォルダー検出済み）か
    /// どうかを取得します。
    /// </summary>
    public bool IsSyncAvailable =>
        _cloudSyncService is not null && _syncTimestampStore is not null;

    /// <summary>
    /// 同期状態の表示テキストを取得します。
    /// </summary>
    public string SyncStatusText
    {
        get => _syncStatusText;
        private set => SetProperty(ref _syncStatusText, value);
    }

    /// <summary>
    /// <see cref="MainViewModel"/> を初期化します。
    /// </summary>
    /// <param name="randomService">ランダム生成サービス。</param>
    /// <param name="itemRepository">SQLite Item リポジトリ。</param>
    /// <param name="groupRepository">SQLite Group リポジトリ。</param>
    /// <param name="appSettingsRepository">アプリ設定リポジトリ。</param>
    /// <param name="cloudSyncService">クラウド同期サービス。null の場合は同期無効。</param>
    /// <param name="syncTimestampStore">同期タイムスタンプストア。null の場合は同期無効。</param>
    public MainViewModel(
        IRandomService randomService,
        IItemRepository itemRepository,
        IGroupRepository groupRepository,
        IAppSettingsRepository appSettingsRepository,
        ICloudSyncService? cloudSyncService = null,
        SyncTimestampStore? syncTimestampStore = null)
    {
        _randomService = randomService;
        _itemRepository = itemRepository;
        _groupRepository = groupRepository;
        _appSettingsRepository = appSettingsRepository;
        _cloudSyncService = cloudSyncService;
        _syncTimestampStore = syncTimestampStore;
        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        SpinCommand = new RelayCommand(Spin, CanSpin);
        ClearItemsCommand = new RelayCommand(ClearItems);
        RenameGroupCommand = new AsyncRelayCommand(RenameGroupAsync, CanRenameGroup);
        ResetGroupNameCommand = new AsyncRelayCommand(
            ResetGroupNameAsync,
            CanResetGroupName);
        AddGroupCommand = new AsyncRelayCommand(AddGroupAsync);
        DuplicateGroupCommand = new AsyncRelayCommand(DuplicateGroupAsync, CanDuplicateGroup);
        DeleteGroupCommand = new AsyncRelayCommand(DeleteGroupAsync, CanDeleteGroup);
        MoveGroupUpCommand = new AsyncRelayCommand(MoveGroupUpAsync, CanMoveGroupUp);
        MoveGroupDownCommand = new AsyncRelayCommand(MoveGroupDownAsync, CanMoveGroupDown);
        AddItemRowCommand = new RelayCommand(AddItemRow, CanAddItemRow);
        DeleteSelectedItemRowCommand = new RelayCommand(DeleteSelectedItemRow, CanDeleteSelectedItemRow);
        MoveItemRowUpCommand = new RelayCommand(MoveItemRowUp, CanMoveItemRowUp);
        MoveItemRowDownCommand = new RelayCommand(MoveItemRowDown, CanMoveItemRowDown);
        SortItemsCommand = new RelayCommand(SortItems, CanReorderItems);
        ShuffleItemsCommand = new RelayCommand(ShuffleItems, CanReorderItems);
        SyncAllCommand = new AsyncRelayCommand(
            SyncAllAsync,
            () => IsSyncAvailable);

        EditableItems.CollectionChanged += EditableItems_CollectionChanged;
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

        await _groupRepository.EnsureDefaultGroupsAsync(groups);
        groups = (await _groupRepository.GetGroupsAsync()).ToList();

        // SQLite から各グループのアイテムを読み込み、グループに充填
        foreach (var group in groups)
        {
            var dbItems = await _itemRepository.GetItemsByGroupAsync(group.Id);
            group.Items = dbItems
                .Select(item => new RouletteItem(item.Label)
                {
                    Weight = item.Weight,
                    IsEnabled = item.IsEnabled,
                })
                .ToList();
        }

        GroupList = new ObservableCollection<RouletteGroup>(groups);

        // 前回起動時に選択されたグループを復元
        var lastSelectedGroupId = await _appSettingsRepository.GetLastSelectedGroupIdAsync();
        var selectedGroup = groups.FirstOrDefault(g => g.Id == lastSelectedGroupId) 
            ?? (GroupList.Count > 0 ? GroupList[0] : null);
        SelectedGroup = selectedGroup;
        UpdateGroupCommandStates();

        await DownloadOnStartupAsync();
    }

    /// <summary>
    /// 起動時に選択中グループの OneDrive フォルダーのデータを
    /// ダウンロードして新しければ適用します。
    /// </summary>
    private async Task DownloadOnStartupAsync()
    {
        if (!IsSyncAvailable)
        {
            return;
        }

        if (SelectedGroup is not null)
        {
            await DownloadAndApplyGroupAsync(SelectedGroup.Id);
        }

        SyncStatusText = SYNC_STATUS_SYNCED;
    }

    /// <summary>
    /// 全グループをクラウドと双方向に同期します。
    /// 各グループについてタイムスタンプの新しい方を採用します。
    /// </summary>
    private async Task SyncAllAsync()
    {
        if (_cloudSyncService is null || _syncTimestampStore is null)
        {
            return;
        }

        SyncStatusText = SYNC_STATUS_SYNCING;

        try
        {
            var remoteGroups = await _cloudSyncService.DownloadAllGroupsAsync();
            var remoteById = remoteGroups.ToDictionary(g => g.GroupId);
            var uploadTargets = new List<SyncGroupData>();

            foreach (var group in GroupList)
            {
                var localTimestamp =
                    await _syncTimestampStore.GetAsync(group.Id)
                    ?? DateTime.MinValue;
                var local = SyncGroupData.FromGroup(group, localTimestamp);
                remoteById.TryGetValue(group.Id, out var remote);

                if (SyncConflictResolver.ShouldApplyRemote(local, remote))
                {
                    await ApplyRemoteGroupAsync(group, remote!);
                }
                else
                {
                    uploadTargets.Add(local);
                }
            }

            var succeeded =
                await _cloudSyncService.UploadAllGroupsAsync(uploadTargets);
            SyncStatusText = succeeded
                ? SYNC_STATUS_SYNCED
                : SYNC_STATUS_FAILED;
        }
        catch (Exception ex)
        {
            SyncStatusText = SYNC_STATUS_FAILED;
            ApplicationLogger.LogError("手動同期", ex);
        }
    }

    /// <summary>
    /// 指定グループのクラウドデータをダウンロードし、
    /// ローカルより新しい場合のみ適用します。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    private async Task DownloadAndApplyGroupAsync(int groupId)
    {
        if (_cloudSyncService is null || _syncTimestampStore is null)
        {
            return;
        }

        try
        {
            var remote = await _cloudSyncService.DownloadGroupAsync(groupId);
            var group = GroupList.FirstOrDefault(g => g.Id == groupId);
            if (group is null || remote is null)
            {
                return;
            }

            var localTimestamp =
                await _syncTimestampStore.GetAsync(groupId)
                ?? DateTime.MinValue;
            var local = SyncGroupData.FromGroup(group, localTimestamp);

            if (SyncConflictResolver.ShouldApplyRemote(local, remote))
            {
                await ApplyRemoteGroupAsync(group, remote);
            }
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogError("クラウドデータ取得", ex);
        }
    }

    /// <summary>
    /// クラウドのグループデータをローカルグループへ適用し、
    /// SQLite とタイムスタンプストアに保存します。
    /// 選択中グループの場合は編集UIも更新します。
    /// </summary>
    /// <param name="group">適用先のローカルグループ。</param>
    /// <param name="remote">適用するクラウドデータ。</param>
    private async Task ApplyRemoteGroupAsync(
        RouletteGroup group, SyncGroupData remote)
    {
        group.DisplayName = remote.DisplayName;
        group.ExcludeOnWin = remote.ExcludeOnWin;
        group.Items = remote.Items
            .Select(i => new RouletteItem(i.Name)
            {
                Weight = i.Weight,
                IsEnabled = i.IsEnabled,
            })
            .Take(RouletteGroup.MAX_ITEM_COUNT)
            .ToList();

        await _groupRepository.SaveGroupNameAsync(
            group.Id, remote.DisplayName);
        await _itemRepository.SaveItemsByGroupAsync(
            group.Id, group.Items);

        if (_syncTimestampStore is not null)
        {
            await _syncTimestampStore.SetAsync(
                group.Id, remote.LastModifiedUtc);
        }

        if (ReferenceEquals(group, SelectedGroup))
        {
            OnSelectedGroupChanged(group);
        }
    }

    /// <summary>
    /// ルーレットを回してアイテムをランダムに選択します。
    /// 各アイテムの Weight 値に基づいて確率的に選択されます。
    /// </summary>
    private void Spin()
    {
        var wheelItems = WheelItems;
        if (wheelItems.Count == 0)
        {
            return;
        }

        var selectedItem = _randomService.SelectByWeight(wheelItems);
        if (selectedItem is not null)
        {
            _lastWinner = selectedItem;
            SelectedItemIndex = wheelItems.ToList().IndexOf(selectedItem);
        }
    }

    /// <summary>
    /// 「当たったら除外」モードが有効な場合、
    /// 直近の当選アイテムを抽選対象から除外して保存します。
    /// 有効なアイテムが残り1件の場合は除外しません。
    /// </summary>
    public void ExcludeWinnerIfNeeded()
    {
        if (SelectedGroup is null
            || !SelectedGroup.ExcludeOnWin
            || _lastWinner is null)
        {
            return;
        }

        if (SelectedGroup.Items.Count(item => item.IsEnabled) <= 1)
        {
            return;
        }

        var groupIndex = SelectedGroup.Items.IndexOf(_lastWinner);
        _lastWinner.IsEnabled = false;
        _lastWinner = null;
        SelectedItemIndex = -1;

        if (groupIndex >= 0 && groupIndex < EditableItems.Count)
        {
            // 編集UIのクローンにも反映し、保存処理を発火させる
            EditableItems[groupIndex].IsEnabled = false;
        }
        else
        {
            ScheduleItemsSave(SelectedGroup);
        }

        OnPropertyChanged(nameof(WheelItems));
        SpinCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanSpinNow));
    }

    /// <summary>
    /// 「当たったら除外」設定を保存し、失敗時はログに記録します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="excludeOnWin">「当たったら除外」の有効状態。</param>
    private async Task SaveExcludeOnWinSafelyAsync(int groupId, bool excludeOnWin)
    {
        try
        {
            await _groupRepository.SaveGroupExcludeOnWinAsync(
                groupId, excludeOnWin);
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogError("当たったら除外設定保存", ex);
        }
    }

    /// <summary>
    /// ルーレットを開始できるかどうかを返します。
    /// 有効なアイテムが 1 件以上存在し、かつ回転中でない場合に <c>true</c>。
    /// </summary>
    private bool CanSpin() =>
        !IsSpinning
        && SelectedGroup is not null
        && SelectedGroup.Items.Any(item => item.IsEnabled);

    /// <summary>
    /// 選択中グループの表示名を変更して保存します。
    /// </summary>
    private async Task RenameGroupAsync()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        var displayName = GroupNameText.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        try
        {
            SaveStatusText = SAVE_STATUS_SAVING;
            SelectedGroup.DisplayName = displayName;
            OnPropertyChanged(nameof(GroupList));
            await _groupRepository.SaveGroupNameAsync(SelectedGroup.Id, displayName);
            UpdateGroupNameUnsavedState();
            SaveStatusText = SAVE_STATUS_SAVED;
        }
        catch (Exception ex)
        {
            SaveStatusText = SAVE_STATUS_FAILED;
            ApplicationLogger.LogError("グループ名保存", ex);
        }
    }

    /// <summary>
    /// 選択中グループの表示名を変更できるかどうかを返します。
    /// </summary>
    /// <returns>変更可能な場合は <c>true</c>。</returns>
    private bool CanRenameGroup() =>
        SelectedGroup is not null && !string.IsNullOrWhiteSpace(GroupNameText);

    /// <summary>
    /// 選択中グループ名の入力欄を既定名へリセットします。
    /// </summary>
    private Task ResetGroupNameAsync()
    {
        if (SelectedGroup is null)
        {
            return Task.CompletedTask;
        }

        GroupNameText = DEFAULT_NEW_GROUP_DISPLAY_NAME;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 選択中グループ名をリセットできるかどうかを返します。
    /// </summary>
    /// <returns>リセット可能な場合は <c>true</c>。</returns>
    private bool CanResetGroupName() => SelectedGroup is not null;

    /// <summary>
    /// 選択中グループを複製できるかどうかを返します。
    /// </summary>
    /// <returns>複製可能な場合は <c>true</c>。</returns>
    private bool CanDuplicateGroup() => SelectedGroup is not null;

    /// <summary>
    /// グループ名入力欄の未保存状態を更新します。
    /// </summary>
    private void UpdateGroupNameUnsavedState()
    {
        IsGroupNameUnsaved = SelectedGroup is not null
            && GroupNameText.Trim() != SelectedGroup.DisplayName;
    }

    /// <summary>
    /// 新しいグループを追加して選択します。
    /// </summary>
    private async Task AddGroupAsync()
    {
        try
        {
            SaveStatusText = SAVE_STATUS_SAVING;
            var displayName = DEFAULT_NEW_GROUP_DISPLAY_NAME;
            var group = await _groupRepository.AddGroupAsync(
                displayName,
                GroupList.Count + 1);

            GroupList.Add(group);
            SelectedGroup = group;
            SaveStatusText = SAVE_STATUS_SAVED;
            UpdateGroupCommandStates();
        }
        catch (Exception ex)
        {
            SaveStatusText = SAVE_STATUS_FAILED;
            ApplicationLogger.LogError("グループ追加", ex);
        }
    }

    /// <summary>
    /// 選択中のグループを複製して選択します。
    /// </summary>
    private async Task DuplicateGroupAsync()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        try
        {
            SaveStatusText = SAVE_STATUS_SAVING;
            var sourceGroup = SelectedGroup;
            var displayName = $"{sourceGroup.DisplayName} Copy";
            var copiedItems = sourceGroup.Items
                .Select(item => new RouletteItem(item.Name)
                {
                    Weight = item.Weight,
                })
                .ToList();
            var group = await _groupRepository.AddGroupAsync(
                displayName,
                GroupList.Count + 1);

            group.Items = copiedItems;
            await _itemRepository.SaveItemsByGroupAsync(group.Id, copiedItems);

            GroupList.Add(group);
            SelectedGroup = group;
            SaveStatusText = SAVE_STATUS_SAVED;
            UpdateGroupCommandStates();
        }
        catch (Exception ex)
        {
            SaveStatusText = SAVE_STATUS_FAILED;
            ApplicationLogger.LogError("グループ複製", ex);
        }
    }

    /// <summary>
    /// 選択中のグループを削除します。
    /// </summary>
    private async Task DeleteGroupAsync()
    {
        if (!CanDeleteGroup() || SelectedGroup is null)
        {
            return;
        }

        try
        {
            SaveStatusText = SAVE_STATUS_SAVING;
            var group = SelectedGroup;
            var currentIndex = GroupList.IndexOf(group);

            await _groupRepository.DeleteGroupAsync(group.Id);
            await _itemRepository.DeleteItemsByGroupAsync(group.Id);
            GroupList.Remove(group);
            await _groupRepository.SaveGroupOrderAsync(GroupList);

            var nextIndex = Math.Min(currentIndex, GroupList.Count - 1);
            SelectedGroup = GroupList[nextIndex];
            SaveStatusText = SAVE_STATUS_SAVED;
            UpdateGroupCommandStates();
        }
        catch (Exception ex)
        {
            SaveStatusText = SAVE_STATUS_FAILED;
            ApplicationLogger.LogError("グループ削除", ex);
        }
    }

    /// <summary>
    /// 選択中のグループを1つ上へ移動します。
    /// </summary>
    private async Task MoveGroupUpAsync()
    {
        if (!CanMoveGroupUp())
        {
            return;
        }

        await MoveSelectedGroupAsync(-1);
    }

    /// <summary>
    /// 選択中のグループを1つ下へ移動します。
    /// </summary>
    private async Task MoveGroupDownAsync()
    {
        if (!CanMoveGroupDown())
        {
            return;
        }

        await MoveSelectedGroupAsync(1);
    }

    /// <summary>
    /// 選択中のグループを指定方向へ移動して表示順を保存します。
    /// </summary>
    /// <param name="direction">移動方向。上へは -1、下へは 1。</param>
    private async Task MoveSelectedGroupAsync(int direction)
    {
        if (SelectedGroup is null)
        {
            return;
        }

        try
        {
            SaveStatusText = SAVE_STATUS_SAVING;
            var selectedGroup = SelectedGroup;
            var oldIndex = GroupList.IndexOf(selectedGroup);
            var newIndex = oldIndex + direction;
            GroupList.Move(oldIndex, newIndex);
            await _groupRepository.SaveGroupOrderAsync(GroupList);
            OnPropertyChanged(nameof(SelectedGroup));
            OnPropertyChanged(nameof(SelectedGroupPositionText));
            SaveStatusText = SAVE_STATUS_SAVED;
            UpdateGroupCommandStates();
        }
        catch (Exception ex)
        {
            SaveStatusText = SAVE_STATUS_FAILED;
            ApplicationLogger.LogError("グループ並び替え", ex);
        }
    }

    /// <summary>
    /// 選択中のグループを削除できるかどうかを返します。
    /// </summary>
    /// <returns>削除可能な場合は <c>true</c>。</returns>
    private bool CanDeleteGroup() => SelectedGroup is not null && GroupList.Count > 1;

    /// <summary>
    /// 選択中のグループを上へ移動できるかどうかを返します。
    /// </summary>
    /// <returns>上へ移動可能な場合は <c>true</c>。</returns>
    private bool CanMoveGroupUp() =>
        SelectedGroup is not null && GroupList.IndexOf(SelectedGroup) > 0;

    /// <summary>
    /// 選択中のグループを下へ移動できるかどうかを返します。
    /// </summary>
    /// <returns>下へ移動可能な場合は <c>true</c>。</returns>
    private bool CanMoveGroupDown() =>
        SelectedGroup is not null
        && GroupList.IndexOf(SelectedGroup) >= 0
        && GroupList.IndexOf(SelectedGroup) < GroupList.Count - 1;

    /// <summary>
    /// グループ操作コマンドの実行可否を更新します。
    /// </summary>
    private void UpdateGroupCommandStates()
    {
        RenameGroupCommand.NotifyCanExecuteChanged();
        ResetGroupNameCommand.NotifyCanExecuteChanged();
        DuplicateGroupCommand.NotifyCanExecuteChanged();
        DeleteGroupCommand.NotifyCanExecuteChanged();
        MoveGroupUpCommand.NotifyCanExecuteChanged();
        MoveGroupDownCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 表形式編集 UI に新しいアイテム行を追加します。
    /// </summary>
    private void AddItemRow()
    {
        if (!CanAddItemRow())
        {
            return;
        }

        var item = new RouletteItem(CreateNewItemName());
        item.PropertyChanged += EditableItem_PropertyChanged;
        EditableItems.Add(item);
        SelectedEditableItem = item;
        ApplyEditableItemsToSelectedGroup();
    }

    /// <summary>
    /// 表形式編集 UI にアイテム行を追加できるかどうかを返します。
    /// </summary>
    private bool CanAddItemRow() =>
        SelectedGroup is not null && EditableItems.Count < RouletteGroup.MAX_ITEM_COUNT;

    /// <summary>
    /// 選択中のアイテム行を削除します。
    /// </summary>
    private void DeleteSelectedItemRow()
    {
        if (SelectedEditableItem is null)
        {
            return;
        }

        var index = EditableItems.IndexOf(SelectedEditableItem);
        EditableItems.Remove(SelectedEditableItem);
        SelectedEditableItem = EditableItems.Count == 0
            ? null
            : EditableItems[Math.Min(index, EditableItems.Count - 1)];
        ApplyEditableItemsToSelectedGroup();
    }

    /// <summary>
    /// 選択中のアイテム行を削除できるかどうかを返します。
    /// </summary>
    private bool CanDeleteSelectedItemRow() => SelectedEditableItem is not null;

    /// <summary>
    /// 選択中のアイテム行を1つ上へ移動します。
    /// </summary>
    private void MoveItemRowUp()
    {
        MoveSelectedItemRow(-1);
    }

    /// <summary>
    /// 選択中のアイテム行を1つ下へ移動します。
    /// </summary>
    private void MoveItemRowDown()
    {
        MoveSelectedItemRow(1);
    }

    /// <summary>
    /// 選択中のアイテム行を指定方向へ移動します。
    /// </summary>
    /// <param name="direction">移動方向。上へは -1、下へは 1。</param>
    private void MoveSelectedItemRow(int direction)
    {
        if (SelectedEditableItem is null)
        {
            return;
        }

        var oldIndex = EditableItems.IndexOf(SelectedEditableItem);
        var newIndex = oldIndex + direction;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= EditableItems.Count)
        {
            return;
        }

        EditableItems.Move(oldIndex, newIndex);
        ApplyEditableItemsToSelectedGroup();
    }

    /// <summary>
    /// 選択中のアイテム行を上へ移動できるかどうかを返します。
    /// </summary>
    private bool CanMoveItemRowUp() =>
        SelectedEditableItem is not null && EditableItems.IndexOf(SelectedEditableItem) > 0;

    /// <summary>
    /// 選択中のアイテム行を下へ移動できるかどうかを返します。
    /// </summary>
    private bool CanMoveItemRowDown() =>
        SelectedEditableItem is not null
        && EditableItems.IndexOf(SelectedEditableItem) >= 0
        && EditableItems.IndexOf(SelectedEditableItem) < EditableItems.Count - 1;

    /// <summary>
    /// アイテムを名前順に並び替えます。
    /// </summary>
    private void SortItems()
    {
        var sortedItems = EditableItems
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(CloneItem)
            .ToList();
        ReplaceEditableItems(sortedItems);
        SelectedEditableItem = EditableItems.FirstOrDefault();
        ApplyEditableItemsToSelectedGroup();
    }

    /// <summary>
    /// アイテムをランダム順に並び替えます。
    /// </summary>
    private void ShuffleItems()
    {
        for (var i = EditableItems.Count - 1; i > 0; i--)
        {
            var j = _randomService.Next(i + 1);
            EditableItems.Move(i, j);
        }

        ApplyEditableItemsToSelectedGroup();
    }

    /// <summary>
    /// アイテムの並び替え操作が可能かどうかを返します。
    /// </summary>
    private bool CanReorderItems() => EditableItems.Count > 1;

    /// <summary>
    /// 表形式編集 UI の各コマンドの実行可否を更新します。
    /// </summary>
    private void UpdateItemEditCommandStates()
    {
        AddItemRowCommand.NotifyCanExecuteChanged();
        DeleteSelectedItemRowCommand.NotifyCanExecuteChanged();
        MoveItemRowUpCommand.NotifyCanExecuteChanged();
        MoveItemRowDownCommand.NotifyCanExecuteChanged();
        SortItemsCommand.NotifyCanExecuteChanged();
        ShuffleItemsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 表形式編集 UI の状態表示とコマンド状態を更新します。
    /// </summary>
    private void UpdateItemEditState()
    {
        var duplicateNames = EditableItems
            .Select(item => item.Name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Take(3)
            .ToList();

        var messages = new List<string>();
        if (EditableItems.Count >= RouletteGroup.MAX_ITEM_COUNT)
        {
            messages.Add($"最大{RouletteGroup.MAX_ITEM_COUNT}件です");
        }

        if (duplicateNames.Count > 0)
        {
            messages.Add($"重複: {string.Join(", ", duplicateNames)}");
        }

        ItemEditStatusText = string.Join(" / ", messages);
        UpdateItemEditCommandStates();
    }

    /// <summary>
    /// 表形式編集 UI の内容を選択中グループ、テキスト、保存処理へ反映します。
    /// </summary>
    private void ApplyEditableItemsToSelectedGroup()
    {
        if (_isSyncingEditableItems || SelectedGroup is null)
        {
            return;
        }

        var items = EditableItems
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Take(RouletteGroup.MAX_ITEM_COUNT)
            .Select(CloneItem)
            .ToList();

        SelectedGroup.Items = items;
        ItemCount = items.Count;
        SelectedItemIndex = -1;
        SetItemsTextWithoutParsing(items);
        UpdateItemEditState();
        ScheduleItemsSave(SelectedGroup);
        OnPropertyChanged(nameof(WheelItems));
        SpinCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanSpinNow));
    }

    /// <summary>
    /// アイテム一覧を表形式編集 UI へ反映します。
    /// </summary>
    /// <param name="items">反映するアイテム一覧。</param>
    private void ReplaceEditableItems(IEnumerable<RouletteItem> items)
    {
        _isSyncingEditableItems = true;
        try
        {
            foreach (var item in EditableItems)
            {
                item.PropertyChanged -= EditableItem_PropertyChanged;
            }

            EditableItems.Clear();
            foreach (var item in items.Take(RouletteGroup.MAX_ITEM_COUNT).Select(CloneItem))
            {
                item.PropertyChanged += EditableItem_PropertyChanged;
                EditableItems.Add(item);
            }
        }
        finally
        {
            _isSyncingEditableItems = false;
        }

        SelectedEditableItem = EditableItems.FirstOrDefault();
        UpdateItemEditState();
        UpdateCanUncheckedForAllItems();
    }

    /// <summary>
    /// テキスト入力の再解析を行わずに <see cref="ItemsText"/> を更新します。
    /// </summary>
    /// <param name="items">テキスト化するアイテム一覧。</param>
    private void SetItemsTextWithoutParsing(IEnumerable<RouletteItem> items)
    {
        var text = FormatItems(items);
        _isSyncingItemsText = true;
        try
        {
            ItemsText = text;
        }
        finally
        {
            _isSyncingItemsText = false;
        }

        _previousItemsText = text;
    }

    /// <summary>
    /// 編集用アイテムコレクションの変更を処理します。
     /// </summary>
     private void EditableItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (RouletteItem item in e.OldItems)
            {
                item.PropertyChanged -= EditableItem_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (RouletteItem item in e.NewItems)
            {
                item.PropertyChanged -= EditableItem_PropertyChanged;
                item.PropertyChanged += EditableItem_PropertyChanged;
            }
        }

        if (!_isSyncingEditableItems)
        {
            UpdateItemEditState();
            UpdateCanUncheckedForAllItems();
        }
    }

    /// <summary>
    /// 表形式編集 UI の行プロパティ変更を処理します。
    /// </summary>
    private void EditableItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
       {
           if (_isSyncingEditableItems)
           {
               return;
           }

           if (e.PropertyName == nameof(RouletteItem.IsEnabled))
           {
               // 有効状態が変わった場合、すべてのアイテムのCanUncheckedを更新
               UpdateCanUncheckedForAllItems();
           }

           ApplyEditableItemsToSelectedGroup();
       }

       /// <summary>
       /// すべてのアイテムの「チェック外し可能」フラグを更新します。
       /// 有効なアイテムが2個以下の場合、有効なアイテムはチェック外し不可になります。
       /// </summary>
       private void UpdateCanUncheckedForAllItems()
       {
           var enabledCount = EditableItems.Count(item => item.IsEnabled);
           foreach (var item in EditableItems)
           {
               // 有効なアイテムが2個以下で、かつこのアイテムが有効な場合は外せない
               item.CanUnchecked = !(enabledCount <= 2 && item.IsEnabled);
           }
       }

    /// <summary>
    /// 新規アイテム行の重複しない既定名を作成します。
    /// </summary>
    private string CreateNewItemName()
    {
        if (EditableItems.All(item => !string.Equals(
                item.Name,
                DEFAULT_NEW_ITEM_NAME,
                StringComparison.CurrentCultureIgnoreCase)))
        {
            return DEFAULT_NEW_ITEM_NAME;
        }

        for (var i = 2; i <= RouletteGroup.MAX_ITEM_COUNT; i++)
        {
            var name = $"{DEFAULT_NEW_ITEM_NAME}{i}";
            if (EditableItems.All(item => !string.Equals(
                    item.Name,
                    name,
                    StringComparison.CurrentCultureIgnoreCase)))
            {
                return name;
            }
        }

        return DEFAULT_NEW_ITEM_NAME;
    }

    /// <summary>
    /// アイテムの編集用コピーを作成します。
    /// </summary>
    /// <param name="item">コピー元アイテム。</param>
    /// <returns>コピーされたアイテム。</returns>
    private static RouletteItem CloneItem(RouletteItem item) =>
       new(item.Name)
       {
           Weight = item.Weight,
           IsEnabled = item.IsEnabled,
           CanUnchecked = item.CanUnchecked,
       };

    /// <summary>
    /// 選択中のグループの全アイテムをクリアします。
    /// アイテムをクリアした後、SQLite にも同期します。
    /// </summary>
    private void ClearItems()
    {
        if (SelectedGroup is null)
        {
            return;
        }

        SelectedGroup.Items.Clear();
        ItemsText = string.Empty;
        ReplaceEditableItems(Array.Empty<RouletteItem>());
        ItemCount = 0;
        SelectedItemIndex = -1;

        ScheduleItemsSave(SelectedGroup);
    }

    /// <summary>
    /// <see cref="ItemsText"/> 変更時にアイテムリストとアイテム数を更新し、
    /// テキストが変わった場合は非同期で SQLite に保存します。
    /// </summary>
    /// <param name="value">変更後のテキスト。</param>
    private void OnItemsTextChanged(string value)
    {
        if (_isSyncingItemsText)
        {
            return;
        }

        if (SelectedGroup is null)
        {
            return;
        }

        var parsedItems = ParseItems(value);
        var items = parsedItems
            .Take(RouletteGroup.MAX_ITEM_COUNT)
            .ToList();
        SelectedGroup.Items = items;
        ReplaceEditableItems(items);
        ItemCount = items.Count;
        SelectedItemIndex = -1;
        OnPropertyChanged(nameof(WheelItems));
        SpinCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanSpinNow));

        if (value != _previousItemsText)
        {
            ScheduleItemsSave(SelectedGroup);
        }

        _previousItemsText = value;
    }

    /// <summary>
    /// 指定されたグループのアイテム保存をデバウンス付きで予約します。
    /// </summary>
    /// <param name="group">保存対象のグループ。</param>
    private void ScheduleItemsSave(RouletteGroup group)
    {
        if (_saveDebounceTokens.TryGetValue(group.Id, out var currentCts))
        {
            currentCts.Cancel();
        }

        var cts = new CancellationTokenSource();
        _saveDebounceTokens[group.Id] = cts;
        SaveStatusText = SAVE_STATUS_SAVING;

        _ = SaveItemsAfterDebounceAsync(group, cts);
    }

    /// <summary>
    /// デバウンス時間の経過後に指定グループのアイテムを保存します。
    /// </summary>
    /// <param name="group">保存対象のグループ。</param>
    /// <param name="cts">キャンセル制御用トークンソース。</param>
    private async Task SaveItemsAfterDebounceAsync(
        RouletteGroup group,
        CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(SAVE_DEBOUNCE_MS, cts.Token);
            await SaveItemsAsync(group, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SaveStatusText = SAVE_STATUS_FAILED;
            ApplicationLogger.LogError("アイテム保存", ex);
        }
        finally
        {
            if (_saveDebounceTokens.TryGetValue(group.Id, out var currentCts)
                && ReferenceEquals(currentCts, cts))
            {
                _saveDebounceTokens.Remove(group.Id);
            }

            cts.Dispose();
        }
    }

    /// <summary>
    /// 指定グループのアイテムを SQLite に保存します。
    /// </summary>
    /// <param name="group">保存対象のグループ。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    private async Task SaveItemsAsync(
        RouletteGroup group,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var itemsSnapshot = group.Items
            .Select(item => new RouletteItem(item.Name)
            {
                Weight = item.Weight,
            })
            .ToList();

        await _itemRepository.SaveItemsByGroupAsync(group.Id, itemsSnapshot);

        cancellationToken.ThrowIfCancellationRequested();
        SaveStatusText = SAVE_STATUS_SAVED;

        // Fire-and-forget: クラウドへの自動アップロードは保存完了を
        // ブロックしない。失敗時はログと状態表示のみ更新する。
        _ = UploadGroupSafelyAsync(group);
    }

    /// <summary>
    /// 同期が利用可能な場合に指定グループを OneDrive フォルダーへ
    /// アップロードします。失敗してもアプリ動作は継続します。
    /// </summary>
    /// <param name="group">アップロードするグループ。</param>
    private async Task UploadGroupSafelyAsync(RouletteGroup group)
    {
        if (_cloudSyncService is null || _syncTimestampStore is null)
        {
            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            await _syncTimestampStore.SetAsync(group.Id, now);
            var data = SyncGroupData.FromGroup(group, now);
            var succeeded = await _cloudSyncService.UploadGroupAsync(data);
            SyncStatusText = succeeded
                ? SYNC_STATUS_SYNCED
                : SYNC_STATUS_FAILED;
        }
        catch (Exception ex)
        {
            SyncStatusText = SYNC_STATUS_FAILED;
            ApplicationLogger.LogError("自動アップロード", ex);
        }
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
            ReplaceEditableItems(Array.Empty<RouletteItem>());
            GroupNameText = string.Empty;
            UpdateGroupNameUnsavedState();
            ItemCount = 0;
            return;
        }

        GroupNameText = value.DisplayName;
        UpdateGroupNameUnsavedState();
        var text = FormatItems(value.Items);
        _previousItemsText = text;
        ReplaceEditableItems(value.Items);
        ItemsText = text;
        ItemCount = value.Items.Count;
        SelectedItemIndex = -1;
        _lastWinner = null;
        OnPropertyChanged(nameof(IsExcludeOnWinEnabled));
        OnPropertyChanged(nameof(WheelItems));

        _ = SaveLastSelectedGroupIdSafelyAsync(value.Id);
    }

    /// <summary>
    /// 最後に選択されたグループIDを保存し、失敗時はログに記録します。
    /// </summary>
    /// <param name="groupId">保存するグループID。</param>
    private async Task SaveLastSelectedGroupIdSafelyAsync(int groupId)
    {
        try
        {
            await _appSettingsRepository.SaveLastSelectedGroupIdAsync(groupId);
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogError("選択グループID保存", ex);
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

    /// <summary>CSVエクスポート時に付与するヘッダー行。</summary>
    private const string CSV_HEADER = "アイテム名,Weight";

    /// <summary>
    /// 選択中グループのアイテム一覧をCSV形式のテキストに変換します。
    /// 先頭行にはヘッダー（<see cref="CSV_HEADER"/>）を付与します。
    /// </summary>
    /// <returns>CSV形式のテキスト。選択中グループがない場合はヘッダーのみを返します。</returns>
    public string ExportItemsToCsv()
    {
        var items = SelectedGroup?.Items ?? new List<RouletteItem>();
        return CSV_HEADER + "\n" + FormatItems(items);
    }

    /// <summary>
    /// CSV形式のテキストを解析し、選択中グループのアイテム一覧に反映します。
    /// 先頭行が <see cref="CSV_HEADER"/> と一致する場合はヘッダー行として除外します。
    /// </summary>
    /// <param name="csvContent">インポートするCSV形式のテキスト。</param>
    public void ImportItemsFromCsv(string csvContent)
    {
        if (SelectedGroup is null || csvContent is null)
        {
            return;
        }

        var normalized = csvContent.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');
        var startIndex = lines.Length > 0
            && string.Equals(lines[0].Trim(), CSV_HEADER, StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        ItemsText = string.Join('\n', lines.Skip(startIndex));
    }

    /// <summary>
    /// テキストの行数を返します。空文字は 0 を返します。
    /// </summary>
    /// <param name="text">対象テキスト。</param>
    /// <returns>行数。</returns>
    public static int CountLines(string text) =>
        string.IsNullOrEmpty(text)
            ? 0
            : text.Split('\n').Length;

    /// <summary>
    /// 指定されたアイテムのインデックスが、ルーレット上でインジケーター位置に来るような
    /// ルーレットの回転角度（ラジアン）を計算します。
    /// </summary>
    /// <param name="selectedIndex">アイテムのインデックス（0 始まり）。</param>
    /// <param name="items">アイテムリスト。</param>
    /// <returns>目標回転角度（ラジアン）。</returns>
    public static float CalcTargetAngle(
        int selectedIndex,
        IReadOnlyList<RouletteItem>? items)
    {
        if (items is null || selectedIndex < 0 || selectedIndex >= items.Count)
        {
            return 0f;
        }

        var totalWeight = 0;
        var precedingWeight = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var weight = Math.Max(0, items[i].Weight);
            totalWeight += weight;

            if (i < selectedIndex)
            {
                precedingWeight += weight;
            }
        }

        var selectedWeight = Math.Max(0, items[selectedIndex].Weight);
        if (totalWeight <= 0 || selectedWeight <= 0)
        {
            return 0f;
        }

        var selectedCenterWeight = precedingWeight + selectedWeight / 2f;

        // Draw では startAngle = rotationAngle - π/2 + 累積Weight角度。
        // インジケーターは 3時方向（角度 0）なので、選択扇形の中心が
        // 角度 0 を向くように targetAngle を決定する。
        return MathF.PI / 2f
            - MathF.PI * 2f * selectedCenterWeight / totalWeight;
    }
}
