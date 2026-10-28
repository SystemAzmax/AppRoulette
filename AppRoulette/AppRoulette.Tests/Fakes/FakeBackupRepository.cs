using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IBackupRepository"/> フェイク実装。
/// 置き換えの内容を、グループとアイテムのフェイクリポジトリに反映します。
/// </summary>
internal class FakeBackupRepository : IBackupRepository
{
    private readonly FakeGroupRepository _groupRepository;
    private readonly FakeItemRepository _itemRepository;
    private readonly bool _shouldFail;

    /// <summary>
    /// 置き換えの反映先を指定して <see cref="FakeBackupRepository"/> を初期化します。
    /// </summary>
    /// <param name="groupRepository">グループの反映先。</param>
    /// <param name="itemRepository">アイテムの反映先。</param>
    /// <param name="shouldFail">置き換えで例外を投げる場合は <c>true</c>。</param>
    public FakeBackupRepository(
        FakeGroupRepository groupRepository,
        FakeItemRepository itemRepository,
        bool shouldFail = false)
    {
        _groupRepository = groupRepository;
        _itemRepository = itemRepository;
        _shouldFail = shouldFail;
    }

    /// <summary>置き換えが呼び出された回数を取得します。</summary>
    public int ReplaceAllCallCount { get; private set; }

    /// <summary>
    /// 全グループとアイテムを置き換えます。
    /// 失敗する設定の場合は、何も変更せずに例外を投げます。
    /// </summary>
    public Task ReplaceAllAsync(IReadOnlyList<RouletteGroup> groups)
    {
        ReplaceAllCallCount++;
        if (_shouldFail)
        {
            throw new IOException("テスト用の保存失敗");
        }

        _groupRepository.ReplaceAll(groups);
        _itemRepository.InitializeWithItems(groups.SelectMany(group => group.Items
            .Select(item => new Item(item.Name, item.Weight, group.Id)
            {
                IsEnabled = item.IsEnabled,
            })));

        return Task.CompletedTask;
    }
}
