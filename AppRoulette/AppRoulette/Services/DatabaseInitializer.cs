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
                // 同期的にデータベースを初期化
                InitializeDatabase();

                // 初期化完了フラグを設定
                _isInitialized = true;
            }
            catch (Exception)
            {
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
        string connectionString = GetConnectionString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Groups (
                Id INTEGER PRIMARY KEY,
                DisplayName TEXT NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                ExcludeOnWin INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Items (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Label TEXT NOT NULL,
                Weight INTEGER NOT NULL,
                [GroupId] INTEGER NOT NULL,
                IsEnabled INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );";

        _ = command.ExecuteNonQuery();

        EnsureColumn(connection, "Groups", "SortOrder", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Groups", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Groups", "ExcludeOnWin", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Items", "IsEnabled", "INTEGER NOT NULL DEFAULT 1");
        using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = @"
            UPDATE Groups
            SET SortOrder = Id
            WHERE SortOrder = 0;";
        _ = updateCommand.ExecuteNonQuery();

        connection.Close();
    }

    /// <summary>
    /// 指定されたテーブルに指定された列が存在しない場合に追加します。
    /// </summary>
    /// <param name="connection">SQLite 接続。</param>
    /// <param name="tableName">対象のテーブル名。</param>
    /// <param name="columnName">追加する列名。</param>
    /// <param name="definition">列定義。</param>
    private static void EnsureColumn(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string definition)
    {
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = checkCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(
                reader.GetString(1),
                columnName,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText =
            $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        _ = alterCommand.ExecuteNonQuery();
    }
}
