using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IDataPersistenceService"/> フェイク実装。
/// 呼び出し回数と引数を記録します。
/// </summary>
internal class FakeDataPersistenceService : IDataPersistenceService
{
    /// <summary>
    /// <see cref="LoadGroupsAsync"/> が返すグループ一覧を取得または設定します。
    /// </summary>
    public IReadOnlyList<RouletteGroup> GroupsToReturn { get; set; } =
        new List<RouletteGroup>();

    /// <summary>
    /// <see cref="SaveGroupsAsync"/> が呼び出された回数を取得します。
    /// </summary>
    public int SaveCallCount { get; private set; }

    /// <summary>
    /// 最後に <see cref="SaveGroupsAsync"/> に渡されたグループ一覧を取得します。
    /// </summary>
    public IReadOnlyList<RouletteGroup>? LastSavedGroups { get; private set; }

    /// <summary>
    /// <see cref="GroupsToReturn"/> を返します。
    /// </summary>
    /// <returns><see cref="GroupsToReturn"/> の値。</returns>
    public Task<IReadOnlyList<RouletteGroup>> LoadGroupsAsync() =>
        Task.FromResult(GroupsToReturn);

    /// <summary>
    /// 呼び出し回数と引数を記録します。
    /// </summary>
    /// <param name="groups">保存対象のグループ一覧。</param>
    public Task SaveGroupsAsync(IReadOnlyList<RouletteGroup> groups)
    {
        SaveCallCount++;
        LastSavedGroups = groups;
        return Task.CompletedTask;
    }
}
