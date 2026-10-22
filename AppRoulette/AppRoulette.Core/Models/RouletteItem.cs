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

    private bool _isEnabled = true;

    private bool _canUnchecked = true;

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
    /// アイテムが抽選対象かどうかを取得または設定します。
    /// false の場合はルーレット盤面・抽選から除外されます。
    /// デフォルトは true です。
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// このアイテムをチェック外し可能かどうかを取得または設定します。
    /// 有効なアイテムが最低2つ必要な場合、2つ以下になる場合は false になります。
    /// </summary>
    public bool CanUnchecked
    {
        get => _canUnchecked;
        set => SetProperty(ref _canUnchecked, value);
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

