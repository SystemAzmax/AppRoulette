using System.Text.Json;
using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="JsonDataPersistenceService"/> のユニットテストクラスです。
/// </summary>
public class JsonDataPersistenceServiceTests : IDisposable
{
    private readonly string _testDataDir;
    private readonly string _testDataFilePath;
    private readonly JsonDataPersistenceService _sut;

    /// <summary>
    /// テスト用の一時ディレクトリと SUT を初期化します。
    /// </summary>
    public JsonDataPersistenceServiceTests()
    {
        _testDataDir = Path.Combine(
            Path.GetTempPath(),
            "AppRouletteTests",
            Guid.NewGuid().ToString());
        _testDataFilePath = Path.Combine(_testDataDir, "data.json");

        _sut = new JsonDataPersistenceService(_testDataDir);
    }

    /// <summary>
    /// テスト後に一時ディレクトリを削除します。
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testDataDir))
        {
            Directory.Delete(_testDataDir, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // LoadGroupsAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task LoadGroupsAsync_ファイルが存在しない場合_デフォルト3グループを返す()
    {
        // Arrange
        // ファイルなし（初期状態）

        // Act
        var result = await _sut.LoadGroupsAsync();

        // Assert
        Assert.Equal(RouletteGroup.GROUP_COUNT, result.Count);
        Assert.Equal("グループ1", result[0].DisplayName);
        Assert.Equal("グループ2", result[1].DisplayName);
        Assert.Equal("グループ3", result[2].DisplayName);
    }

    [Fact]
    public async Task LoadGroupsAsync_ファイルが存在しない場合_各グループのアイテムが空である()
    {
        // Arrange
        // ファイルなし（初期状態）

        // Act
        var result = await _sut.LoadGroupsAsync();

        // Assert
        Assert.All(result, g => Assert.Empty(g.Items));
    }

    [Fact]
    public async Task LoadGroupsAsync_保存済みデータがある場合_そのデータを返す()
    {
        // Arrange
        var groups = new List<RouletteGroup>
        {
            new(1, "グループ1")
            {
                Items = new List<RouletteItem>
                {
                    new("アイテムA"),
                    new("アイテムB"),
                }
            },
            new(2, "グループ2"),
            new(3, "グループ3"),
        };
        await _sut.SaveGroupsAsync(groups);

        // Act
        var result = await _sut.LoadGroupsAsync();

        // Assert
        Assert.Equal(2, result[0].Items.Count);
        Assert.Equal("アイテムA", result[0].Items[0].Name);
        Assert.Equal("アイテムB", result[0].Items[1].Name);
    }

    [Fact]
    public async Task LoadGroupsAsync_JSONが破損している場合_デフォルト3グループを返す()
    {
        // Arrange
        Directory.CreateDirectory(_testDataDir);
        await File.WriteAllTextAsync(_testDataFilePath, "{ invalid json }");

        // Act
        var result = await _sut.LoadGroupsAsync();

        // Assert
        Assert.Equal(RouletteGroup.GROUP_COUNT, result.Count);
    }

    // ---------------------------------------------------------------
    // SaveGroupsAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task SaveGroupsAsync_グループを保存した場合_JSONファイルが生成される()
    {
        // Arrange
        var groups = new List<RouletteGroup>
        {
            new(1, "グループ1"),
            new(2, "グループ2"),
            new(3, "グループ3"),
        };

        // Act
        await _sut.SaveGroupsAsync(groups);

        // Assert
        Assert.True(File.Exists(_testDataFilePath));
    }

    [Fact]
    public async Task SaveGroupsAsync_アイテムを保存した場合_再読み込みで同じアイテムが取得できる()
    {
        // Arrange
        var groups = new List<RouletteGroup>
        {
            new(1, "グループ1")
            {
                Items = new List<RouletteItem> { new("テストアイテム") }
            },
            new(2, "グループ2"),
            new(3, "グループ3"),
        };

        // Act
        await _sut.SaveGroupsAsync(groups);
        var result = await _sut.LoadGroupsAsync();

        // Assert
        Assert.Single(result[0].Items);
        Assert.Equal("テストアイテム", result[0].Items[0].Name);
    }

    [Fact]
    public async Task SaveGroupsAsync_ディレクトリが存在しない場合_ディレクトリを作成して保存する()
    {
        // Arrange
        // _testDataDir はまだ存在しない
        var groups = new List<RouletteGroup> { new(1, "グループ1") };

        // Act
        await _sut.SaveGroupsAsync(groups);

        // Assert
        Assert.True(Directory.Exists(_testDataDir));
        Assert.True(File.Exists(_testDataFilePath));
    }

    // ---------------------------------------------------------------
    // RouletteGroup.TryAddItem
    // ---------------------------------------------------------------

    [Fact]
    public void TryAddItem_上限未満の場合_アイテムを追加してtrueを返す()
    {
        // Arrange
        var group = new RouletteGroup(1, "グループ1");

        // Act
        var result = group.TryAddItem(new RouletteItem("アイテム1"));

        // Assert
        Assert.True(result);
        Assert.Single(group.Items);
    }

    [Fact]
    public void TryAddItem_上限に達した場合_アイテムを追加せずfalseを返す()
    {
        // Arrange
        var group = new RouletteGroup(1, "グループ1");
        for (var i = 0; i < RouletteGroup.MAX_ITEM_COUNT; i++)
        {
            group.TryAddItem(new RouletteItem($"アイテム{i}"));
        }

        // Act
        var result = group.TryAddItem(new RouletteItem("超過アイテム"));

        // Assert
        Assert.False(result);
        Assert.Equal(RouletteGroup.MAX_ITEM_COUNT, group.Items.Count);
    }
}
