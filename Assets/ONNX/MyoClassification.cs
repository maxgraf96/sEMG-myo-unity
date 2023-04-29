using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using UnityEngine;
using Debug = UnityEngine.Debug;
using ThreadPriority = System.Threading.ThreadPriority;

public class MyoClassification : MonoBehaviour
{
    public static readonly int SEQ_LEN = 150;
    public static readonly int FEATURE_LEN = 6;
    public static readonly int TGT_LEN = 1;
    public static readonly int INPUT_DIM = 8;
    public static readonly int OUTPUT_DIM = 8;
    public static readonly int INPUT_TENSOR_LEN = FEATURE_LEN + SEQ_LEN + 1;
    
    public float[] resultValues = new float[8];

    public static ConcurrentQueue<float[][]> inputQ = new();
    public static ConcurrentQueue<DenseTensor<float>> outputQ = new();
    static ConcurrentQueue<float> fpsQueue = new();

    private Thread workThread;
    
    public void Start()
    {
        BetterStreamingAssets.Initialize();
        workThread = new Thread(ThreadWork.DoWork);
        workThread.Priority = ThreadPriority.Highest;
        workThread.Start();
    }

    public void Invoke()
    {
        // Copy MyoSample.inputTensor
        float[][] rawEMGData = new float[MyoSample.inputTensor.Length][];
        for(int i = 0; i < MyoSample.inputTensor.Length; i++)
        {
            rawEMGData[i] = new float[MyoSample.inputTensor[i].Length];
            for(int j = 0; j < MyoSample.inputTensor[i].Length; j++)
            {
                rawEMGData[i][j] = MyoSample.inputTensor[i][j];
            }
        }
        
        inputQ.Enqueue(rawEMGData);
    }

    private void Update()
    {
        // FPS stuff
        while (fpsQueue.Count > 0)
        {
            if (fpsQueue.TryDequeue(out var fps))
            {
                Debug.Log("FPS: " + fps);
                Debug.Log("Input q lag:" + inputQ.Count);
            }
        }
    }

    private void OnApplicationQuit()
    {
        ThreadWork.isRunning = false;
        workThread.Join();
    }

    public class ThreadWork
    {
        private static InferenceSession session;
        private static DenseTensor<float> inputTensor;
        private static List<NamedOnnxValue> inputs;

        public static bool isRunning = true;

        public static void DoWork()
        {
            BetterStreamingAssets.Initialize();
            var modelBytes = BetterStreamingAssets.ReadAllBytes("model_hu_2022_rnn_big_finetuned.onnx");
            var options = new SessionOptions();
            session = new InferenceSession(modelBytes, options);
            
            Stopwatch stopwatch = new Stopwatch();
        
            // Create a tensor with the shape of the input
            inputTensor = new DenseTensor<float>(new[] { 1, INPUT_TENSOR_LEN, INPUT_DIM });
            inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            var pythonInterop = new PythonInteropDemo();
            
            while (isRunning)
            {
                // Check if new input is available
                if (!inputQ.TryDequeue(out var input))
                {
                    // Debug.Log("No input");
                    Thread.Sleep(1);
                    continue;
                }

                // Take time using C#
                stopwatch.Restart();
                
                float[] emgData1D = new float[input.Length * input[0].Length];
                for(int i = 0; i < input.Length; i++)
                {
                    for(int j = 0; j < input[i].Length; j++)
                    {
                        emgData1D[i * input[i].Length + j] = input[i][j];
                    }
                }
                // PythonInterop
                var processedEMG = pythonInterop.ProcessEMGData(emgData1D);
                
                // Without filtering
                for (int i = 0; i < INPUT_TENSOR_LEN; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        inputTensor[0, i, j] = processedEMG[i][j];
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
            
            pythonInterop.Quit();
            session.Dispose();
        }
    }

}
