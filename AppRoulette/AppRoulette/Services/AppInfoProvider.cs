using System.Reflection;

namespace AppRoulette.Services
{
    /// <summary>
    /// 実行中のアプリのバージョンなどの情報を取得するクラス。
    /// </summary>
    public static class AppInfoProvider
    {
        /// <summary>アプリ名を取得できない場合に使う名前。</summary>
        private const string DEFAULT_PRODUCT_NAME = "AppRoulette";

        /// <summary>
        /// アプリ名を取得します。
        /// </summary>
        /// <returns>アプリ名。</returns>
        public static string GetProductName() =>
            typeof(AppInfoProvider).Assembly
                .GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? DEFAULT_PRODUCT_NAME;

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
