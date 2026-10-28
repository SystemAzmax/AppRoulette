using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AppRoulette.Services
{
    /// <summary>
    /// <see cref="ContentDialog"/> を表示して、利用者に操作の実行を確認するサービス。
    /// </summary>
    public class ContentDialogConfirmationService : IConfirmationService
    {
        /// <summary>キャンセルボタンの表示テキスト。</summary>
        private const string CANCEL_BUTTON_TEXT = "キャンセル";

        /// <summary>ダイアログを表示する XamlRoot を取得する関数。</summary>
        private readonly Func<XamlRoot?> _getXamlRoot;

        /// <summary>
        /// <see cref="ContentDialogConfirmationService"/> を初期化します。
        /// </summary>
        /// <param name="getXamlRoot">
        /// ダイアログを表示する XamlRoot を取得する関数。
        /// XamlRoot はウィンドウの読み込み後に決まるため、表示のたびに呼び出します。
        /// </param>
        public ContentDialogConfirmationService(Func<XamlRoot?> getXamlRoot)
        {
            _getXamlRoot = getXamlRoot;
        }

        /// <inheritdoc />
        public async Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmButtonText)
        {
            var xamlRoot = _getXamlRoot();
            if (xamlRoot is null)
            {
                // ダイアログを表示できない場合は、安全のため実行しない
                return false;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                },
                PrimaryButtonText = confirmButtonText,
                CloseButtonText = CANCEL_BUTTON_TEXT,
                // Enter キーで誤って実行しないよう、既定のボタンはキャンセルにする
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = xamlRoot,
            };

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
    }
}
