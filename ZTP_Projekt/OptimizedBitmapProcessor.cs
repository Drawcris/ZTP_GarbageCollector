namespace ZTP_Projekt;
using System;
using System.Drawing;
using System.IO;
using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

class OptimizedBitmapProcessor // zoptymalizowana wersja
{
    public static void ProcessAllInFolder(string inputFolder, string outputFolder)
    {
        if (!Directory.Exists(inputFolder))
            return;

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        var files = Directory.GetFiles(inputFolder, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string output = Path.Combine(outputFolder, $"{name}_pooled.png");

            try
            {
                ProcessSingle(file, output);
                Console.WriteLine($"✓ {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {name}: {ex.Message}");
            }
        }
    }

    public static void ProcessSingle(string inputPath, string outputPath)
    {
        using Bitmap bmp = new Bitmap(inputPath);
        int width = bmp.Width;
        int height = bmp.Height;

        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        int stride = data.Stride;

        // Rentuj bufor z ArrayPool (dla grayscale)
        var pool = ArrayPool<byte>.Shared;
        byte[] grayBuffer = pool.Rent(width * height); // 1 bajt na piksel

        try
        {
            unsafe
            {
                byte* scan0 = (byte*)data.Scan0;

                fixed (byte* gray = grayBuffer) // przypinamy do POH
                {
                    // Skala szarości
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte* pixel = scan0 + y * stride + x * 3;
                            byte val = (byte)(0.3 * pixel[2] + 0.59 * pixel[1] + 0.11 * pixel[0]);
                            gray[y * width + x] = val;
                        }
                    }

                    // Filtr Laplace'a
                    int[,] mask = { { 0, 1, 0 }, { 1, -4, 1 }, { 0, 1, 0 } };
                    using Bitmap resultBmp = new Bitmap(width - 2, height - 2, PixelFormat.Format24bppRgb);
                    BitmapData resultData = resultBmp.LockBits(
                        new Rectangle(0, 0, resultBmp.Width, resultBmp.Height),
                        ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                    byte* dst = (byte*)resultData.Scan0;
                    int resultStride = resultData.Stride;

                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            int sum = 0;
                            for (int j = -1; j <= 1; j++)
                                for (int i = -1; i <= 1; i++)
                                {
                                    byte g = gray[(y + j) * width + (x + i)];
                                    sum += g * mask[j + 1, i + 1];
                                }

                            byte val = (byte)Math.Clamp(sum, 0, 255);
                            byte* outPixel = dst + (y - 1) * resultStride + (x - 1) * 3;
                            outPixel[0] = outPixel[1] = outPixel[2] = val;
                        }
                    }

                    resultBmp.UnlockBits(resultData);
                    resultBmp.Save(outputPath);
                }
            }

            bmp.UnlockBits(data);
        }
        finally
        {
            pool.Return(grayBuffer); 
        }
    }
}