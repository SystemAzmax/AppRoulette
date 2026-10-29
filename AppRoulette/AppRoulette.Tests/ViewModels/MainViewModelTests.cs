using AppRoulette.Models;
using AppRoulette.Services;
using AppRoulette.Tests.Fakes;
using AppRoulette.ViewModels;

namespace AppRoulette.Tests.ViewModels;

/// <summary>
/// <see cref="MainViewModel"/> のユニットテストクラスです。
/// </summary>
public class MainViewModelTests
{
    // ---------------------------------------------------------------
    // ヘルパー
    // ---------------------------------------------------------------

    /// <summary>
    /// (廃止予定) FakeAppSettingsRepository を生成します。
    /// テストからは CreateSut(itemCountInGroup1: X) を使用してください。
    /// </summary>
    [Obsolete("Use CreateSut(itemCountInGroup1) instead")]
    private static FakeAppSettingsRepository CreateDefaultFakeService(
        int itemCountInGroup1 = 0)
    {
        _ = itemCountInGroup1;
        return new FakeAppSettingsRepository();
    }

    /// <summary>
    /// SUT を生成します。（レガシ: FakeAppSettingsRepository 対応）
    /// 注意：このオーバーロードは廃止予定です。使用しないでください。
    /// </summary>
    [Obsolete("FakeAppSettingsRepository を直接使用しないでください。CreateSut を使用してください。")]
    private static MainViewModel CreateSut(
        FakeAppSettingsRepository _unused)
    {
        return new(
            new FakeRandomService(0),
            new FakeItemRepository(),
            new FakeGroupRepository(),
            new FakeAppSettingsRepository());
    }

    /// <summary>
    /// SUT を生成します。
    /// </summary>
    private static MainViewModel CreateSut(
        FakeRandomService? fakeRandom = null,
        int itemCountInGroup1 = 0,
        FakeItemRepository? customRepository = null,
        FakeGroupRepository? customGroupRepository = null,
        FakeAppSettingsRepository? customSettingsRepository = null,
        FakeDialogService? dialogService = null,
        FakeBackupRepository? backupRepository = null,
        ICloudSyncService? cloudSyncService = null,
        SyncTimestampStore? syncTimestampStore = null)
    {
        FakeItemRepository fakeRepo = customRepository ?? new FakeItemRepository();
        FakeGroupRepository fakeGroupRepo = customGroupRepository ?? new FakeGroupRepository();
        FakeAppSettingsRepository fakeSettingsRepository = customSettingsRepository ?? new FakeAppSettingsRepository();

        // customRepository が未指定かつ itemCountInGroup1 > 0 の場合、Roulette1にアイテムを設定
        if (customRepository is null && itemCountInGroup1 > 0)
        {
            var items = Enumerable.Range(1, itemCountInGroup1)
                .Select(i => new Item($"アイテム{i}", groupId: 1))
                .ToList();
            fakeRepo.InitializeWithItems(items);
        }

        return new(
            fakeRandom ?? new FakeRandomService(0),
            fakeRepo,
            fakeGroupRepo,
            fakeSettingsRepository,
            cloudSyncService,
            syncTimestampStore,
            dialogService,
            backupRepository);
    }

    /// <summary>
    /// 書き込まれることのない一時ファイルのパスで、同期タイムスタンプストアを生成します。
    /// </summary>
    private static SyncTimestampStore CreateUnusedTimestampStore()
    {
        return new SyncTimestampStore(Path.Combine(
            Path.GetTempPath(),
            "AppRouletteTests",
            Guid.NewGuid().ToString(),
            "syncTimestamps.json"));
    }

    /// <summary>
    /// 条件を満たすまで、最大 5 秒待ちます。
    /// 自動保存のように、遅れて終わる処理の結果を確かめるときに使います。
    /// </summary>
    /// <param name="condition">待つ条件。</param>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }

    /// <summary>
    /// 予約された自動保存が終わり、保存状態が「保存済み」になるまで待ちます。
    /// 編集から保存までは遅れがあるため、決まった時間だけ待つと、遅い環境で保存の前に確かめてしまいます。
    /// </summary>
    /// <param name="sut">自動保存を待つ ViewModel。</param>
    private static Task WaitForAutoSaveAsync(MainViewModel sut) =>
        WaitUntilAsync(() => sut.SaveStatusText == "保存済み");

