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
        /// <summary>エラーのログファイルの名前。</summary>
        private const string LOG_FILE_NAME = "error.log";

        /// <summary>予期しない例外を利用者に知らせるときの見出し。</summary>
        private const string UNEXPECTED_ERROR_TITLE = "予期しないエラーが発生しました";

        private MainWindow? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            // リリース版でも原因を調べられるよう、エラーをファイルにも記録する
            ApplicationLogger.Configure(GetLogFilePath());

            UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
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
                _window.ViewModel.ShowError(
                    "データベースを開けませんでした",
                    "リストとアイテムの保存先を準備できませんでした。"
                    + "変更が保存されない場合があります。アプリを再起動してください。"
                    + $"\n\n詳細: {ex.Message}");
            }

            // ウィンドウアイコンを設定（exe と同じ ico を出力フォルダーから読み込む）
            try
            {
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Copilot_20260614_202528.ico");
                if (File.Exists(iconPath))
                {
                    _window.AppWindow.SetIcon(iconPath);
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("ウィンドウアイコン設定", ex);
            }

            _window.Activate();
        }

        /// <summary>
        /// エラーのログファイルのパスを取得します。
        /// ストア版は、パッケージの LocalState に置きます。
        /// %LOCALAPPDATA% の下に置くと MSIX が別の場所へリダイレクトするため、
        /// 「ログを開く」でメモ帳などに渡したパスでは開けなくなるためです。
        /// </summary>
        /// <returns>ログファイルのフルパス。</returns>
        private static string GetLogFilePath()
        {
            string folder;
            try
            {
                folder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            }
            catch (Exception)
            {
                // exe 版（パッケージなし）では ApplicationData を使えないため、データと同じフォルダーに置く
                folder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AppRoulette");
            }

            return System.IO.Path.Combine(folder, "logs", LOG_FILE_NAME);
        }

        /// <summary>
        /// UI スレッドで処理されなかった例外を記録し、利用者に知らせます。
        /// アプリを終了させずに、操作を続けられるようにします。
        /// </summary>
        /// <param name="sender">イベント発生元。</param>
        /// <param name="e">イベント引数。</param>
        private void OnUnhandledException(
            object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            ApplicationLogger.LogError("予期しない例外", e.Exception);

            if (_window is null)
            {
                // ウィンドウを作る前は知らせる先が無いため、終了に任せる
                return;
            }

            e.Handled = true;
            _window.ViewModel.ShowError(
                UNEXPECTED_ERROR_TITLE,
                "完了していない操作がある可能性があります。"
                + "動作がおかしい場合は、アプリを再起動してください。"
                + $"\n\n詳細: {e.Exception.Message}");
        }

        /// <summary>
        /// UI スレッド以外で処理されなかった例外を記録します。
        /// この場合はアプリが終了するため、利用者には知らせられません。
        /// </summary>
        /// <param name="sender">イベント発生元。</param>
        /// <param name="e">イベント引数。</param>
        private static void OnDomainUnhandledException(
            object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                ApplicationLogger.LogError("予期しない例外（アプリ終了）", exception);
            }
        }

        /// <summary>
        /// 待機されずに失敗したタスクの例外を記録します。
        /// </summary>
        /// <param name="sender">イベント発生元。</param>
        /// <param name="e">イベント引数。</param>
        private static void OnUnobservedTaskException(
            object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ApplicationLogger.LogError("待機されなかったタスクの例外", e.Exception);
            e.SetObserved();
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
