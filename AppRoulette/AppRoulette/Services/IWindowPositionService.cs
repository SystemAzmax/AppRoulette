namespace AppRoulette.Services
{
    /// <summary>
    /// ウィンドウの位置情報を管理するサービスのインターフェース。
    /// </summary>
    public interface IWindowPositionService
    {
        /// <summary>
        /// ウィンドウの位置情報を取得します。
        /// </summary>
        /// <returns>
        /// ウィンドウの位置情報（X, Y, Width, Height）。
        /// 保存されたデータがない場合は null を返します。
        /// </returns>
        WindowPositionInfo? GetWindowPosition();

        /// <summary>
        /// ウィンドウの位置情報を保存します。
        /// </summary>
        /// <param name="position">保存するウィンドウの位置情報。</param>
        Task SaveWindowPositionAsync(WindowPositionInfo position);
    }

    /// <summary>
    /// ウィンドウの位置情報を表すクラス。
    /// </summary>
    public class WindowPositionInfo
    {
        /// <summary>
        /// ウィンドウの X 座標を取得または設定します。
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// ウィンドウの Y 座標を取得または設定します。
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// ウィンドウの幅を取得または設定します。
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// ウィンドウの高さを取得または設定します。
        /// </summary>
        public int Height { get; set; }
    }
}
