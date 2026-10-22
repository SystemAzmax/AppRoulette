namespace AppRoulette.Services;

/// <summary>
/// Weight（重み付け値）を持つアイテムを表すインターフェース。
/// </summary>
public interface IWeighted
{
    /// <summary>
    /// ルーレット選択時の確率に影響する重み付け値。
    /// デフォルトは1です。値が大きいほど選択されやすくなります。
    /// </summary>
    int Weight { get; }
}
