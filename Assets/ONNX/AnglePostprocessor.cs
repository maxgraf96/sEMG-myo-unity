using System.Collections.Generic;
using NWaves.Filters.Butterworth;
using NWaves.Signals;

namespace ONNX
{
    public class AnglePostprocessor
    {
        private static LowPassFilter lowPassFilterOutputAngles = new LowPassFilter(0.2f, 6);
        static Dictionary<int, List<float>> rawOutputAngles = new();
        static Dictionary<int, DiscreteSignal> filteredOutputAngles = new();
        private static int warmupCounter = 0;

        public static float[] PostProcessAngles(float[] angleReading)
        {
            // Add to discrete signal
            if (rawOutputAngles.Count == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    rawOutputAngles.Add(i, new List<float>());
                    filteredOutputAngles.Add(i, new DiscreteSignal(100, new float[MyoClassification.SEQ_LEN]));
                }
            }
            
            // Add results to raw samples
            for (int i = 0; i < 8; i++)
            {
                rawOutputAngles[i].Add(angleReading[i]);
                if (rawOutputAngles[i].Count > MyoClassification.SEQ_LEN)
                    rawOutputAngles[i].RemoveAt(0);
            }
            warmupCounter += 1;
            if (warmupCounter < MyoClassification.SEQ_LEN)
                return null;
            
            // Convert raw samples to discrete signal
            foreach (var jointIndex in rawOutputAngles.Keys)
            {
                for (int i = 0; i < MyoClassification.SEQ_LEN; i++)
                {
                    filteredOutputAngles[jointIndex].Samples[i] = rawOutputAngles[jointIndex][i];
                }
                var filteredSignal = lowPassFilterOutputAngles.ApplyTo(filteredOutputAngles[jointIndex]);
                filteredOutputAngles[jointIndex] = filteredSignal;
            }
            
            float[] filteredAngles = new float[MyoClassification.OUTPUT_DIM];
            for (int i = 0; i < 8; i++)
            {
                filteredAngles[i] = filteredOutputAngles[i].Samples[^1];
            }

            return filteredAngles;
        }

        public static void ResetPostprocessor()
        {
            rawOutputAngles.Clear();
            filteredOutputAngles.Clear();
            warmupCounter = 0;
        }
        
    }
}