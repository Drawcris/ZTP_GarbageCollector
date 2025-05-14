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

class SIMDBitmapProcessor
{
    public static void ProcessAllInFolder(string inputFolder, string outputFolder)
    {
        if (!Directory.Exists(inputFolder)) return;
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        foreach (var file in Directory.GetFiles(inputFolder, "*.*")
            .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".bmp")))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            string output = Path.Combine(outputFolder, $"{name}_simd.png");
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
        int totalPixels = width * height;

        var pool = ArrayPool<byte>.Shared;
        byte[] grayBuffer = pool.Rent(totalPixels);

        try
        {
            unsafe
            {
                byte* scan0 = (byte*)data.Scan0;
                fixed (byte* gray = grayBuffer)
                {
                    Vector<float> vR = new Vector<float>(0.299f);
                    Vector<float> vG = new Vector<float>(0.587f);
                    Vector<float> vB = new Vector<float>(0.114f);

                    int vectorSize = Vector<float>.Count;

                    for (int y = 0; y < height; y++)
                    {
                        int offset = y * width;

                        int x = 0;
                        for (; x <= width - vectorSize; x += vectorSize)
                        {
                            float[] r = new float[vectorSize];
                            float[] g = new float[vectorSize];
                            float[] b = new float[vectorSize];

                            for (int i = 0; i < vectorSize; i++)
                            {
                                byte* pixel = scan0 + y * stride + (x + i) * 3;
                                b[i] = pixel[0];
                                g[i] = pixel[1];
                                r[i] = pixel[2];
                            }

                            var v_r = new Vector<float>(r);
                            var v_g = new Vector<float>(g);
                            var v_b = new Vector<float>(b);

                            var grayVec = v_r * vR + v_g * vG + v_b * vB;

                            for (int i = 0; i < vectorSize; i++)
                            {
                                gray[offset + x + i] = (byte)Math.Clamp(grayVec[i], 0, 255);
                            }
                        }

                        // resztka
                        for (; x < width; x++)
                        {
                            byte* pixel = scan0 + y * stride + x * 3;
                            float val = 0.299f * pixel[2] + 0.587f * pixel[1] + 0.114f * pixel[0];
                            gray[offset + x] = (byte)Math.Clamp(val, 0, 255);
                        }
                    }
                }
            }

            bmp.UnlockBits(data);

            // zapisz do bitmapy
            using Bitmap outBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData outData = outBmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            unsafe
            {
                byte* dst = (byte*)outData.Scan0;
                int outStride = outData.Stride;

                fixed (byte* gray = grayBuffer)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte val = gray[y * width + x];
                            byte* pixel = dst + y * outStride + x * 3;
                            pixel[0] = pixel[1] = pixel[2] = val;
                        }
                    }
                }
            }

            outBmp.UnlockBits(outData);
            outBmp.Save(outputPath);
        }
        finally
        {
            pool.Return(grayBuffer);
        }
    }
}