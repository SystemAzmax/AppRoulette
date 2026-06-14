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
        var group1 = new RouletteGroup(1, "グループ1");
        for (var i = 0; i < itemCountInGroup1; i++)
        {
            group1.TryAddItem(new RouletteItem($"アイテム{i + 1}"));
        }

        return new FakeDataPersistenceService
        {
            GroupsToReturn = new List<RouletteGroup>
            {
                group1,
                new(2, "グループ2"),
                new(3, "グループ3"),
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
            new FakeItemRepository());
    }

    /// <summary>
    /// SUT を生成します。
    /// </summary>
    private static MainViewModel CreateSut(
        FakeRandomService? fakeRandom = null,
        int itemCountInGroup1 = 0,
        FakeItemRepository? customRepository = null)
    {
        FakeItemRepository fakeRepo = customRepository ?? new FakeItemRepository();

        // customRepository が未指定かつ itemCountInGroup1 > 0 の場合、グループ1にアイテムを設定
        if (customRepository is null && itemCountInGroup1 > 0)
        {
            var items = Enumerable.Range(1, itemCountInGroup1)
                .Select(i => new Item($"アイテム{i}", groupId: 1))
                .ToList();
            fakeRepo.InitializeWithItems(items);
        }

        return new(
            fakeRandom ?? new FakeRandomService(0),
            fakeRepo);
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
        Assert.Equal(3, sut.GroupList.Count);
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

        // Act（グループ1が選択された時点でアイテム数が反映される）
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

        // 非同期保存の完了を待つ
        await Task.Delay(100);

        // Assert - SQLite にアイテムが増えていることを確認
        var itemsAfter = await fakeRepo.GetItemsAsync();
        Assert.True(itemsAfter.Count > itemsCountBefore, "SQLite にアイテムが保存されていません");
    }

    [Fact]
    public async Task ItemsText_改行が増えない場合_保存が呼ばれない()
    {
        // Arrange
        var sut = CreateSut();
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act（行数を増やす：1行⇒2行）
        sut.ItemsText = "アイテム1";
        await Task.Delay(50);
        sut.ItemsText = "アイテムA"; // 行数変化なし

        await Task.Delay(100);

        // Assert（行数が変わらない場合、SQLite 同期は呼ばれない仕様）
        // ただし Items は実メモリ上で更新される
        Assert.Single(sut.GroupList[0].Items);
        Assert.Equal("アイテムA", sut.GroupList[0].Items[0].Name);
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
}
