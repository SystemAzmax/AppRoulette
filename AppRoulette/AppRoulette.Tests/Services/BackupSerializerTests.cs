using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="BackupSerializer"/> のテスト。
/// バックアップファイルの作成と、読み込み時の検証・補正を検証します。
/// </summary>
public class BackupSerializerTests
{
    /// <summary>
    /// テスト用のリスト一覧を作成します。
    /// </summary>
    private static List<RouletteGroup> CreateGroups() => new()
    {
        new(5, "朝会")
        {
            ExcludeOnWin = true,
            Items =
            {
                new RouletteItem("Aさん") { Weight = 2 },
                new RouletteItem("Bさん") { IsEnabled = false },
            },
        },
        new(9, "昼食")
        {
            Items = { new RouletteItem("カレー") { Weight = 5 } },
        },
    };

    /// <summary>
    /// 書き出したバックアップを読み込むと同じ内容に戻り、
    /// リストの識別子は表示順に 1 から振り直されることを検証します。
    /// </summary>
    [Fact]
    public void Serialize_リスト一覧_読み込むと同じ内容に戻りIDは表示順に振り直す()
    {
        // Act
        var json = BackupSerializer.Serialize(CreateGroups(), DateTime.UtcNow);
        var groups = BackupSerializer.Deserialize(json);

        // Assert
        Assert.Equal(new[] { 1, 2 }, groups.Select(g => g.Id));
        Assert.Equal(new[] { "朝会", "昼食" }, groups.Select(g => g.DisplayName));
        Assert.True(groups[0].ExcludeOnWin);
        Assert.False(groups[1].ExcludeOnWin);
        Assert.Equal(new[] { "Aさん", "Bさん" }, groups[0].Items.Select(i => i.Name));
        Assert.Equal(new[] { 2, 1 }, groups[0].Items.Select(i => i.Weight));
        Assert.Equal(new[] { true, false }, groups[0].Items.Select(i => i.IsEnabled));
        Assert.Equal(5, Assert.Single(groups[1].Items).Weight);
    }

    /// <summary>
    /// 日本語をエスケープせずに書き出し、形式の情報を含めることを検証します。
    /// </summary>
    [Fact]
    public void Serialize_日本語を含む場合_エスケープせずに書き出す()
    {
        // Act
        var json = BackupSerializer.Serialize(CreateGroups(), DateTime.UtcNow);

        // Assert
        Assert.Contains("朝会", json);
        Assert.Contains("\"App\": \"AppRoulette\"", json);
        Assert.Contains("\"FormatVersion\": 1", json);
    }

    /// <summary>
    /// JSON として正しくない場合は、形式が正しくないことを知らせる例外を投げることを検証します。
    /// </summary>
    /// <param name="json">読み込む内容。</param>
    [Theory]
    [InlineData("")]
    [InlineData("バックアップではない文字列")]
    [InlineData("[1, 2, 3]")]
    public void Deserialize_JSONとして正しくない場合_形式の例外を投げる(string json)
    {
        // Act
        var ex = Assert.Throws<BackupFormatException>(() => BackupSerializer.Deserialize(json));

        // Assert
        Assert.Contains("形式が正しくありません", ex.Message);
    }

    /// <summary>
    /// このアプリのバックアップではない JSON の場合は、そのことを知らせる例外を投げることを検証します。
    /// </summary>
    /// <param name="json">読み込む内容。</param>
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{ \"App\": \"OtherApp\", \"FormatVersion\": 1, \"Groups\": [] }")]
    public void Deserialize_このアプリのバックアップではない場合_例外を投げる(string json)
    {
        // Act
        var ex = Assert.Throws<BackupFormatException>(() => BackupSerializer.Deserialize(json));

        // Assert
        Assert.Contains("AppRoulette のバックアップファイルではありません", ex.Message);
    }

    /// <summary>
    /// 新しい形式のバックアップの場合は、アプリの更新を促す例外を投げることを検証します。
    /// </summary>
    [Fact]
    public void Deserialize_新しい形式のバージョンの場合_更新を促す例外を投げる()
    {
        // Arrange
        var json = "{ \"App\": \"AppRoulette\", \"FormatVersion\": 2, "
            + "\"Groups\": [ { \"DisplayName\": \"朝会\" } ] }";

        // Act
        var ex = Assert.Throws<BackupFormatException>(() => BackupSerializer.Deserialize(json));

        // Assert
        Assert.Contains("新しいバージョン", ex.Message);
    }

    /// <summary>
    /// リストが 1 件も無い場合は例外を投げることを検証します。
    /// </summary>
    [Fact]
    public void Deserialize_リストが無い場合_例外を投げる()
    {
        // Arrange
        var json = "{ \"App\": \"AppRoulette\", \"FormatVersion\": 1, \"Groups\": [] }";

        // Act
        var ex = Assert.Throws<BackupFormatException>(() => BackupSerializer.Deserialize(json));

        // Assert
        Assert.Contains("リストがありません", ex.Message);
    }

    /// <summary>
    /// 空の名前や範囲外の Weight などを、取り込める形に直すことを検証します。
    /// </summary>
    [Fact]
    public void Deserialize_不正な値を含む場合_取り込める形に直す()
    {
        // Arrange
        var json = "{ \"App\": \"AppRoulette\", \"FormatVersion\": 1, \"Groups\": [ {"
            + " \"DisplayName\": \"  \", \"Items\": ["
            + " { \"Name\": \" \", \"Weight\": 3 },"
            + " { \"Name\": \"  Aさん  \", \"Weight\": 9 },"
            + " null,"
            + " { \"Name\": \"Bさん\", \"Weight\": -1 } ] } ] }";

        // Act
        var group = Assert.Single(BackupSerializer.Deserialize(json));

        // Assert
        Assert.Equal("Roulette1", group.DisplayName);
        Assert.Equal(new[] { "Aさん", "Bさん" }, group.Items.Select(i => i.Name));
        Assert.Equal(new[] { 5, 1 }, group.Items.Select(i => i.Weight));
    }

    /// <summary>
    /// 最大件数を超えるアイテムは、最大件数までにすることを検証します。
    /// </summary>
    [Fact]
    public void Deserialize_最大件数を超えるアイテムの場合_最大件数までにする()
    {
        // Arrange
        var group = new RouletteGroup(1, "多いリスト");
        group.Items.AddRange(Enumerable.Range(1, RouletteGroup.MAX_ITEM_COUNT + 5)
            .Select(i => new RouletteItem($"アイテム{i}")));
        var json = BackupSerializer.Serialize(new[] { group }, DateTime.UtcNow);

        // Act
        var restoredGroup = Assert.Single(BackupSerializer.Deserialize(json));

        // Assert
        Assert.Equal(RouletteGroup.MAX_ITEM_COUNT, restoredGroup.Items.Count);
        Assert.Equal("アイテム1", restoredGroup.Items[0].Name);
    }
}
