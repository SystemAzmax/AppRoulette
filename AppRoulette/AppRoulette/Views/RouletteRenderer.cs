using System.Numerics;
using AppRoulette.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;

namespace AppRoulette.Views;

/// <summary>
/// Win2D を使用してルーレット円を描画するユーティリティクラスです。
/// </summary>
internal static class RouletteRenderer
{
    /// <summary>円の半径比率（CanvasControl サイズに対する割合）。</summary>
    internal const float RADIUS_RATIO = 0.92f;

    /// <summary>テキスト配置半径比率。</summary>
    private const float TEXT_RADIUS_RATIO = 0.62f;

    /// <summary>中心装飾円の半径比率。</summary>
    private const float CENTER_CIRCLE_RATIO = 0.08f;

    /// <summary>扇形の境界線の太さ。</summary>
    private const float BORDER_STROKE_WIDTH = 1.5f;

    /// <summary>ルーレット外周線の太さ。</summary>
    private const float OUTER_STROKE_WIDTH = 2f;

    /// <summary>弧を近似する分割数（多いほど滑らか）。</summary>
    private const int ARC_SEGMENTS = 60;

    /// <summary>テキストの最大文字数計算に使う1文字あたりの推定幅（px）。</summary>
    private const float TEXT_CHAR_WIDTH_ESTIMATE = 11f;

    /// <summary>扇形に使用するカラーパレット（12色サイクル）。</summary>
    private static readonly Windows.UI.Color[] SECTOR_COLORS =
    {
        Windows.UI.Color.FromArgb(255, 255,  99,  71),  // tomato
        Windows.UI.Color.FromArgb(255, 255, 165,   0),  // orange
        Windows.UI.Color.FromArgb(255, 255, 215,   0),  // gold
        Windows.UI.Color.FromArgb(255, 144, 238, 144),  // lightgreen
        Windows.UI.Color.FromArgb(255, 135, 206, 235),  // skyblue
        Windows.UI.Color.FromArgb(255, 147, 112, 219),  // mediumpurple
        Windows.UI.Color.FromArgb(255, 255, 182, 193),  // lightpink
        Windows.UI.Color.FromArgb(255,  64, 224, 208),  // turquoise
        Windows.UI.Color.FromArgb(255, 255, 140,   0),  // darkorange
        Windows.UI.Color.FromArgb(255,   0, 180,   0),  // green
        Windows.UI.Color.FromArgb(255,  30, 144, 255),  // dodgerblue
        Windows.UI.Color.FromArgb(255, 220,  20,  60),  // crimson
    };

    private static readonly Windows.UI.Color BORDER_COLOR =
        Windows.UI.Color.FromArgb(255, 80, 80, 80);

    private static readonly Windows.UI.Color TEXT_COLOR =
        Windows.UI.Color.FromArgb(255, 20, 20, 20);

    private static readonly Windows.UI.Color PLACEHOLDER_FILL =
        Windows.UI.Color.FromArgb(255, 200, 200, 200);

    private static readonly Windows.UI.Color PLACEHOLDER_BORDER =
        Windows.UI.Color.FromArgb(255, 120, 120, 120);

    private static readonly Windows.UI.Color PLACEHOLDER_TEXT =
        Windows.UI.Color.FromArgb(255, 80, 80, 80);

    private static readonly Windows.UI.Color CENTER_CIRCLE_COLOR =
        Windows.UI.Color.FromArgb(255, 60, 60, 60);

    // ---------------------------------------------------------------
    // 公開 API
    // ---------------------------------------------------------------

    /// <summary>
    /// ルーレット円全体を描画します。
    /// アイテムが 0 件の場合はプレースホルダーを表示します。
    /// </summary>
    /// <param name="session">Win2D 描画セッション。</param>
    /// <param name="cx">円の中心 X 座標（px）。</param>
    /// <param name="cy">円の中心 Y 座標（px）。</param>
    /// <param name="radius">円の半径（px）。</param>
    /// <param name="items">描画するアイテム一覧。</param>
    /// <param name="rotationAngle">
    /// 現在の回転角度（ラジアン）。フェーズ6のアニメーション用。
    /// </param>
    public static void Draw(
        CanvasDrawingSession session,
        float cx,
        float cy,
        float radius,
        IReadOnlyList<RouletteItem> items,
        float rotationAngle = 0f)
    {
        if (items.Count == 0)
        {
            DrawPlaceholder(session, cx, cy, radius);
            return;
        }

        var sweepAngle = 2f * MathF.PI / items.Count;
        var fontSize = CalcFontSize(items.Count);
        var textRadius = radius * TEXT_RADIUS_RATIO;

        for (var i = 0; i < items.Count; i++)
        {
            var startAngle = rotationAngle + sweepAngle * i - MathF.PI / 2f;
            var color = SECTOR_COLORS[i % SECTOR_COLORS.Length];

            DrawSector(session, cx, cy, radius, startAngle, sweepAngle, color);
            DrawSectorText(
                session,
                cx, cy, textRadius,
                startAngle, sweepAngle,
                items[i].Name,
                fontSize);
        }

        // 中心の装飾円
        session.FillCircle(
            cx, cy, radius * CENTER_CIRCLE_RATIO, CENTER_CIRCLE_COLOR);

        // ルーレット外周線
        session.DrawCircle(cx, cy, radius, BORDER_COLOR, OUTER_STROKE_WIDTH);
    }

