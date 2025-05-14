namespace ZTP_Projekt;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

class UnmanagedBitmapProcessor
{
    public static void ProcessAllInFolder(string inputFolder, string outputFolder)
    {
        if (!Directory.Exists(inputFolder))
        {
            Console.WriteLine($"Folder wejściowy nie istnieje: {inputFolder}");
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string[] files = Directory.GetFiles(inputFolder, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine("Brak obrazów do przetworzenia.");
            return;
        }

        Console.WriteLine($"Znaleziono {files.Length} plików. Rozpoczynanie przetwarzania...");

        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string outputFile = Path.Combine(outputFolder, $"{fileName}_filtered.png");

            try
            {
                ProcessSingle(file, outputFile);
                Console.WriteLine($"✓ Przetworzono: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Błąd przy {fileName}: {ex.Message}");
            }
        }

        Console.WriteLine("Przetwarzanie zakończone.");
    }

    public static void ProcessSingle(string inputPath, string outputPath)
    {
        using Bitmap bmp = new Bitmap(inputPath);
        int width = bmp.Width;
        int height = bmp.Height;
        int sm = 1;

        BitmapData bmpData = bmp.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        int stride = bmpData.Stride;

        IntPtr A_ptr = Marshal.AllocHGlobal(width * height); // szarość
        IntPtr B_ptr = Marshal.AllocHGlobal((width - 2 * sm) * (height - 2 * sm) * sizeof(int)); // wynik

        try
        {
            unsafe
            {
                byte* scan0 = (byte*)bmpData.Scan0;
                byte* A = (byte*)A_ptr;
                int* B = (int*)B_ptr;

                // Szarość
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        byte* pixel = scan0 + (y * stride) + (x * 3);
                        A[y * width + x] = (byte)(0.2989 * pixel[2] + 0.5870 * pixel[1] + 0.1140 * pixel[0]);
                    }

                bmp.UnlockBits(bmpData);

                // Filtracja Laplace'a
                int[,] W = { { 0, 1, 0 }, { 1, -4, 1 }, { 0, 1, 0 } };
                for (int y = sm; y < height - sm; y++)
                    for (int x = sm; x < width - sm; x++)
                    {
                        int sum = 0;
                        for (int ky = -sm; ky <= sm; ky++)
                            for (int kx = -sm; kx <= sm; kx++)
                                sum += W[ky + sm, kx + sm] * A[(y + ky) * width + (x + kx)];
                        B[(y - sm) * (width - 2 * sm) + (x - sm)] = Math.Clamp(sum, 0, 255);
                    }

                // Tworzenie nowej bitmapy
                using Bitmap resultBmp = new Bitmap(width - 2 * sm, height - 2 * sm, PixelFormat.Format24bppRgb);
                BitmapData resultData = resultBmp.LockBits(
                    new Rectangle(0, 0, resultBmp.Width, resultBmp.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format24bppRgb);

                byte* dst = (byte*)resultData.Scan0;
                int newStride = resultData.Stride;

                for (int y = 0; y < resultBmp.Height; y++)
                    for (int x = 0; x < resultBmp.Width; x++)
                    {
                        byte val = (byte)B[y * resultBmp.Width + x];
                        byte* pixel = dst + y * newStride + x * 3;
                        pixel[0] = pixel[1] = pixel[2] = val;
                    }

                resultBmp.UnlockBits(resultData);
                resultBmp.Save(outputPath);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(A_ptr);
            Marshal.FreeHGlobal(B_ptr);
        }
    }
}
