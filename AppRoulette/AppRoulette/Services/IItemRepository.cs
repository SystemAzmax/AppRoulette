using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// ルーレットアイテムへのデータアクセスを定義するインターフェース。
/// CRUD 操作の抽象化により、将来的な実装変更や MAUI への移植を容易にします。
/// </summary>
public interface IItemRepository
{
    /// <summary>
    /// すべてのアイテムを非同期で取得します。
    /// </summary>
    /// <returns>すべてのアイテムを含むリスト。</returns>
    Task<List<Item>> GetItemsAsync();

    /// <summary>
    /// 指定されたグループに属するアイテムをすべて非同期で取得します。
    /// </summary>
    /// <param name="groupId">グループの識別子。</param>
    /// <returns>指定されたグループに属するアイテムのリスト。</returns>
    Task<List<Item>> GetItemsByGroupAsync(int groupId);

    /// <summary>
    /// 指定された識別子を持つアイテムを非同期で取得します。
    /// </summary>
    /// <param name="id">アイテムの識別子。</param>
    /// <returns>見つかったアイテム、または見つからない場合は null。</returns>
    Task<Item?> GetItemByIdAsync(int id);

    /// <summary>
    /// 新しいアイテムを非同期で追加します。
    /// </summary>
    /// <param name="item">追加するアイテム。</param>
    /// <returns>追加されたアイテムの識別子。</returns>
    Task<int> AddItemAsync(Item item);

    /// <summary>
    /// 既存のアイテムを非同期で更新します。
    /// </summary>
    /// <param name="item">更新するアイテム。</param>
    /// <returns>更新されたアイテム数（通常は 1）。</returns>
    Task<int> UpdateItemAsync(Item item);

    /// <summary>
    /// 指定された識別子を持つアイテムを非同期で削除します。
    /// </summary>
    /// <param name="id">削除するアイテムの識別子。</param>
    /// <returns>削除されたアイテム数。</returns>
    Task<int> DeleteItemAsync(int id);

    /// <summary>
    /// 指定されたグループに属するすべてのアイテムを非同期で削除します。
    /// </summary>
    /// <param name="groupId">削除対象グループの識別子。</param>
    /// <returns>削除されたアイテム数。</returns>
    Task<int> DeleteItemsByGroupAsync(int groupId);
}
