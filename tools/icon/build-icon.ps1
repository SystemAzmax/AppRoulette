<#
.SYNOPSIS
    アイコンの元画像から、透明背景の円形アイコンを作ります。

.DESCRIPTION
    元画像（背景あり、1024px）の円の部分を切り抜いて真円に伸ばし、外側を透明にした
    1024px の画像（icon-master-1024.png）を作ります。
    そこから、exe とウィンドウ用の ico（AppRoulette.ico、16～256px の 10 サイズ）と、
    確認用のプレビュー画像（preview-small.png、preview-large.png）を作ります。
    作った ico をアプリで使う手順は docs\BuildGuide.md を参照してください。

    System.Drawing を使うため、Windows PowerShell 5.1 で実行します。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\icon\build-icon.ps1 -OutDir $env:TEMP\AppRouletteIcon
#>
param(
    # アイコンの元画像（背景あり、1024px）。省略した場合は docs\Copilot_20260614_202528.png
    [string]$Source,

    # 出力先のフォルダー（無ければ作る）
    [Parameter(Mandatory)]
    [string]$OutDir,

    # 元画像の円はわずかに横長の楕円のため、中心と横・縦の半径で指定する
    [double]$Cx = 512.4,
    [double]$Cy = 493.25,
    [double]$Rx = 347.9,
    [double]$Ry = 338.25,

    # 縁の外側の色が混ざらないよう、円の縁から内側に切り抜く幅（元画像のピクセル）
    [double]$Inset = 1.5
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

public static class IconBuilder
{
    public static int OutlineAlpha = 80;

    // 元画像から楕円を切り抜いて真円に伸ばし、外側を透明にする
    public static Bitmap CreateMaster(Bitmap source, double cx, double cy, double rx, double ry, int size, double insetSourcePx)
    {
        var master = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(master))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            var src = new RectangleF((float)(cx - rx), (float)(cy - ry), (float)(2 * rx), (float)(2 * ry));
            g.DrawImage(source, new RectangleF(0, 0, size, size), src, GraphicsUnit.Pixel);
        }

        double scale = size / (2 * rx);
        double radius = size / 2.0 - insetSourcePx * scale;
        double c = size / 2.0;
        var data = master.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var bytes = new byte[data.Stride * size];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < 4; sy++)
                {
                    for (int sx = 0; sx < 4; sx++)
                    {
                        double px = x + (sx + 0.5) / 4.0 - c;
                        double py = y + (sy + 0.5) / 4.0 - c;
                        if (px * px + py * py <= radius * radius) inside++;
                    }
                }
                int i = y * data.Stride + x * 4;
                bytes[i + 3] = (byte)(255 * inside / 16);
            }
        }
        Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        master.UnlockBits(data);
        return master;
    }

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

    // 半分ずつ縮小してから、少し余白を空けて円を配置する
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

        // 明るい背景でも白い縁が見えるよう、半透明の細い輪郭線を付ける
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

    static byte[] ReadBgra(Bitmap img)
    {
        int s = img.Width;
        var data = img.LockBits(new Rectangle(0, 0, s, s), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var px = new byte[s * s * 4];
        for (int y = 0; y < s; y++) Marshal.Copy(data.Scan0 + y * data.Stride, px, y * s * 4, s * 4);
        img.UnlockBits(data);
        return px;
    }

    static byte[] EncodeDib(Bitmap img)
    {
        int s = img.Width;
        var px = ReadBgra(img);
        int maskStride = ((s + 31) / 32) * 4;
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            w.Write(40); w.Write(s); w.Write(s * 2); w.Write((ushort)1); w.Write((ushort)32);
            w.Write(0); w.Write(s * s * 4 + maskStride * s); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            for (int y = s - 1; y >= 0; y--) w.Write(px, y * s * 4, s * 4);
            for (int y = s - 1; y >= 0; y--)
            {
                var row = new byte[maskStride];
                for (int x = 0; x < s; x++)
                {
                    if (px[(y * s + x) * 4 + 3] == 0) row[x / 8] |= (byte)(0x80 >> (x % 8));
                }
                w.Write(row);
            }
            return ms.ToArray();
        }
    }

    static byte[] EncodePng(Bitmap img)
    {
        using (var straight = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(straight))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImage(img, 0, 0, img.Width, img.Height);
            }
            using (var ms = new MemoryStream())
            {
                straight.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }

    // 256px は PNG、それより小さいサイズは BMP 形式で ico に格納する
    public static void SaveIco(string path, Bitmap[] images)
    {
        var entries = new List<byte[]>();
        foreach (var img in images) entries.Add(img.Width >= 256 ? EncodePng(img) : EncodeDib(img));
        using (var fs = File.Create(path))
        using (var w = new BinaryWriter(fs))
        {
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)images.Length);
            int offset = 6 + 16 * images.Length;
            for (int i = 0; i < images.Length; i++)
            {
                int s = images[i].Width;
                w.Write((byte)(s >= 256 ? 0 : s)); w.Write((byte)(s >= 256 ? 0 : s));
                w.Write((byte)0); w.Write((byte)0);
                w.Write((ushort)1); w.Write((ushort)32);
                w.Write(entries[i].Length); w.Write(offset);
                offset += entries[i].Length;
            }
            foreach (var e in entries) w.Write(e);
        }
    }

    // 確認用: 各サイズを白・濃い灰色・薄い灰色の背景に並べる（64px 以下は最近傍で 4 倍に拡大）
    public static void SavePreview(string path, Bitmap[] images)
    {
        int cell = 280, pad = 12;
        var bg = new[] { Color.White, Color.FromArgb(32, 32, 32), Color.FromArgb(243, 243, 243) };
        int width = pad + images.Length * (cell + pad);
        int height = pad + bg.Length * (cell + pad);
        using (var preview = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(preview))
        {
            g.Clear(Color.FromArgb(128, 128, 128));
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            for (int r = 0; r < bg.Length; r++)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    int x0 = pad + i * (cell + pad), y0 = pad + r * (cell + pad);
                    using (var b = new SolidBrush(bg[r])) g.FillRectangle(b, x0, y0, cell, cell);
                    int s = images[i].Width;
                    int zoom = s <= 64 ? 4 : 1;
                    int d = s * zoom;
                    g.DrawImage(images[i], new Rectangle(x0 + (cell - d) / 2, y0 + (cell - d) / 2, d, d));
                }
            }
            preview.Save(path, ImageFormat.Png);
        }
    }
}
'@

