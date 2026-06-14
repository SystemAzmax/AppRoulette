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
}
