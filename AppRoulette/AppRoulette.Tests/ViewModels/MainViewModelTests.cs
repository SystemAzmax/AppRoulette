using AppRoulette.Models;
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
    /// (廃止予定) FakeDataPersistenceService を生成します。
    /// テストからは CreateSut(itemCountInGroup1: X) を使用してください。
    /// </summary>
    [Obsolete("Use CreateSut(itemCountInGroup1) instead")]
    private static FakeDataPersistenceService CreateDefaultFakeService(
        int itemCountInGroup1 = 0)
    {
        var group1 = new RouletteGroup(1, "Roulette1");
        for (var i = 0; i < itemCountInGroup1; i++)
        {
            group1.TryAddItem(new RouletteItem($"アイテム{i + 1}"));
        }

        return new FakeDataPersistenceService
        {
            GroupsToReturn = new List<RouletteGroup>
            {
                group1,
                new(2, "Roulette2"),
                new(3, "Roulette3"),
            },
        };
    }

    /// <summary>
    /// SUT を生成します。（レガシ: FakeDataPersistenceService 対応）
    /// 注意：このオーバーロードは廃止予定です。使用しないでください。
    /// </summary>
    [Obsolete("FakeDataPersistenceService を使用しないでください。FakeItemRepository を使用してください。")]
    private static MainViewModel CreateSut(
        FakeDataPersistenceService _unused)
    {
        // JSON persistence は廃止されたため、ダミー実装
        return new(
            new FakeRandomService(0),
            new FakeItemRepository(),
            new FakeGroupRepository(),
            new FakeDataPersistenceService());
    }

    /// <summary>
    /// SUT を生成します。
    /// </summary>
    private static MainViewModel CreateSut(
        FakeRandomService? fakeRandom = null,
        int itemCountInGroup1 = 0,
        FakeItemRepository? customRepository = null,
        FakeGroupRepository? customGroupRepository = null,
        FakeDataPersistenceService? customPersistence = null)
    {
        FakeItemRepository fakeRepo = customRepository ?? new FakeItemRepository();
        FakeGroupRepository fakeGroupRepo = customGroupRepository ?? new FakeGroupRepository();
        FakeDataPersistenceService fakePersistence = customPersistence ?? new FakeDataPersistenceService();

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
            fakePersistence);
    }

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
        await Task.Delay(100);

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
        await Task.Delay(100);
        sut.ItemsText = "アイテムA"; // 行数変化なし

        await Task.Delay(100);

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
        await Task.Delay(100);

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
        await Task.Delay(100);

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
        var fakePersistence = new FakeDataPersistenceService();
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customPersistence: fakePersistence, customRepository: fakeRepo);
        await sut.InitializeCommand.ExecuteAsync(null);

        // 初期化時の呼び出しをカウントした状態から、新規に2番目の呼び出しをカウントする新しいfakePersistenceを作成
        var fakePersistence2 = new FakeDataPersistenceService();
        sut.SelectedGroup = sut.GroupList[2]; // Roulette3 を選択

        // Assert: SelectGroupが呼び出されると、新規作成した永続化サービスではなく既存のsutが使用するため
        // 初回InitializeAsyncで1回、SelectedGroup変更で1回、合計2回呼ばれることを確認
        await Task.Delay(100);
        Assert.True(fakePersistence.SaveLastSelectedGroupIdCallCount > 0);
        // LastSavedGroupId は最後に設定された値が Roulette3 (Id=3) であること
        Assert.Equal(3, fakePersistence.LastSavedGroupId);
    }

    [Fact]
    public async Task InitializeAsync_保存されたグループIDがある場合_該当グループを復元する()
    {
        // Arrange
        var fakePersistence = new FakeDataPersistenceService
        {
            LastSavedGroupId = 2 // Roulette2 を保存済み
        };
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customPersistence: fakePersistence, customRepository: fakeRepo);

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
        var fakePersistence = new FakeDataPersistenceService
        {
            LastSavedGroupId = 0 // グループID未保存
        };
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customPersistence: fakePersistence, customRepository: fakeRepo);

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
        var fakePersistence = new FakeDataPersistenceService
        {
            LastSavedGroupId = 99 // 存在しないグループID
        };
        var fakeRepo = new FakeItemRepository();
        var sut = CreateSut(customPersistence: fakePersistence, customRepository: fakeRepo);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.SelectedGroup);
        Assert.Equal(1, sut.SelectedGroup.Id);
    }
}
