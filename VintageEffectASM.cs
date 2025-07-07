using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
public static class VintageEffectASM
{
    [DllImport(@"C:\Users\kubah\OneDrive\Pulpit\STUDIA\SEM 5\JA proj\ASM ostateczny\JaProj\x64\Debug\JAAsm.dll")]
    private static extern int ProcessSepiaEffect(
        IntPtr input, // Wskaźnik do danych wejściowych
        IntPtr output, // Wskaźnik do danych wyjściowych
        int pixelCount, // Liczba pikseli do przetworzenia
        float intensity, // Intensywność efektu
        int threadIndex); // Indeks wątku
    public static unsafe Bitmap Process(Bitmap input, int threadCount, float intensity)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        Bitmap output = null;
        BitmapData inputData = null;
        BitmapData outputData = null;
        try
        {
            output = new Bitmap(input.Width, input.Height);
            // Zablokuj obie bitmapy na raz
            inputData = input.LockBits(
                new Rectangle(0, 0, input.Width, input.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            outputData = output.LockBits(
                new Rectangle(0, 0, output.Width, output.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);
            // Oblicz rozmiar segmentu dla każdego wątku
            int totalPixels = input.Width * input.Height;
            int pixelsPerThread = totalPixels / threadCount;
            int remainingPixels = totalPixels;
            // Utwórz tablicę zadań
            var tasks = new Task[threadCount];
            // Uruchom zadania równolegle
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                int pixelsToProcess = (i == threadCount - 1)
                    ? remainingPixels
                    : pixelsPerThread;
                if (pixelsToProcess <= 0) break;
                tasks[i] = Task.Run(() =>
                {
                    IntPtr inputOffset = inputData.Scan0 + (threadIndex * pixelsPerThread * 4);
                    IntPtr outputOffset = outputData.Scan0 + (threadIndex * pixelsPerThread * 4);
                    ProcessSepiaEffect(inputOffset, outputOffset, pixelsToProcess, intensity, threadIndex);
                });
                remainingPixels -= pixelsToProcess;
            }
            // Poczekaj na zakończenie wszystkich zadań
            Task.WaitAll(tasks.Where(t => t != null).ToArray());
            return output;
        }
        catch (Exception ex)
        {
            output?.Dispose();
            MessageBox.Show($"Error processing image: {ex.Message}");
            throw;
        }
        finally
        {
            if (inputData != null)
                input.UnlockBits(inputData);
            if (outputData != null && output != null)
                output.UnlockBits(outputData);
        }
    }
}