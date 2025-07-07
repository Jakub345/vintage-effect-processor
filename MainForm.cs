using System;
using System.Drawing; // Klasy do obługi grafiki (Bitmap)
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks; // Wielowątkowość
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using JaProj;
using System.Collections.Generic;
using System.IO;

// Klasa reprezentująca główne okno aplikacji
public class MainForm : Form
{
    // Kontrola layoutu
    private Panel scrollPanel;
    private FlowLayoutPanel benchmarkResultsPanel;

    // Elementy wyświetlania
    private PictureBox pictureBox;
    private TextBox resultBox;
    private Label benchmarkStatusLabel;
    private Label threadLabel;
    private Label effectIntensityLabel;

    // Elementy kontrolne
    private Button loadButton;
    private Button processButton;
    private Button benchmarkButton;
    private TrackBar threadTrackBar; // Suwak do wątków
    private TrackBar effectIntensityTrackBar; // Suwak do intensywności
    private RadioButton asmRadio;
    private RadioButton csharpRadio;

    // Elementy danych
    private Bitmap originalImage; // Przechowuje oryginalny obraz

    // Elementy postępu
    private ProgressBar benchmarkProgress; // Pasek postępu benchmarku



    public MainForm()
    {
        InitializeComponents();
        SetDefaultThreadCount();
    }

    private void InitializeComponents()
    {
        this.MinimumSize = new Size(500, 400);
        this.Size = new Size(800, 650);
        this.Text = "Vintage Effect Processor";

        // Główny panel ze scrollbarami
        scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            AutoScrollMinSize = new Size(800, 800)  // Zwiększamy minimalną wysokość
        };
        this.Controls.Add(scrollPanel);

        pictureBox = new PictureBox
        {
            Size = new Size(600, 400),
            Location = new Point(100, 20),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };

        loadButton = new Button
        {
            Text = "Load Image",
            Location = new Point(100, 440),
            Size = new Size(90, 30)
        };
        loadButton.Click += LoadButton_Click;

        processButton = new Button
        {
            Text = "Apply Effect",
            Location = new Point(200, 440),
            Size = new Size(90, 30),
            Enabled = false
        };
        processButton.Click += ProcessButton_Click;

        threadTrackBar = new TrackBar
        {
            Location = new Point(100, 480),
            Width = 300,
            Minimum = 1,
            Maximum = 64,
            Value = 1,
            TickFrequency = 4,
            TickStyle = TickStyle.Both
        };
        threadTrackBar.ValueChanged += TrackBar_ValueChanged;

        threadLabel = new Label
        {
            Location = new Point(420, 480),
            AutoSize = true
        };

        effectIntensityTrackBar = new TrackBar
        {
            Location = new Point(100, 550),
            Width = 300,
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            TickFrequency = 10,
            TickStyle = TickStyle.Both
        };
        effectIntensityTrackBar.ValueChanged += EffectIntensity_ValueChanged;

        effectIntensityLabel = new Label
        {
            Location = new Point(420, 550),
            AutoSize = true,
            Text = "Effect Intensity: 50%"
        };

        asmRadio = new RadioButton
        {
            Text = "Assembly",
            Location = new Point(100, 520),
            Checked = true
        };

        csharpRadio = new RadioButton
        {
            Text = "C#",
            Location = new Point(250, 520)
        };

        resultBox = new TextBox
        {
            Location = new Point(500, 440),
            Size = new Size(200, 100),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };

        benchmarkButton = new Button
        {
            Text = "Run Benchmark",
            Location = new Point(300, 440),
            Size = new Size(90, 30)
        };
        benchmarkButton.Click += BenchmarkButton_Click;

        benchmarkProgress = new ProgressBar
        {
            Location = new Point(100, 600),
            Size = new Size(400, 23),
            Style = ProgressBarStyle.Continuous
        };

        benchmarkStatusLabel = new Label
        {
            Location = new Point(100, 580),
            AutoSize = true,
            Text = "Benchmark status:"
        };

        benchmarkResultsPanel = new FlowLayoutPanel
        {
            Location = new Point(100, 630),
            Size = new Size(600, 150),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BorderStyle = BorderStyle.FixedSingle
        };


