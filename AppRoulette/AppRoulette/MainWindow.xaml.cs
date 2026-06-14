using AppRoulette.Services;
using AppRoulette.ViewModels;
using AppRoulette.Views;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ViewModel.ItemCount)
                                   or nameof(ViewModel.SelectedGroup)
                                   or nameof(ViewModel.SelectedItemIndex))
                {
                    RouletteCanvas.Invalidate();
                }
            };
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
        /// <see cref="RouletteRenderer"/> に描画処理を委譲します。
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
            var radius = size / 2f * RouletteRenderer.RADIUS_RATIO;

            var items = (IReadOnlyList<AppRoulette.Models.RouletteItem>?)
                        ViewModel.SelectedGroup?.Items
                        ?? System.Array.Empty<AppRoulette.Models.RouletteItem>();

            RouletteRenderer.Draw(
                args.DrawingSession,
                cx, cy, radius,
                items);
        }

        /// <summary>
        /// ルーレットキャンバスのタップイベントハンドラー。
        /// アイテムが存在する場合にスピンコマンドを実行します。
        /// </summary>
        /// <param name="sender">タップされたキャンバス。</param>
        /// <param name="e">タップイベント引数。</param>
        private void RouletteCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.SpinCommand.CanExecute(null))
            {
                ViewModel.SpinCommand.Execute(null);
            }
        }
    }
}
