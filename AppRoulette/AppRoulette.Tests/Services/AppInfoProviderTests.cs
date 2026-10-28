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
    /// 本文に、AppRoulette.csproj で設定したアプリ名と著作権表記が入ることを検証します。
    /// </summary>
    [Fact]
    public void CreateAboutMessage_パッケージなしで実行した場合_exe版としてアプリ名と著作権表記を含む()
    {
        // Arrange
        var copyright = typeof(AppInfoProvider).Assembly
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

        // Act
        var message = AppInfoProvider.CreateAboutMessage();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(copyright));
        Assert.StartsWith("AppRoulette\nバージョン ", message);
        Assert.Contains("（exe 版）", message);
        Assert.EndsWith($"\n\n{copyright}", message);
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
}
