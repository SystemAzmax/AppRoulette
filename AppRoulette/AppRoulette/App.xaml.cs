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
            catch
            {
                // アイコン生成に失敗しても続行
            }
        }

        /// <summary>
        /// アプリケーションアイコンを生成して保存します。
        /// </summary>
        private void GenerateApplicationIcon()
        {
            string assetsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets");
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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();

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
            catch
            {
                // アイコン設定に失敗しても続行
            }

            _window.Activate();
        }
    }
}
