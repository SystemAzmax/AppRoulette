using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IAppSettingsRepository"/> フェイク実装です。
/// </summary>
internal class FakeAppSettingsRepository : IAppSettingsRepository
{
    /// <summary>
    /// 最後に保存されたグループIDを取得または設定します。
    /// </summary>
    public int LastSavedGroupId { get; set; }

    /// <summary>
    /// GetLastSelectedGroupIdAsync() が呼び出された回数を取得します。
    /// </summary>
    public int GetLastSelectedGroupIdCallCount { get; private set; }

    /// <summary>
    /// SaveLastSelectedGroupIdAsync() が呼び出された回数を取得します。
    /// </summary>
    public int SaveLastSelectedGroupIdCallCount { get; private set; }

    /// <summary>
    /// 保存されたグループIDを返します。呼び出し回数をカウントします。
    /// </summary>
    /// <returns><see cref="LastSavedGroupId"/> の値。</returns>
    public Task<int> GetLastSelectedGroupIdAsync()
    {
        GetLastSelectedGroupIdCallCount++;
        return Task.FromResult(LastSavedGroupId);
    }

    /// <summary>
    /// グループIDを記録します。呼び出し回数をカウントします。
    /// </summary>
    /// <param name="groupId">保存するグループID。</param>
    public Task SaveLastSelectedGroupIdAsync(int groupId)
    {
        SaveLastSelectedGroupIdCallCount++;
        LastSavedGroupId = groupId;
        return Task.CompletedTask;
    }
}
