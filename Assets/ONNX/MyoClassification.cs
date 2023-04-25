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
    public static readonly int SEQ_LEN = 100;
    public static readonly int FEATURE_LEN = 6;
    public static readonly int TGT_LEN = 1;
    public static readonly int INPUT_DIM = 8;
    public static readonly int OUTPUT_DIM = 8;
    public static readonly int INPUT_TENSOR_LEN = FEATURE_LEN + SEQ_LEN + 1;
    
    
    public float[] resultValues = new float[8];

    private List<float[]> output = new();
    
    float[,,] inputs = new float[1, FEATURE_LEN, INPUT_DIM];

    static ConcurrentQueue<DenseTensor<float>> inputQ = new();
    public static ConcurrentQueue<DenseTensor<float>> outputQ = new();
    static ConcurrentQueue<float> fpsQueue = new();
    
    private static InferenceSession session;
    private static DenseTensor<float> inputTensor;
    private static List<NamedOnnxValue> onnxInputs;
    
    
    public void Start()
    {
        BetterStreamingAssets.Initialize();
        // var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_big_finetuned.onnx");
        // var options = new SessionOptions();
        // session = new InferenceSession(modelBytes, options);
        //
        // // Create a tensor with the shape of the input
        // inputTensor = new DenseTensor<float>(new[] { 1, INPUT_TENSOR_LEN, 8 });
        // onnxInputs = new List<NamedOnnxValue>
        // {
        //     NamedOnnxValue.CreateFromTensor("input", inputTensor)
        // };
    }

    public MyoClassification()
    {
        Thread thread = new Thread(ThreadWork.DoWork);
        thread.Start();
    }

    public void Invoke()
    {
        // Convert input tensor to ONNX tensor
        var onnxInput = new DenseTensor<float>(new[] { 1, INPUT_TENSOR_LEN, INPUT_DIM });
        for (int i = 0; i < INPUT_TENSOR_LEN; i++)
        {
            for (int j = 0; j < INPUT_DIM; j++)
            {
                // inputTensor[0, i, j] = MyoSample.inputTensor[0, i, j];
                onnxInput[0, i, j] = MyoSample.inputTensor[0, i, j];
            }
        }
        
        // using var output = session.Run(onnxInputs);
        // var outputData = output.First().AsTensor<float>();
        // for(int i = 0; i < OUTPUT_DIM; i++)
        // {
        //     var value = outputData[0, TGT_LEN - 1, i];
        //     resultValues[i] = value;
        // }

        inputQ.Enqueue(onnxInput);
    }

    private void Update()
    {
        // while(outputQ.Count > 0)
        // {
        //     bool gotResult = outputQ.TryDequeue(out var outputTensor);
        //     if (!gotResult)
        //         continue;
        //     
        //     output.Add(new float[OUTPUT_DIM]);
        //     for(int i = 0; i < OUTPUT_DIM; i++)
        //     {
        //         var value = outputTensor[0, 0, i];
        //         output[^1][i] = value;
        //         resultValues[i] = value;
        //     }
        //     
        //     if(output.Count > TGT_LEN)
        //         output.RemoveAt(0);
        // }
        
        // FPS stuff
        while (fpsQueue.Count > 0)
        {
            fpsQueue.TryDequeue(out var fps);
            Debug.Log("FPS: " + fps);
        }
    }
    
    public class ThreadWork
    {
        private static InferenceSession session;
        private static DenseTensor<float> inputTensor;
        private static List<NamedOnnxValue> inputs;

        public static void DoWork()
        {
            BetterStreamingAssets.Initialize();
            var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_big_finetuned.onnx");
            var options = new SessionOptions();
            session = new InferenceSession(modelBytes, options);
        
            // Create a tensor with the shape of the input
            inputTensor = new DenseTensor<float>(new[] { 1, INPUT_TENSOR_LEN, INPUT_DIM });
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
                    // Take time using C#
                    stopwatch.Restart();

                    // Without filtering
                    for (int i = 0; i < INPUT_TENSOR_LEN; i++)
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
                    Thread.Sleep(1);
                    continue;
                }
            }
        }
    }

}