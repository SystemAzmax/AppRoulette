using System.Text;

namespace AppRoulette.Services;

/// <summary>
/// エラーの内容をテキストファイルに追記するログです。
/// ファイルが上限の大きさを超えたら 1 世代前のファイルに切り替え、
/// ディスクを使い続けないようにします。
/// </summary>
public class ErrorLogFile
{
    /// <summary>ログファイルの大きさの上限（バイト）の既定値。</summary>
    public const long DEFAULT_MAX_FILE_SIZE = 1024 * 1024;

    /// <summary>1 世代前のログファイルで、拡張子の前に付ける文字列。</summary>
    private const string OLD_FILE_SUFFIX = ".old";

    /// <summary>複数のスレッドから同時に書き込まないためのロック。</summary>
    private readonly object _lockObject = new();

    /// <summary>ログファイルの大きさの上限（バイト）。</summary>
    private readonly long _maxFileSize;

    /// <summary>
    /// <see cref="ErrorLogFile"/> を初期化します。
    /// ファイルとフォルダーは、最初に書き込むときに作成します。
    /// </summary>
    /// <param name="filePath">ログファイルのパス。</param>
    /// <param name="maxFileSize">
    /// ログファイルの大きさの上限（バイト）。超えると 1 世代前のファイルに切り替えます。
    /// </param>
    public ErrorLogFile(string filePath, long maxFileSize = DEFAULT_MAX_FILE_SIZE)
    {
        FilePath = filePath;
        _maxFileSize = maxFileSize;
    }

    /// <summary>
    /// ログファイルのパスを取得します。
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 1 世代前のログファイルのパスを取得します（error.log なら error.old.log）。
    /// </summary>
    public string OldFilePath =>
        Path.Combine(
            Path.GetDirectoryName(FilePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(FilePath)
                + OLD_FILE_SUFFIX
                + Path.GetExtension(FilePath));

    /// <summary>
    /// エラーを日時と一緒にログファイルへ追記します。
    /// 書き込めない場合は何もしません。
    /// </summary>
    /// <param name="context">エラーが発生した処理の説明。</param>
    /// <param name="exception">記録する例外。</param>
    public void Write(string context, Exception exception)
    {
        var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{context}]"
            + Environment.NewLine
            + exception
            + Environment.NewLine
            + Environment.NewLine;

        lock (_lockObject)
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                MoveToOldFileIfTooLarge();
                File.AppendAllText(FilePath, entry, Encoding.UTF8);
            }
            catch (Exception)
            {
                // ログを書けなくても、アプリの動作は続ける
            }
        }
    }

    /// <summary>
    /// ログファイルが上限の大きさを超えている場合、1 世代前のファイルに移します。
    /// 前からある 1 世代前のファイルは上書きします。
    /// </summary>
    private void MoveToOldFileIfTooLarge()
    {
        var file = new FileInfo(FilePath);
        if (!file.Exists || file.Length < _maxFileSize)
        {
            return;
        }

        File.Move(FilePath, OldFilePath, overwrite: true);
    }
}
