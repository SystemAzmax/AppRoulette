namespace AppRoulette.Services;

using System.Diagnostics;

/// <summary>
/// アプリケーション内の例外ログ出力を集約するロガーです。
/// </summary>
public static class ApplicationLogger
{
    /// <summary>
    /// 例外情報をエラーログとして記録します。
    /// </summary>
    /// <param name="context">例外が発生した処理の説明。</param>
    /// <param name="exception">記録する例外。</param>
    public static void LogError(string context, Exception exception)
    {
        Trace.TraceError($"{context}: {exception}");
    }
}