# Windows PowerShell 5.1 では param の既定値で $PSScriptRoot を使えないため、ここで決める
if (-not $Source) {
    $Source = Join-Path $PSScriptRoot '..\..\docs\Copilot_20260614_202528.png'
}

# System.Drawing は PowerShell の現在のフォルダーを基準にしないため、フルパスにする
$Source = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Source)
$OutDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutDir)
New-Item -ItemType Directory -Force $OutDir | Out-Null

$src = [System.Drawing.Bitmap]::FromFile($Source)
$master = [IconBuilder]::CreateMaster($src, $Cx, $Cy, $Rx, $Ry, 1024, $Inset)
$src.Dispose()
$master.Save((Join-Path $OutDir 'icon-master-1024.png'), [System.Drawing.Imaging.ImageFormat]::Png)

$sizes = 16, 20, 24, 32, 40, 48, 64, 96, 128, 256
$images = foreach ($s in $sizes) { [IconBuilder]::CreateIconImage($master, $s) }
[IconBuilder]::SaveIco((Join-Path $OutDir 'AppRoulette.ico'), $images)
[IconBuilder]::SavePreview((Join-Path $OutDir 'preview-small.png'), @($images[0], $images[2], $images[3], $images[5], $images[6]))
[IconBuilder]::SavePreview((Join-Path $OutDir 'preview-large.png'), @($images[9]))
foreach ($i in $images) { $i.Dispose() }
$master.Dispose()
"作成しました: $OutDir"
