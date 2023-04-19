using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using UnityEngine;
using Debug = UnityEngine.Debug;

// using NWaves.Filters;

public class MyoClassification : MonoBehaviour
{
    public static readonly int SEQ_LEN = 100;
    public static readonly int TGT_LEN = 100;
    public static readonly int INPUT_DIM = 8;
    public static readonly int OUTPUT_DIM = 8;
    
    public float[] resultValues = new float[8];

    private List<float[]> output = new();
    
    float[,,] inputs = new float[1, SEQ_LEN, INPUT_DIM];

    static ConcurrentQueue<DenseTensor<float>> inputQ = new();
    static ConcurrentQueue<DenseTensor<float>> outputQ = new();
    static ConcurrentQueue<float> fpsQueue = new();
    
    public class ThreadWork
    {
        private static InferenceSession session;
        private static DenseTensor<float> inputTensor;
        private static List<NamedOnnxValue> inputs;
        private static int inputQSize = 0;

        public static void DoWork()
        {
            BetterStreamingAssets.Initialize();
            // var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_opt.onnx");
            var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn.with_runtime_opt.ort");
            var options = new SessionOptions();
            session = new InferenceSession(modelBytes, options);
        
            // Create a tensor with the shape of the input
            inputTensor = new DenseTensor<float>(new[] { 1, 100, 8 });
            inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };
            
            Stopwatch stopwatch = new Stopwatch();

            while (true)
            {
                // Check if new input is available
                if (inputQ.TryDequeue(out var input))
                {
                    // Fill the tensor with some data
                    for (int i = 0; i < 100; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            inputTensor[0, i, j] = input[0, i, j];
                        }
                    }
                    // Take time using C#
                    stopwatch.Restart();
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
                    Thread.Sleep(10);
                    continue;
                }
            }
        }
    }

    public MyoClassification()
    {
        Thread thread = new Thread(ThreadWork.DoWork);
        thread.Start();
        // var butter = new Butterworth.BandPassFilter(freq1, freq2, order);
    }

    public void Invoke(float[,,] emgTensor)
    {
        FillInputTensor(emgTensor);
        
        // Convert input tensor to ONNX tensor
        var onnxInput = new DenseTensor<float>(new int[] { 1, SEQ_LEN, INPUT_DIM });
        for (int i = 0; i < SEQ_LEN; i++)
        {
            for (int j = 0; j < INPUT_DIM; j++)
            {
                onnxInput[0, i, j] = inputs[0, i, j];
            }
        }

        inputQ.Enqueue(onnxInput);
    }

    private void Update()
    {
        while(outputQ.Count > 0)
        {
            outputQ.TryDequeue(out var outputTensor);
            
            // Add copy of output_temp to outputs
            output.Add(new float[OUTPUT_DIM]);
            for (int i = 0; i < OUTPUT_DIM; i++)
            {
                // Get last "output_dim"" values of outputArray
                var value = (float) outputTensor[0, TGT_LEN - 1, i];
                output[^1][i] = value;
                // Write to result -> read in MyoSample
                resultValues[i] = value;
            }
        
            // average the last 10 outputs
            // if(output.Count > 10)
            // {
            //     for (int i = 0; i < OUTPUT_DIM; i++)
            //     {
            //         var sum = 0f;
            //         for (int j = 1; j < 10; j++)
            //         {
            //             sum += output[^j][i];
            //         }
            //         resultValues[i] = sum / 10;
            //     }
            // }
        
            if(output.Count > TGT_LEN)
                output.RemoveAt(0);
        }
        
        // FPS stuff
        var newFpsQueueSize = fpsQueue.Count;
        while (fpsQueue.Count > 0)
        {
            fpsQueue.TryDequeue(out var fps);
            Debug.Log("FPS: " + fps);
        }
    }

    private void FillInputTensor(float[,,] emgTensor)
    {
        // Write emgTensor
        for(int i = 0; i < SEQ_LEN; i++)
        {
            for (int j = 0; j < INPUT_DIM; j++)
            {
                inputs[0, i, j] = emgTensor[0, i, j];
            }
        }
    }
}