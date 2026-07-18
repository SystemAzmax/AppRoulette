using AppRoulette.Models;
using AppRoulette.Services;
using AppRoulette.ViewModels;
using AppRoulette.Views;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Microsoft.UI;
using System.Linq;

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

        // ---------------------------------------------------------------
        // アニメーション状態
        // ---------------------------------------------------------------

        /// <summary>フレーム更新用タイマー。</summary>
        private readonly DispatcherTimer _spinTimer;

        /// <summary>
        /// テキストボックスの手動同期中かを示すフラグ。
        /// この間は OnItemsTextBoxTextChanged を無視します。
        /// </summary>
        private bool _isUpdatingTextBox;

        /// <summary>
        /// ユーザー入力中かを示すフラグ。
        /// この間は ItemsText 変更時の TextBox 更新を無視します。
        /// </summary>
        private bool _isUserInput;

        /// <summary>ウィンドウ位置情報を管理するサービス。</summary>
        private IWindowPositionService _positionService;

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
                new RandomService(),
                new SqliteItemRepository(),
                new SqliteGroupRepository(),
                new JsonDataPersistenceService());

            ViewModel.PropertyChanged += (_, e) =>
            {
                try
                {
                    // ルーレット再描画：アイテム数またはグループ変更時
                    if (e.PropertyName is nameof(ViewModel.ItemCount)
                                       or nameof(ViewModel.SelectedGroup))
                    {
                        RouletteCanvas?.Invalidate();
                    }

                    // ItemsText 変更時もルーレットを再描画
                    if (e.PropertyName == nameof(ViewModel.ItemsText))
                    {
                        RouletteCanvas?.Invalidate();
                    }

                    // グループ名未保存状態の視覚的フィードバック
                    if (e.PropertyName == nameof(ViewModel.IsGroupNameUnsaved))
                    {
                        UpdateGroupNameUnsavedVisualState();
                    }
                }
                catch (Exception ex)
                {
                    ApplicationLogger.LogError("PropertyChanged ハンドラ", ex);
                }
            };

            _spinTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(TIMER_INTERVAL_MS),
            };
            _spinTimer.Tick += OnSpinTimerTick;

            try
            {
                // ウィンドウサイズを1300×850に設定
                ExtendsContentIntoTitleBar = false;
                AppWindow.Resize(new Windows.Graphics.SizeInt32(1300, 850));
                UpdateGroupNameUnsavedVisualState();

                // ウィンドウ位置情報を読み込んで、保存されているなら適用
                _positionService = new WindowPositionService();
                var savedPosition = _positionService.GetWindowPosition();

                if (savedPosition != null)
                {
                    // 保存された位置がある場合は、Activated イベント時に適用
                    Activated += (sender, args) =>
                    {
                        if (args.WindowActivationState != WindowActivationState.Deactivated)
                        {
                            AppWindow.Move(new PointInt32(savedPosition.X, savedPosition.Y));
                        }
                    };
                }

                // ウィンドウを閉じる際に位置を保存
                Closed += async (sender, args) =>
                {
                    try
                    {
                        var placement = AppWindow.Position;
                        var currentPosition = new WindowPositionInfo
                        {
                            X = placement.X,
                            Y = placement.Y,
                            Width = AppWindow.Size.Width,
                            Height = AppWindow.Size.Height,
                        };
                        await _positionService.SaveWindowPositionAsync(currentPosition);
                    }
                    catch (Exception ex)
                    {
                        ApplicationLogger.LogError("ウィンドウ位置保存", ex);
                    }
                };
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("ウィンドウ初期化", ex);
            }
        }

        /// <summary>
        /// グループ名入力欄の未保存状態を表示に反映します。
        /// </summary>
        private void UpdateGroupNameUnsavedVisualState()
        {
            if (GroupNameUnsavedBorder is null || GroupNameUnsavedText is null)
            {
                return;
            }

            if (ViewModel.IsGroupNameUnsaved)
            {
                GroupNameUnsavedBorder.BorderBrush = new SolidColorBrush(Colors.DarkOrange);
                GroupNameUnsavedText.Opacity = 1;
                return;
            }

            GroupNameUnsavedBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            GroupNameUnsavedText.Opacity = 0;
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
        // グループ選択
        // ---------------------------------------------------------------

        /// <summary>
        /// グループ ComboBox の選択変更イベントハンドラー。
        /// ViewModel を確実に更新します。
        /// </summary>
        /// <param name="sender">ComboBox。</param>
        /// <param name="e">イベント引数。</param>
        private void GroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Models.RouletteGroup group)
            {
                ViewModel.SelectedGroup = group;
            }
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
                ViewModel.SpinAnimation.RotationAngle);
        }

        // ---------------------------------------------------------------
        // スピン開始（タップ）
        // ---------------------------------------------------------------

        /// <summary>
        /// ルーレットキャンバスのタップイベントハンドラー。
        /// ルーレット円内をタップし、かつアイテムが存在し回転中でない場合にアニメーションを開始します。
        /// </summary>
        /// <param name="sender">タップされたキャンバス。</param>
        /// <param name="e">タップイベント引数。</param>
        private void RouletteCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!ViewModel.SpinCommand.CanExecute(null))
            {
                return;
            }

            // タップ位置を取得
            var tapPoint = e.GetPosition(sender as UIElement);
            var cx = RouletteCanvas.ActualWidth / 2;
            var cy = RouletteCanvas.ActualHeight / 2;
            var size = (float)System.Math.Min(RouletteCanvas.ActualWidth, RouletteCanvas.ActualHeight);
            var radius = size / 2f * RouletteRenderer.RADIUS_RATIO;

            // タップ位置から円の中心までの距離を計算
            var dx = tapPoint.X - cx;
            var dy = tapPoint.Y - cy;
            var distance = System.Math.Sqrt(dx * dx + dy * dy);

            // 円内のみで反応
            if (distance > radius)
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

            var spinAnimation = ViewModel.SpinAnimation;
            spinAnimation.SpinStartAngle = spinAnimation.RotationAngle;
            spinAnimation.SpinElapsedSec = 0f;

            // 目的角度：選択されたアイテムがインジケーター位置に来るように調整
            var targetAngle = MainViewModel.CalcTargetAngle(
                ViewModel.SelectedItemIndex,
                ViewModel.SelectedGroup?.Items);

            // 最低 MIN_SPIN_RADIANS 以上の回転を加える
            var rawDelta = targetAngle - spinAnimation.SpinStartAngle;
            while (rawDelta < SpinAnimationViewModel.MIN_SPIN_RADIANS)
            {
                rawDelta += MathF.PI * 2f;
            }

            // 最大を MAX_SPIN_RADIANS に収める（超えた分は 2π で切り捨て）
            while (rawDelta > SpinAnimationViewModel.MAX_SPIN_RADIANS)
            {
                rawDelta -= MathF.PI * 2f;
            }

            spinAnimation.SpinTotalRadians = rawDelta;
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
            var spinAnimation = ViewModel.SpinAnimation;
            spinAnimation.AdvanceAnimation(TIMER_INTERVAL_MS);

            RouletteCanvas.Invalidate();

            if (spinAnimation.IsAnimationComplete)
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

            // テキストボックス内でアイテムをハイライト選択
            HighlightItemInTextBox(index);

            // ボタンを中央に配置するためのカスタムコンテンツ
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var textBlock = new TextBlock
            {
                Text = $"「{selectedName}」が選ばれました！",
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
            };

            var button = new Button
            {
                Content = "OK",
                Width = 120,
                Height = 44,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 0, 120, 212)), // WinUI 青色 (#0078D4)
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 255, 255, 255)), // 白色
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14,
            };

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(button);

            var dialog = new ContentDialog
            {
                Title = "🎉 結果",
                Content = stackPanel,
                XamlRoot = Content.XamlRoot,
                CloseButtonText = string.Empty,
            };

            button.Click += (sender, e) =>
            {
                dialog.Hide();
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// テキストボックス内で指定インデックスのアイテムを選択状態にします。
        /// </summary>
        /// <param name="itemIndex">選択するアイテムのインデックス。</param>
        private void HighlightItemInTextBox(int itemIndex)
        {
            if (itemIndex < 0 || ItemsTextBox is null)
            {
                return;
            }

            // テキストボックスのテキストを取得
            string text = ItemsTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // 1行ごとに分割してインデックスのアイテムを見つける
            // StringSplitOptions.RemoveEmptyEntries で空行を除去
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, 
                                   StringSplitOptions.RemoveEmptyEntries);

            if (itemIndex >= lines.Length)
            {
                return;
            }

            // 現在のインデックスのアイテムのテキスト位置を計算
            // 元のテキスト内で実際の位置を探す
            int lineCount = 0;
            int startPosition = 0;
            int i = 0;

            while (i < text.Length && lineCount < itemIndex)
            {
                if (i + 1 < text.Length && text[i] == '\r' && text[i + 1] == '\n')
                {
                    i += 2; // \r\n をスキップ
                    lineCount++;
                }
                else if (text[i] == '\n' || text[i] == '\r')
                {
                    i += 1; // \n または \r をスキップ
                    lineCount++;
                }
                else
                {
                    i++;
                }
            }

            startPosition = i;

            // テキストボックスでそのアイテムを選択状態にする
            ItemsTextBox.Focus(FocusState.Programmatic);
            ItemsTextBox.Select(startPosition, lines[itemIndex].Length);
        }

        /// <summary>
        /// ContentDialogのボタンパネルを見つけます。
        /// </summary>
        private StackPanel? FindButtonsPanel(DependencyObject parent)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is StackPanel stackPanel)
                {
                    // ボタンを含むStackPanelを探す
                    if (stackPanel.Children.Any(c => c is Button))
                    {
                        return stackPanel;
                    }
                }

                var result = FindButtonsPanel(child);
                if (result != null)
                    return result;
            }

            return null;
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
        /// 表形式編集 UI の名前入力変更を ViewModel に反映します。
        /// </summary>
        /// <param name="sender">テキストボックス。</param>
        /// <param name="e">イベント引数。</param>
        private void OnEditableItemTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.DataContext is not RouletteItem item)
            {
                return;
            }

            ViewModel.SelectedEditableItem = item;
            if (item.Name != textBox.Text)
            {
                item.Name = textBox.Text;
            }
        }

        /// <summary>
        /// 表形式編集 UI の Weight 入力変更を ViewModel に反映します。
        /// </summary>
        /// <param name="sender">NumberBox。</param>
        /// <param name="e">イベント引数。</param>
        private void OnEditableItemWeightChanged(NumberBox sender, NumberBoxValueChangedEventArgs e)
        {
            if (sender.DataContext is not RouletteItem item || double.IsNaN(sender.Value))
            {
                return;
            }

            ViewModel.SelectedEditableItem = item;
            var weight = (int)Math.Round(sender.Value);
            if (item.Weight != weight)
            {
                item.Weight = weight;
            }
        }

        /// <summary>
        /// アイテム入力テキストボックスのテキスト変更イベントハンドラー。
        /// WinUI 3 の TextBox は Enter で \r のみ挿入するため、\n に正規化して ViewModel に通知します。
        /// グループ選択時の自動同期中は無視し、ユーザー入力中は TextBox の再更新を防ぎます。
        /// </summary>
        /// <param name="sender">テキストボックス。</param>
        /// <param name="e">イベント引数。</param>
        private void OnItemsTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // グループ選択時の TextBox 自動更新中は無視
            if (_isUpdatingTextBox)
            {
                return;
            }

            if (sender is TextBox textBox)
            {
                // ユーザー入力中フラグを立てて、PropertyChanged ハンドラーの TextBox 更新を防ぐ
                _isUserInput = true;
                try
                {
                    // \r\n -> \n -> \r の順に正規化（WinUI3 TextBox は \r のみ使用）
                    var normalized = textBox.Text
                        .Replace("\r\n", "\n")
                        .Replace('\r', '\n');
                    ViewModel.ItemsText = normalized;
                }
                finally
                {
                    _isUserInput = false;
                }
            }
        }
    }
}
