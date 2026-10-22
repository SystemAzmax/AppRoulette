namespace AppRoulette.Models;

/// <summary>
/// ルーレットアイテムを表すモデルクラス。
/// SQLite データベースの Items テーブルに対応しています。
/// </summary>
public class Item
{
    /// <summary>
    /// アイテムの一意な識別子。
    /// データベースの PRIMARY KEY AUTOINCREMENT に対応。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// アイテムの表示ラベル。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// アイテムの重み付け値。
    /// ルーレット選択時の確率に影響します。
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// アイテムが抽選対象かどうか。
    /// デフォルトは true です。
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// アイテムが属するルーレットグループ。
    /// </summary>
    public RouletteGroup? Group { get; set; }

    /// <summary>
    /// アイテムが属するルーレットグループのID（データベース用）。
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// 指定されたラベル、グループでアイテムを初期化します。
    /// </summary>
    /// <param name="label">アイテムの表示ラベル。</param>
    /// <param name="groupId">アイテムが属するグループのID。</param>
    public Item(string label, int groupId)
        : this(label, weight: 1, groupId)
    {
    }

    /// <summary>
    /// 指定されたラベル、重み付け値、グループでアイテムを初期化します。
    /// </summary>
    /// <param name="label">アイテムの表示ラベル。</param>
    /// <param name="weight">アイテムの重み付け値。</param>
    /// <param name="groupId">アイテムが属するグループのID。</param>
    public Item(string label, int weight, int groupId)
    {
        Label = label;
        Weight = weight;
        GroupId = groupId;
    }

    /// <summary>
    /// JSON デシリアライズ用のパラメーターなしコンストラクター。
    /// </summary>
    public Item()
    {
    }

    /// <summary>
    /// アイテムの文字列表現を取得します。
    /// </summary>
    /// <returns>ラベルと重み付けを含む文字列。</returns>
    public override string ToString()
    {
        return $"Item(Id:{Id}, Label:{Label}, Weight:{Weight}, GroupId:{GroupId})";
    }
}

