using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IItemRepository"/> フェイク実装。
/// メモリ内にアイテムを保存し、CRUD 操作をシミュレートします。
/// </summary>
internal class FakeItemRepository : IItemRepository
{
    private readonly List<Item> _items = new();
    private int _nextId = 1;

    /// <summary>
    /// すべてのアイテムを非同期で取得します。
    /// </summary>
    public Task<List<Item>> GetItemsAsync() =>
        Task.FromResult(new List<Item>(_items));

    /// <summary>
    /// 指定されたグループに属するアイテムを非同期で取得します。
    /// </summary>
    public Task<List<Item>> GetItemsByGroupAsync(int groupId) =>
        Task.FromResult(_items.Where(i => i.GroupId == groupId).ToList());

    /// <summary>
    /// 指定された識別子を持つアイテムを非同期で取得します。
    /// </summary>
    public Task<Item?> GetItemByIdAsync(int id) =>
        Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

    /// <summary>
    /// 新しいアイテムを非同期で追加します。
    /// </summary>
    public Task<int> AddItemAsync(Item item)
    {
        item.Id = _nextId++;
        _items.Add(item);
        return Task.FromResult(item.Id);
    }

    /// <summary>
    /// 既存のアイテムを非同期で更新します。
    /// </summary>
    public Task<int> UpdateItemAsync(Item item)
    {
        var existing = _items.FirstOrDefault(i => i.Id == item.Id);
        if (existing is not null)
        {
            existing.Label = item.Label;
            existing.Weight = item.Weight;
            existing.GroupId = item.GroupId;
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    /// <summary>
    /// テスト用に初期データを設定します。
    /// </summary>
    public void InitializeWithItems(IEnumerable<Item> items)
    {
        _items.Clear();
        _nextId = 1;
        foreach (var item in items)
        {
            item.Id = _nextId++;
            _items.Add(item);
        }
    }

    /// <summary>
    /// 指定された識別子を持つアイテムを非同期で削除します。
    /// </summary>
    public Task<int> DeleteItemAsync(int id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is not null)
        {
            _items.Remove(item);
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    /// <summary>
    /// 指定されたグループのすべてのアイテムを非同期で削除します。
    /// </summary>
    public Task<int> DeleteItemsByGroupAsync(int groupId)
    {
        var itemsToDelete = _items.Where(i => i.GroupId == groupId).ToList();
        int count = itemsToDelete.Count;

        foreach (var item in itemsToDelete)
        {
            _items.Remove(item);
        }

        return Task.FromResult(count);
    }
}
