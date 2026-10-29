using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="OneDriveFolderSyncService"/> のユニットテストクラスです。
/// </summary>
public class OneDriveFolderSyncServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly OneDriveFolderSyncService _sut;

    /// <summary>
    /// テスト用の一時ディレクトリと SUT を初期化します。
    /// </summary>
    public OneDriveFolderSyncServiceTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            "AppRouletteTests",
            Guid.NewGuid().ToString());
        _sut = new OneDriveFolderSyncService(_testDir);
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

    /// <summary>
    /// テスト用の同期データを作成します。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    /// <returns>作成した同期データ。</returns>
    private static SyncGroupData CreateData(int groupId)
    {
        var group = new RouletteGroup(groupId, $"Group{groupId}");
        group.TryAddItem(new RouletteItem("Item1"));
        return SyncGroupData.FromGroup(group, DateTime.UtcNow);
    }

    [Fact]
    public void IsAvailable_ベースフォルダー指定時_trueを返す()
    {
        Assert.True(_sut.IsAvailable);
    }

    [Fact]
    public void IsAvailable_ベースフォルダーがnullの場合_falseを返す()
    {
        var sut = new OneDriveFolderSyncService(baseDir: null);

        Assert.False(sut.IsAvailable);
    }

    [Fact]
    public async Task UploadGroupAsync_保存に成功する()
    {
        var data = CreateData(1);

        var result = await _sut.UploadGroupAsync(data);

        Assert.True(result);
        Assert.True(File.Exists(
            Path.Combine(_testDir, "groups", "1.json")));
    }

    [Fact]
    public async Task UploadGroupAsync_ベースフォルダーがnullの場合_falseを返す()
    {
        var sut = new OneDriveFolderSyncService(baseDir: null);

        var result = await sut.UploadGroupAsync(CreateData(1));

        Assert.False(result);
    }

    [Fact]
    public async Task DownloadGroupAsync_保存したデータを復元できる()
    {
        var data = CreateData(2);
        await _sut.UploadGroupAsync(data);

        var loaded = await _sut.DownloadGroupAsync(2);

        Assert.NotNull(loaded);
        Assert.Equal(data.GroupId, loaded.GroupId);
        Assert.Equal(data.DisplayName, loaded.DisplayName);
        Assert.Equal(data.Items.Count, loaded.Items.Count);
    }

    [Fact]
    public async Task DownloadGroupAsync_ファイルがない場合_nullを返す()
    {
        var loaded = await _sut.DownloadGroupAsync(99);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task DownloadGroupAsync_ファイルが壊れている場合_nullではなく例外を投げる()
    {
        var groupsDir = Path.Combine(_testDir, "groups");
        Directory.CreateDirectory(groupsDir);
        await File.WriteAllTextAsync(Path.Combine(groupsDir, "1.json"), "{ 壊れたデータ");

        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(
            () => _sut.DownloadGroupAsync(1));
    }

    [Fact]
    public async Task DownloadAllGroupsAsync_全グループを取得できる()
    {
        await _sut.UploadGroupAsync(CreateData(1));
        await _sut.UploadGroupAsync(CreateData(2));

        var all = await _sut.DownloadAllGroupsAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task DownloadAllGroupsAsync_フォルダーがない場合_空リストを返す()
    {
        var all = await _sut.DownloadAllGroupsAsync();

        Assert.Empty(all);
    }

    [Fact]
    public async Task UploadAllGroupsAsync_全件保存に成功する()
    {
        var groups = new List<SyncGroupData>
        {
            CreateData(1),
            CreateData(2),
        };

        var result = await _sut.UploadAllGroupsAsync(groups);

        Assert.True(result);
        Assert.Equal(2, (await _sut.DownloadAllGroupsAsync()).Count);
    }
}
