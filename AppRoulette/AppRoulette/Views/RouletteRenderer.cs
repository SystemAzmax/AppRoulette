using System.Collections.Generic;
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

    /// <summary>テキスト配置半径比率（外縁部の中央）。</summary>
    private const float TEXT_RADIUS_RATIO = 0.77f;

    /// <summary>中心装飾円の半径比率。</summary>
    private const float CENTER_CIRCLE_RATIO = 0.08f;

    /// <summary>インジケーター（三角形）の高さ（px）。</summary>
    private const float INDICATOR_HEIGHT = 48f;

    /// <summary>インジケーター（三角形）の幅（px）。</summary>
    private const float INDICATOR_WIDTH = 40f;

    /// <summary>インジケーターの色。</summary>
    private static readonly Windows.UI.Color INDICATOR_COLOR =
        Windows.UI.Color.FromArgb(255, 220, 30, 30);

    /// <summary>扇形の境界線の太さ。</summary>
    private const float BORDER_STROKE_WIDTH = 1.5f;

    /// <summary>ルーレット外周線の太さ。</summary>
    private const float OUTER_STROKE_WIDTH = 2f;

    /// <summary>弧を近似する分割数（多いほど滑らか）。</summary>
    private const int ARC_SEGMENTS = 60;

    /// <summary>テキストの最大文字数計算に使う1文字あたりの推定幅（px）。</summary>
    private const float TEXT_CHAR_WIDTH_ESTIMATE = 7f;

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
            DrawIndicator(session, cx, cy, radius);
            return;
        }

        var fontSize = CalcFontSize(items.Count);
        var textRadius = radius * TEXT_RADIUS_RATIO;

        // 全Weight合計を計算
        int totalWeight = 0;
        foreach (var item in items)
        {
            totalWeight += item.Weight;
        }

        // 各アイテムのsweepAngleを計算（Weight比率ベース）
        var currentAngle = rotationAngle - MathF.PI / 2f;

        for (var i = 0; i < items.Count; i++)
        {
            // 現在のアイテムの角度（Weight比率から計算）
            var sweepAngle = (2f * MathF.PI * items[i].Weight) / totalWeight;
            var startAngle = currentAngle;
            var color = SECTOR_COLORS[i % SECTOR_COLORS.Length];

            DrawSector(session, cx, cy, radius, startAngle, sweepAngle, color);
            DrawSectorText(
                session,
                cx, cy, textRadius,
                startAngle, sweepAngle,
                items[i].Name,
                fontSize);

            // 次のアイテムの開始角度を設定
            currentAngle += sweepAngle;
        }

        // 中心の装飾円
        session.FillCircle(
            cx, cy, radius * CENTER_CIRCLE_RATIO, CENTER_CIRCLE_COLOR);

        // ルーレット外周線
        session.DrawCircle(cx, cy, radius, BORDER_COLOR, OUTER_STROKE_WIDTH);

        // インジケーター（3時方向・円の外側から円にかかる赤い三角）
        DrawIndicator(session, cx, cy, radius);
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

        // テキストを扇形の放射方向に沿って左に90°回転
        // 元の放射方向: midAngle + π/2
        // 左に90°回転: -π/2 を追加
        var rotateAngle = midAngle + MathF.PI / 2f - MathF.PI / 2f;  // = midAngle
        var oldTransform = session.Transform;
        session.Transform = Matrix3x2.CreateRotation(
            rotateAngle,
            new Vector2(textX, textY));

        var maxChars = CalcMaxChars(sweepAngle, textRadius);

        // テキストを複数行で表示（最大2行）
        var lines = SplitTextIntoLines(text, maxChars, 2);
        var lineHeight = fontSize * 1.2f;  // 行間を少し開ける
        var totalHeight = lineHeight * lines.Length;
        var startY = textY + totalHeight / 2f - lineHeight / 2f;

        using var textFormat = new CanvasTextFormat
        {
            FontSize = fontSize,
            HorizontalAlignment = CanvasHorizontalAlignment.Right,
            VerticalAlignment = CanvasVerticalAlignment.Center,
        };

        for (var i = 0; i < lines.Length; i++)
        {
            var lineY = startY + i * lineHeight;
            session.DrawText(lines[i], textX, lineY, TEXT_COLOR, textFormat);
        }

        session.Transform = oldTransform;
    }

    /// <summary>
    /// テキストを複数行に分割します。
    /// </summary>
    /// <param name="text">元のテキスト。</param>
    /// <param name="charsPerLine">1行あたりの最大文字数。</param>
    /// <param name="maxLines">最大行数。</param>
    /// <returns>分割されたテキストの行配列。</returns>
    private static string[] SplitTextIntoLines(string text, int charsPerLine, int maxLines)
    {
        if (string.IsNullOrEmpty(text))
            return new[] { "" };

        var lines = new List<string>();
        var remaining = text;

        for (var i = 0; i < maxLines && remaining.Length > 0; i++)
        {
            if (remaining.Length <= charsPerLine)
            {
                lines.Add(remaining);
                break;
            }
            else
            {
                // charsPerLine文字で切り取る
                var line = remaining[..charsPerLine];
                lines.Add(line);
                remaining = remaining[charsPerLine..];
            }
        }

        // 最後の行が満杯だったら省略記号を追加
        if (remaining.Length > 0 && lines.Count > 0)
        {
            var lastLine = lines[lines.Count - 1];
            if (lastLine.Length >= 2)
            {
                lines[lines.Count - 1] = lastLine[..^2] + "…";
            }
            else
            {
                lines[lines.Count - 1] = "…";
            }
        }

        return lines.Count > 0 ? lines.ToArray() : new[] { "" };
    }

    /// <summary>
    /// アイテム未登録時のプレースホルダー円を描画します。
    /// </summary>
    /// <param name="session">描画セッション。</param>
    /// <param name="cx">円の中心 X 座標。</param>
    /// <param name="cy">円の中心 Y 座標。</param>
    /// <param name="radius">半径。</param>
    private static void DrawIndicator(
        CanvasDrawingSession session,
        float cx,
        float cy,
        float radius)
    {
        // 3時方向（右）の円の外縁にかかる三角形を描画
        // 三角形の頂点（右向き）：先端が円の内側、底辺が円の外側
        var tipX = cx + radius - INDICATOR_WIDTH * 0.5f;
        var baseX = cx + radius + INDICATOR_HEIGHT * 0.5f;
        var halfH = INDICATOR_HEIGHT / 2f;

        var p0 = new Vector2(tipX, cy);               // 先端（左・円内側）
        var p1 = new Vector2(baseX, cy - halfH);      // 右上
        var p2 = new Vector2(baseX, cy + halfH);      // 右下

        using var pathBuilder = new CanvasPathBuilder(session);
        pathBuilder.BeginFigure(p0);
        pathBuilder.AddLine(p1);
        pathBuilder.AddLine(p2);
        pathBuilder.EndFigure(CanvasFigureLoop.Closed);

        using var geometry = CanvasGeometry.CreatePath(pathBuilder);
        session.FillGeometry(geometry, INDICATOR_COLOR);
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
