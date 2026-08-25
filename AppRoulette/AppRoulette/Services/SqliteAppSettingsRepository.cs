using Microsoft.Data.Sqlite;

namespace AppRoulette.Services;

/// <summary>
/// SQLite を使用した <see cref="IAppSettingsRepository"/> の実装です。
/// </summary>
public class SqliteAppSettingsRepository : IAppSettingsRepository
{
    private const string LAST_SELECTED_GROUP_ID_KEY = "LastSelectedGroupId";

    private readonly string _connectionString;

    /// <summary>
    /// <see cref="SqliteAppSettingsRepository"/> を初期化します。
    /// </summary>
    public SqliteAppSettingsRepository()
    {
        _connectionString = DatabaseInitializer.GetConnectionString();
    }

    /// <summary>
    /// 最後に選択されたグループ ID を非同期で取得します。
    /// </summary>
    /// <returns>グループ ID。未保存または解析失敗時は 0。</returns>
    public async Task<int> GetLastSelectedGroupIdAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Value
            FROM AppSettings
            WHERE Key = @key";
        command.Parameters.AddWithValue("@key", LAST_SELECTED_GROUP_ID_KEY);

        var value = await command.ExecuteScalarAsync();
        return value is string text && int.TryParse(text, out var groupId)
            ? groupId
            : 0;
    }

    /// <summary>
    /// 最後に選択されたグループ ID を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存するグループ ID。</param>
    public async Task SaveLastSelectedGroupIdAsync(int groupId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO AppSettings (Key, Value)
            VALUES (@key, @value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value";
        command.Parameters.AddWithValue("@key", LAST_SELECTED_GROUP_ID_KEY);
        command.Parameters.AddWithValue("@value", groupId.ToString());

        await command.ExecuteNonQueryAsync();
    }
}
