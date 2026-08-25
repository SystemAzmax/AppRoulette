using System.Text.Json;
using AppRoulette.Models;

namespace AppRoulette.Tests.Models;

/// <summary>
/// <see cref="SyncGroupData"/> の変換・シリアライズの
/// ユニットテストクラスです。
/// </summary>
public class SyncGroupDataTests
{
    // ---------------------------------------------------------------
    // FromGroup / ToGroup
    // ---------------------------------------------------------------

    [Fact]
    public void FromGroup_グループの内容をDTOへ変換する()
    {
        // Arrange
        var group = new RouletteGroup(3, "テストグループ");
        group.TryAddItem(new RouletteItem("アイテムA") { Weight = 2 });
        group.TryAddItem(new RouletteItem("アイテムB") { Weight = 5 });
        var timestamp = new DateTime(2026, 1, 15, 12, 0, 0);

        // Act
        var data = SyncGroupData.FromGroup(group, timestamp);

        // Assert
        Assert.Equal(3, data.GroupId);
        Assert.Equal("テストグループ", data.DisplayName);
        Assert.Equal(2, data.Items.Count);
        Assert.Equal("アイテムA", data.Items[0].Name);
        Assert.Equal(2, data.Items[0].Weight);
        Assert.Equal("アイテムB", data.Items[1].Name);
        Assert.Equal(5, data.Items[1].Weight);
        Assert.Equal(timestamp, data.LastModifiedUtc);
    }

    [Fact]
    public void ToGroup_DTOからグループを復元する()
    {
        // Arrange
        var data = new SyncGroupData
        {
            GroupId = 2,
            DisplayName = "復元グループ",
            Items = new List<SyncItemData>
            {
                new() { Name = "アイテムX", Weight = 3 },
            },
        };

        // Act
        var group = data.ToGroup();

        // Assert
        Assert.Equal(2, group.Id);
        Assert.Equal("復元グループ", group.DisplayName);
        Assert.Single(group.Items);
        Assert.Equal("アイテムX", group.Items[0].Name);
        Assert.Equal(3, group.Items[0].Weight);
    }

    [Fact]
    public void ToGroup_アイテムが上限を超える場合_上限までのみ追加する()
    {
        // Arrange
        var data = new SyncGroupData
        {
            GroupId = 1,
            DisplayName = "上限テスト",
            Items = Enumerable
                .Range(1, RouletteGroup.MAX_ITEM_COUNT + 5)
                .Select(i => new SyncItemData { Name = $"アイテム{i}" })
                .ToList(),
        };

        // Act
        var group = data.ToGroup();

        // Assert
        Assert.Equal(RouletteGroup.MAX_ITEM_COUNT, group.Items.Count);
    }

    // ---------------------------------------------------------------
    // JSON シリアライズ
    // ---------------------------------------------------------------

    [Fact]
    public void Serialize_往復変換で内容が保持される()
    {
        // Arrange
        var original = new SyncGroupData
        {
            GroupId = 5,
            DisplayName = "往復テスト",
            Items = new List<SyncItemData>
            {
                new() { Name = "アイテムA", Weight = 1 },
                new() { Name = "アイテムB", Weight = 4 },
            },
            LastModifiedUtc = new DateTime(
                2026, 2, 1, 8, 30, 0, DateTimeKind.Utc),
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<SyncGroupData>(json);

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(original.GroupId, restored.GroupId);
        Assert.Equal(original.DisplayName, restored.DisplayName);
        Assert.Equal(original.Items.Count, restored.Items.Count);
        Assert.Equal(original.Items[1].Name, restored.Items[1].Name);
        Assert.Equal(original.Items[1].Weight, restored.Items[1].Weight);
        Assert.Equal(original.LastModifiedUtc, restored.LastModifiedUtc);
    }
}
