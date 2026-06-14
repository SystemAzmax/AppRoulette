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
    /// デフォルトの3グループを持つ FakeDataPersistenceService を生成します。
    /// </summary>
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
    /// SUT を生成します。
    /// </summary>
    private static MainViewModel CreateSut(
        FakeDataPersistenceService? fakeService = null,
        FakeRandomService? fakeRandom = null) =>
        new(
            fakeService ?? CreateDefaultFakeService(),
            fakeRandom ?? new FakeRandomService(0));

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
        // Arrange
        var fakeService = new FakeDataPersistenceService
        {
            GroupsToReturn = new List<RouletteGroup>(),
        };
        var sut = CreateSut(fakeService);

        // Act
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Null(sut.SelectedGroup);
    }

    // ---------------------------------------------------------------
    // SelectedGroup 変更
    // ---------------------------------------------------------------

    [Fact]
    public async Task SelectedGroup_グループを切り替えた場合_ItemsTextが更新される()
    {
        // Arrange
        var fakeService = new FakeDataPersistenceService
        {
            GroupsToReturn = new List<RouletteGroup>
            {
                new(1, "グループ1")
                {
                    Items = new List<RouletteItem> { new("アイテムA") },
                },
                new(2, "グループ2")
                {
                    Items = new List<RouletteItem>
                    {
                        new("アイテムX"),
                        new("アイテムY"),
                    },
                },
                new(3, "グループ3"),
            },
        };
        var sut = CreateSut(fakeService);
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
        var fakeService = CreateDefaultFakeService(itemCountInGroup1: 3);
        var sut = CreateSut(fakeService);
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
        var sut = CreateSut(CreateDefaultFakeService(itemCountInGroup1: 2));
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
        sut.ItemsText = "アイテム1\nアイテム2\nアイテム3";

        // Assert
        Assert.Equal(3, sut.ItemCount);
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
        var fakeService = CreateDefaultFakeService();
        var sut = CreateSut(fakeService);
        await sut.InitializeCommand.ExecuteAsync(null);
        var saveCountBefore = fakeService.SaveCallCount;

        // Act（行数を増やす：1行→2行）
        sut.ItemsText = "アイテム1";
        sut.ItemsText = "アイテム1\nアイテム2";

        // 非同期保存の完了を待つ
        await Task.Delay(100);

        // Assert
        Assert.True(fakeService.SaveCallCount > saveCountBefore);
    }

    [Fact]
    public async Task ItemsText_改行が増えない場合_保存が呼ばれない()
    {
        // Arrange
        var fakeService = CreateDefaultFakeService();
        var sut = CreateSut(fakeService);
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act（同じ行数のままテキストを変更）
        sut.ItemsText = "アイテム1";
        var saveCountAfterFirst = fakeService.SaveCallCount;
        sut.ItemsText = "アイテムA"; // 行数変化なし

        await Task.Delay(100);

        // Assert（保存回数が増えていない）
        Assert.Equal(saveCountAfterFirst, fakeService.SaveCallCount);
    }

    // ---------------------------------------------------------------
    // SpinCommand.CanExecute
    // ---------------------------------------------------------------

    [Fact]
    public async Task SpinCommand_アイテムが0件の場合_CanExecuteがfalseになる()
    {
        // Arrange
        var sut = CreateSut(CreateDefaultFakeService(itemCountInGroup1: 0));
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.SpinCommand.CanExecute(null));
    }

    [Fact]
    public async Task SpinCommand_アイテムが1件以上でスピン中でない場合_CanExecuteがtrueになる()
    {
        // Arrange
        var sut = CreateSut(CreateDefaultFakeService(itemCountInGroup1: 3));
        await sut.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.SpinCommand.CanExecute(null));
    }

    [Fact]
    public async Task SpinCommand_スピン中の場合_CanExecuteがfalseになる()
    {
        // Arrange
        var sut = CreateSut(CreateDefaultFakeService(itemCountInGroup1: 3));
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.IsSpinning = true;

        // Assert
        Assert.False(sut.SpinCommand.CanExecute(null));
    }

    // ---------------------------------------------------------------
    // Spin（ランダム選択）
    // ---------------------------------------------------------------

    [Fact]
    public async Task Spin_実行した場合_SelectedItemIndexが設定される()
    {
        // Arrange
        var sut = CreateSut(
            CreateDefaultFakeService(itemCountInGroup1: 5),
            new FakeRandomService(2));
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
            CreateDefaultFakeService(itemCountInGroup1: itemCount),
            new FakeRandomService(fixedIndex));
        await sut.InitializeCommand.ExecuteAsync(null);

        // Act
        sut.SpinCommand.Execute(null);

        // Assert
        Assert.InRange(sut.SelectedItemIndex, 0, itemCount - 1);
    }

    // ---------------------------------------------------------------
    // ParseItems / FormatItems / CountLines（ユーティリティ）
    // ---------------------------------------------------------------

    [Fact]
    public void ParseItems_改行区切りのテキスト_アイテムリストに変換できる()
    {
        // Arrange
        const string text = "アイテム1\nアイテム2\nアイテム3";

        // Act
        var result = MainViewModel.ParseItems(text);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("アイテム1", result[0].Name);
        Assert.Equal("アイテム3", result[2].Name);
    }

    [Fact]
    public void ParseItems_空行が含まれる場合_空行を除外する()
    {
        // Arrange
        const string text = "アイテム1\n\n  \nアイテム2";

        // Act
        var result = MainViewModel.ParseItems(text);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FormatItems_アイテムリスト_改行区切りのテキストに変換できる()
    {
        // Arrange
        var items = new List<RouletteItem>
        {
            new("アイテム1"),
            new("アイテム2"),
        };

        // Act
        var result = MainViewModel.FormatItems(items);

        // Assert
        Assert.Equal("アイテム1\nアイテム2", result);
    }

    [Fact]
    public void CountLines_空文字の場合_0を返す()
    {
        // Arrange & Act
        var result = MainViewModel.CountLines(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountLines_改行なしの場合_1を返す()
    {
        // Arrange & Act
        var result = MainViewModel.CountLines("アイテム1");

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void CountLines_改行ありの場合_行数を返す()
    {
        // Arrange & Act
        var result = MainViewModel.CountLines("アイテム1\nアイテム2\nアイテム3");

        // Assert
        Assert.Equal(3, result);
    }
}
