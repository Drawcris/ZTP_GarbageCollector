using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime;
using ZTP_Projekt;

class Program
{
    static void Main()
    {
        string inputFolder = @"C:\Users\sirwo\Desktop\ZTP\Program\ZTP_Projekt\ZTP_Projekt\zdjecia";
        string outputFolder = @"C:\Users\sirwo\Desktop\ZTP\Program\ZTP_Projekt\ZTP_Projekt\zdjecia_filtered_managed";
        string outputUnmanaged = @"C:\Users\sirwo\Desktop\ZTP\Program\ZTP_Projekt\ZTP_Projekt\zdjecia_filtered_unmanaged";

        //UnmanagedBitmapProcessor.ProcessAllInFolder(inputFolder, outputUnmanaged);
        //ManagedBitmapProcessor.ProcessAllInFolder(inputFolder, outputFolder);
        
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        Stopwatch sw = Stopwatch.StartNew();
        ManagedBitmapProcessor.ProcessAllInFolder(inputFolder, outputFolder);
        sw.Stop();

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
    }
}



/*
class MatrixTest
{
    public static void Main()
    {
        int[,] largeMatrix = new int[3000, 200];
        int[,] smallMatrix = new int[5, 5];
        int[,] result = new int[2996, 196];

        // Inicjalizacja macierzy
        Random rand = new Random();
        for (int i = 0; i < 3000; i++)
        for (int j = 0; j < 200; j++)
            largeMatrix[i, j] = rand.Next(1, 10);

        for (int i = 0; i < 5; i++)
        for (int j = 0; j < 5; j++)
            smallMatrix[i, j] = 1;

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 2996; i++)
        for (int j = 0; j < 196; j++)
        {
            int sum = 0;
            for (int k = 0; k < 5; k++)
            for (int l = 0; l < 5; l++)
                sum += largeMatrix[i + k, j + l] * smallMatrix[k, l];
            result[i, j] = sum;
        }
        sw.Stop();

        Console.WriteLine($"Czas wykonania: {sw.ElapsedMilliseconds} ms");
    }
}
*/
