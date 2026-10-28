using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IDialogService"/> フェイク実装。
/// 確認の結果として常に固定値を返し、確認やメッセージの内容を記録します。
/// </summary>
internal class FakeDialogService : IDialogService
{
    private readonly bool _confirmResult;

    /// <summary>
    /// 確認の結果として常に指定値を返す <see cref="FakeDialogService"/> を初期化します。
    /// </summary>
    /// <param name="confirmResult">確認の結果。実行を選んだ場合を <c>true</c> とします。</param>
    public FakeDialogService(bool confirmResult = true)
    {
        _confirmResult = confirmResult;
    }

    /// <summary>確認が求められた回数を取得します。</summary>
    public int ConfirmCallCount { get; private set; }

    /// <summary>最後に確認が求められたときの本文を取得します。</summary>
    public string? LastConfirmMessage { get; private set; }

    /// <summary>表示されたメッセージの本文を、表示された順に取得します。</summary>
    public List<string> ShownMessages { get; } = new();

    /// <summary>
    /// 確認が求められた内容を記録し、固定の結果を返します。
    /// </summary>
    public Task<bool> ConfirmAsync(string title, string message, string confirmButtonText)
    {
        ConfirmCallCount++;
        LastConfirmMessage = message;
        return Task.FromResult(_confirmResult);
    }

    /// <summary>
    /// 表示されたメッセージの本文を記録します。
    /// </summary>
    public Task ShowMessageAsync(string title, string message)
    {
        ShownMessages.Add(message);
        return Task.CompletedTask;
    }
}
