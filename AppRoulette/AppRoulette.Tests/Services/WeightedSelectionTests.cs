using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="RandomService.SelectByWeight{T}"/> メソッドのテスト。
/// 重み付けに基づいたランダム選択ロジックをテストします。
/// </summary>
public class WeightedSelectionTests
{
    /// <summary>
    /// 空のコレクションを渡した場合、null が返されることを検証します。
    /// </summary>
    [Fact]
    public void 空のコレクション_NULLが返される()
    {
        // Arrange
        var service = new RandomService();
        var items = new List<RouletteItem>();

        // Act
        var result = service.SelectByWeight(items);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 単一アイテムの場合、そのアイテムが返されることを検証します。
    /// </summary>
    [Fact]
    public void 単一アイテム_そのアイテムが返される()
    {
        // Arrange
        var service = new RandomService();
        var item = new RouletteItem("OnlyItem") { Weight = 1 };
        var items = new List<RouletteItem> { item };

        // Act
        var result = service.SelectByWeight(items);

        // Assert
        Assert.Same(item, result);
    }

    /// <summary>
    /// 同じWeightを持つ複数アイテムの場合、
    /// どのアイテムかが統計的に均等であることを検証します。
    /// </summary>
    [Fact]
    public void 同じWeight複数アイテム_統計的に均等に選択される()
    {
        // Arrange
        var service = new RandomService();
        var items = new List<RouletteItem>
        {
            new("Item1") { Weight = 1 },
            new("Item2") { Weight = 1 },
            new("Item3") { Weight = 1 },
        };

        // Act
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < 3000; i++)
        {
            var result = service.SelectByWeight(items);
            if (result is not null)
            {
                if (!counts.ContainsKey(result.Name))
                {
                    counts[result.Name] = 0;
                }
                counts[result.Name]++;
            }
        }

        // Assert - 各アイテムがほぼ1000回選択されること（±300の許容範囲）
        Assert.Equal(3, counts.Count);
        foreach (var count in counts.Values)
        {
            Assert.InRange(count, 700, 1300);
        }
    }

    /// <summary>
    /// Weight の比率に応じた確率で選択されることを検証します。
    /// Weight 比 1:2:3 の場合、統計的に 1/6:2/6:3/6 の確率で選択される。
    /// </summary>
    [Fact]
    public void Weight比1対2対3_確率比に従って選択される()
    {
        // Arrange
        var service = new RandomService();
        var items = new List<RouletteItem>
        {
            new("Item1") { Weight = 1 },
            new("Item2") { Weight = 2 },
            new("Item3") { Weight = 3 },
        };

        // Act
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < 6000; i++)
        {
            var result = service.SelectByWeight(items);
            if (result is not null)
            {
                if (!counts.ContainsKey(result.Name))
                {
                    counts[result.Name] = 0;
                }
                counts[result.Name]++;
            }
        }

        // Assert
        // Item1: 1/6 = 1000回±300
        // Item2: 2/6 = 2000回±300
        // Item3: 3/6 = 3000回±300
        Assert.InRange(counts["Item1"], 700, 1300);
        Assert.InRange(counts["Item2"], 1700, 2300);
        Assert.InRange(counts["Item3"], 2700, 3300);
    }

    /// <summary>
    /// 大きなWeight値でも正常に動作することを検証します。
    /// </summary>
    [Fact]
    public void 大きなWeight値_正常に動作()
    {
        // Arrange
        var service = new RandomService();
        var items = new List<RouletteItem>
        {
            new("Item1") { Weight = 1000000 },
            new("Item2") { Weight = 2000000 },
        };

        // Act
        var result = service.SelectByWeight(items);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Name == "Item1" || result.Name == "Item2");
    }
}
