using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using NWaves.Filters.Butterworth;
using NWaves.Signals;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MyoClassification : MonoBehaviour
{
    public static readonly int SEQ_LEN = 50;
    public static readonly int TGT_LEN = 50;
    public static readonly int INPUT_DIM = 8;
    public static readonly int OUTPUT_DIM = 8;
    
    public float[] resultValues = new float[8];

    private List<float[]> output = new();
    
    float[,,] inputs = new float[1, SEQ_LEN, INPUT_DIM];

    static ConcurrentQueue<DenseTensor<float>> inputQ = new();
    public static ConcurrentQueue<DenseTensor<float>> outputQ = new();
    static ConcurrentQueue<float> fpsQueue = new();
    
    private static InferenceSession session;
    private static DenseTensor<float> inputTensor;
    private static List<NamedOnnxValue> onnxInputs;
    
    
    public void Start()
    {
        BetterStreamingAssets.Initialize();
        // var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn.onnx");
        var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_big.onnx");
        var options = new SessionOptions();
        session = new InferenceSession(modelBytes, options);
        
        // Create a tensor with the shape of the input
        inputTensor = new DenseTensor<float>(new[] { 1, SEQ_LEN, 8 });
        onnxInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };
    }

    public MyoClassification()
    {
        // Thread thread = new Thread(ThreadWork.DoWork);
        // thread.Start();
    }

    public void Invoke()
    {
        // Convert input tensor to ONNX tensor
        // var onnxInput = new DenseTensor<float>(new int[] { 1, SEQ_LEN, INPUT_DIM });
        for (int i = 0; i < SEQ_LEN; i++)
        {
            for (int j = 0; j < INPUT_DIM; j++)
            {
                inputTensor[0, i, j] = MyoSample.emgTensor[0, i, j];
            }
        }
        
        using var output = session.Run(onnxInputs);
        var outputData = output.First().AsTensor<float>();
        // var clone = (DenseTensor<float>) outputData.Clone();
        
        for(int i = 0; i < OUTPUT_DIM; i++)
        {
            var value = outputData[0, TGT_LEN - 1, i];
            resultValues[i] = value;
        }

        // inputQ.Enqueue(onnxInput);
    }

    private void Update()
    {
        // while(outputQ.Count > 0)
        // {
        //     bool gotResult = outputQ.TryDequeue(out var outputTensor);
        //     if (!gotResult)
        //         continue;
        //     
        //     int numOutputSamples = 1;
        //     // Add copy of output_temp to outputs
        //     output.Add(new float[OUTPUT_DIM]);
        //     for (int sample = 0; sample < numOutputSamples; sample++)
        //     {
        //         for (int i = 0; i < OUTPUT_DIM; i++)
        //         {
        //             // Get last "output_dim" values of outputArray
        //             var value = outputTensor[0, TGT_LEN - sample - 1, i];
        //             output[^1][i] += value;
        //         }
        //     }
        //     for(int i = 0; i < OUTPUT_DIM; i++)
        //     {
        //         output[^1][i] /= numOutputSamples;
        //         resultValues[i] = output[^1][i];
        //     }
        //     
        //     
        //     if(output.Count > TGT_LEN)
        //         output.RemoveAt(0);
        // }
        
        // FPS stuff
        // while (fpsQueue.Count > 0)
        // {
        //     fpsQueue.TryDequeue(out var fps);
        //     Debug.Log("FPS: " + fps);
        // }
    }
    
    public class ThreadWork
    {
        private static InferenceSession session;
        private static DenseTensor<float> inputTensor;
        private static List<NamedOnnxValue> inputs;

        public static void DoWork()
        {
            BetterStreamingAssets.Initialize();
            // var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn.onnx");
            var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_big.onnx");
            var options = new SessionOptions();
            session = new InferenceSession(modelBytes, options);
        
            // Create a tensor with the shape of the input
            inputTensor = new DenseTensor<float>(new[] { 1, SEQ_LEN, 8 });
            inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };
            
            Stopwatch stopwatch = new Stopwatch();
            
            // var fs = 100.0f;
            // var lowCut = 10.0f / fs;
            // var highCut = 40.0f / fs;
            // var filterOrder = 2;
            // BandPassFilter butter = new BandPassFilter(lowCut, highCut, filterOrder);
            // // LowPassFilter butter = new LowPassFilter(40.0 / fs, filterOrder);
            // List<DiscreteSignal> rawSignals = new List<DiscreteSignal>();
            // List<DiscreteSignal> filteredSignals = new List<DiscreteSignal>();
            
            while (true)
            {
                // Check if new input is available
                if (inputQ.TryDequeue(out var input))
                {
                    // Take time using C#
                    stopwatch.Restart();
                    
                    // // Filter samples
                    // rawSignals.Clear();
                    // filteredSignals.Clear();
                    //
                    // for (int channel = 0; channel < 8; channel++)
                    // {
                    //     float[] samples = new float[SEQ_LEN];
                    //     for (int j = 0; j < SEQ_LEN; j++)
                    //     {
                    //         samples[j] = input[0, j, channel];
                    //     }
                    //     var signal = new DiscreteSignal((int) fs, samples);
                    //     rawSignals.Add(signal);
                    // }
                    //
                    // foreach (var signal in rawSignals)
                    // {
                    //     filteredSignals.Add(butter.ApplyTo(signal));
                    // }
                    //
                    // // Fill the tensor with filtered signal data
                    // for(int channel = 0; channel < 8; channel++)
                    // {
                    //     for (int i = 0; i < SEQ_LEN; i++)
                    //     {
                    //         inputTensor[0, i, channel] = filteredSignals[channel].Samples[i];
                    //     }
                    // }
                    
                    // Without filtering
                    for (int i = 0; i < SEQ_LEN; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            inputTensor[0, i, j] = input[0, i, j];
                        }
                    }
                    
                    // Get the output tensor
                    using var output = session.Run(inputs);
                    var outputData = output.First().AsTensor<float>();
                    var clone = (DenseTensor<float>) outputData.Clone();

                    outputQ.Enqueue(clone);

                    stopwatch.Stop();
                    var duration = stopwatch.ElapsedMilliseconds / 1000.0f;
                    fpsQueue.Enqueue(1.0f / duration);
                }
                else
                {
                    // Debug.Log("No input");
                    // Thread.Sleep(1);
                    continue;
                }
            }
        }
    }

}