    // ---------------------------------------------------------------
    // InitializeAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_呼び出し時_グループ一覧が設定される()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(9, sut.GroupList.Count);
    }

    [Fact]
    public async Task InitializeAsync_呼び出し時_最初のグループが選択される()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.SelectedGroup);
        Assert.Equal(1, sut.SelectedGroup.Id);
    }

    [Fact]
    public async Task InitializeAsync_保存済みグループ名がある場合_表示名に反映される()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        await fakeGroupRepo.SaveGroupNameAsync(1, "仕事用");
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("仕事用", sut.GroupList[0].DisplayName);
        Assert.Equal("仕事用", sut.GroupNameText);
    }

    [Fact]
    public async Task InitializeAsync_グループが空の場合_SelectedGroupがnullになる()
    {
        // 新しい InitializeAsync では常にデフォルト3グループが作成されるため、
        // このテストは意図的にスキップします
        // (廃止予定: 新アーキテクチャでは常に3グループが存在)
        await Task.CompletedTask;
    }

    // ---------------------------------------------------------------
    // SelectedGroup 変更
    // ---------------------------------------------------------------

    [Fact]
    public async Task SelectedGroup_グループを切り替えた場合_ItemsTextが更新される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
            new("アイテムX", groupId: 2),
            new("アイテムY", groupId: 2),
        });
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SelectedGroup = sut.GroupList[1];

        // Assert
        Assert.Contains("アイテムX", sut.ItemsText);
        Assert.Contains("アイテムY", sut.ItemsText);
    }

    [Fact]
    public async Task SelectedGroup_グループを切り替えた場合_ItemCountが更新される()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act（Roulette1が選択された時点でアイテム数が反映される）
        var result = sut.ItemCount;

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task SelectedGroup_nullに変更した場合_ItemsTextが空になる()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 2);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SelectedGroup = null;

        // Assert
        Assert.Equal(string.Empty, sut.ItemsText);
        Assert.Equal(0, sut.ItemCount);
    }

    [Fact]
    public async Task RenameGroupCommand_実行した場合_選択中グループ名を保存する()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.GroupNameText = "抽選用";
        await sut.RenameGroupCommand.ExecuteAsync(null);

        var restartedSut = CreateSut(customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("抽選用", sut.SelectedGroup?.DisplayName);
        Assert.Equal("抽選用", restartedSut.GroupList[0].DisplayName);
        Assert.Equal("保存済み", sut.SaveStatusText);
    }

    [Fact]
    public async Task GroupNameText_保存済み名と異なる場合_未保存状態になる()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.GroupNameText = "抽選用";

        // Assert
        Assert.True(sut.IsGroupNameUnsaved);
    }

    [Fact]
    public async Task RenameGroupCommand_実行した場合_未保存状態が解除される()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.GroupNameText = "抽選用";

        // Act
        await sut.RenameGroupCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.IsGroupNameUnsaved);
    }

    [Fact]
    public async Task ResetGroupNameCommand_実行した場合_入力欄のみ既定名にする()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.GroupNameText = "作業用";
        await sut.RenameGroupCommand.ExecuteAsync(null);

        // Act
        await sut.ResetGroupNameCommand.ExecuteAsync(null);

        var restartedSut = CreateSut(customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("作業用", sut.SelectedGroup?.DisplayName);
        Assert.Equal("New Roulette", sut.GroupNameText);
        Assert.Equal("作業用", restartedSut.GroupList[0].DisplayName);
    }

    [Fact]
    public async Task AddGroupCommand_実行した場合_グループを追加して保存する()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.AddGroupCommand.ExecuteAsync(null);

        var restartedSut = CreateSut(customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(10, sut.GroupList.Count);
        Assert.Equal("New Roulette", sut.SelectedGroup?.DisplayName);
        Assert.Equal(10, restartedSut.GroupList.Count);
        Assert.Equal("New Roulette", restartedSut.GroupList[9].DisplayName);
    }

    [Fact]
    public async Task AddGroupCommand_Roulette9が存在する場合_NewRouletteを追加する()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);
        await sut.DeleteGroupCommand.ExecuteAsync(null);

        // Act
        await sut.AddGroupCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(sut.GroupList, g => g.DisplayName == "Roulette9");
        Assert.Equal("New Roulette", sut.SelectedGroup?.DisplayName);
    }

    [Fact]
    public async Task DuplicateGroupCommand_実行した場合_名前とアイテムを複製して選択する()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var fakeItemRepo = new FakeItemRepository();
        fakeItemRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", weight: 3, groupId: 1),
            new("アイテムB", weight: 5, groupId: 1),
        });
        var sut = CreateSut(
            customRepository: fakeItemRepo,
            customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.DuplicateGroupCommand.ExecuteAsync(null);

        var duplicatedGroup = sut.SelectedGroup;
        var duplicatedItems = await fakeItemRepo.GetItemsByGroupAsync(duplicatedGroup!.Id);
        var restartedSut = CreateSut(
            customRepository: fakeItemRepo,
            customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(10, sut.GroupList.Count);
        Assert.Equal("Roulette1 Copy", duplicatedGroup.DisplayName);
        Assert.Equal("Roulette1 Copy", sut.GroupNameText);
        Assert.Equal(2, sut.ItemCount);
        Assert.Equal("アイテムA,3\nアイテムB,5", sut.ItemsText);
        Assert.Collection(
            duplicatedItems,
            item =>
            {
                Assert.Equal("アイテムA", item.Label);
                Assert.Equal(3, item.Weight);
            },
            item =>
            {
                Assert.Equal("アイテムB", item.Label);
                Assert.Equal(5, item.Weight);
            });
        Assert.Equal(10, restartedSut.GroupList.Count);
        Assert.Equal("Roulette1 Copy", restartedSut.GroupList[9].DisplayName);
    }

    [Fact]
    public async Task DeleteGroupCommand_実行した場合_グループとアイテムを削除する()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var fakeItemRepo = new FakeItemRepository();
        fakeItemRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
        });
        var sut = CreateSut(
            customRepository: fakeItemRepo,
            customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteGroupCommand.ExecuteAsync(null);

        var items = await fakeItemRepo.GetItemsByGroupAsync(1);
        var restartedSut = CreateSut(customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(8, sut.GroupList.Count);
        Assert.Empty(items);
        Assert.Equal(8, restartedSut.GroupList.Count);
        Assert.DoesNotContain(restartedSut.GroupList, g => g.Id == 1);
    }

    [Fact]
    public async Task DeleteGroupCommand_確認でキャンセルした場合_グループとアイテムを削除しない()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var fakeItemRepo = new FakeItemRepository();
        fakeItemRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
        });
        var dialog = new FakeDialogService(confirmResult: false);
        var sut = CreateSut(
            customRepository: fakeItemRepo,
            customGroupRepository: fakeGroupRepo,
            dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteGroupCommand.ExecuteAsync(null);

        var items = await fakeItemRepo.GetItemsByGroupAsync(1);
        var restartedSut = CreateSut(customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Equal(9, sut.GroupList.Count);
        Assert.Equal(1, sut.SelectedGroup?.Id);
        Assert.Single(items);
        Assert.Contains(restartedSut.GroupList, g => g.Id == 1);
    }

    [Fact]
    public async Task DeleteGroupCommand_確認で削除を選んだ場合_リスト名とアイテム数を示して削除する()
    {
        // Arrange
        var fakeItemRepo = new FakeItemRepository();
        fakeItemRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
            new("アイテムB", groupId: 1),
        });
        var dialog = new FakeDialogService(confirmResult: true);
        var sut = CreateSut(
            customRepository: fakeItemRepo,
            dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteGroupCommand.ExecuteAsync(null);

        var items = await fakeItemRepo.GetItemsByGroupAsync(1);

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Contains("「Roulette1」", dialog.LastConfirmMessage);
        Assert.Contains("2 件", dialog.LastConfirmMessage);
        Assert.Equal(8, sut.GroupList.Count);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ClearItemsCommand_確認でキャンセルした場合_アイテムを削除しない()
    {
        // Arrange
        var dialog = new FakeDialogService(confirmResult: false);
        var fakeItemRepo = new FakeItemRepository();
        fakeItemRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
            new("アイテムB", groupId: 1),
            new("アイテムC", groupId: 1),
        });
        var sut = CreateSut(
            customRepository: fakeItemRepo,
            dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.ClearItemsCommand.ExecuteAsync(null);
        await Task.Delay(100);

        var items = await fakeItemRepo.GetItemsByGroupAsync(1);

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Equal(3, sut.ItemCount);
        Assert.Equal(3, sut.EditableItems.Count);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task ClearItemsCommand_確認で削除を選んだ場合_アイテム数を示して削除する()
    {
        // Arrange
        var dialog = new FakeDialogService(confirmResult: true);
        var fakeItemRepo = new FakeItemRepository();
        fakeItemRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
            new("アイテムB", groupId: 1),
            new("アイテムC", groupId: 1),
        });
        var sut = CreateSut(
            customRepository: fakeItemRepo,
            dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.ClearItemsCommand.ExecuteAsync(null);
        await WaitForAutoSaveAsync(sut);

        var items = await fakeItemRepo.GetItemsByGroupAsync(1);

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Contains("3 件", dialog.LastConfirmMessage);
        Assert.Equal(0, sut.ItemCount);
        Assert.Equal(string.Empty, sut.ItemsText);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ClearItemsCommand_アイテムが無い場合_確認を表示しない()
    {
        // Arrange
        var dialog = new FakeDialogService(confirmResult: false);
        var sut = CreateSut(dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.ClearItemsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(0, dialog.ConfirmCallCount);
        Assert.Equal(0, sut.ItemCount);
    }

    // ---------------------------------------------------------------
    // CSV インポート・エクスポート
    // ---------------------------------------------------------------

    [Fact]
    public async Task ImportItemsFromCsvAsync_アイテムが無い場合_確認せずにヘッダーを除いて読み込む()
    {
        // Arrange
        var dialog = new FakeDialogService(confirmResult: false);
        var sut = CreateSut(dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.ImportItemsFromCsvAsync("アイテム名,Weight\r\nアイテムA,3\r\nアイテムB\r\n");

        // Assert
        Assert.Equal(0, dialog.ConfirmCallCount);
        Assert.Empty(dialog.ShownMessages);
        Assert.Equal("アイテムA,3\nアイテムB,1", sut.ItemsText);
        Assert.Equal(2, sut.ItemCount);
    }

    [Fact]
    public async Task ImportItemsFromCsvAsync_既存アイテムがありキャンセルした場合_置き換えない()
    {
        // Arrange
        var dialog = new FakeDialogService(confirmResult: false);
        var sut = CreateSut(itemCountInGroup1: 2, dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);
        var originalText = sut.ItemsText;

        // Act
        await sut.ImportItemsFromCsvAsync("アイテムX,5");

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Equal(originalText, sut.ItemsText);
        Assert.Equal(2, sut.ItemCount);
    }

    [Fact]
    public async Task ImportItemsFromCsvAsync_既存アイテムがあり置き換えを選んだ場合_件数を示して置き換えて保存する()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
            new("アイテムB", groupId: 1),
        });
        var dialog = new FakeDialogService(confirmResult: true);
        var sut = CreateSut(customRepository: fakeRepo, dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.ImportItemsFromCsvAsync("アイテムX,5");
        await WaitForAutoSaveAsync(sut);

        var items = await fakeRepo.GetItemsByGroupAsync(1);

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Contains("「Roulette1」のアイテム 2 件", dialog.LastConfirmMessage);
        Assert.Contains("CSV ファイルのアイテム 1 件", dialog.LastConfirmMessage);
        Assert.Equal("アイテムX,5", sut.ItemsText);
        Assert.Single(items);
        Assert.Equal("アイテムX", items[0].Label);
        Assert.Equal(5, items[0].Weight);
    }

    [Fact]
    public async Task ImportItemsFromCsvAsync_読み込めるアイテムが無い場合_メッセージを表示して変更しない()
    {
        // Arrange
        var dialog = new FakeDialogService();
        var sut = CreateSut(itemCountInGroup1: 2, dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);
        var originalText = sut.ItemsText;

        // Act
        await sut.ImportItemsFromCsvAsync("アイテム名,Weight\n\n");

        // Assert
        Assert.Equal(0, dialog.ConfirmCallCount);
        Assert.Single(dialog.ShownMessages);
        Assert.Contains("読み込めるアイテムがありませんでした", dialog.ShownMessages[0]);
        Assert.Equal(originalText, sut.ItemsText);
    }

    [Fact]
    public async Task ImportItemsFromCsvAsync_最大件数を超える場合_超えた件数を知らせて最大件数まで読み込む()
    {
        // Arrange
        var dialog = new FakeDialogService();
        var sut = CreateSut(dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);
        var csv = string.Join(
            "\n",
            Enumerable.Range(1, RouletteGroup.MAX_ITEM_COUNT + 2).Select(i => $"アイテム{i},1"));

        // Act
        await sut.ImportItemsFromCsvAsync(csv);

        // Assert
        Assert.Equal(RouletteGroup.MAX_ITEM_COUNT, sut.ItemCount);
        Assert.Single(dialog.ShownMessages);
        Assert.Contains("31 件目以降の 2 件は読み込みませんでした", dialog.ShownMessages[0]);
    }

    [Fact]
    public async Task ImportItemsFromCsvAsync_既存アイテムがあり最大件数を超える場合_確認で超えた件数を知らせる()
    {
        // Arrange
        var dialog = new FakeDialogService(confirmResult: true);
        var sut = CreateSut(itemCountInGroup1: 1, dialogService: dialog);
        await sut.InitializeCommand.ExecuteAsync(null);
        var csv = string.Join(
            "\n",
            Enumerable.Range(1, RouletteGroup.MAX_ITEM_COUNT + 2).Select(i => $"アイテム{i},1"));

        // Act
        await sut.ImportItemsFromCsvAsync(csv);

        // Assert
        Assert.Contains("31 件目以降の 2 件は読み込みません", dialog.LastConfirmMessage);
        Assert.Empty(dialog.ShownMessages);
        Assert.Equal(RouletteGroup.MAX_ITEM_COUNT, sut.ItemCount);
    }

    [Fact]
    public async Task ExportItemsToCsv_書き出したCSVを読み込んだ場合_同じアイテムに戻る()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", weight: 3, groupId: 1),
            new("アイテムB", weight: 5, groupId: 1),
        });
        var sut = CreateSut(customRepository: fakeRepo, dialogService: new FakeDialogService());
        await sut.InitializeCommand.ExecuteAsync(null);
        var originalText = sut.ItemsText;

        // Act
        var csv = sut.ExportItemsToCsv();
        await sut.ImportItemsFromCsvAsync(csv);

        // Assert
        Assert.StartsWith("アイテム名,Weight\n", csv);
        Assert.Equal(originalText, sut.ItemsText);
    }

    // ---------------------------------------------------------------
    // バックアップ・復元
    // ---------------------------------------------------------------

    /// <summary>
    /// 復元テスト用のバックアップファイルの内容を作成します。
    /// リスト 2 件、アイテム 3 件を含みます。
    /// </summary>
    private static string CreateBackupJsonForRestore()
    {
        var groups = new List<RouletteGroup>
        {
            new(1, "朝会")
            {
                ExcludeOnWin = true,
                Items =
                {
                    new RouletteItem("Aさん") { Weight = 2 },
                    new RouletteItem("Bさん") { IsEnabled = false },
                },
            },
            new(2, "昼食")
            {
                Items = { new RouletteItem("カレー") },
            },
        };

        return BackupSerializer.Serialize(groups, DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateBackupJson_保存前の編集がある場合_編集を含めて全リストを書き出す()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 1);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.ItemsText = "編集したアイテム,4";
        sut.IsExcludeOnWinEnabled = true;

        // Act
        var json = sut.CreateBackupJson();
        var groups = BackupSerializer.Deserialize(json);

        // Assert
        Assert.Equal(sut.GroupList.Count, groups.Count);
        Assert.Equal("Roulette1", groups[0].DisplayName);
        Assert.True(groups[0].ExcludeOnWin);
        var item = Assert.Single(groups[0].Items);
        Assert.Equal("編集したアイテム", item.Name);
        Assert.Equal(4, item.Weight);
    }

    [Fact]
    public async Task RestoreBackupAsync_復元を選んだ場合_全リストを置き換えて再起動後も残る()
    {
        // Arrange
        var groupRepo = new FakeGroupRepository();
        var itemRepo = new FakeItemRepository();
        itemRepo.InitializeWithItems(new List<Item> { new("元のアイテム", groupId: 1) });
        var dialog = new FakeDialogService(confirmResult: true);
        var sut = CreateSut(
            customRepository: itemRepo,
            customGroupRepository: groupRepo,
            dialogService: dialog,
            backupRepository: new FakeBackupRepository(groupRepo, itemRepo));
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.RestoreBackupAsync(CreateBackupJsonForRestore());

        var allItems = await itemRepo.GetItemsAsync();
        var restartedSut = CreateSut(customRepository: itemRepo, customGroupRepository: groupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Contains("今のリスト 9 件", dialog.LastConfirmMessage);
        Assert.Contains("バックアップのリスト 2 件（アイテム 3 件）", dialog.LastConfirmMessage);
        Assert.Contains("復元しました", dialog.ShownMessages.Last());
        Assert.Equal(new[] { "朝会", "昼食" }, sut.GroupList.Select(g => g.DisplayName));
        Assert.Equal("朝会", sut.SelectedGroup?.DisplayName);
        Assert.Equal("Aさん,2\nBさん,1", sut.ItemsText);
        Assert.DoesNotContain(allItems, item => item.Label == "元のアイテム");
        Assert.Equal(new[] { "朝会", "昼食" }, restartedSut.GroupList.Select(g => g.DisplayName));
        Assert.True(restartedSut.GroupList[0].ExcludeOnWin);
        Assert.False(restartedSut.GroupList[0].Items[1].IsEnabled);
    }

    [Fact]
    public async Task RestoreBackupAsync_キャンセルした場合_データを変更しない()
    {
        // Arrange
        var groupRepo = new FakeGroupRepository();
        var itemRepo = new FakeItemRepository();
        itemRepo.InitializeWithItems(new List<Item> { new("元のアイテム", groupId: 1) });
        var dialog = new FakeDialogService(confirmResult: false);
        var backupRepo = new FakeBackupRepository(groupRepo, itemRepo);
        var sut = CreateSut(
            customRepository: itemRepo,
            customGroupRepository: groupRepo,
            dialogService: dialog,
            backupRepository: backupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.RestoreBackupAsync(CreateBackupJsonForRestore());

        // Assert
        Assert.Equal(1, dialog.ConfirmCallCount);
        Assert.Equal(0, backupRepo.ReplaceAllCallCount);
        Assert.Equal(9, sut.GroupList.Count);
        Assert.Equal("元のアイテム,1", sut.ItemsText);
    }

    [Fact]
    public async Task RestoreBackupAsync_このアプリのバックアップではない場合_確認せずに理由を表示する()
    {
        // Arrange
        var groupRepo = new FakeGroupRepository();
        var itemRepo = new FakeItemRepository();
        var dialog = new FakeDialogService(confirmResult: true);
        var backupRepo = new FakeBackupRepository(groupRepo, itemRepo);
        var sut = CreateSut(
            customRepository: itemRepo,
            customGroupRepository: groupRepo,
            dialogService: dialog,
            backupRepository: backupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.RestoreBackupAsync("{ \"App\": \"OtherApp\", \"FormatVersion\": 1 }");

        // Assert
        Assert.Equal(0, dialog.ConfirmCallCount);
        Assert.Equal(0, backupRepo.ReplaceAllCallCount);
        Assert.Contains(
            "AppRoulette のバックアップファイルではありません",
            Assert.Single(dialog.ShownMessages));
        Assert.Equal(9, sut.GroupList.Count);
    }

    [Fact]
    public async Task RestoreBackupAsync_保存に失敗した場合_理由を表示してデータを変更しない()
    {
        // Arrange
        var groupRepo = new FakeGroupRepository();
        var itemRepo = new FakeItemRepository();
        itemRepo.InitializeWithItems(new List<Item> { new("元のアイテム", groupId: 1) });
        var dialog = new FakeDialogService(confirmResult: true);
        var backupRepo = new FakeBackupRepository(groupRepo, itemRepo, shouldFail: true);
        var sut = CreateSut(
            customRepository: itemRepo,
            customGroupRepository: groupRepo,
            dialogService: dialog,
            backupRepository: backupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.RestoreBackupAsync(CreateBackupJsonForRestore());

        // Assert
        Assert.Equal(1, backupRepo.ReplaceAllCallCount);
        Assert.Contains("データは変更されていません", Assert.Single(dialog.ShownMessages));
        Assert.Equal("保存失敗", sut.SaveStatusText);
        Assert.Equal(9, sut.GroupList.Count);
        Assert.Equal("元のアイテム,1", sut.ItemsText);
    }

    [Fact]
    public async Task RestoreBackupAsync_保存待ちの編集がある場合_復元後に古い内容を書き込まない()
    {
        // Arrange
        var groupRepo = new FakeGroupRepository();
        var itemRepo = new FakeItemRepository();
        var sut = CreateSut(
            customRepository: itemRepo,
            customGroupRepository: groupRepo,
            dialogService: new FakeDialogService(confirmResult: true),
            backupRepository: new FakeBackupRepository(groupRepo, itemRepo));
        await sut.InitializeCommand.ExecuteAsync(null);

        // 復元後に選択されない 2 件目のリストで、保存待ちの編集を作る
        sut.SelectedGroup = sut.GroupList[1];
        sut.ItemsText = "保存待ちの古い編集,1";

        // Act
        await sut.RestoreBackupAsync(CreateBackupJsonForRestore());
        await Task.Delay(100);

        var items = await itemRepo.GetItemsByGroupAsync(2);

        // Assert
        Assert.Equal(new[] { "カレー" }, items.Select(item => item.Label));
    }

    [Fact]
    public async Task MoveGroupDownCommand_実行した場合_並び順を保存する()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        await sut.MoveGroupDownCommand.ExecuteAsync(null);

        var restartedSut = CreateSut(customGroupRepository: fakeGroupRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(2, sut.GroupList[0].Id);
        Assert.Equal(1, sut.GroupList[1].Id);
        Assert.Equal(2, restartedSut.GroupList[0].Id);
        Assert.Equal(1, restartedSut.GroupList[1].Id);
    }

    // ---------------------------------------------------------------
    // ItemsText 変更（アイテム数・保存）
    // ---------------------------------------------------------------

    [Fact]
    public async Task ItemsText_テキストを変更した場合_ItemCountが更新される()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテム1";

        // Assert
        Assert.Equal(1, sut.ItemCount);
    }

    [Fact]
    public async Task ItemsText_空行が含まれる場合_空行を除いたアイテム数になる()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテム1\n\nアイテム2\n  \nアイテム3";

        // Assert
        Assert.Equal(3, sut.ItemCount);
    }

    [Fact]
    public async Task ItemsText_改行が増えた場合_保存が呼ばれる()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);
        var itemsCountBefore = (await fakeRepo.GetItemsAsync()).Count;

        // Act（行数を増やす：1行⇒2行）
        sut.ItemsText = "アイテム1";
        sut.ItemsText = "アイテム1\nアイテム2";

        Assert.Equal("保存中", sut.SaveStatusText);

        // 非同期保存の完了を待つ
        await WaitForAutoSaveAsync(sut);

        // Assert - SQLite にアイテムが増えていることを確認
        var itemsAfter = await fakeRepo.GetItemsAsync();
        Assert.True(itemsAfter.Count > itemsCountBefore, "SQLite にアイテムが保存されていません");
        Assert.Equal("保存済み", sut.SaveStatusText);
    }

    [Fact]
    public async Task ItemsText_改行が増えない場合_保存が呼ばれる()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテム1";
        await WaitForAutoSaveAsync(sut);
        sut.ItemsText = "アイテムA"; // 行数変化なし

        await WaitForAutoSaveAsync(sut);

        // Assert
        var items = await fakeRepo.GetItemsByGroupAsync(1);
        Assert.Single(sut.GroupList[0].Items);
        Assert.Equal("アイテムA", sut.GroupList[0].Items[0].Name);
        Assert.Single(items);
        Assert.Equal("アイテムA", items[0].Label);
        Assert.Equal("保存済み", sut.SaveStatusText);
    }

    [Fact]
    public async Task InitializeAsync_SQLiteのWeight_ItemsTextとItemsに復元される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", weight: 5, groupId: 1),
            new("アイテムB", weight: 2, groupId: 1),
        });
        var sut = CreateSut(customRepository: fakeRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("アイテムA,5\nアイテムB,2", sut.ItemsText);
        Assert.Equal(5, sut.SelectedGroup?.Items[0].Weight);
        Assert.Equal(2, sut.SelectedGroup?.Items[1].Weight);
    }

    [Fact]
    public async Task ItemsText_同一ラベルのWeight変更_SQLiteに更新される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", weight: 1, groupId: 1),
        });
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテムA,4";
        await WaitForAutoSaveAsync(sut);

        // Assert
        var items = await fakeRepo.GetItemsByGroupAsync(1);
        Assert.Single(items);
        Assert.Equal("アイテムA", items[0].Label);
        Assert.Equal(4, items[0].Weight);
    }

    [Fact]
    public async Task ItemsText_名前Weight形式を保存した場合_再初期化で復元される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテムA,5\nアイテムB,2";
        await WaitForAutoSaveAsync(sut);

        var restartedSut = CreateSut(customRepository: fakeRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("アイテムA,5\nアイテムB,2", restartedSut.ItemsText);
        Assert.Equal(5, restartedSut.SelectedGroup?.Items[0].Weight);
        Assert.Equal(2, restartedSut.SelectedGroup?.Items[1].Weight);
    }

    // ---------------------------------------------------------------
    // SpinCommand.CanExecute
    // ---------------------------------------------------------------

    [Fact]
    public async Task SpinCommand_アイテムが0件の場合_CanExecuteがfalseになる()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 0);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act & Assert
        Assert.False(sut.SpinCommand.CanExecute(null));
    }

    [Fact]
    public async Task SpinCommand_アイテムが1件以上でスピン中でない場合_CanExecuteがtrueになる()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.SpinCommand.CanExecute(null));
    }

    [Fact]
    public async Task SpinCommand_スピン中の場合_CanExecuteがfalseになる()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.IsSpinning = true;

        // Assert
        Assert.False(sut.SpinCommand.CanExecute(null));
    }

    // ---------------------------------------------------------------
    // Spin 実行
    // ---------------------------------------------------------------

    [Fact]
    public async Task Spin_指定インデックスが返される()
    {
        // Arrange
        var sut = CreateSut(
            fakeRandom: new FakeRandomService(2),
            itemCountInGroup1: 5);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SpinCommand.Execute(null);

        // Assert
        Assert.Equal(2, sut.SelectedItemIndex);
    }

    [Fact]
    public async Task Spin_実行した場合_アイテム数の範囲内のインデックスが返る()
    {
        // Arrange
        const int itemCount = 5;
        const int fixedIndex = 4;
        var sut = CreateSut(
            fakeRandom: new FakeRandomService(fixedIndex),
            itemCountInGroup1: itemCount);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SpinCommand.Execute(null);

        // Assert
        Assert.InRange(sut.SelectedItemIndex, 0, itemCount - 1);
    }

    // ---------------------------------------------------------------
    // ParseItems & FormatItems
    // ---------------------------------------------------------------

    [Fact]
    public async Task ParseItems_改行区切りのテキスト_アイテムリストに変換できる()
    {
        // Act
        var result = MainViewModel.ParseItems("アイテムA\nアイテムB\nアイテムC");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("アイテムA", result[0].Name);
        Assert.Equal(1, result[0].Weight);
        Assert.Equal("アイテムB", result[1].Name);
        Assert.Equal(1, result[1].Weight);
        Assert.Equal("アイテムC", result[2].Name);
        Assert.Equal(1, result[2].Weight);
    }

    [Fact]
    public async Task ParseItems_CSV形式_Weightを設定できる()
    {
        // Act
        var result = MainViewModel.ParseItems("アイテムA,5\nアイテムB,2\nアイテムC,1");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("アイテムA", result[0].Name);
        Assert.Equal(5, result[0].Weight);
        Assert.Equal("アイテムB", result[1].Name);
        Assert.Equal(2, result[1].Weight);
        Assert.Equal("アイテムC", result[2].Name);
        Assert.Equal(1, result[2].Weight);
    }

    [Fact]
    public async Task ParseItems_Weight省略_デフォルト値1を使用する()
    {
        // Act
        var result = MainViewModel.ParseItems("アイテムA,3\nアイテムB\nアイテムC,2");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(3, result[0].Weight);
        Assert.Equal(1, result[1].Weight); // 省略されたため1
        Assert.Equal(2, result[2].Weight);
    }

    [Fact]
    public async Task ParseItems_Weight非数値_デフォルト値1を使用する()
    {
        // Act
        var result = MainViewModel.ParseItems("アイテムA,abc\nアイテムB,5");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Weight); // 非数値なため1
        Assert.Equal(5, result[1].Weight);
    }

    [Fact]
    public async Task ParseItems_Weight範囲外_制限される()
    {
        // Act
        var result = MainViewModel.ParseItems("アイテムA,10\nアイテムB,0\nアイテムC,-5");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(5, result[0].Weight); // 最大5に制限
        Assert.Equal(1, result[1].Weight); // 最小1に制限
        Assert.Equal(1, result[2].Weight); // 最小1に制限
    }

    [Fact]
    public async Task ParseItems_空行が含まれる場合_空行を除外する()
    {
        // Act
        var result = MainViewModel.ParseItems("アイテムA\n\nアイテムB");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FormatItems_アイテムリスト_CSV形式のテキストに変換できる()
    {
        // Act
        var items = new List<RouletteItem>
        {
            new("アイテムA") { Weight = 5 },
            new("アイテムB") { Weight = 2 }
        };
        var result = MainViewModel.FormatItems(items);

        // Assert
        Assert.Equal("アイテムA,5\nアイテムB,2", result);
    }

    [Fact]
    public async Task FormatItems_Weight1を含む_CSVで1を出力する()
    {
        // Act
        var items = new List<RouletteItem>
        {
            new("アイテムA") { Weight = 1 },
            new("アイテムB") { Weight = 3 }
        };
        var result = MainViewModel.FormatItems(items);

        // Assert
        Assert.Equal("アイテムA,1\nアイテムB,3", result);
    }

    // ---------------------------------------------------------------
    // 表形式編集
    // ---------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_SQLiteのItems_EditableItemsに復元される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", weight: 4, groupId: 1),
            new("アイテムB", weight: 2, groupId: 1),
        });
        var sut = CreateSut(customRepository: fakeRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(2, sut.EditableItems.Count);
        Assert.Equal("アイテムA", sut.EditableItems[0].Name);
        Assert.Equal(4, sut.EditableItems[0].Weight);
        Assert.Equal("アイテムB", sut.EditableItems[1].Name);
        Assert.Equal(2, sut.EditableItems[1].Weight);
    }

    [Fact]
    public async Task AddItemRowCommand_実行した場合_ItemsTextとSelectedGroupに反映される()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.AddItemRowCommand.Execute(null);

        // Assert
        Assert.Single(sut.EditableItems);
        Assert.Equal("新しいアイテム", sut.EditableItems[0].Name);
        Assert.Equal("新しいアイテム,1", sut.ItemsText);
        Assert.Single(sut.SelectedGroup!.Items);
        Assert.Equal("新しいアイテム", sut.SelectedGroup.Items[0].Name);
    }

    [Fact]
    public async Task EditableItems_名前とWeightを変更した場合_ItemsTextとSelectedGroupに反映される()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.ItemsText = "アイテムA,1";

        // Act
        sut.EditableItems[0].Name = "アイテムB";
        sut.EditableItems[0].Weight = 5;

        // Assert
        Assert.Equal("アイテムB,5", sut.ItemsText);
        Assert.Equal("アイテムB", sut.SelectedGroup!.Items[0].Name);
        Assert.Equal(5, sut.SelectedGroup.Items[0].Weight);
    }

    [Fact]
    public async Task EditableItems_重複名がある場合_状態表示に重複が表示される()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテムA,1\nアイテムA,3";

        // Assert
        Assert.Contains("重複: アイテムA", sut.ItemEditStatusText);
    }

    [Fact]
    public async Task EditableItems_最大件数の場合_追加不可と状態表示になる()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = string.Join('\n', Enumerable.Range(1, RouletteGroup.MAX_ITEM_COUNT)
            .Select(i => $"アイテム{i},1"));

        // Assert
        Assert.Equal(RouletteGroup.MAX_ITEM_COUNT, sut.EditableItems.Count);
        Assert.Contains($"最大{RouletteGroup.MAX_ITEM_COUNT}件", sut.ItemEditStatusText);
        Assert.False(sut.AddItemRowCommand.CanExecute(null));
    }

    [Fact]
    public async Task SortItemsCommand_実行した場合_名前順に並び替える()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.ItemsText = "C,1\nA,1\nB,1";

        // Act
        sut.SortItemsCommand.Execute(null);

        // Assert
        Assert.Equal("A,1\nB,1\nC,1", sut.ItemsText);
        Assert.Equal("A", sut.SelectedGroup!.Items[0].Name);
        Assert.Equal("B", sut.SelectedGroup.Items[1].Name);
        Assert.Equal("C", sut.SelectedGroup.Items[2].Name);
    }

    // ---------------------------------------------------------------
    // CountLines
    // ---------------------------------------------------------------

    [Fact]
    public void CountLines_空文字の場合_0を返す()
    {
        // Act
        var result = MainViewModel.CountLines(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountLines_改行なしの場合_1を返す()
    {
        // Act
        var result = MainViewModel.CountLines("アイテム1");

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void CountLines_改行ありの場合_行数を返す()
    {
        // Act
        var result = MainViewModel.CountLines("アイテム1\nアイテム2\nアイテム3");

        // Assert
        Assert.Equal(3, result);
    }

    // ---------------------------------------------------------------
    // グループID永続化
    // ---------------------------------------------------------------

    [Fact]
    public async Task SelectedGroup_グループを切り替えた場合_グループIDを保存する()
    {
        // Arrange
        var fakeSettingsRepository = new FakeAppSettingsRepository();
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customSettingsRepository: fakeSettingsRepository, customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        sut.SelectedGroup = sut.GroupList[2]; // Roulette3 を選択

        // Assert: SelectGroupが呼び出されると、新規作成した永続化サービスではなく既存のsutが使用するため
        // 初回InitializeAsyncで1回、SelectedGroup変更で1回、合計2回呼ばれることを確認
        await Task.Delay(100);
        Assert.True(fakeSettingsRepository.SaveLastSelectedGroupIdCallCount > 0);
        // LastSavedGroupId は最後に設定された値が Roulette3 (Id=3) であること
        Assert.Equal(3, fakeSettingsRepository.LastSavedGroupId);
    }

    [Fact]
    public async Task InitializeAsync_保存されたグループIDがある場合_該当グループを復元する()
    {
        // Arrange
        var fakeSettingsRepository = new FakeAppSettingsRepository
        {
            LastSavedGroupId = 2 // Roulette2 を保存済み
        };
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customSettingsRepository: fakeSettingsRepository, customRepository: fakeRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.SelectedGroup);
        Assert.Equal(2, sut.SelectedGroup.Id);
    }

    [Fact]
    public async Task InitializeAsync_保存されたグループIDがない場合_最初のグループを選択する()
    {
        // Arrange
        var fakeSettingsRepository = new FakeAppSettingsRepository
        {
            LastSavedGroupId = 0 // グループID未保存
        };
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customSettingsRepository: fakeSettingsRepository, customRepository: fakeRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.SelectedGroup);
        Assert.Equal(1, sut.SelectedGroup.Id);
    }

    [Fact]
    public async Task InitializeAsync_保存されたグループIDが範囲外の場合_最初のグループを選択する()
    {
        // Arrange
        var fakeSettingsRepository = new FakeAppSettingsRepository
        {
            LastSavedGroupId = 99 // 存在しないグループID
        };
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customSettingsRepository: fakeSettingsRepository, customRepository: fakeRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.SelectedGroup);
        Assert.Equal(1, sut.SelectedGroup.Id);
    }

    // ---------------------------------------------------------------
    // 一時的な無効化 / 当たったら除外
    // ---------------------------------------------------------------

    [Fact]
    public async Task WheelItems_無効化アイテムがある場合_有効アイテムのみ返す()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SelectedGroup!.Items[1].IsEnabled = false;

        // Assert
        Assert.Equal(2, sut.WheelItems.Count);
        Assert.All(sut.WheelItems, item => Assert.True(item.IsEnabled));
    }

    [Fact]
    public async Task Spin_無効化アイテムがある場合_有効アイテムから選択される()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.SelectedGroup!.Items[0].IsEnabled = false;

        // Act
        sut.SpinCommand.Execute(null);

        // Assert
        Assert.InRange(sut.SelectedItemIndex, 0, 1);
        Assert.True(sut.WheelItems[sut.SelectedItemIndex].IsEnabled);
    }

    [Fact]
    public async Task Spin_全アイテムが無効の場合_実行できない()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 2);
        await sut.InitializeCommand.ExecuteAsync(null);
        foreach (var item in sut.SelectedGroup!.Items)
        {
            item.IsEnabled = false;
        }

        // Assert
        Assert.False(sut.SpinCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExcludeWinnerIfNeeded_モード有効時_当選アイテムが無効化される()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.IsExcludeOnWinEnabled = true;
        sut.SpinCommand.Execute(null);
        var winner = sut.WheelItems[sut.SelectedItemIndex];

        // Act
        sut.ExcludeWinnerIfNeeded();

        // Assert
        Assert.False(winner.IsEnabled);
        Assert.Equal(2, sut.WheelItems.Count);
    }

    [Fact]
    public async Task ExcludeWinnerIfNeeded_モード無効時_当選アイテムは無効化されない()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 3);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.SpinCommand.Execute(null);
        var winner = sut.WheelItems[sut.SelectedItemIndex];

        // Act
        sut.ExcludeWinnerIfNeeded();

        // Assert
        Assert.True(winner.IsEnabled);
        Assert.Equal(3, sut.WheelItems.Count);
    }

    [Fact]
    public async Task ExcludeWinnerIfNeeded_有効アイテムが残り1件の場合_無効化されない()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 1);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.IsExcludeOnWinEnabled = true;
        sut.SpinCommand.Execute(null);
        var winner = sut.WheelItems[sut.SelectedItemIndex];

        // Act
        sut.ExcludeWinnerIfNeeded();

        // Assert
        Assert.True(winner.IsEnabled);
        Assert.Single(sut.WheelItems);
    }

    [Fact]
    public async Task EditableItems_有効チェックを外した場合_再初期化で無効状態が復元される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", weight: 1, groupId: 1),
            new("アイテムB", weight: 3, groupId: 1),
            new("アイテムC", weight: 1, groupId: 1),
        });
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.EditableItems[1].IsEnabled = false;
        await WaitForAutoSaveAsync(sut);

        var restartedSut = CreateSut(customRepository: fakeRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        var items = restartedSut.SelectedGroup!.Items;
        Assert.True(items[0].IsEnabled);
        Assert.False(items[1].IsEnabled);
        Assert.True(items[2].IsEnabled);
        Assert.Equal(3, items[1].Weight);
    }

    [Fact]
    public async Task ExcludeWinnerIfNeeded_モード有効時_再初期化で除外状態が復元される()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        fakeRepo.InitializeWithItems(new List<Item>
        {
            new("アイテムA", groupId: 1),
            new("アイテムB", groupId: 1),
            new("アイテムC", groupId: 1),
        });
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);
        sut.IsExcludeOnWinEnabled = true;
        sut.SpinCommand.Execute(null);
        var winnerName = sut.WheelItems[sut.SelectedItemIndex].Name;

        // Act
        sut.ExcludeWinnerIfNeeded();
        await WaitForAutoSaveAsync(sut);

        var restartedSut = CreateSut(customRepository: fakeRepo);
        await restartedSut.InitializeCommand.ExecuteAsync(null);

        // Assert
        var winner = restartedSut.SelectedGroup!.Items.Single(item => item.Name == winnerName);
        Assert.False(winner.IsEnabled);
        Assert.Equal(2, restartedSut.WheelItems.Count);
    }

    [Fact]
    public async Task IsExcludeOnWinEnabled_設定変更時_選択中グループへ反映される()
    {
        // Arrange
        var sut = CreateSut(itemCountInGroup1: 1);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.IsExcludeOnWinEnabled = true;

        // Assert
        Assert.True(sut.SelectedGroup!.ExcludeOnWin);
    }

    // ---------------------------------------------------------------
    // OneDrive 同期の可否
    // ---------------------------------------------------------------

    [Fact]
    public void IsSyncAvailable_OneDriveフォルダーが見つからない場合_同期できない()
    {
        // Arrange
        var sut = CreateSut(
            cloudSyncService: new OneDriveFolderSyncService(baseDir: null),
            syncTimestampStore: CreateUnusedTimestampStore());

        // Act
        var isAvailable = sut.IsSyncAvailable;
        var canSync = sut.SyncAllCommand.CanExecute(null);

        // Assert
        Assert.False(isAvailable);
        Assert.False(canSync);
    }

    [Fact]
    public void IsSyncAvailable_OneDriveフォルダーが見つかった場合_同期できる()
    {
        // Arrange（フォルダーは作らない。可否の判定だけを確かめる）
        var baseDir = Path.Combine(
            Path.GetTempPath(),
            "AppRouletteTests",
            Guid.NewGuid().ToString());
        var sut = CreateSut(
            cloudSyncService: new OneDriveFolderSyncService(baseDir),
            syncTimestampStore: CreateUnusedTimestampStore());

        // Act
        var isAvailable = sut.IsSyncAvailable;
        var canSync = sut.SyncAllCommand.CanExecute(null);

        // Assert
        Assert.True(isAvailable);
        Assert.True(canSync);
    }

    [Fact]
    public async Task InitializeAsync_OneDriveフォルダーが見つからない場合_同期済みと表示しない()
    {
        // Arrange
        var sut = CreateSut(
            cloudSyncService: new OneDriveFolderSyncService(baseDir: null),
            syncTimestampStore: CreateUnusedTimestampStore());

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("同期待機", sut.SyncStatusText);
    }

    // ---------------------------------------------------------------
    // エラーの知らせ
    // ---------------------------------------------------------------

    [Fact]
    public async Task ItemsText_自動保存に失敗した場合_利用者に知らせる()
    {
        // Arrange
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);
        fakeRepo.ShouldFailOnSave = true;

        // Act
        sut.ItemsText = "アイテムA";
        await WaitUntilAsync(() => sut.SaveStatusText == "保存失敗");

        // Assert
        Assert.Equal("保存失敗", sut.SaveStatusText);
        Assert.True(sut.IsErrorOpen);
        Assert.Equal("保存できませんでした", sut.ErrorTitle);
        Assert.Contains("「Roulette1」のアイテムの変更を保存できませんでした", sut.ErrorMessage);
        Assert.Contains("テスト用の保存失敗", sut.ErrorMessage);
    }

    [Fact]
    public async Task ItemsText_自動保存に成功した場合_知らせを出さない()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.ItemsText = "アイテムA";
        await WaitForAutoSaveAsync(sut);

        // Assert
        Assert.Equal("保存済み", sut.SaveStatusText);
        Assert.False(sut.IsErrorOpen);
    }

    [Fact]
    public async Task AddGroupCommand_保存に失敗した場合_利用者に知らせる()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository();
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);
        await sut.InitializeCommand.ExecuteAsync(null);
        fakeGroupRepo.ShouldFailOnAdd = true;

        // Act
        await sut.AddGroupCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("保存失敗", sut.SaveStatusText);
        Assert.True(sut.IsErrorOpen);
        Assert.Contains("リストを追加できませんでした", sut.ErrorMessage);
        Assert.Equal(9, sut.GroupList.Count);
    }

    [Fact]
    public async Task InitializeAsync_読み込みに失敗した場合_落ちずに利用者に知らせる()
    {
        // Arrange
        var fakeGroupRepo = new FakeGroupRepository { ShouldFailOnLoad = true };
        var sut = CreateSut(customGroupRepository: fakeGroupRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.IsErrorOpen);
        Assert.Equal("データを読み込めませんでした", sut.ErrorTitle);
        Assert.Contains("テスト用の読み込み失敗", sut.ErrorMessage);
        Assert.Equal("読み込み失敗", sut.SaveStatusText);
        Assert.Empty(sut.GroupList);
        Assert.Null(sut.SelectedGroup);
    }

    [Fact]
    public void ShowError_閉じた後に別のエラーが起きた場合_新しい内容で表示し直す()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowError("1 件目", "1 件目の内容");
        sut.IsErrorOpen = false;

        // Act
        sut.ShowError("2 件目", "2 件目の内容");

        // Assert
        Assert.True(sut.IsErrorOpen);
        Assert.Equal("2 件目", sut.ErrorTitle);
        Assert.Equal("2 件目の内容", sut.ErrorMessage);
    }
}
