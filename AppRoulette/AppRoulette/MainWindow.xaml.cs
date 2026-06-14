using AppRoulette.Services;
using AppRoulette.ViewModels;
using AppRoulette.Views;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace AppRoulette
{
    /// <summary>
    /// アプリケーションのメインウィンドウ。
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // ---------------------------------------------------------------
        // アニメーション定数
        // ---------------------------------------------------------------

        /// <summary>1フレームのインターバル（ミリ秒）。約60fps。</summary>
        private const int TIMER_INTERVAL_MS = 16;

        /// <summary>アニメーションの総回転量（ラジアン）の最小値。</summary>
        private const float MIN_SPIN_RADIANS = MathF.PI * 8f;   // 4周

        /// <summary>アニメーションの総回転量（ラジアン）の最大値。</summary>
        private const float MAX_SPIN_RADIANS = MathF.PI * 14f;  // 7周

        /// <summary>アニメーション総時間（秒）。</summary>
        private const float SPIN_DURATION_SEC = 3.5f;

        // ---------------------------------------------------------------
        // アニメーション状態
        // ---------------------------------------------------------------

        /// <summary>現在の回転角度（ラジアン）。</summary>
        private float _rotationAngle;

        /// <summary>アニメーション開始時点の回転角度（ラジアン）。</summary>
        private float _spinStartAngle;

        /// <summary>アニメーションの総回転量（ラジアン）。</summary>
        private float _spinTotalRadians;

        /// <summary>アニメーション経過時間（秒）。</summary>
        private float _spinElapsedSec;

        /// <summary>フレーム更新用タイマー。</summary>
        private readonly DispatcherTimer _spinTimer;

        // ---------------------------------------------------------------
        // ViewModel
        // ---------------------------------------------------------------

        /// <summary>
        /// メイン ViewModel を取得します。
        /// x:Bind のデータソースとして使用されます。
        /// </summary>
        public MainViewModel ViewModel { get; }

        // ---------------------------------------------------------------
        // コンストラクター
        // ---------------------------------------------------------------

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
                                   or nameof(ViewModel.SelectedGroup))
                {
                    _rotationAngle = 0f;
                    RouletteCanvas.Invalidate();
                }
            };

            _spinTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(TIMER_INTERVAL_MS),
            };
            _spinTimer.Tick += OnSpinTimerTick;
        }

        // ---------------------------------------------------------------
        // 初期化
        // ---------------------------------------------------------------

        /// <summary>
        /// ルート Grid 読み込み完了時にグループデータを初期化します。
        /// </summary>
        /// <param name="sender">イベント発生元。</param>
        /// <param name="e">イベント引数。</param>
        private async void OnRootGridLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.InitializeCommand.ExecuteAsync(null);
        }

        // ---------------------------------------------------------------
        // 描画
        // ---------------------------------------------------------------

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
                items,
                _rotationAngle);
        }

        // ---------------------------------------------------------------
        // スピン開始（タップ）
        // ---------------------------------------------------------------

        /// <summary>
        /// ルーレットキャンバスのタップイベントハンドラー。
        /// アイテムが存在し回転中でない場合にアニメーションを開始します。
        /// </summary>
        /// <param name="sender">タップされたキャンバス。</param>
        /// <param name="e">タップイベント引数。</param>
        private void RouletteCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!ViewModel.SpinCommand.CanExecute(null))
            {
                return;
            }

            // ランダムな出目を ViewModel に決定させる
            ViewModel.SpinCommand.Execute(null);

            StartSpinAnimation();
        }

        /// <summary>
        /// アニメーション状態を初期化してタイマーを開始します。
        /// </summary>
        private void StartSpinAnimation()
        {
            ViewModel.IsSpinning = true;

            _spinStartAngle = _rotationAngle;
            _spinElapsedSec = 0f;

            // 目的アングル：選択されたアイテムが 12時（上・-π/2）に来るように調整
            var targetAngle = CalcTargetAngle(
                ViewModel.SelectedItemIndex,
                ViewModel.SelectedGroup?.Items.Count ?? 1);

            // 最低 MIN_SPIN_RADIANS 以上の回転を加える
            var rawDelta = targetAngle - _spinStartAngle;
            while (rawDelta < MIN_SPIN_RADIANS)
            {
                rawDelta += MathF.PI * 2f;
            }

            // 最大を MAX_SPIN_RADIANS に収める（超えた分は 2π で切り捨て）
            while (rawDelta > MAX_SPIN_RADIANS)
            {
                rawDelta -= MathF.PI * 2f;
            }

            _spinTotalRadians = rawDelta;
            _spinTimer.Start();
        }

        // ---------------------------------------------------------------
        // アニメーションタイマー
        // ---------------------------------------------------------------

        /// <summary>
        /// タイマー Tick ごとにイージングで角度を進め、完了時に結果を表示します。
        /// </summary>
        /// <param name="sender">タイマー。</param>
        /// <param name="e">引数（未使用）。</param>
        private async void OnSpinTimerTick(object? sender, object e)
        {
            _spinElapsedSec += TIMER_INTERVAL_MS / 1000f;

            var t = System.Math.Min(_spinElapsedSec / SPIN_DURATION_SEC, 1f);
            var eased = EaseOutCubic((float)t);

            _rotationAngle = _spinStartAngle + _spinTotalRadians * eased;
            RouletteCanvas.Invalidate();

            if (t >= 1f)
            {
                _spinTimer.Stop();
                ViewModel.IsSpinning = false;
                await ShowResultDialogAsync();
            }
        }

        // ---------------------------------------------------------------
        // 結果ダイアログ
        // ---------------------------------------------------------------

        /// <summary>
        /// 選択結果を <see cref="ContentDialog"/> で表示します。
        /// </summary>
        private async System.Threading.Tasks.Task ShowResultDialogAsync()
        {
            var index = ViewModel.SelectedItemIndex;
            var items = ViewModel.SelectedGroup?.Items;
            if (index < 0 || items is null || index >= items.Count)
            {
                return;
            }

            var selectedName = items[index].Name;

            var dialog = new ContentDialog
            {
                Title = "🎉 結果",
                Content = $"「{selectedName}」が選ばれました！",
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot,
            };

            await dialog.ShowAsync();
        }

        // ---------------------------------------------------------------
        // ユーティリティ
        // ---------------------------------------------------------------

        /// <summary>
        /// ease-out 3次関数（減速停止）を計算します。
        /// </summary>
        /// <param name="t">正規化時間（0.0〜1.0）。</param>
        /// <returns>イージング後の値（0.0〜1.0）。</returns>
        private static float EaseOutCubic(float t)
        {
            var f = 1f - t;
            return 1f - f * f * f;
        }

        /// <summary>
        /// 選択インデックスのアイテムが 12時位置（インジケーター）を向く
        /// 目標回転角度（ラジアン）を返します。
        /// </summary>
        /// <param name="selectedIndex">選択されたアイテムのインデックス。</param>
        /// <param name="totalItems">全アイテム数。</param>
        /// <returns>目標角度（ラジアン）。</returns>
        private static float CalcTargetAngle(int selectedIndex, int totalItems)
        {
            if (totalItems <= 0)
            {
                return 0f;
            }

            var sweepAngle = MathF.PI * 2f / totalItems;

            // インジケーターは 3時方向（右端中央）→ π/2 を加算して補正
            // 扇形の中心が 3時方向を向くように targetAngle を決定
            // Draw では startAngle = rotationAngle + sweepAngle*i - π/2 で描くため
            // インジケーターが 3時（角度 0）を指すように計算する:
            //   rotationAngle + sweepAngle*selectedIndex + sweepAngle/2 - π/2 = 0
            //   => rotationAngle = π/2 - sweepAngle*(selectedIndex + 0.5)
            return MathF.PI / 2f - sweepAngle * (selectedIndex + 0.5f);
        }
    }
}
