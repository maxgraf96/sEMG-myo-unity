using System;
using System.Collections.Generic;
using System.Linq;
using TensorFlowLite;
using UnityEngine;
// using NWaves.Filters;

public class MyoClassification : IDisposable
{
    public static readonly int SEQ_LEN = 100;
    // public static readonly int TGT_LEN = 100;
    public static readonly int TGT_LEN = 55;
    public static readonly int INPUT_DIM = 8;
    public static readonly int OUTPUT_DIM = 8;
    
    public float[] resultValues = new float[8];

    Interpreter interpreter;
    private List<float[]> output = new();
    
    float[,,,] inputs = new float[1, 1, SEQ_LEN, INPUT_DIM];

    private float[,,,] outputArray = new float[1, TGT_LEN, 1, OUTPUT_DIM];

    InterpreterOptions options;

    public MyoClassification(string modelPath)
    {
        #if UNITY_EDITOR
        options = CreateOptions(0);
        #elif UNITY_ANDROID && !UNITY_EDITOR
        options = CreateOptions(1);
        #endif
        // interpreter = new Interpreter(FileUtil.LoadFile(modelPath), options);
        interpreter = new Interpreter(FileUtil.LoadFile(modelPath), options);

        var inputInfo0 = interpreter.GetInputTensorInfo(0);
        // var inputInfo1 = interpreter.GetInputTensorInfo(1);
        var outputInfo = interpreter.GetOutputTensorInfo(0);
        
        Debug.Log("Input info:" + inputInfo0.name + " with shape " + inputInfo0.shape[0] + " " + inputInfo0.shape[1] + " " + inputInfo0.shape[2] + " " + inputInfo0.shape[3]);
        Debug.Log("Output info:" + outputInfo.name + " with shape " + outputInfo.shape[0] + " " + outputInfo.shape[1] + " " + outputInfo.shape[2] + " " + outputInfo.shape[3]);
        
        interpreter.ResizeInputTensor(0, inputInfo0.shape);
        // interpreter.ResizeInputTensor(1, inputInfo1.shape);
        interpreter.AllocateTensors();
        
        // var butter = new Butterworth.BandPassFilter(freq1, freq2, order);
    }

    public void Invoke(float[,,] emgTensor)
    {
        FillInputTensor(emgTensor);

        interpreter.SetInputTensorData(0, inputs);
        interpreter.Invoke();
        interpreter.GetOutputTensorData(0, outputArray);
        
        // Add copy of output_temp to outputs
        output.Add(new float[OUTPUT_DIM]);
        for (int i = 0; i < OUTPUT_DIM; i++)
        {
            // Get last "output_dim"" values of outputArray
            var value = (float) outputArray.GetValue(0, TGT_LEN - 1, 0, i);
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

    private void FillInputTensor(float[,,] emgTensor)
    {
        // Write emgTensor
        for(int i = 0; i < SEQ_LEN; i++)
        {
            for (int j = 0; j < INPUT_DIM; j++)
            {
                inputs[0, 0, i, j] = emgTensor[0, i, j];
            }
        }
    }
    
    public void Dispose()
    {
        interpreter?.Dispose();
    }
    
    protected static InterpreterOptions CreateOptions(int accelerator)
    {
        var options = new InterpreterOptions();

        switch (accelerator)
        {
            // CPU
            case 0:
                options.threads = SystemInfo.processorCount;
                break;
            // NNAPI
            case 1:
                if (Application.platform == RuntimePlatform.Android)
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                        // Create NNAPI delegate with default options
                        options.AddDelegate(new NNAPIDelegate());
#endif // UNITY_ANDROID && !UNITY_EDITOR
                }
                else
                {
                    Debug.LogError("NNAPI is only supported on Android");
                }
                break;
            // GPU
            case 2:
                options.AddGpuDelegate();
                break;
            // XNNPACK
            case 3:
                options.threads = SystemInfo.processorCount;
                options.AddDelegate(XNNPackDelegate.DelegateForType(typeof(float)));
                break;
            default:
                options.Dispose();
                throw new System.NotImplementedException();
        }
        return options;
    }
}