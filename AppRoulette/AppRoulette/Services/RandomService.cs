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
}
