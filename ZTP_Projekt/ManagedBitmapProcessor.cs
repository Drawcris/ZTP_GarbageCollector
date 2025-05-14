using System;
using System.Drawing;
using System.IO;

class ManagedBitmapProcessor
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
                Console.WriteLine($"✗ Błąd podczas przetwarzania {fileName}: {ex.Message}");
            }
        }

        Console.WriteLine("Przetwarzanie zakończone.");
    }

    public static void ProcessSingle(string inputPath, string outputPath)
    {
        using Bitmap bmp = new Bitmap(inputPath);
        int width = bmp.Width;
        int height = bmp.Height;

        byte[,] gray = new byte[height, width];

        // Skala szarości
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color pixel = bmp.GetPixel(x, y);
                byte luminance = (byte)(0.3 * pixel.R + 0.59 * pixel.G + 0.11 * pixel.B);
                gray[y, x] = luminance;
            }

        // Filtr Laplace’a
        int[,] mask = new int[3, 3]
        {
            { 0, 1, 0 },
            { 1, -4, 1 },
            { 0, 1, 0 }
        };

        int[,] filtered = new int[height - 2, width - 2];
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                int sum = 0;
                for (int j = -1; j <= 1; j++)
                    for (int i = -1; i <= 1; i++)
                        sum += gray[y + j, x + i] * mask[j + 1, i + 1];
                filtered[y - 1, x - 1] = Math.Clamp(sum, 0, 255);
            }

        // Tworzenie nowej bitmapy
        using Bitmap output = new Bitmap(width - 2, height - 2);
        for (int y = 0; y < output.Height; y++)
            for (int x = 0; x < output.Width; x++)
            {
                byte val = (byte)filtered[y, x];
                output.SetPixel(x, y, Color.FromArgb(val, val, val));
            }

        output.Save(outputPath);
    }
}
