using AppRoulette.Views;

namespace AppRoulette.Tests.Views;

/// <summary>
/// <see cref="RouletteRenderer"/> のユニットテストクラスです。
/// </summary>
public class RouletteRendererTests
{
    [Fact]
    public void SplitTextIntoLines_1行に収まる場合_省略せずにそのまま返す()
    {
        var lines = RouletteRenderer.SplitTextIntoLines("読書", 10, 2);

        Assert.Equal(new[] { "読書" }, lines);
    }

    [Fact]
    public void SplitTextIntoLines_2行に分ければ収まる場合_省略せずに分ける()
    {
        var lines = RouletteRenderer.SplitTextIntoLines("映画を見る", 3, 2);

        Assert.Equal(new[] { "映画を", "見る" }, lines);
    }

    [Fact]
    public void SplitTextIntoLines_最大行数に収まらない場合_最後の行の末尾を省略記号にする()
    {
        var lines = RouletteRenderer.SplitTextIntoLines("カフェでのんびり", 3, 2);

        Assert.Equal(new[] { "カフェ", "で…" }, lines);
    }

    [Fact]
    public void SplitTextIntoLines_空文字の場合_空の1行を返す()
    {
        var lines = RouletteRenderer.SplitTextIntoLines(string.Empty, 3, 2);

        Assert.Equal(new[] { string.Empty }, lines);
    }
}
