using System.Globalization;
using System.Reflection;

namespace AppRoulette.Services
{
    /// <summary>
    /// 実行中のアプリのバージョンなどの情報を取得するクラス。
    /// </summary>
    public static class AppInfoProvider
    {
        /// <summary>使用しているソフトウェアのライセンス表示のファイル名。</summary>
        private const string THIRD_PARTY_NOTICES_FILE_NAME = "ThirdPartyNotices.txt";

        /// <summary>使い方ガイド（docs/UserGuide.md、docs/UserGuide_ja.md）のあるフォルダーの URL。</summary>
        private const string USER_GUIDE_BASE_URL = "https://github.com/SystemAzmax/AppRoulette/blob/main/docs";

        /// <summary>
        /// 使用しているソフトウェアのライセンス表示のファイルのパスを取得します。
        /// ファイルはアプリと同じフォルダーに配置されます。
        /// </summary>
        public static string ThirdPartyNoticesPath =>
            Path.Combine(AppContext.BaseDirectory, THIRD_PARTY_NOTICES_FILE_NAME);

        /// <summary>
        /// 表示言語（日本語なら日本語版、それ以外は英語版）の使い方ガイドの URL を取得します。
        /// </summary>
        public static string UserGuideUrl =>
            AppAboutInfo.CreateUserGuideUrl(USER_GUIDE_BASE_URL, CultureInfo.CurrentUICulture);

        /// <summary>
        /// アプリ名を取得します。
        /// 表示言語が日本語の場合は日本語名、それ以外の場合は英語名を返します。
        /// </summary>
        /// <returns>アプリ名。</returns>
        public static string GetProductName() => AppDisplayName.GetCurrent();

        /// <summary>
        /// アプリのバージョンと、ストア版（パッケージあり）かどうかを取得します。
        /// ストア版は Package.appxmanifest のバージョン、
        /// exe 版は AppRoulette.csproj の Version から作られるアセンブリのバージョンを返します。
        /// </summary>
        /// <returns>バージョンと、ストア版かどうか。</returns>
        public static (Version Version, bool IsPackaged) GetVersion()
        {
            try
            {
                var version = Windows.ApplicationModel.Package.Current.Id.Version;
                return (
                    new Version(version.Major, version.Minor, version.Build, version.Revision),
                    true);
            }
            catch (Exception)
            {
                // exe 版（パッケージなし）では Package.Current を使えないため、
                // アセンブリのバージョンを使う
                var assemblyVersion = typeof(AppInfoProvider).Assembly.GetName().Version
                    ?? new Version(0, 0, 0, 0);
                return (assemblyVersion, false);
            }
        }

        /// <summary>
        /// 「このアプリについて」の本文を作成します。
        /// </summary>
        /// <returns>アプリ名・バージョン・著作権表記を含む本文。</returns>
        public static string CreateAboutMessage()
        {
            var (version, isPackaged) = GetVersion();
            var copyright = typeof(AppInfoProvider).Assembly
                .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

            return AppAboutInfo.CreateMessage(
                GetProductName(),
                version,
                isPackaged,
                copyright);
        }
    }
}