    // ---------------------------------------------------------------
    // 内部メソッド
    // ---------------------------------------------------------------

    /// <summary>
    /// 1 つの扇形を多角形近似で塗りつぶし描画します。
    /// </summary>
    /// <param name="session">描画セッション。</param>
    /// <param name="cx">円の中心 X 座標。</param>
    /// <param name="cy">円の中心 Y 座標。</param>
    /// <param name="radius">半径。</param>
    /// <param name="startAngle">開始角度（ラジアン）。</param>
    /// <param name="sweepAngle">掃引角度（ラジアン）。</param>
    /// <param name="color">塗りつぶし色。</param>
    private static void DrawSector(
        CanvasDrawingSession session,
        float cx,
        float cy,
        float radius,
        float startAngle,
        float sweepAngle,
        Windows.UI.Color color)
    {
        using var pathBuilder = new CanvasPathBuilder(session);

        pathBuilder.BeginFigure(cx, cy);

        for (var s = 0; s <= ARC_SEGMENTS; s++)
        {
            var angle = startAngle + sweepAngle * (float)s / ARC_SEGMENTS;
            pathBuilder.AddLine(
                cx + radius * MathF.Cos(angle),
                cy + radius * MathF.Sin(angle));
        }

        pathBuilder.EndFigure(CanvasFigureLoop.Closed);

        using var geometry = CanvasGeometry.CreatePath(pathBuilder);
        session.FillGeometry(geometry, color);
        session.DrawGeometry(geometry, BORDER_COLOR, BORDER_STROKE_WIDTH);
    }

    /// <summary>
    /// 扇形中央にアイテム名を回転テキストで描画します。
    /// </summary>
    /// <param name="session">描画セッション。</param>
    /// <param name="cx">円の中心 X 座標。</param>
    /// <param name="cy">円の中心 Y 座標。</param>
    /// <param name="textRadius">テキスト配置半径。</param>
    /// <param name="startAngle">扇形の開始角度（ラジアン）。</param>
    /// <param name="sweepAngle">扇形の掃引角度（ラジアン）。</param>
    /// <param name="text">表示するテキスト。</param>
    /// <param name="fontSize">フォントサイズ（pt）。</param>
    private static void DrawSectorText(
        CanvasDrawingSession session,
        float cx,
        float cy,
        float textRadius,
        float startAngle,
        float sweepAngle,
        string text,
        float fontSize)
    {
        var midAngle = startAngle + sweepAngle / 2f;
        var textX = cx + textRadius * MathF.Cos(midAngle);
        var textY = cy + textRadius * MathF.Sin(midAngle);

        // テキストを扇形の放射方向に沿って90°回転
        var rotateAngle = midAngle + MathF.PI / 2f;
        var oldTransform = session.Transform;
        session.Transform = Matrix3x2.CreateRotation(
            rotateAngle,
            new Vector2(textX, textY));

        var maxChars = CalcMaxChars(sweepAngle, textRadius);
        var displayText = text.Length > maxChars
            ? text[..maxChars] + "…"
            : text;

        using var textFormat = new CanvasTextFormat
        {
            FontSize = fontSize,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center,
        };

        session.DrawText(displayText, textX, textY, TEXT_COLOR, textFormat);
        session.Transform = oldTransform;
    }

    /// <summary>
    /// アイテム未登録時のプレースホルダー円を描画します。
    /// </summary>
    /// <param name="session">描画セッション。</param>
    /// <param name="cx">円の中心 X 座標。</param>
    /// <param name="cy">円の中心 Y 座標。</param>
    /// <param name="radius">半径。</param>
    private static void DrawPlaceholder(
        CanvasDrawingSession session,
        float cx,
        float cy,
        float radius)
    {
        session.FillCircle(cx, cy, radius, PLACEHOLDER_FILL);
        session.DrawCircle(cx, cy, radius, PLACEHOLDER_BORDER, OUTER_STROKE_WIDTH);

        using var textFormat = new CanvasTextFormat
        {
            FontSize = 18,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center,
        };

        session.DrawText(
            "クリックして回転させる",
            cx, cy,
            PLACEHOLDER_TEXT,
            textFormat);
    }

    /// <summary>
    /// アイテム数に応じたフォントサイズを返します。
    /// </summary>
    /// <param name="itemCount">アイテム数。</param>
    /// <returns>フォントサイズ（pt）。</returns>
    private static float CalcFontSize(int itemCount) =>
        itemCount <= 5  ? 16f :
        itemCount <= 10 ? 13f :
        itemCount <= 20 ? 10f : 8f;

    /// <summary>
    /// 扇形の弧長から表示可能な最大文字数を推定します。
    /// </summary>
    /// <param name="sweepAngle">掃引角度（ラジアン）。</param>
    /// <param name="textRadius">テキスト配置半径。</param>
    /// <returns>最大文字数（最低1文字）。</returns>
    private static int CalcMaxChars(float sweepAngle, float textRadius)
    {
        var arcLength = sweepAngle * textRadius;
        return Math.Max(1, (int)(arcLength / TEXT_CHAR_WIDTH_ESTIMATE));
    }
}
