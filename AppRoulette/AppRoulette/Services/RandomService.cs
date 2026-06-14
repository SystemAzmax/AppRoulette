namespace AppRoulette.Services;

/// <summary>
/// <see cref="System.Random"/> を使用してランダムな整数値を生成するサービスです。
/// </summary>
public class RandomService : IRandomService
{
    private readonly Random _random = new();

    /// <summary>
    /// 0 以上 <paramref name="maxValue"/> 未満のランダムな整数を返します。
    /// </summary>
    /// <param name="maxValue">上限値（この値自体は含まれない）。</param>
    /// <returns>0 以上 <paramref name="maxValue"/> 未満の整数。</returns>
    public int Next(int maxValue) => _random.Next(maxValue);

    /// <summary>
    /// 重み付け値(Weight)に基づいてアイテムを確率的に選択します。
    /// </summary>
    /// <param name="items">Weight プロパティを持つアイテムのコレクション。</param>
    /// <typeparam name="T">アイテムの種類。IWeighted インターフェースを実装している必要がある。</typeparam>
    /// <returns>重み付け値に基づいて選択されたアイテム。
    /// コレクションが空または合計Weight が 0 の場合は null を返す。</returns>
    public T? SelectByWeight<T>(IReadOnlyList<T> items) where T : class, IWeighted
    {
        if (items.Count == 0)
        {
            return null;
        }

        // 全アイテムの合計Weight を計算
        int totalWeight = 0;
        foreach (var item in items)
        {
            totalWeight += item.Weight;
        }

        // 合計Weight が 0 の場合は null を返す
        if (totalWeight <= 0)
        {
            return null;
        }

        // 0 以上 totalWeight 未満のランダム値を生成
        int randomValue = _random.Next(totalWeight);

        // 累積和で該当アイテムを特定
        int cumulativeWeight = 0;
        foreach (var item in items)
        {
            cumulativeWeight += item.Weight;
            if (randomValue < cumulativeWeight)
            {
                return item;
            }
        }

        // フォールバック（理論的には到達しない）
        return items[items.Count - 1];
    }
}
