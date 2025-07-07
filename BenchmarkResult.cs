using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaProj
{
    public class BenchmarkResult
    {
        public Size ImageSize { get; set; }
        public int ThreadCount { get; set; }
        public float Intensity { get; set; }
        public long[] Times { get; set; }
        public double AverageTime { get; set; }
        public string Implementation { get; set; }
        public double StdDeviation { get; set; }

        public string ToCsvLine()
        {
            return $"{ImageSize.Width}x{ImageSize.Height},{Implementation},{ThreadCount}," +
                   $"{Intensity:F2},{string.Join(";", Times)},{AverageTime:F2},{StdDeviation:F2}";
        }

        public static string CsvHeader()
        {
            return "ImageSize,Implementation,ThreadCount,Intensity,Time1,Time2,Time3,Time4,Time5,Average,StdDev";
        }
    }
}
