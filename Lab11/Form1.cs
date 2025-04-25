using System;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace Lab8._3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private const double alpha = 0.05; // Уровень значимости

        private void button1_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(Meaninput.Text, out double mean) ||
                !double.TryParse(Varnput.Text, out double variance) || variance <= 0)
            {
                MessageBox.Show("Input var/ mean");
                return;
            }

            int K = (int)intervalUpDawn.Value; // bins
            int[] sampleSizes = { 10, 100, 1000, 10000 };

            chart.Series.Clear();
            chart.Series.Add("Histogram");
            chart.Series[0].ChartType = SeriesChartType.Column;

            foreach (int N in sampleSizes)
            {
                if (K > N)
                {
                    MessageBox.Show("K must < N");
                    continue;
                }

                double[] normalDistribution = GenerateNormalSamples(mean, Math.Sqrt(variance), N);

                (int[] statistic, double min, double step) = BuildHistogram(normalDistribution, K);

                // Draw histogram
                if (N == sampleSizes.Last())
                {
                    chart.Series[0].Points.Clear();
                    for (int j = 0; j < K; j++)
                    {
                        chart.Series[0].Points.AddXY(min + j * step, statistic[j]);
                    }
                }

                double[] freqs = statistic.Select(interval => (double)interval / N).ToArray();

                double eMean = GetMean(freqs, step, min);
                double eVar = GetVariance(freqs, eMean, step, min);

                // Теоретическая вероятность для хи-квадрат
                double[] probs = new double[K];
                for (int i = 0; i < K; i++)
                {
                    double a = min + i * step;
                    double b = min + (i + 1) * step;
                    probs[i] = Normal.CDF(mean, Math.Sqrt(variance), b) - Normal.CDF(mean, Math.Sqrt(variance), a);
                }

                // chi-square
                double chiSquare = GetChiSquare(statistic, probs, N);
                int df = K - 3; // K - 1 - 2
                double tableValue = ChiSquared.InvCDF(df, 1 - alpha);
                bool isAccepted = chiSquare <= tableValue;

                if (N == sampleSizes.Last())
                {
                    Mean.Text = $"{eMean:F4} (Error = {Math.Abs(eMean - mean) / Math.Abs(mean) * 100:F2}%)";
                    Dispersion.Text = $"{eVar:F4} (Error = {Math.Abs(eVar - variance) / Math.Abs(variance) * 100:F2}%)";
                    ChiSquare.Text = $"{chiSquare:F4}";
                    HypRes.Text = isAccepted ? "TRUE" : "FALSE";
                }
            }
        }

        private double[] GenerateNormalSamples(double mean, double stdDev, int N)
        {
            var normal = new Normal(mean, stdDev);
            return normal.Samples().Take(N).ToArray();
        }

        // histogram
        private (int[], double, double) BuildHistogram(double[] data, int K)
        {
            double min = data.Min();
            double max = data.Max();
            double step = (max - min) / K;
            int[] statistic = new int[K];

            foreach (double value in data)
            {
                int j = (int)((value - min) / step);
                if (j >= K) j = K - 1;
                if (j >= 0) statistic[j]++;
            }

            return (statistic, min, step);
        }

        private double GetMean(double[] freqs, double step, double min)
        {
            double sum = 0;
            for (int i = 0; i < freqs.Length; i++)
            {
                double x = min + (i + 0.5) * step;
                sum += freqs[i] * x;
            }
            return sum;
        }

        private double GetVariance(double[] freqs, double mean, double step, double min)
        {
            double variance = 0;
            for (int i = 0; i < freqs.Length; i++)
            {
                double x = min + (i + 0.5) * step;
                variance += freqs[i] * Math.Pow(x, 2);
            }
            return variance - Math.Pow(mean, 2);
        }

        // chi-square
        private double GetChiSquare(int[] observed, double[] expected, int N)
        {
            double chiSquare = 0;
            for (int i = 0; i < observed.Length; i++)
            {
                double expectedCount = expected[i] * N;
                if (expectedCount > 0)
                {
                    chiSquare += Math.Pow(observed[i] - expectedCount, 2) / expectedCount;
                }
            }
            return chiSquare;
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = Meaninput.Text.Length > 0 && Varnput.Text.Length > 0;
        }
    }
}