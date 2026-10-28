namespace AppRoulette.Models;

/// <summary>
/// バックアップファイル全体のデータ転送オブジェクト。
/// </summary>
public class BackupData
{
    /// <summary>
    /// このアプリのバックアップであることを示す識別名を取得または設定します。
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// バックアップファイルの形式のバージョンを取得または設定します。
    /// </summary>
    public int FormatVersion { get; set; }

    /// <summary>
    /// バックアップの作成日時（UTC）を取得または設定します。
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// リストの一覧（表示順）を取得または設定します。
    /// </summary>
    public List<BackupGroupData>? Groups { get; set; }
}

/// <summary>
/// バックアップファイル内の1リスト分のデータ転送オブジェクト。
/// </summary>
public class BackupGroupData
{
    /// <summary>
    /// リストの表示名を取得または設定します。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 「当たったら除外」モードの有効状態を取得または設定します。
    /// </summary>
    public bool ExcludeOnWin { get; set; }

    /// <summary>
    /// リストに属するアイテムの一覧を取得または設定します。
    /// </summary>
    public List<BackupItemData>? Items { get; set; }

    /// <summary>
    /// <see cref="RouletteGroup"/> からバックアップ用DTOを生成します。
    /// </summary>
    /// <param name="group">変換元のグループ。</param>
    /// <returns>バックアップ用DTO。</returns>
    public static BackupGroupData FromGroup(RouletteGroup group)
    {
        return new BackupGroupData
        {
            DisplayName = group.DisplayName,
            ExcludeOnWin = group.ExcludeOnWin,
            Items = group.Items
                .Select(i => new BackupItemData
                {
                    Name = i.Name,
                    Weight = i.Weight,
                    IsEnabled = i.IsEnabled,
                })
                .ToList(),
        };
    }

    /// <summary>
    /// バックアップ用DTOから <see cref="RouletteGroup"/> を生成します。
    /// 名前が空のアイテムは除き、アイテムは最大件数までにします。
    /// </summary>
    /// <param name="id">復元後のグループの識別子。</param>
    /// <returns>復元されたグループ。</returns>
    public RouletteGroup ToGroup(int id)
    {
        var displayName = string.IsNullOrWhiteSpace(DisplayName)
            ? $"Roulette{id}"
            : DisplayName.Trim();
        var group = new RouletteGroup(id, displayName)
        {
            ExcludeOnWin = ExcludeOnWin,
        };

        foreach (var item in Items ?? new List<BackupItemData>())
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            group.TryAddItem(new RouletteItem(item.Name.Trim())
            {
                Weight = item.Weight,
                IsEnabled = item.IsEnabled,
            });
        }

        return group;
    }
}

/// <summary>
/// バックアップファイル内の1アイテム分のデータ転送オブジェクト。
/// </summary>
public class BackupItemData
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
