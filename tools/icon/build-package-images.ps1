<#
.SYNOPSIS
    透明背景の円形アイコンから、ストア版のパッケージ画像を作ります。

.DESCRIPTION
    build-icon.ps1 で作った icon-master-1024.png から、Package.appxmanifest で使う画像
    （アプリのアイコン、ストアのロゴ、タイル、スプラッシュ画面）を、表示スケールや
    大きさの違いごとに作ります。アイコンの描き方は build-icon.ps1 と同じです。
    タイルとスプラッシュ画面は、画像の高さの半分の大きさのアイコンを中央に置きます。

    System.Drawing を使うため、Windows PowerShell 5.1 で実行します。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\icon\build-package-images.ps1 -MasterPath $env:TEMP\AppRouletteIcon\icon-master-1024.png -PreviewPath $env:TEMP\AppRouletteIcon\preview-package.png
#>
param(
    # build-icon.ps1 で作った、透明背景の 1024px の画像
    [Parameter(Mandatory)]
    [string]$MasterPath,

    # 出力先のフォルダー。省略した場合はストア版のパッケージ画像のフォルダー
    [string]$OutDir,

    # 確認用のプレビュー画像の保存先（省略した場合は作らない）
    [string]$PreviewPath
)

$ErrorActionPreference = 'Stop'

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class PackageImageBuilder
{
    public static int OutlineAlpha = 80;

    static Bitmap ResizeOnce(Bitmap src, int w, int h, int x, int y, int canvas)
    {
        var dst = new Bitmap(canvas, canvas, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(dst))
        using (var attr = new ImageAttributes())
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            attr.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(src, new Rectangle(x, y, w, h), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
        }
        return dst;
    }

    // exe のアイコン（build-icon.ps1）と同じ描き方: 半分ずつ縮小し、少し余白を空けて円を置き、細い輪郭線を付ける
    public static Bitmap CreateIconImage(Bitmap master, int size)
    {
        int margin = (int)Math.Round(size / 32.0, MidpointRounding.ToEven);
        int diameter = size - margin * 2;
        Bitmap current = master;
        while (current.Width / 2 >= diameter * 2)
        {
            int half = current.Width / 2;
            var next = ResizeOnce(current, half, half, 0, 0, half);
            if (!ReferenceEquals(current, master)) current.Dispose();
            current = next;
        }
        var result = ResizeOnce(current, diameter, diameter, margin, margin, size);
        if (!ReferenceEquals(current, master)) current.Dispose();

        using (var g = Graphics.FromImage(result))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float w = Math.Max(1f, size / 64f);
            using (var pen = new Pen(Color.FromArgb(OutlineAlpha, 70, 90, 130), w))
            {
                g.DrawEllipse(pen, margin + w / 2, margin + w / 2, diameter - w, diameter - w);
            }
        }
        return result;
    }

    static Bitmap ToStraight(Bitmap img)
    {
        var straight = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(straight))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImage(img, 0, 0, img.Width, img.Height);
        }
        return straight;
    }

    // 指定の大きさのアイコンを、透明な画像の中央に置く（拡大縮小はせず、画素をそのまま写す）
    public static Bitmap CreateCentered(Bitmap master, int width, int height, int iconSize)
    {
        var canvas = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var icon = CreateIconImage(master, iconSize))
        using (var straight = ToStraight(icon))
        {
            int ox = (width - iconSize) / 2, oy = (height - iconSize) / 2;
            var src = straight.LockBits(new Rectangle(0, 0, iconSize, iconSize), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var dst = canvas.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var row = new byte[iconSize * 4];
            for (int y = 0; y < iconSize; y++)
            {
                Marshal.Copy(src.Scan0 + y * src.Stride, row, 0, row.Length);
                Marshal.Copy(row, 0, dst.Scan0 + (oy + y) * dst.Stride + ox * 4, row.Length);
            }
            straight.UnlockBits(src);
            canvas.UnlockBits(dst);
        }
        return canvas;
    }

    public static Bitmap CreateSquare(Bitmap master, int size)
    {
        using (var icon = CreateIconImage(master, size))
        {
            return ToStraight(icon);
        }
    }

    // 確認用: 画像を白・濃い灰色・薄い灰色の背景に並べる（64px 以下は最近傍で 4 倍に拡大）
    public static void SavePreview(string path, string[] files)
    {
        int pad = 12;
        var bg = new[] { Color.White, Color.FromArgb(32, 32, 32), Color.FromArgb(243, 243, 243) };
        var images = new List<Bitmap>();
        foreach (var f in files) images.Add(new Bitmap(f));
        int width = pad, cellH = 0;
        foreach (var img in images)
        {
            int z = img.Width <= 64 ? 4 : 1;
            width += img.Width * z + pad * 3;
            cellH = Math.Max(cellH, img.Height * z + pad * 2);
        }
        int height = pad + bg.Length * (cellH + pad);
        using (var preview = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(preview))
        {
            g.Clear(Color.FromArgb(128, 128, 128));
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            for (int r = 0; r < bg.Length; r++)
            {
                int x0 = pad, y0 = pad + r * (cellH + pad);
                foreach (var img in images)
                {
                    int z = img.Width <= 64 ? 4 : 1;
                    int cw = img.Width * z + pad * 2;
                    using (var b = new SolidBrush(bg[r])) g.FillRectangle(b, x0, y0, cw, cellH);
                    g.DrawImage(img, new Rectangle(x0 + pad, y0 + (cellH - img.Height * z) / 2, img.Width * z, img.Height * z));
                    x0 += cw + pad;
                }
            }
            preview.Save(path, ImageFormat.Png);
        }
        foreach (var img in images) img.Dispose();
    }
}
'@

