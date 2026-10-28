namespace AppRoulette.Services;

/// <summary>
/// 利用者への確認やメッセージの表示を担うサービスのインターフェースです。
/// WinUI に依存しないよう、表示方法は実装側に任せます。
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 確認メッセージを表示し、利用者が操作の実行を選んだかどうかを返します。
    /// </summary>
    /// <param name="title">確認のタイトル。</param>
    /// <param name="message">確認の本文。</param>
    /// <param name="confirmButtonText">実行ボタンの表示テキスト。</param>
    /// <returns>利用者が実行を選んだ場合は <c>true</c>。</returns>
    Task<bool> ConfirmAsync(string title, string message, string confirmButtonText);

    /// <summary>
    /// メッセージを表示し、利用者が閉じるまで待ちます。
    /// </summary>
    /// <param name="title">メッセージのタイトル。</param>
    /// <param name="message">メッセージの本文。</param>
    Task ShowMessageAsync(string title, string message);
}
