using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IRandomService"/> フェイク実装。
/// 常に固定値を返します。
/// </summary>
internal class FakeRandomService : IRandomService
{
    private readonly int _fixedValue;

    /// <summary>
    /// 指定した固定値を常に返す <see cref="FakeRandomService"/> を初期化します。
    /// </summary>
    /// <param name="fixedValue">常に返す固定値。</param>
    public FakeRandomService(int fixedValue)
    {
        _fixedValue = fixedValue;
    }

    /// <summary>
    /// 常にコンストラクターで指定した固定値を返します。
    /// </summary>
    /// <param name="maxValue">上限値（無視されます）。</param>
    /// <returns>固定値。</returns>
    public int Next(int maxValue) => _fixedValue;

    /// <summary>
    /// 重み付けに基づいてアイテムを選択します。
    /// テスト用に固定値に最も近い累積Weight を持つアイテムを返します。
    /// </summary>
    /// <param name="items">Weight プロパティを持つアイテムのコレクション。</param>
    /// <typeparam name="T">アイテムの種類。</typeparam>
    /// <returns>選択されたアイテム、またはコレクションが空の場合は null。</returns>
    public T? SelectByWeight<T>(IReadOnlyList<T> items) where T : class, IWeighted
    {
        if (items.Count == 0)
        {
            return null;
        }

        // テスト用: 固定値を累積Weight のシミュレーションに使用
        int cumulativeWeight = 0;
        foreach (var item in items)
        {
            cumulativeWeight += item.Weight;
            if (_fixedValue < cumulativeWeight)
            {
                return item;
            }
        }

        return items[items.Count - 1];
    }
}

