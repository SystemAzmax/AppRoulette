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
    /// ダウンロードで例外を投げる（同期ファイルを読めない）グループの ID を取得します。
    /// </summary>
    public HashSet<int> UnreadableGroupIds { get; } = new();

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

    /// <summary>
    /// クラウドにあるグループのデータを取得します。
    /// </summary>
    /// <param name="groupId">グループID。</param>
    /// <returns>グループのデータ。無い場合は null。</returns>
    public SyncGroupData? GetRemoteGroup(int groupId)
    {
        _groups.TryGetValue(groupId, out var group);
        return group;
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
        if (UnreadableGroupIds.Contains(groupId))
        {
            throw new IOException("テスト用の読み込み失敗");
        }

        return Task.FromResult(GetRemoteGroup(groupId));
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
