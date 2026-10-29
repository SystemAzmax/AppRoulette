namespace AppRoulette.Services;

using System.Diagnostics;

/// <summary>
/// アプリケーション内の例外ログ出力を集約するロガーです。
/// </summary>
public static class ApplicationLogger
{
    /// <summary>
    /// エラーを記録するログファイル。
    /// <see cref="Configure"/> を呼び出すまでは null で、ファイルには記録しません。
    /// </summary>
    private static ErrorLogFile? _logFile;

    /// <summary>
    /// エラーを記録するログファイルのパスを取得します。
    /// ファイルに記録しない設定の場合は null。
    /// </summary>
    public static string? LogFilePath => _logFile?.FilePath;

    /// <summary>
    /// リリース版でも原因を調べられるよう、エラーをファイルにも記録するように設定します。
    /// アプリの起動時に一度だけ呼び出します（テストでは呼び出さず、ファイルに記録しません）。
    /// </summary>
    /// <param name="logFilePath">ログファイルのパス。</param>
    public static void Configure(string logFilePath)
    {
        _logFile = new ErrorLogFile(logFilePath);
    }

    /// <summary>
    /// 例外情報をエラーログとして記録します。
    /// </summary>
    /// <param name="context">例外が発生した処理の説明。</param>
    /// <param name="exception">記録する例外。</param>
    public static void LogError(string context, Exception exception)
    {
        Trace.TraceError($"{context}: {exception}");
        _logFile?.Write(context, exception);
    }
}
