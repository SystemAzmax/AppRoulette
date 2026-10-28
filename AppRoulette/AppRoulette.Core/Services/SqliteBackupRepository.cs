using AppRoulette.Models;
using Microsoft.Data.Sqlite;

namespace AppRoulette.Services;

/// <summary>
/// SQLite を使用した <see cref="IBackupRepository"/> の実装。
/// Groups テーブルと Items テーブルを 1 つのトランザクションで置き換えます。
/// </summary>
public class SqliteBackupRepository : IBackupRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// アプリのデータベースを使用して SqliteBackupRepository を初期化します。
    /// </summary>
    public SqliteBackupRepository()
        : this(DatabaseInitializer.GetConnectionString())
    {
    }

    /// <summary>
    /// 接続先のデータベースを指定して SqliteBackupRepository を初期化します。
    /// </summary>
    /// <param name="connectionString">データベースの接続文字列。</param>
    public SqliteBackupRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task ReplaceAllAsync(IReadOnlyList<RouletteGroup> groups)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            // 既定グループの再作成を防ぐため、既存のグループは行を残して削除済みにする
            await using (var deleteGroupsCommand = connection.CreateCommand())
            {
                deleteGroupsCommand.Transaction = transaction;
                deleteGroupsCommand.CommandText = "UPDATE Groups SET IsDeleted = 1";
                await deleteGroupsCommand.ExecuteNonQueryAsync();
            }

            await using (var deleteItemsCommand = connection.CreateCommand())
            {
                deleteItemsCommand.Transaction = transaction;
                deleteItemsCommand.CommandText = "DELETE FROM Items";
                await deleteItemsCommand.ExecuteNonQueryAsync();
            }

            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];

                await using (var groupCommand = connection.CreateCommand())
                {
                    groupCommand.Transaction = transaction;
                    groupCommand.CommandText = @"
                        INSERT INTO Groups (Id, DisplayName, SortOrder, IsDeleted, ExcludeOnWin)
                        VALUES (@id, @displayName, @sortOrder, 0, @excludeOnWin)
                        ON CONFLICT(Id) DO UPDATE SET
                            DisplayName = excluded.DisplayName,
                            SortOrder = excluded.SortOrder,
                            IsDeleted = 0,
                            ExcludeOnWin = excluded.ExcludeOnWin";
                    groupCommand.Parameters.AddWithValue("@id", group.Id);
                    groupCommand.Parameters.AddWithValue("@displayName", group.DisplayName);
                    groupCommand.Parameters.AddWithValue("@sortOrder", i + 1);
                    groupCommand.Parameters.AddWithValue(
                        "@excludeOnWin", group.ExcludeOnWin ? 1 : 0);
                    await groupCommand.ExecuteNonQueryAsync();
                }

                foreach (var item in group.Items)
                {
                    await using var itemCommand = connection.CreateCommand();
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = @"
                        INSERT INTO Items (Label, Weight, [GroupId], IsEnabled)
                        VALUES (@label, @weight, @groupId, @isEnabled);";
                    itemCommand.Parameters.AddWithValue("@label", item.Name);
                    itemCommand.Parameters.AddWithValue("@weight", item.Weight);
                    itemCommand.Parameters.AddWithValue("@groupId", group.Id);
                    itemCommand.Parameters.AddWithValue(
                        "@isEnabled", item.IsEnabled ? 1 : 0);
                    await itemCommand.ExecuteNonQueryAsync();
                }
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
