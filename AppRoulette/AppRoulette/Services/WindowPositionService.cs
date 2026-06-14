using System.Text.Json;

namespace AppRoulette.Services
{
    /// <summary>
    /// ウィンドウの位置情報をファイルに保存・読込するサービス。
    /// </summary>
    public class WindowPositionService : IWindowPositionService
    {
        /// <summary>ファイル名。</summary>
        private const string POSITION_FILE_NAME = "window_position.json";

        /// <summary>位置情報ファイルの保存先フォルダを取得します。</summary>
        private static string PositionFolderPath =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "AppRoulette");

        /// <summary>位置情報ファイルのフルパスを取得します。</summary>
        private static string PositionFilePath =>
            Path.Combine(PositionFolderPath, POSITION_FILE_NAME);

        /// <summary>
        /// ウィンドウの位置情報を取得します。
        /// </summary>
        /// <returns>
        /// ウィンドウの位置情報。ファイルが存在しない場合は null。
        /// </returns>
        public WindowPositionInfo? GetWindowPosition()
        {
            try
            {
                if (!File.Exists(PositionFilePath))
                {
                    return null;
                }

                string json = File.ReadAllText(PositionFilePath);
                return JsonSerializer.Deserialize<WindowPositionInfo>(json);
            }
            catch (Exception)
            {
                // ファイル読込エラーの場合は null を返す
                return null;
            }
        }

        /// <summary>
        /// ウィンドウの位置情報を保存します。
        /// </summary>
        /// <param name="position">保存するウィンドウの位置情報。</param>
        public async Task SaveWindowPositionAsync(WindowPositionInfo position)
        {
            try
            {
                // フォルダが存在しない場合は作成
                if (!Directory.Exists(PositionFolderPath))
                {
                    Directory.CreateDirectory(PositionFolderPath);
                }

                // JSON にシリアライズ
                string json = JsonSerializer.Serialize(position);

                // ファイルに保存
                await File.WriteAllTextAsync(PositionFilePath, json);
            }
            catch (Exception)
            {
                // ファイル保存エラーは無視
            }
        }
    }
}
