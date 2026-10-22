using AppRoulette.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppRoulette
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            // アプリケーションアイコンを初期化
            try
            {
                GenerateApplicationIcon();
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("アプリケーションアイコン生成", ex);
            }
        }

        /// <summary>
        /// アプリケーションアイコンを生成して保存します。
        /// </summary>
        private void GenerateApplicationIcon()
        {            string assetsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets");
            string iconPath = System.IO.Path.Combine(assetsPath, "app.ico");

            // Assetsフォルダが存在しなければ作成
            if (!Directory.Exists(assetsPath))
            {
                Directory.CreateDirectory(assetsPath);
            }

            // アイコンが既に存在する場合はスキップ
            if (File.Exists(iconPath))
            {
                return;
            }

            // ルーレットアイコンを生成
            byte[] iconData = Services.AppIconService.GenerateRouletteIcon(256);

            // ファイルに保存
            File.WriteAllBytes(iconPath, iconData);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // ウィンドウを最初に作成
            _window = new MainWindow();

            // 旧パス（パッケージ化アプリの ApplicationData）から
            // 新パス（%LOCALAPPDATA%\AppRoulette）へデータベースを移行
            try
            {
                MigrateLegacyDatabase();
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("データベース移行", ex);
            }

            // データベースを初期化（ウィンドウ作成後に実行）
            try
            {
                // 同期的に初期化を実行
                await DatabaseInitializer.InitializeAsync();
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("データベース初期化", ex);
            }

            // ウィンドウアイコンを設定
            try
            {
                // 複数のアイコン候補を試す
                string[] iconPaths = new[]
                {
                    System.IO.Path.Combine(AppContext.BaseDirectory, "Copilot_20260614_202528.ico"),
                    System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"),
                };

                foreach (string iconPath in iconPaths)
                {
                    if (File.Exists(iconPath))
                    {
                        _window.AppWindow.SetIcon(iconPath);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("ウィンドウアイコン設定", ex);
            }

            _window.Activate();
        }

        /// <summary>
        /// パッケージ化アプリの旧データフォルダー（ApplicationData.Current.LocalFolder）
        /// に roulette.db が存在し、新データフォルダー（%LOCALAPPDATA%\AppRoulette）に
        /// まだ存在しない場合、旧ファイルを新パスへコピーします。
        /// </summary>
        private static void MigrateLegacyDatabase()
        {
            const string DB_FILE_NAME = "roulette.db";

            string newPath = DatabaseInitializer.GetDatabasePath();

            // 新パスに既にデータベースが存在する場合は移行不要
            if (File.Exists(newPath))
            {
                return;
            }

            string? legacyPath = null;
            try
            {
                legacyPath = System.IO.Path.Combine(
                    Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                    DB_FILE_NAME);
            }
            catch (Exception ex)
            {
                // パッケージ化されていない実行環境では ApplicationData が
                // 利用できないため、移行をスキップする
                ApplicationLogger.LogError("旧データベースパス取得", ex);
                return;
            }

            if (legacyPath is null || !File.Exists(legacyPath))
            {
                return;
            }

            string newDir = System.IO.Path.GetDirectoryName(newPath)!;
            Directory.CreateDirectory(newDir);
            File.Copy(legacyPath, newPath);
        }
    }
}
