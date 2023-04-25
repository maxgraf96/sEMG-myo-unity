using System;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

public class FeatureExtractor
{
    private const int SamplingFrequency = 100;

    public static double[] PowerSpectralDensity(float[] emgData)
    {
        int n = emgData.Length;
        double[] psd = new double[n / 2 + 1];

        Complex32[] data = emgData.Select(x => new Complex32((float)x, 0)).ToArray();
        Fourier.Forward(data, FourierOptions.Default);

        for (int j = 0; j < n / 2 + 1; j++)
        {
            psd[j] = (data[j].Real * data[j].Real + data[j].Imaginary * data[j].Imaginary) / n;
        }

        return psd;
    }

    public static float MedianFrequency(double[] psd)
    {
        double totalPower = psd.Sum();
        double cumPower = 0;
        int index;

        for (index = 0; index < psd.Length; index++)
        {
            cumPower += psd[index];
            if (cumPower >= totalPower / 2)
            {
                break;
            }
        }

        return (float)index * SamplingFrequency / psd.Length;
    }

    public static float MeanFrequency(double[] psd)
    {
        double totalPower = psd.Sum();
        double weightedSum = 0;

        for (int i = 0; i < psd.Length; i++)
        {
            weightedSum += psd[i] * i;
        }

        return (float) (weightedSum * SamplingFrequency / (psd.Length * totalPower));
    }

    public static float PeakFrequency(double[] psd)
    {
        int maxIndex = Array.IndexOf(psd, psd.Max());
        return (float) maxIndex * SamplingFrequency / psd.Length;
    }

    public static void Main(string[] args)
    {
        double[][] emgData = new double[50][];  // Replace this with your 50x8 EMG data

        // Initialize the emgData array with random values for demonstration purposes
        Random random = new Random();
        for (int i = 0; i < 50; i++)
        {
            emgData[i] = new double[8];
            for (int j = 0; j < 8; j++)
            {
                emgData[i][j] = random.NextDouble();
            }
        }

        for (int channel = 0; channel < 8; channel++)
        {
            double[] channelData = new double[50];
            for (int i = 0; i < 50; i++)
            {
                channelData[i] = emgData[i][channel];
            }

            // double[] psd = PowerSpectralDensity(channelData);
            // double mdf = MedianFrequency(psd);
            // double mnf = MeanFrequency(psd);
            // double pf = PeakFrequency(psd);

            // Console.WriteLine($"Channel {channel + 1}:");
            // Console.WriteLine($"  Median Frequency: {mdf}");
            // Console.WriteLine($"  Mean Frequency: {mnf}");
            // Console.WriteLine($"  Peak Frequency: {pf}");
        }
    }
}
