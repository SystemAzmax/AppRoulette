using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="ICloudSyncService"/> フェイク実装です。
/// クラウドのデータをメモリ内に保持し、ダウンロードの失敗をシミュレートします。
/// </summary>
internal class FakeCloudSyncService : ICloudSyncService
{
    private readonly Dictionary<int, SyncGroupData> _groups = new();

    /// <summary>
    /// ダウンロードで例外を投げるかどうかを取得または設定します。
    /// </summary>
    public bool ShouldFailOnDownload { get; set; }

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <summary>
    /// クラウドにあるデータとして、グループのデータを設定します。
    /// </summary>
    /// <param name="group">設定するグループのデータ。</param>
    public void SetRemoteGroup(SyncGroupData group)
    {
        _groups[group.GroupId] = group;
    }

    /// <inheritdoc/>
    public Task<bool> UploadGroupAsync(SyncGroupData group)
    {
        _groups[group.GroupId] = group;
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<SyncGroupData?> DownloadGroupAsync(int groupId)
    {
        if (ShouldFailOnDownload)
        {
            throw new IOException("テスト用の読み込み失敗");
        }

        _groups.TryGetValue(groupId, out var group);
        return Task.FromResult(group);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncGroupData>> DownloadAllGroupsAsync()
    {
        if (ShouldFailOnDownload)
        {
            throw new IOException("テスト用の読み込み失敗");
        }

        return Task.FromResult((IReadOnlyList<SyncGroupData>)_groups.Values.ToList());
    }

    /// <inheritdoc/>
    public Task<bool> UploadAllGroupsAsync(IReadOnlyList<SyncGroupData> groups)
    {
        foreach (var group in groups)
        {
            _groups[group.GroupId] = group;
        }

        return Task.FromResult(true);
    }
}
