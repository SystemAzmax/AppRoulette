using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AppRoulette.Services
{
    /// <summary>
    /// アプリケーションアイコンを生成するサービス。
    /// </summary>
    public class AppIconService
    {
        /// <summary>
        /// ルーレットアイコンをICO形式で生成します。
        /// </summary>
        /// <param name="size">アイコンサイズ（ピクセル）。</param>
        /// <returns>アイコンデータ。</returns>
        public static byte[] GenerateRouletteIcon(int size = 256)
        {
            // ICOファイルヘッダーを作成
            List<byte> icoData = new();

            // ICO Header (6 bytes)
            icoData.AddRange(new byte[] { 0, 0, 1, 0, 1, 0 }); // Signature + Image Count

            // ICONDIRENTRY (16 bytes)
            int width = size;
            int height = size;
            int imageDataSize = width * height * 4 + 40; // BMP data size
            int imageDataOffset = 22; // After ICO header and ICONDIRENTRY

            icoData.Add((byte)width);
            icoData.Add((byte)height);
            icoData.Add(0); // Color palette (0 = no palette)
            icoData.Add(0); // Reserved
            icoData.AddRange(BitConverter.GetBytes((ushort)1)); // Color planes
            icoData.AddRange(BitConverter.GetBytes((ushort)32)); // Bits per pixel
            icoData.AddRange(BitConverter.GetBytes(imageDataSize)); // Image data size
            icoData.AddRange(BitConverter.GetBytes(imageDataOffset)); // Image data offset

            // BMP Header (40 bytes)
            icoData.AddRange(BitConverter.GetBytes(40)); // Header size
            icoData.AddRange(BitConverter.GetBytes(width)); // Width
            icoData.AddRange(BitConverter.GetBytes(height * 2)); // Height (doubled for ICO)
            icoData.AddRange(BitConverter.GetBytes((ushort)1)); // Color planes
            icoData.AddRange(BitConverter.GetBytes((ushort)32)); // Bits per pixel
            icoData.AddRange(BitConverter.GetBytes(0)); // Compression (none)
            icoData.AddRange(BitConverter.GetBytes(0)); // Image size
            icoData.AddRange(BitConverter.GetBytes(0)); // H resolution
            icoData.AddRange(BitConverter.GetBytes(0)); // V resolution
            icoData.AddRange(BitConverter.GetBytes(0)); // Colors used
            icoData.AddRange(BitConverter.GetBytes(0)); // Important colors

            // ルーレット画像データを生成（BGRA形式）
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // ピクセルの色を計算
                    int centerX = width / 2;
                    int centerY = height / 2;
                    int dx = x - centerX;
                    int dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                    if (angle < 0) angle += 360;

                    (byte b, byte g, byte r, byte a) color;

                    // 外側のリング（ルーレット部分）
                    if (distance > centerX * 0.4 && distance < centerX * 0.95)
                    {
                        // 各セクションの色
                        if (angle >= 0 && angle < 45) color = (255, 200, 0, 255); // Yellow
                        else if (angle >= 45 && angle < 90) color = (200, 255, 100, 255); // Light Green
                        else if (angle >= 90 && angle < 135) color = (100, 255, 200, 255); // Cyan
                        else if (angle >= 135 && angle < 180) color = (100, 200, 255, 255); // Light Blue
                        else if (angle >= 180 && angle < 225) color = (150, 100, 255, 255); // Purple
                        else if (angle >= 225 && angle < 270) color = (255, 150, 200, 255); // Pink
                        else if (angle >= 270 && angle < 315) color = (255, 100, 100, 255); // Red
                        else color = (255, 150, 100, 255); // Orange
                    }
                    // 中央の球体
                    else if (distance < centerX * 0.3)
                    {
                        if (distance < centerX * 0.25)
                        {
                            color = (255, 255, 255, 255); // White center
                        }
                        else
                        {
                            color = (200, 220, 240, 255); // Light gray ring
                        }
                    }
                    // 指標器（矢印）
                    else if (angle > 340 || angle < 20)
                    {
                        if (distance > centerX * 0.9)
                        {
                            color = (200, 0, 0, 255); // Red indicator
                        }
                        else
                        {
                            color = (240, 240, 240, 255); // Light background
                        }
                    }
                    else
                    {
                        color = (240, 240, 245, 255); // Light background
                    }

                    icoData.Add(color.b);
                    icoData.Add(color.g);
                    icoData.Add(color.r);
                    icoData.Add(color.a);
                }
            }

            // AND mask (1 bit per pixel, aligned to 32-bit)
            int maskStride = (width + 31) / 32 * 4;
            for (int i = 0; i < height * maskStride; i++)
            {
                icoData.Add(0); // No transparency mask
            }

            return icoData.ToArray();
        }
    }
}