        // Dodajemy kontrolki do panelu ze scrollbarami zamiast bezpośrednio do formularza
        scrollPanel.Controls.AddRange(new Control[] {
            pictureBox, loadButton, processButton,
            threadTrackBar, threadLabel, asmRadio,
            csharpRadio, resultBox, effectIntensityTrackBar,
            effectIntensityLabel, 
            benchmarkButton,
            benchmarkProgress,
            benchmarkStatusLabel,
            benchmarkResultsPanel  // Dodajemy nowy panel
        });
    }

    private void SetDefaultThreadCount()
    {
        int logicalProcessors = Environment.ProcessorCount;
        threadTrackBar.Value = Math.Min(logicalProcessors, threadTrackBar.Maximum);
        UpdateThreadLabel();
    }

    private void UpdateThreadLabel()
    {
        threadLabel.Text = $"Threads: {threadTrackBar.Value}";
    }

    private void EffectIntensity_ValueChanged(object sender, EventArgs e)
    {
        effectIntensityLabel.Text = $"Effect Intensity: {effectIntensityTrackBar.Value}%";
    }

    private void TrackBar_ValueChanged(object sender, EventArgs e)
    {
        UpdateThreadLabel();
    }

    private void LoadButton_Click(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                originalImage?.Dispose();
                originalImage = new Bitmap(dialog.FileName);
                pictureBox.Image?.Dispose();
                pictureBox.Image = new Bitmap(originalImage);
                processButton.Enabled = true;
            }
        }
    }

    private async void ProcessButton_Click(object sender, EventArgs e)
    {
        if (originalImage == null) return;

        processButton.Enabled = false;
        int threadCount = threadTrackBar.Value;
        float intensity = effectIntensityTrackBar.Value / 100f;
        Bitmap result = null;

        try
        {
            var sw = new Stopwatch();
            sw.Start();

            if (asmRadio.Checked)
            {
                result = await Task.Run(() => VintageEffectASM.Process(originalImage, threadCount, intensity));
            }
            else
            {
                result = await Task.Run(() => VintageEffectCS.Process(originalImage, threadCount, intensity));
            }

            sw.Stop();

            pictureBox.Image?.Dispose();
            pictureBox.Image = result;
            resultBox.AppendText($"{(asmRadio.Checked ? "ASM" : "C#")} - {threadCount} threads, {effectIntensityTrackBar.Value}% intensity: {sw.ElapsedMilliseconds}ms\r\n");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            result?.Dispose();
        }
        finally
        {
            processButton.Enabled = true;
        }
    }

    private double CalculateStdDeviation(long[] values, double average)
    {
        double sumOfSquaresOfDifferences = values.Sum(val => Math.Pow(val - average, 2));
        return Math.Sqrt(sumOfSquaresOfDifferences / values.Length);
    }

    private void UpdateBenchmarkStatus(string status, int progressPercentage)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateBenchmarkStatus(status, progressPercentage)));
            return;
        }

        benchmarkStatusLabel.Text = $"Benchmark status: {status}";
        benchmarkProgress.Value = progressPercentage;
        Application.DoEvents();
    }

    private async Task<BenchmarkResult> RunBenchmark(Bitmap image, int threadCount, float intensity, bool useAsm)
    {
        var times = new long[5];
        var sw = new Stopwatch();

        for (int i = 0; i < 5; i++)
        {
            sw.Restart();
            using (var processedImage = useAsm ?
                await Task.Run(() => VintageEffectASM.Process(image, threadCount, intensity)) :
                await Task.Run(() => VintageEffectCS.Process(image, threadCount, intensity)))
            {
                sw.Stop();
                times[i] = sw.ElapsedMilliseconds;
            }
            GC.Collect();
        }

        double average = times.Average();

        return new BenchmarkResult
        {
            ImageSize = image.Size,
            ThreadCount = threadCount,
            Intensity = intensity,
            Times = times,
            AverageTime = average,
            StdDeviation = CalculateStdDeviation(times, average),
            Implementation = useAsm ? "ASM" : "C#"
        };
    }

    private async void BenchmarkButton_Click(object sender, EventArgs e)
    {
        if (originalImage == null)
        {
            MessageBox.Show("Please load an image first.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        benchmarkButton.Enabled = false;
        processButton.Enabled = false;
        loadButton.Enabled = false;
        benchmarkResultsPanel.Controls.Clear();
        var results = new List<BenchmarkResult>();

        try
        {
            var testSizes = new[] {
                new Size(800, 600),    // Mały
                new Size(1920, 1080),  // Średni
                new Size(3840, 2160)   // Duży
            };
            var threadCounts = new[] { 1, 2, 4, 8, 16, 32, 64 };

            int totalTests = testSizes.Length * threadCounts.Length * 2;
            int currentTest = 0;

            foreach (var size in testSizes)
            {
                using (var testImage = new Bitmap(originalImage, size))
                {
                    foreach (var threadCount in threadCounts)
                    {
                        UpdateBenchmarkStatus(
                            $"Processing {size.Width}x{size.Height}, threads: {threadCount}",
                            (currentTest * 100) / totalTests);

                        float intensity = effectIntensityTrackBar.Value / 100f;

                        // Test ASM
                        var asmResult = await RunBenchmark(testImage, threadCount, intensity, true);
                        results.Add(asmResult);
                        currentTest++;

                        // Test C#
                        var csResult = await RunBenchmark(testImage, threadCount, intensity, false);
                        results.Add(csResult);
                        DisplayBenchmarkComparison(asmResult, csResult);
                        currentTest++;
                    }
                }
            }

            UpdateBenchmarkStatus("Completed", 100);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during benchmark: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateBenchmarkStatus("Error", 0);
        }
        finally
        {
            benchmarkButton.Enabled = true;
            processButton.Enabled = true;
            loadButton.Enabled = true;
        }
    }

    private void DisplayBenchmarkComparison(BenchmarkResult asmResult, BenchmarkResult csResult)
    {
        if (csResult == null) return;

        var comparisonLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(580, 0),
            Margin = new Padding(5),
            Text = $"Resolution: {asmResult.ImageSize.Width}x{asmResult.ImageSize.Height}, Threads: {asmResult.ThreadCount}\n" +
                  $"ASM avg: {asmResult.AverageTime:F2}ms\n" +
                  $"C# avg: {csResult.AverageTime:F2}ms\n" +
                  $"Speedup: {(csResult.AverageTime / asmResult.AverageTime):F2}x\n" +
                  $"-------------------------------------------"
        };

        if (InvokeRequired)
        {
            Invoke(new Action(() => benchmarkResultsPanel.Controls.Add(comparisonLabel)));
        }
        else
        {
            benchmarkResultsPanel.Controls.Add(comparisonLabel);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        originalImage?.Dispose();
        pictureBox.Image?.Dispose();
        base.OnFormClosing(e);
    }
}