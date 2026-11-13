using System.Globalization;
using System.Reflection;
using AppRoulette.Services;

namespace AppRoulette.Tests.Services;

/// <summary>
/// <see cref="AppInfoProvider"/> のテスト。
/// テストはパッケージなしで実行されるため、exe 版と同じ動きになることを検証します。
/// </summary>
public class AppInfoProviderTests
{
    /// <summary>
    /// パッケージなしで実行した場合は、例外にならず、
    /// アセンブリのバージョンと exe 版であることを返すことを検証します。
    /// </summary>
    [Fact]
    public void GetVersion_パッケージなしで実行した場合_アセンブリのバージョンを返す()
    {
        // Act
        var (version, isPackaged) = AppInfoProvider.GetVersion();

        // Assert
        Assert.False(isPackaged);
        Assert.Equal(typeof(AppInfoProvider).Assembly.GetName().Version, version);
    }

    /// <summary>
    /// 本文に、表示言語に合ったアプリ名と、AppRoulette.csproj で設定した著作権表記が入ることを検証します。
    /// </summary>
    /// <param name="cultureName">表示言語。</param>
    /// <param name="expectedName">期待するアプリ名。</param>
    [Theory]
    [InlineData("ja-JP", "かんたんルーレット")]
    [InlineData("en-US", "AppRoulette")]
    public void CreateAboutMessage_パッケージなしで実行した場合_exe版としてアプリ名と著作権表記を含む(
        string cultureName,
        string expectedName)
    {
        // Arrange
        var copyright = typeof(AppInfoProvider).Assembly
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

        try
        {
            // Act
            var message = AppInfoProvider.CreateAboutMessage();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(copyright));
            Assert.StartsWith($"{expectedName}\nバージョン ", message);
            Assert.Contains("（exe 版）", message);
            Assert.EndsWith($"\n\n{copyright}", message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    /// ライセンス表示のファイルが、アプリと同じフォルダーに出力されていることを検証します。
    /// </summary>
    [Fact]
    public void ThirdPartyNoticesPath_ビルド出力_ライセンス表示のファイルがある()
    {
        // Act
        var path = AppInfoProvider.ThirdPartyNoticesPath;

        // Assert
        Assert.True(File.Exists(path), $"ファイルがありません: {path}");
        Assert.Contains("SQLitePCLRaw", File.ReadAllText(path));
    }

    /// <summary>
    /// 使い方ガイドの URL が、このアプリのリポジトリの docs フォルダーのガイドを指すことを検証します。
    /// 日本語版と英語版のどちらになるかは、テストを実行する PC の表示言語によります。
    /// </summary>
    [Fact]
    public void UserGuideUrl_リポジトリのdocsフォルダー_使い方ガイドを指す()
    {
        // Act
        var url = AppInfoProvider.UserGuideUrl;

        // Assert
        Assert.StartsWith("https://github.com/SystemAzmax/AppRoulette/blob/", url);
        Assert.Matches(@"/docs/UserGuide(_ja)?\.md$", url);
    }
}
