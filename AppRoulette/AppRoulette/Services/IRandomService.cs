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
}
