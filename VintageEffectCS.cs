using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

public static class VintageEffectCS
{
    // Stałe LCG identyczne jak w wersji ASM
    private const long SEED_BASE = 123456789;
    private const long RANDOM_A = 1664525;
    private const long RANDOM_C = 1013904223;

    private class LcgRandom
    {
        private long seed;

        public LcgRandom(long initialSeed)
        {
            seed = initialSeed;
        }

        public int Next()
        {
            // Implementacja identyczna jak w ASM
            seed = (seed * RANDOM_A + RANDOM_C);

            // Użyj bitów 16-31 (jak w ASM: shr rax, 16)
            int value = (int)((seed >> 16) & 0xFFFF);

            // Zmiana zakresu
            value &= 0xC7;    // Zmiana z 0x63 na 0xC7 (zakres 0-199)
            return value - 100; // Zmiana z 50 na 100 (zakres -100 do 99)
        }
    }

    public static unsafe Bitmap Process(Bitmap input, int threadCount, float intensity)
    {
        Bitmap output = new Bitmap(input.Width, input.Height);
        BitmapData inputData = input.LockBits(
            new Rectangle(0, 0, input.Width, input.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        BitmapData outputData = output.LockBits(
            new Rectangle(0, 0, output.Width, output.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            int totalPixels = input.Width * input.Height;
            int pixelsPerThread = totalPixels / threadCount;
            int remainingPixels = totalPixels;

            // Tworzymy generatory LCG dla każdego wątku
            var randoms = new LcgRandom[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                // Inicjalizacja ziarna tak samo jak w ASM
                randoms[i] = new LcgRandom(i * SEED_BASE);
            }

            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                int pixelsToProcess = (i == threadCount - 1)
                    ? remainingPixels
                    : pixelsPerThread;
                if (pixelsToProcess <= 0) break;

                tasks[i] = Task.Run(() =>
                {
                    var threadRandom = randoms[threadIndex];
                    int offset = threadIndex * pixelsPerThread * 4;
                    byte* inputPtr = (byte*)inputData.Scan0 + offset;
                    byte* outputPtr = (byte*)outputData.Scan0 + offset;

                    for (int j = 0; j < pixelsToProcess; j++)
                    {
                        byte b = *inputPtr;
                        byte g = *(inputPtr + 1);
                        byte r = *(inputPtr + 2);

                        // Obliczenie efektu sepii (współczynniki identyczne jak w ASM)
                        float tr = (r * 0.393f) + (g * 0.769f) + (b * 0.189f);
                        float tg = (r * 0.349f) + (g * 0.686f) + (b * 0.168f);
                        float tb = (r * 0.272f) + (g * 0.534f) + (b * 0.131f);

                        // Mieszanie oryginalnego koloru z efektem sepii
                        float mixR = (tr * intensity + r * (1 - intensity));
                        float mixG = (tg * intensity + g * (1 - intensity));
                        float mixB = (tb * intensity + b * (1 - intensity));

                        // Generowanie i skalowanie szumu tak samo jak w ASM
                        int noise = (int)(threadRandom.Next() * intensity);

                        // Dodanie szumu i ograniczenie wartości do zakresu 0-255
                        *outputPtr = (byte)Math.Min(255, Math.Max(0, mixB + noise));
                        *(outputPtr + 1) = (byte)Math.Min(255, Math.Max(0, mixG + noise));
                        *(outputPtr + 2) = (byte)Math.Min(255, Math.Max(0, mixR + noise));
                        *(outputPtr + 3) = 255;

                        inputPtr += 4;
                        outputPtr += 4;
                    }
                });
                remainingPixels -= pixelsPerThread;
            }

            Task.WaitAll(tasks.Where(t => t != null).ToArray());
            return output;
        }
        finally
        {
            input.UnlockBits(inputData);
            output.UnlockBits(outputData);
        }
    }
}