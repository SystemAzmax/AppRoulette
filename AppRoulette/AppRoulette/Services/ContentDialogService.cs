using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AppRoulette.Services
{
    /// <summary>
    /// <see cref="ContentDialog"/> を表示して、利用者への確認やメッセージの表示を行うサービス。
    /// </summary>
    public class ContentDialogService : IDialogService
    {
        /// <summary>キャンセルボタンの表示テキスト。</summary>
        private const string CANCEL_BUTTON_TEXT = "キャンセル";

        /// <summary>メッセージを閉じるボタンの表示テキスト。</summary>
        private const string OK_BUTTON_TEXT = "OK";

        /// <summary>ダイアログを表示する XamlRoot を取得する関数。</summary>
        private readonly Func<XamlRoot?> _getXamlRoot;

        /// <summary>
        /// <see cref="ContentDialogService"/> を初期化します。
        /// </summary>
        /// <param name="getXamlRoot">
        /// ダイアログを表示する XamlRoot を取得する関数。
        /// XamlRoot はウィンドウの読み込み後に決まるため、表示のたびに呼び出します。
        /// </param>
        public ContentDialogService(Func<XamlRoot?> getXamlRoot)
        {
            _getXamlRoot = getXamlRoot;
        }

        /// <inheritdoc />
        public async Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmButtonText)
        {
            var dialog = CreateDialog(title, message);
            if (dialog is null)
            {
                // ダイアログを表示できない場合は、安全のため実行しない
                return false;
            }

            dialog.PrimaryButtonText = confirmButtonText;
            dialog.CloseButtonText = CANCEL_BUTTON_TEXT;
            // Enter キーで誤って実行しないよう、既定のボタンはキャンセルにする
            dialog.DefaultButton = ContentDialogButton.Close;

            try
            {
                var result = await dialog.ShowAsync();
                return result == ContentDialogResult.Primary;
            }
            catch (Exception ex)
            {
                // 別のダイアログが開いているなどで表示できない場合は、安全のため実行しない
                ApplicationLogger.LogError("確認ダイアログ表示", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task ShowMessageAsync(string title, string message)
        {
            var dialog = CreateDialog(title, message);
            if (dialog is null)
            {
                return;
            }

            dialog.CloseButtonText = OK_BUTTON_TEXT;
            dialog.DefaultButton = ContentDialogButton.Close;

            try
            {
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError("メッセージダイアログ表示", ex);
            }
        }

        /// <summary>
        /// タイトルと本文を設定したダイアログを作成します。
        /// </summary>
        /// <param name="title">ダイアログのタイトル。</param>
        /// <param name="message">ダイアログの本文。</param>
        /// <returns>作成したダイアログ。表示先が無い場合は null。</returns>
        private ContentDialog? CreateDialog(string title, string message)
        {
            var xamlRoot = _getXamlRoot();
            if (xamlRoot is null)
            {
                return null;
            }

            return new ContentDialog
            {
                Title = title,
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                },
                XamlRoot = xamlRoot,
            };
        }
    }
}
