using Microsoft.Data.Sqlite;
using AppRoulette.Models;

namespace AppRoulette.Services;

/// <summary>
/// SQLite を使用した <see cref="IGroupRepository"/> の実装です。
/// </summary>
public class SqliteGroupRepository : IGroupRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// <see cref="SqliteGroupRepository"/> を初期化します。
    /// </summary>
    public SqliteGroupRepository()
    {
        _connectionString = DatabaseInitializer.GetConnectionString();
    }

    /// <summary>
    /// 有効なグループ一覧を表示順で非同期に取得します。
    /// </summary>
    /// <returns>有効なグループ一覧。</returns>
    public async Task<IReadOnlyList<RouletteGroup>> GetGroupsAsync()
    {
        var groups = new List<RouletteGroup>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, DisplayName, ExcludeOnWin
            FROM Groups
            WHERE IsDeleted = 0
            ORDER BY SortOrder, Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            groups.Add(new RouletteGroup(reader.GetInt32(0), reader.GetString(1))
            {
                ExcludeOnWin = reader.GetInt32(2) != 0
            });
        }

        return groups;
    }

    /// <summary>
    /// 既定グループが未登録の場合に補完します。
    /// </summary>
    /// <param name="groups">補完対象の既定グループ一覧。</param>
    public async Task EnsureDefaultGroupsAsync(IReadOnlyList<RouletteGroup> groups)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var group in groups)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Groups (Id, DisplayName, SortOrder, IsDeleted)
                VALUES (@id, @displayName, @sortOrder, 0)
                ON CONFLICT(Id) DO NOTHING";
            command.Parameters.AddWithValue("@id", group.Id);
            command.Parameters.AddWithValue("@displayName", group.DisplayName);
            command.Parameters.AddWithValue("@sortOrder", group.Id);

            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 保存済みのグループ名を非同期で取得します。
    /// </summary>
    /// <returns>グループIDと表示名の対応表。</returns>
    public async Task<IReadOnlyDictionary<int, string>> GetGroupNamesAsync()
    {
        var groupNames = new Dictionary<int, string>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, DisplayName FROM Groups ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            groupNames[reader.GetInt32(0)] = reader.GetString(1);
        }

        return groupNames;
    }

    /// <summary>
    /// 指定されたグループ名を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="displayName">保存する表示名。</param>
    public async Task SaveGroupNameAsync(int groupId, string displayName)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Groups (Id, DisplayName)
            VALUES (@id, @displayName)
            ON CONFLICT(Id) DO UPDATE SET DisplayName = excluded.DisplayName";
        command.Parameters.AddWithValue("@id", groupId);
        command.Parameters.AddWithValue("@displayName", displayName);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 指定されたグループの「当たったら除外」設定を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="excludeOnWin">「当たったら除外」の有効状態。</param>
    public async Task SaveGroupExcludeOnWinAsync(int groupId, bool excludeOnWin)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE Groups SET ExcludeOnWin = @excludeOnWin WHERE Id = @id";
        command.Parameters.AddWithValue("@id", groupId);
        command.Parameters.AddWithValue("@excludeOnWin", excludeOnWin ? 1 : 0);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 新しいグループを非同期で追加します。
    /// </summary>
    /// <param name="displayName">追加するグループの表示名。</param>
    /// <param name="sortOrder">追加するグループの表示順。</param>
    /// <returns>追加されたグループ。</returns>
    public async Task<RouletteGroup> AddGroupAsync(string displayName, int sortOrder)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Groups (DisplayName, SortOrder, IsDeleted)
            VALUES (@displayName, @sortOrder, 0)
            RETURNING Id";
        command.Parameters.AddWithValue("@displayName", displayName);
        command.Parameters.AddWithValue("@sortOrder", sortOrder);

        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return new RouletteGroup(id, displayName);
    }

    /// <summary>
    /// 指定されたグループを非同期で削除済みにします。
    /// </summary>
    /// <param name="groupId">削除対象グループの識別子。</param>
    public async Task DeleteGroupAsync(int groupId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Groups SET IsDeleted = 1 WHERE Id = @id";
        command.Parameters.AddWithValue("@id", groupId);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// グループ一覧の表示順を非同期で保存します。
    /// </summary>
    /// <param name="groups">表示順を保存するグループ一覧。</param>
    public async Task SaveGroupOrderAsync(IReadOnlyList<RouletteGroup> groups)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            for (var i = 0; i < groups.Count; i++)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    "UPDATE Groups SET SortOrder = @sortOrder WHERE Id = @id";
                command.Parameters.AddWithValue("@sortOrder", i + 1);
                command.Parameters.AddWithValue("@id", groups[i].Id);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
