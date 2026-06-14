namespace AppRoulette.Models;

/// <summary>
/// ルーレットのグループを表すモデルクラス。
/// アイテムを最大 <see cref="MAX_ITEM_COUNT"/> 件まで保持します。
/// </summary>
public class RouletteGroup
{
    /// <summary>グループあたりのアイテム最大件数。</summary>
    public const int MAX_ITEM_COUNT = 30;

    /// <summary>グループ数（固定）。</summary>
    public const int GROUP_COUNT = 3;

    /// <summary>
    /// グループの識別子（1 始まり）を取得または設定します。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// グループの表示名を取得または設定します。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// グループに属するルーレットアイテムの一覧を取得または設定します。
    /// </summary>
    public List<RouletteItem> Items { get; set; } = new();

    /// <summary>
    /// 指定した識別子と表示名で <see cref="RouletteGroup"/> を初期化します。
    /// </summary>
    /// <param name="id">グループの識別子（1 始まり）。</param>
    /// <param name="displayName">グループの表示名。</param>
    public RouletteGroup(int id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    /// <summary>
    /// JSON デシリアライズ用のパラメーターなしコンストラクターです。
    /// </summary>
    public RouletteGroup() { }

    /// <summary>
    /// アイテムを追加します。
    /// アイテム数が <see cref="MAX_ITEM_COUNT"/> に達している場合は追加しません。
    /// </summary>
    /// <param name="item">追加するアイテム。</param>
    /// <returns>追加に成功した場合は <c>true</c>、上限に達していた場合は <c>false</c>。</returns>
    public bool TryAddItem(RouletteItem item)
    {
        if (Items.Count >= MAX_ITEM_COUNT)
        {
            return false;
        }

        Items.Add(item);
        return true;
    }
}