# Windows PowerShell 5.1 では param の既定値で $PSScriptRoot を使えないため、ここで決める
if (-not $OutDir) {
    $OutDir = Join-Path $PSScriptRoot '..\..\AppRoulette\AppRoulette (Package)\Images'
}

# System.Drawing は PowerShell の現在のフォルダーを基準にしないため、フルパスにする
$MasterPath =$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($MasterPath)
$OutDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutDir)
if ($PreviewPath) {
    $PreviewPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PreviewPath)
}

$png = [System.Drawing.Imaging.ImageFormat]::Png
$master = [System.Drawing.Bitmap]::FromFile($MasterPath)
New-Item -ItemType Directory -Force $OutDir | Out-Null

function Scaled([int]$base, [int]$scale) {
    [int][Math]::Round($base * $scale / 100.0, [MidpointRounding]::AwayFromZero)
}

function Save([System.Drawing.Bitmap]$img, [string]$name) {
    $img.Save((Join-Path $OutDir $name), $png)
    $img.Dispose()
}

# タイルとスプラッシュ画面のアイコンの大きさ: 画像の高さの半分
# （上下の余白が同じになるよう、画像の高さとの差を偶数にする）
function TileIconSize([int]$width, [int]$height) {
    $s = [int][Math]::Round($height / 2.0, [MidpointRounding]::AwayFromZero)
    if (($height - $s) % 2 -ne 0) { $s += 1 }
    $s
}

$scales = 100, 125, 150, 200, 400

# アプリのアイコン（Square44x44Logo）: 表示スケールごとの画像と、大きさごとの画像（プレートあり・なし・ライト テーマ用）
foreach ($sc in $scales) {
    Save ([PackageImageBuilder]::CreateSquare($master, (Scaled 44 $sc))) "Square44x44Logo.scale-$sc.png"
}
foreach ($t in 16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256) {
    foreach ($suffix in '', '_altform-unplated', '_altform-lightunplated') {
        Save ([PackageImageBuilder]::CreateSquare($master, $t)) "Square44x44Logo.targetsize-$t$suffix.png"
    }
}

# パッケージのロゴ（StoreLogo）
foreach ($sc in $scales) {
    Save ([PackageImageBuilder]::CreateSquare($master, (Scaled 50 $sc))) "StoreLogo.scale-$sc.png"
}

# 中タイル、ワイド タイル、スプラッシュ画面
$canvases = @(
    @{ Name = 'Square150x150Logo'; W = 150; H = 150 },
    @{ Name = 'Wide310x150Logo'; W = 310; H = 150 },
    @{ Name = 'SplashScreen'; W = 620; H = 300 }
)
foreach ($c in $canvases) {
    foreach ($sc in $scales) {
        $w = Scaled $c.W $sc
        $h = Scaled $c.H $sc
        Save ([PackageImageBuilder]::CreateCentered($master, $w, $h, (TileIconSize $w $h))) ("{0}.scale-{1}.png" -f $c.Name, $sc)
    }
}
$master.Dispose()

if ($PreviewPath) {
    $names = 'Square44x44Logo.targetsize-16_altform-unplated.png', 'Square44x44Logo.targetsize-24_altform-unplated.png',
        'Square44x44Logo.targetsize-32_altform-unplated.png', 'Square44x44Logo.targetsize-48_altform-unplated.png',
        'StoreLogo.scale-100.png', 'Square150x150Logo.scale-100.png', 'Wide310x150Logo.scale-100.png', 'SplashScreen.scale-100.png'
    [PackageImageBuilder]::SavePreview($PreviewPath, @($names | ForEach-Object { Join-Path $OutDir $_ }))
}
"作成しました: $OutDir"
