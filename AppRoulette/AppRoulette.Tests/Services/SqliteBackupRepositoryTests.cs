using AppRoulette.Models;
using AppRoulette.Services;
using Microsoft.Data.Sqlite;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="SqliteBackupRepository"/> のテスト。
/// 一時ファイルの SQLite データベースを使い、アプリのデータベースには触れません。
/// </summary>
public class SqliteBackupRepositoryTests : IDisposable
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"AppRouletteTest-{Guid.NewGuid():N}.db");

    private readonly string _connectionString;

    /// <summary>
    /// 一時ファイルのデータベースを作成し、アプリと同じテーブルを用意します。
    /// </summary>
    public SqliteBackupRepositoryTests()
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            // テスト後にファイルを削除できるよう、接続をプールしない
            Pooling = false,
        }.ToString();

        DatabaseInitializer.InitializeDatabase(_connectionString);
        Execute(@"
            INSERT INTO Groups (Id, DisplayName, SortOrder, IsDeleted, ExcludeOnWin)
            VALUES (1, '旧リスト1', 1, 0, 0), (2, '旧リスト2', 2, 0, 0), (3, '旧リスト3', 3, 0, 1);
            INSERT INTO Items (Label, Weight, [GroupId], IsEnabled)
            VALUES ('旧アイテムA', 1, 1, 1), ('旧アイテムB', 2, 3, 0);");
    }

    /// <summary>
    /// 一時ファイルのデータベースを削除します。
    /// </summary>
    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    /// <summary>
    /// 一時データベースに SQL を実行します。
    /// </summary>
    /// <param name="sql">実行する SQL。</param>
    private void Execute(string sql)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 一時データベースのグループを、削除済みのものも含めて Id 順に読み込みます。
    /// </summary>
    private List<(int Id, string DisplayName, int SortOrder, bool IsDeleted, bool ExcludeOnWin)> ReadGroups()
    {
        var groups = new List<(int, string, int, bool, bool)>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, DisplayName, SortOrder, IsDeleted, ExcludeOnWin FROM Groups ORDER BY Id";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            groups.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3) != 0,
                reader.GetInt32(4) != 0));
        }

        return groups;
    }

    /// <summary>
    /// 一時データベースのアイテムを追加順に読み込みます。
    /// </summary>
    private List<(string Label, int Weight, int GroupId, bool IsEnabled)> ReadItems()
    {
        var items = new List<(string, int, int, bool)>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Label, Weight, [GroupId], IsEnabled FROM Items ORDER BY Id";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add((
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3) != 0));
        }

        return items;
    }

    /// <summary>
    /// 既存のリストとアイテムが、指定したリスト一覧ですべて置き換わることを検証します。
    /// 既存のリストの行は、既定リストの再作成を防ぐため削除済みとして残ります。
    /// </summary>
    [Fact]
    public async Task ReplaceAllAsync_既存データがある場合_全リストとアイテムを置き換える()
    {
        // Arrange
        var groups = new List<RouletteGroup>
        {
            new(1, "朝会")
            {
                ExcludeOnWin = true,
                Items =
                {
                    new RouletteItem("Aさん") { Weight = 2 },
                    new RouletteItem("Bさん") { IsEnabled = false },
                },
            },
            new(2, "昼食")
            {
                Items = { new RouletteItem("カレー") },
            },
        };
        var sut = new SqliteBackupRepository(_connectionString);

        // Act
        await sut.ReplaceAllAsync(groups);

        // Assert
        Assert.Equal(
            new[]
            {
                (1, "朝会", 1, false, true),
                (2, "昼食", 2, false, false),
                (3, "旧リスト3", 3, true, true),
            },
            ReadGroups());
        Assert.Equal(
            new[]
            {
                ("Aさん", 2, 1, true),
                ("Bさん", 1, 1, false),
                ("カレー", 1, 2, true),
            },
            ReadItems());
    }

    /// <summary>
    /// 置き換えの途中で失敗した場合は、何も変更されないことを検証します。
    /// </summary>
    [Fact]
    public async Task ReplaceAllAsync_途中で失敗した場合_何も変更しない()
    {
        // Arrange
        var groupsBefore = ReadGroups();
        var itemsBefore = ReadItems();
        var groups = new List<RouletteGroup>
        {
            new(1, "朝会")
            {
                Items = { new RouletteItem("Aさん") },
            },
            // 表示名が null のため、2 件目のリストの保存で失敗する
            new(2, null!),
        };
        var sut = new SqliteBackupRepository(_connectionString);

        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => sut.ReplaceAllAsync(groups));

        // Assert
        Assert.Equal(groupsBefore, ReadGroups());
        Assert.Equal(itemsBefore, ReadItems());
    }
}
