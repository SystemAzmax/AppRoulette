namespace AppRoulette.Services;

/// <summary>
/// バックアップファイルの内容が正しくないため、復元できないことを示す例外です。
/// <see cref="Exception.Message"/> は、そのまま利用者に表示できる文にします。
/// </summary>
public class BackupFormatException : Exception
{
    /// <summary>
    /// 利用者に表示するメッセージを指定して <see cref="BackupFormatException"/> を初期化します。
    /// </summary>
    /// <param name="message">利用者に表示するメッセージ。</param>
    public BackupFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 利用者に表示するメッセージと原因の例外を指定して
    /// <see cref="BackupFormatException"/> を初期化します。
    /// </summary>
    /// <param name="message">利用者に表示するメッセージ。</param>
    /// <param name="innerException">原因の例外。</param>
    public BackupFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
