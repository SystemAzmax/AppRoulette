using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="AppAboutInfo"/> のテスト。
/// 「このアプリについて」に表示するバージョンと本文を検証します。
/// </summary>
public class AppAboutInfoTests
{
    /// <summary>
    /// バージョンの 4 つ目の数字が 0 の場合は省き、0 以外の場合は表示することを検証します。
    /// </summary>
    /// <param name="version">表示するバージョン。</param>
    /// <param name="expected">期待する表示。</param>
    [Theory]
    [InlineData("1.0.0.0", "1.0.0")]
    [InlineData("1.2.3.0", "1.2.3")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("1.2", "1.2.0")]
    public void FormatVersion_バージョン_リビジョンが0なら省いて表示する(
        string version,
        string expected)
    {
        // Act
        var text = AppAboutInfo.FormatVersion(Version.Parse(version));

        // Assert
        Assert.Equal(expected, text);
    }

    /// <summary>
    /// ストア版の本文に、アプリ名・バージョン・版の種類・著作権表記が入ることを検証します。
    /// </summary>
    [Fact]
    public void CreateMessage_ストア版の場合_バージョンと著作権表記を含む()
    {
        // Act
        var message = AppAboutInfo.CreateMessage(
            "AppRoulette",
            new Version(1, 2, 3, 0),
            isPackaged: true,
            copyright: "© 2026 azmax");

        // Assert
        Assert.Equal("AppRoulette\nバージョン 1.2.3（ストア版）\n\n© 2026 azmax", message);
    }

    /// <summary>
    /// exe 版で著作権表記が無い場合は、版の種類を exe 版とし、著作権表記の行を出さないことを検証します。
    /// </summary>
    [Fact]
    public void CreateMessage_exe版で著作権表記が無い場合_著作権表記の行を出さない()
    {
        // Act
        var message = AppAboutInfo.CreateMessage(
            "AppRoulette",
            new Version(1, 0, 0, 0),
            isPackaged: false,
            copyright: null);

        // Assert
        Assert.Equal("AppRoulette\nバージョン 1.0.0（exe 版）", message);
    }
}
