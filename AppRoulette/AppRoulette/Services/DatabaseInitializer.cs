using Microsoft.Data.Sqlite;
using Windows.Storage;

namespace AppRoulette.Services;

/// <summary>
/// SQLite データベースの初期化を担当するクラス。
/// アプリケーション起動時に一度だけ実行され、テーブルが存在しない場合は自動作成します。
/// WinUI に依存しない設計で、将来的に MAUI への移植を想定しています。
/// </summary>
public class DatabaseInitializer
{
    /// <summary>
    /// SQLite データベースファイルの名前。
    /// </summary>
    private const string DB_FILE_NAME = "roulette.db";

    /// <summary>
    /// データベースが既に初期化済みかどうかを示すフラグ。
    /// </summary>
    private static bool _isInitialized;

    /// <summary>
    /// 初期化処理用のロック。複数スレッドからのアクセスを防ぐ。
    /// </summary>
    private static readonly object _lockObject = new();

    /// <summary>
    /// SQLite データベースの接続文字列を取得します。
    /// </summary>
    /// <returns>データベース接続文字列。</returns>
    public static string GetConnectionString()
    {
        string dbPath = GetDatabasePath();
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// データベースファイルのフルパスを取得します。
    /// </summary>
    /// <returns>データベースファイルの完全パス。</returns>
    public static string GetDatabasePath()
    {
        string localFolderPath = ApplicationData.Current.LocalFolder.Path;
        return Path.Combine(localFolderPath, DB_FILE_NAME);
    }

    /// <summary>
    /// データベースを非同期で初期化します。
    /// 複数回呼び出された場合でも、実際の初期化は一度だけ実行されます。
    /// </summary>
    /// <returns>初期化が成功したかどうかを示すタスク。</returns>
    public static async Task InitializeAsync()
    {
        // 既に初期化済みならすぐに返す
        if (_isInitialized)
        {
            return;
        }

        // 複数スレッドが同時にアクセスしないようロック
        lock (_lockObject)
        {
            // ロック内で再度確認（Double-check locking pattern）
            if (_isInitialized)
            {
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DatabaseInitializer] Starting DB initialization at: {GetDatabasePath()}");

                // 同期的にデータベースを初期化
                InitializeDatabase();

                // 初期化完了フラグを設定
                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine(
                    $"[DatabaseInitializer] DB initialized successfully at: {GetDatabasePath()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DatabaseInitializer] DB initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(
                    $"[DatabaseInitializer] Exception Details: {ex}");
                throw;
            }
        }

        // 비동기 작업として完了を返す
        await Task.CompletedTask;
    }

    /// <summary>
    /// データベースの同期初期化処理を実行します。
    /// Items テーブルが存在しない場合は自動作成します。
    /// </summary>
    private static void InitializeDatabase()
    {
        string dbPath = GetDatabasePath();
        string connectionString = GetConnectionString();

        System.Diagnostics.Debug.WriteLine(
            $"[DatabaseInitializer] Connecting to: {dbPath}");

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        System.Diagnostics.Debug.WriteLine(
            "[DatabaseInitializer] Connection opened successfully");

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Items (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Label TEXT NOT NULL,
                Weight INTEGER NOT NULL,
                [GroupId] INTEGER NOT NULL
            );";

        int result = command.ExecuteNonQuery();

        System.Diagnostics.Debug.WriteLine(
            $"[DatabaseInitializer] CREATE TABLE executed, result: {result}");
        System.Diagnostics.Debug.WriteLine(
            "[DatabaseInitializer] Items table checked/created successfully");

        // テーブル確認クエリ
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Items'";
        var tableExists = checkCommand.ExecuteScalar();

        System.Diagnostics.Debug.WriteLine(
            $"[DatabaseInitializer] Table confirmation - Items table exists: {tableExists != null}");

        connection.Close();
    }
}
