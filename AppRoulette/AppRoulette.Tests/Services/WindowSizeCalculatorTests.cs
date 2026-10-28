using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="WindowSizeCalculator"/> のテスト。
/// 表示スケールと作業領域に合わせた、ウィンドウの既定の位置とサイズを検証します。
/// </summary>
public class WindowSizeCalculatorTests
{
    /// <summary>表示スケール 100% のときの既定の幅。</summary>
    private const int BASE_WIDTH = 1300;

    /// <summary>表示スケール 100% のときの既定の高さ。</summary>
    private const int BASE_HEIGHT = 850;

    /// <summary>
    /// 作業領域を作成します。
    /// </summary>
    private static WindowPositionInfo CreateWorkArea(int x, int y, int width, int height) =>
        new() { X = x, Y = y, Width = width, Height = height };

    /// <summary>
    /// 表示スケールに合わせてサイズを拡大し、作業領域の中央に置くことを検証します。
    /// </summary>
    /// <param name="scale">表示スケールの倍率。</param>
    /// <param name="expectedWidth">期待する幅。</param>
    /// <param name="expectedHeight">期待する高さ。</param>
    [Theory]
    [InlineData(1.0, 1300, 850)]
    [InlineData(1.25, 1625, 1063)]
    [InlineData(1.5, 1950, 1275)]
    public void CalculateDefaultBounds_表示スケールに合わせて拡大し_作業領域の中央に置く(
        double scale,
        int expectedWidth,
        int expectedHeight)
    {
        // Arrange
        var workArea = CreateWorkArea(0, 0, 3840, 2088);

        // Act
        var bounds = WindowSizeCalculator.CalculateDefaultBounds(
            BASE_WIDTH, BASE_HEIGHT, scale, workArea);

        // Assert
        Assert.Equal(expectedWidth, bounds.Width);
        Assert.Equal(expectedHeight, bounds.Height);
        Assert.Equal((3840 - expectedWidth) / 2, bounds.X);
        Assert.Equal((2088 - expectedHeight) / 2, bounds.Y);
    }

    /// <summary>
    /// 拡大したサイズが作業領域より大きい場合は、作業領域に収めることを検証します。
    /// </summary>
    [Fact]
    public void CalculateDefaultBounds_作業領域より大きい場合_作業領域に収める()
    {
        // Arrange（1920×1080 の画面で表示スケール 150%）
        var workArea = CreateWorkArea(0, 0, 1920, 1032);

        // Act
        var bounds = WindowSizeCalculator.CalculateDefaultBounds(
            BASE_WIDTH, BASE_HEIGHT, 1.5, workArea);

        // Assert
        Assert.Equal(1920, bounds.Width);
        Assert.Equal(1032, bounds.Height);
        Assert.Equal(0, bounds.X);
        Assert.Equal(0, bounds.Y);
    }

    /// <summary>
    /// 表示スケールを取得できなかった（0 の）場合は、100% とみなすことを検証します。
    /// </summary>
    [Fact]
    public void CalculateDefaultBounds_表示スケールが0の場合_100パーセントとみなす()
    {
        // Act
        var bounds = WindowSizeCalculator.CalculateDefaultBounds(
            BASE_WIDTH, BASE_HEIGHT, 0, CreateWorkArea(0, 0, 1920, 1040));

        // Assert
        Assert.Equal(BASE_WIDTH, bounds.Width);
        Assert.Equal(BASE_HEIGHT, bounds.Height);
    }

    /// <summary>
    /// 作業領域の左上が原点でない画面（左側のサブモニターなど）でも、その中央に置くことを検証します。
    /// </summary>
    [Fact]
    public void CalculateDefaultBounds_作業領域の位置がずれている場合_その作業領域の中央に置く()
    {
        // Arrange
        var workArea = CreateWorkArea(-1920, 0, 1920, 1040);

        // Act
        var bounds = WindowSizeCalculator.CalculateDefaultBounds(
            BASE_WIDTH, BASE_HEIGHT, 1.0, workArea);

        // Assert
        Assert.Equal(-1920 + 310, bounds.X);
        Assert.Equal(95, bounds.Y);
    }

    /// <summary>
    /// 現在の位置を指定し、既定のサイズでも作業領域に収まる場合は、位置を変えないことを検証します。
    /// </summary>
    [Fact]
    public void CalculateDefaultBounds_現在の位置で収まる場合_位置を変えない()
    {
        // Act
        var bounds = WindowSizeCalculator.CalculateDefaultBounds(
            BASE_WIDTH, BASE_HEIGHT, 1.0, CreateWorkArea(0, 0, 1920, 1040), 100, 50);

        // Assert
        Assert.Equal(100, bounds.X);
        Assert.Equal(50, bounds.Y);
    }

    /// <summary>
    /// 現在の位置のままでは作業領域からはみ出す場合は、収まる位置までずらすことを検証します。
    /// </summary>
    [Fact]
    public void CalculateDefaultBounds_現在の位置ではみ出す場合_収まる位置にずらす()
    {
        // Act
        var bounds = WindowSizeCalculator.CalculateDefaultBounds(
            BASE_WIDTH, BASE_HEIGHT, 1.0, CreateWorkArea(0, 0, 1920, 1040), 1000, 600);

        // Assert
        Assert.Equal(1920 - BASE_WIDTH, bounds.X);
        Assert.Equal(1040 - BASE_HEIGHT, bounds.Y);
    }
}
