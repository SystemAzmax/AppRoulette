using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IConfirmationService"/> フェイク実装。
/// 確認の結果として常に固定値を返し、確認が求められた内容を記録します。
/// </summary>
internal class FakeConfirmationService : IConfirmationService
{
    private readonly bool _result;

    /// <summary>
    /// 確認の結果として常に指定値を返す <see cref="FakeConfirmationService"/> を初期化します。
    /// </summary>
    /// <param name="result">確認の結果。実行を選んだ場合を <c>true</c> とします。</param>
    public FakeConfirmationService(bool result)
    {
        _result = result;
    }

    /// <summary>確認が求められた回数を取得します。</summary>
    public int CallCount { get; private set; }

    /// <summary>最後に確認が求められたときの本文を取得します。</summary>
    public string? LastMessage { get; private set; }

    /// <summary>
    /// 確認が求められた内容を記録し、固定の結果を返します。
    /// </summary>
    public Task<bool> ConfirmAsync(string title, string message, string confirmButtonText)
    {
        CallCount++;
        LastMessage = message;
        return Task.FromResult(_result);
    }
}
