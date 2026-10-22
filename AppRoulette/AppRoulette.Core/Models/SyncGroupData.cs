namespace AppRoulette.Models;

/// <summary>
/// OneDrive 同期用の1アイテム分のデータ転送オブジェクト。
/// </summary>
public class SyncItemData
{
    /// <summary>
    /// アイテムの表示名を取得または設定します。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// アイテムの重み付け値（1〜5）を取得または設定します。
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// アイテムが抽選対象かどうかを取得または設定します。
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// OneDrive 同期用の1グループ分のデータ転送オブジェクト。
/// タイムスタンプ比較による競合解決に使用します。
/// </summary>
public class SyncGroupData
{
    /// <summary>
    /// グループの識別子（1 始まり）を取得または設定します。
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// グループの表示名を取得または設定します。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// グループに属するアイテムの一覧を取得または設定します。
    /// </summary>
    public List<SyncItemData> Items { get; set; } = new();

    /// <summary>
    /// 「当たったら除外」モードの有効状態を取得または設定します。
    /// </summary>
    public bool ExcludeOnWin { get; set; }

    /// <summary>
    /// 最終更新日時（UTC）を取得または設定します。
    /// 競合解決時に新しい方を採用する判定に使用します。
    /// </summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// <see cref="RouletteGroup"/> から同期用DTOを生成します。
    /// </summary>
    /// <param name="group">変換元のグループ。</param>
    /// <param name="lastModifiedUtc">最終更新日時（UTC）。</param>
    /// <returns>同期用DTO。</returns>
    public static SyncGroupData FromGroup(
        RouletteGroup group, DateTime lastModifiedUtc)
    {
        return new SyncGroupData
        {
            GroupId = group.Id,
            DisplayName = group.DisplayName,
            ExcludeOnWin = group.ExcludeOnWin,
            Items = group.Items
                .Select(i => new SyncItemData
                {
                    Name = i.Name,
                    Weight = i.Weight,
                    IsEnabled = i.IsEnabled,
                })
                .ToList(),
            LastModifiedUtc = lastModifiedUtc,
        };
    }

    /// <summary>
    /// 同期用DTOから <see cref="RouletteGroup"/> を生成します。
    /// </summary>
    /// <returns>復元されたグループ。</returns>
    public RouletteGroup ToGroup()
    {
        var group = new RouletteGroup(GroupId, DisplayName)
        {
            ExcludeOnWin = ExcludeOnWin,
        };
        foreach (var item in Items)
        {
            group.TryAddItem(new RouletteItem(item.Name)
            {
                Weight = item.Weight,
                IsEnabled = item.IsEnabled,
            });
        }

        return group;
    }
}
