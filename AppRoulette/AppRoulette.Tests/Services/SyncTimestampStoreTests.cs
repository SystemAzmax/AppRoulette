using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="SyncTimestampStore"/> のユニットテストクラスです。
/// </summary>
public class SyncTimestampStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testFilePath;
    private readonly SyncTimestampStore _sut;

    /// <summary>
    /// テスト用の一時ディレクトリと SUT を初期化します。
    /// </summary>
    public SyncTimestampStoreTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            "AppRouletteTests",
            Guid.NewGuid().ToString());
        _testFilePath = Path.Combine(_testDir, "syncTimestamps.json");
        _sut = new SyncTimestampStore(_testFilePath);
    }

    /// <summary>
    /// テスト後に一時ディレクトリを削除します。
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_記録がない場合_nullを返す()
    {
        // Act
        var result = await _sut.GetAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_保存後にGetAsyncで取得できる()
    {
        // Arrange
        var timestamp = new DateTime(
            2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        // Act
        await _sut.SetAsync(2, timestamp);
        var result = await _sut.GetAsync(2);

        // Assert
        Assert.Equal(timestamp, result);
    }

    [Fact]
    public async Task SetAsync_別インスタンスからも読み込める()
    {
        // Arrange
        var timestamp = new DateTime(
            2026, 3, 2, 15, 30, 0, DateTimeKind.Utc);
        await _sut.SetAsync(3, timestamp);

        // Act
        var other = new SyncTimestampStore(_testFilePath);
        var result = await other.GetAsync(3);

        // Assert
        Assert.Equal(timestamp, result);
    }

    [Fact]
    public async Task SetAsync_複数グループを独立して保存できる()
    {
        // Arrange
        var timestamp1 = new DateTime(
            2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp2 = new DateTime(
            2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        // Act
        await _sut.SetAsync(1, timestamp1);
        await _sut.SetAsync(2, timestamp2);

        // Assert
        Assert.Equal(timestamp1, await _sut.GetAsync(1));
        Assert.Equal(timestamp2, await _sut.GetAsync(2));
    }
}
