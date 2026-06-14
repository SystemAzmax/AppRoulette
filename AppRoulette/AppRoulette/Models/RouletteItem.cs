namespace AppRoulette.Models;

using AppRoulette.Services;

/// <summary>
/// ルーレットの1アイテムを表すモデルクラス。
/// </summary>
public class RouletteItem : IWeighted
{
    /// <summary>
    /// アイテムの表示名を取得または設定します。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// アイテムの重み付け値。
    /// ルーレット選択時の確率に影響します。
    /// デフォルトは1です。
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// 指定した名前で <see cref="RouletteItem"/> を初期化します。
    /// </summary>
    /// <param name="name">アイテムの表示名。</param>
    public RouletteItem(string name)
    {
        Name = name;
    }

    /// <summary>
    /// JSON デシリアライズ用のパラメーターなしコンストラクターです。
    /// </summary>
    public RouletteItem() { }
}
