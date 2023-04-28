using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Debug = UnityEngine.Debug;
using NWaves.Filters;
using NWaves.Filters.Butterworth;

public class ONNXInference : MonoBehaviour
{
    static ConcurrentQueue<Tensor<float>> queue = new();
    static ConcurrentQueue<float> fpsQueue = new();

    public class ThreadWork
    {
        private static InferenceSession session;

        private static Tensor<float> inputTensor;
        private static List<NamedOnnxValue> inputs;

        public static void DoWork()
        {
            BetterStreamingAssets.Initialize();
            // var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_opt.onnx");
            var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn.with_runtime_opt.ort");
            var options = new SessionOptions();
            session = new InferenceSession(modelBytes, options);

            // Create a tensor with the shape of the input
            inputTensor = new DenseTensor<float>(new[] { 1, 100, 8 });
            // Fill the tensor with some data
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    inputTensor[0, i, j] = 0.0f;
                }
            }
            inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };
            
            Stopwatch stopwatch = new Stopwatch();

            while (true)
            {
                // Take time using C#
                stopwatch.Restart();
                // Get the output tensor
                using var output = session.Run(inputs);
                var outputData = output.First().AsTensor<float>();
                var clone = outputData.Clone();
                // outputData.Reshape(new[] { 1, 100, 8 });
                // Debug.Log("ONNX Session output");
                // Print the first 10 values

                queue.Enqueue(clone);
                
                Thread.Sleep(10);
                
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds / 1000.0f;
                fpsQueue.Enqueue(1.0f / duration);
            }
        }
    }
    
    
    private int queueSize = 0;
    private int fpsQueueSize = 0;
    void Start()
    {
        Thread thread = new Thread(ThreadWork.DoWork);
        thread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        var newQueueSize = queue.Count;
        if (newQueueSize != queueSize)
        {
            // Get diff
            var diff = newQueueSize - queueSize;
            // Take diff samples
            for (int i = 0; i < diff; i++)
            {
                queue.TryDequeue(out var tensor);
                // Log first three values
                Debug.Log(tensor[0, 0, 1] + " " + tensor[0, 0, 2] + " " + tensor[0, 0, 3]);
            }
        }
        
        var newFpsQueueSize = fpsQueue.Count;
        if (newFpsQueueSize != fpsQueueSize)
        {
            // Get diff
            var diff = newFpsQueueSize - fpsQueueSize;
            // Take diff samples
            for (int i = 0; i < diff; i++)
            {
                fpsQueue.TryDequeue(out var fps);
                // Log first three values
                Debug.Log("FPS: " + fps);
            }
        }
        // // Take time
        // var startTime = Time.realtimeSinceStartup;
        //
        // using var output = session.Run(inputs);
        // var outputData = output.First().AsTensor<float>();
        //
        // var endTime = Time.realtimeSinceStartup;
        // var duration = endTime - startTime;
    }
}
