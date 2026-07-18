namespace AppRoulette.Models;

using AppRoulette.Services;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ルーレットの1アイテムを表すモデルクラス。
/// </summary>
public class RouletteItem : ObservableObject, IWeighted
{
    private string _name = string.Empty;

    private int _weight = 1;

    /// <summary>
    /// アイテムの表示名を取得または設定します。
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// アイテムの重み付け値。
    /// ルーレット選択時の確率に影響します。
    /// デフォルトは1です。
    /// </summary>
    public int Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, Math.Max(1, Math.Min(5, value)));
    }

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
