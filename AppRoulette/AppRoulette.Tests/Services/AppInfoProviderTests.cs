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
        // Act
        var message = AppInfoProvider.CreateAboutMessage();

        // Assert
        Assert.StartsWith("AppRoulette\nバージョン ", message);
        Assert.Contains("（exe 版）", message);
        Assert.Contains("© 2026 azmax", message);
    }
}
