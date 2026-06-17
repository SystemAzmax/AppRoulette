using AppRoulette.Models;
using Microsoft.Data.Sqlite;

namespace AppRoulette.Services;

/// <summary>
/// SQLite を使用した IItemRepository の実装。
/// すべての CRUD 操作が非同期で実行され、SQL インジェクション対策のため
/// パラメータ化クエリを使用します。
/// WinUI に依存しない設計により、将来的に MAUI への移植を想定しています。
/// </summary>
public class SqliteItemRepository : IItemRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// SqliteItemRepository を初期化します。
    /// </summary>
    public SqliteItemRepository()
    {
        _connectionString = DatabaseInitializer.GetConnectionString();
    }

    /// <summary>
    /// すべてのアイテムを非同期で取得します。
    /// </summary>
    /// <returns>すべてのアイテムを含むリスト。</returns>
    public async Task<List<Item>> GetItemsAsync()
    {
        var items = new List<Item>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Label, Weight, [GroupId] FROM Items ORDER BY [GroupId], Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Item
            {
                Id = reader.GetInt32(0),
                Label = reader.GetString(1),
                Weight = reader.GetInt32(2),
                GroupId = reader.GetInt32(3)
            });
        }

        return items;
    }

    /// <summary>
    /// 指定されたグループに属するアイテムをすべて非同期で取得します。
    /// </summary>
    /// <param name="groupId">グループの識別子。</param>
    /// <returns>指定されたグループに属するアイテムのリスト。</returns>
    public async Task<List<Item>> GetItemsByGroupAsync(int groupId)
    {
        var items = new List<Item>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Label, Weight, [GroupId] FROM Items WHERE [GroupId] = @groupId ORDER BY Id";
        command.Parameters.AddWithValue("@groupId", groupId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new Item
            {
                Id = reader.GetInt32(0),
                Label = reader.GetString(1),
                Weight = reader.GetInt32(2),
                GroupId = reader.GetInt32(3)
            });
        }

        return items;
    }

    /// <summary>
    /// 指定された識別子を持つアイテムを非同期で取得します。
    /// </summary>
    /// <param name="id">アイテムの識別子。</param>
    /// <returns>見つかったアイテム、または見つからない場合は null。</returns>
    public async Task<Item?> GetItemByIdAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Label, Weight, [GroupId] FROM Items WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var item = new Item
            {
                Id = reader.GetInt32(0),
                Label = reader.GetString(1),
                Weight = reader.GetInt32(2),
                GroupId = reader.GetInt32(3)
            };

            return item;
        }

        return null;
    }

    /// <summary>
    /// 新しいアイテムを非同期で追加します。
    /// </summary>
    /// <param name="item">追加するアイテム。</param>
    /// <returns>追加されたアイテムの識別子。</returns>
    public async Task<int> AddItemAsync(Item item)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Items (Label, Weight, [GroupId])
            VALUES (@label, @weight, @groupId);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@label", item.Label);
        command.Parameters.AddWithValue("@weight", item.Weight);
        command.Parameters.AddWithValue("@groupId", item.GroupId);

        var result = await command.ExecuteScalarAsync();
        int insertedId = Convert.ToInt32(result);

        return insertedId;
    }

    /// <summary>
    /// 既存のアイテムを非同期で更新します。
    /// </summary>
    /// <param name="item">更新するアイテム。</param>
    /// <returns>更新されたアイテム数（通常は 1）。</returns>
    public async Task<int> UpdateItemAsync(Item item)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Items
            SET Label = @label, Weight = @weight, [GroupId] = @groupId
            WHERE Id = @id";

        command.Parameters.AddWithValue("@id", item.Id);
        command.Parameters.AddWithValue("@label", item.Label);
        command.Parameters.AddWithValue("@weight", item.Weight);
        command.Parameters.AddWithValue("@groupId", item.GroupId);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows;
    }

    /// <summary>
    /// 指定された識別子を持つアイテムを非同期で削除します。
    /// </summary>
    /// <param name="id">削除するアイテムの識別子。</param>
    /// <returns>削除されたアイテム数。</returns>
    public async Task<int> DeleteItemAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Items WHERE Id = @id";
        command.Parameters.AddWithValue("@id", id);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows;
    }

    /// <summary>
    /// 指定されたグループに属するすべてのアイテムを非同期で削除します。
    /// </summary>
    /// <param name="groupId">削除対象グループの識別子。</param>
    /// <returns>削除されたアイテム数。</returns>
    public async Task<int> DeleteItemsByGroupAsync(int groupId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Items WHERE [GroupId] = @groupId";
        command.Parameters.AddWithValue("@groupId", groupId);

        int affectedRows = await command.ExecuteNonQueryAsync();

        return affectedRows;
    }
}
