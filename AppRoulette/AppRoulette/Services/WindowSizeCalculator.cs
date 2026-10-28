namespace AppRoulette.Services
{
    /// <summary>
    /// 表示スケールと画面の作業領域に合わせて、ウィンドウの既定の位置とサイズを計算するクラス。
    /// 座標とサイズは、いずれも画面のピクセル（物理ピクセル）で扱います。
    /// </summary>
    public static class WindowSizeCalculator
    {
        /// <summary>
        /// ウィンドウの既定の位置とサイズを計算します。
        /// サイズは表示スケールに合わせて拡大し、作業領域より大きい場合は作業領域に収めます。
        /// 位置は、現在の位置が指定されていればその位置を作業領域に収まるようにずらし、
        /// 指定されていなければ作業領域の中央にします。
        /// </summary>
        /// <param name="baseWidth">表示スケール 100% のときの幅。</param>
        /// <param name="baseHeight">表示スケール 100% のときの高さ。</param>
        /// <param name="scale">表示スケールの倍率（125% なら 1.25）。0 以下の場合は 1 とみなします。</param>
        /// <param name="workArea">ウィンドウを表示する画面の作業領域（タスクバーを除いた領域）。</param>
        /// <param name="currentX">現在のウィンドウの X 座標。中央に置く場合は null。</param>
        /// <param name="currentY">現在のウィンドウの Y 座標。中央に置く場合は null。</param>
        /// <returns>ウィンドウの位置とサイズ。</returns>
        public static WindowPositionInfo CalculateDefaultBounds(
            int baseWidth,
            int baseHeight,
            double scale,
            WindowPositionInfo workArea,
            int? currentX = null,
            int? currentY = null)
        {
            var effectiveScale = scale > 0 ? scale : 1.0;
            var width = Math.Min(ScaleSize(baseWidth, effectiveScale), workArea.Width);
            var height = Math.Min(ScaleSize(baseHeight, effectiveScale), workArea.Height);

            var x = currentX.HasValue
                ? Math.Clamp(currentX.Value, workArea.X, workArea.X + workArea.Width - width)
                : workArea.X + (workArea.Width - width) / 2;
            var y = currentY.HasValue
                ? Math.Clamp(currentY.Value, workArea.Y, workArea.Y + workArea.Height - height)
                : workArea.Y + (workArea.Height - height) / 2;

            return new WindowPositionInfo
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            };
        }

        /// <summary>
        /// サイズに表示スケールの倍率を掛け、小数点以下を四捨五入します。
        /// </summary>
        /// <param name="size">表示スケール 100% のときのサイズ。</param>
        /// <param name="scale">表示スケールの倍率。</param>
        /// <returns>拡大したサイズ。</returns>
        private static int ScaleSize(int size, double scale) =>
            (int)Math.Round(size * scale, MidpointRounding.AwayFromZero);
    }
}
