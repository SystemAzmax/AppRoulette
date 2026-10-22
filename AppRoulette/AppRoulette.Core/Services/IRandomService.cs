namespace AppRoulette.Services;

/// <summary>
/// ランダムな整数値を生成するサービスのインターフェース。
/// テスト時に固定値を注入できるよう抽象化しています。
/// </summary>
public interface IRandomService
{
    /// <summary>
    /// 0 以上 <paramref name="maxValue"/> 未満のランダムな整数を返します。
    /// </summary>
    /// <param name="maxValue">上限値（この値自体は含まれない）。</param>
    /// <returns>0 以上 <paramref name="maxValue"/> 未満の整数。</returns>
    int Next(int maxValue);

    /// <summary>
    /// 重み付け値(Weight)に基づいてアイテムを確率的に選択します。
    /// 
    /// 処理法：
    /// 1. 各アイテムの Weight 値を合算して totalWeight を計算
    /// 2. 0 以上 totalWeight 未満の範囲でランダム値を生成
    /// 3. 累積和を算出し、ランダム値以上の最初のアイテムを返す
    /// </summary>
    /// <param name="items">Weight プロパティを持つアイテムのコレクション。</param>
    /// <typeparam name="T">アイテムの種類。Weight プロパティを持つ必要がある。</typeparam>
    /// <returns>重み付け値に基づいて選択されたアイテム。
    /// コレクションが空や全が totalWeight が 0 の場合は null を返す。</returns>
    T? SelectByWeight<T>(IReadOnlyList<T> items) where T : class, IWeighted;
}
