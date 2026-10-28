namespace AppRoulette.Services;

/// <summary>
/// 「このアプリについて」に表示する文を作成するクラスです。
/// </summary>
public static class AppAboutInfo
{
    /// <summary>
    /// バージョンを表示用の文字列にします。
    /// ストアに出すパッケージは 4 つ目の数字（リビジョン）が 0 のため、0 の場合は省きます。
    /// </summary>
    /// <param name="version">表示するバージョン。</param>
    /// <returns>「1.0.0」や「1.0.0.1」の形のバージョン。</returns>
    public static string FormatVersion(Version version)
    {
        var build = Math.Max(version.Build, 0);
        return version.Revision > 0
            ? $"{version.Major}.{version.Minor}.{build}.{version.Revision}"
            : $"{version.Major}.{version.Minor}.{build}";
    }

    /// <summary>
    /// 「このアプリについて」の本文を作成します。
    /// </summary>
    /// <param name="productName">アプリ名。</param>
    /// <param name="version">アプリのバージョン。</param>
    /// <param name="isPackaged">ストア版（パッケージあり）の場合は <c>true</c>、exe 版の場合は <c>false</c>。</param>
    /// <param name="copyright">著作権表記。無い場合は null。</param>
    /// <returns>アプリ名・バージョン・著作権表記を含む本文。</returns>
    public static string CreateMessage(
        string productName,
        Version version,
        bool isPackaged,
        string? copyright)
    {
        var edition = isPackaged ? "ストア版" : "exe 版";
        var message = $"{productName}\nバージョン {FormatVersion(version)}（{edition}）";
        return string.IsNullOrWhiteSpace(copyright)
            ? message
            : $"{message}\n\n{copyright}";
    }
}
