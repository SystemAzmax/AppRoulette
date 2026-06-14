using AppRoulette.Services;
using AppRoulette.ViewModels;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace AppRoulette
{
    /// <summary>
    /// アプリケーションのメインウィンドウ。
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// メイン ViewModel を取得します。
        /// x:Bind のデータソースとして使用されます。
        /// </summary>
        public MainViewModel ViewModel { get; }

        /// <summary>
        /// <see cref="MainWindow"/> を初期化します。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainViewModel(
                new JsonDataPersistenceService(),
                new RandomService());
        }

        /// <summary>
        /// ルート Grid 読み込み完了時にグループデータを初期化します。
        /// </summary>
        /// <param name="sender">イベント発生元。</param>
        /// <param name="e">イベント引数。</param>
        private async void OnRootGridLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeCommand.ExecuteAsync(null);
        }

        /// <summary>
        /// Win2D CanvasControl の描画イベントハンドラー。
        /// フェーズ5で扇形ルーレットの描画ロジックを実装します。
        /// </summary>
        /// <param name="sender">描画対象の <see cref="CanvasControl"/>。</param>
        /// <param name="args">描画セッションを含む引数。</param>
        private void RouletteCanvas_Draw(
            CanvasControl sender,
            CanvasDrawEventArgs args)
        {
            var size = (float)System.Math.Min(sender.ActualWidth, sender.ActualHeight);
            var cx = (float)(sender.ActualWidth / 2);
            var cy = (float)(sender.ActualHeight / 2);
            var radius = size / 2f * 0.92f;

            using var session = args.DrawingSession;

            // プレースホルダー：グレーの円（フェーズ5で扇形に置き換え）
            session.FillCircle(cx, cy, radius,
                Windows.UI.Color.FromArgb(255, 200, 200, 200));
            session.DrawCircle(cx, cy, radius,
                Windows.UI.Color.FromArgb(255, 120, 120, 120), 2f);

            // 中央の案内テキスト
            session.DrawText(
                "クリックして回転させる",
                cx, cy,
                Colors.Black,
                new Microsoft.Graphics.Canvas.Text.CanvasTextFormat
                {
                    FontSize = 20,
                    HorizontalAlignment =
                        Microsoft.Graphics.Canvas.Text.CanvasHorizontalAlignment.Center,
                    VerticalAlignment =
                        Microsoft.Graphics.Canvas.Text.CanvasVerticalAlignment.Center,
                });
        }
    }
}
