﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ML.OnnxRuntime.Tensors;
using UnityEngine;
using NWaves.Filters.Butterworth;
using NWaves.Signals;
using Oculus.Interaction;
using ONNX;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using LowPassFilter = NWaves.Filters.Butterworth.LowPassFilter;

public class MyoSample : MonoBehaviour
{
    private MyoClassification _myoClassification;
    // Visualisation hand that doesn't follow Oculus tracking, just for demo purposes of produced joint angle data
    [FormerlySerializedAs("_handModifierR")] public OVRsEMGHandModifier _visHandModifierR;

    // When hands are synced, we activate this hand modifier, which is attached to the virtual hand that actually
    // follows Oculus tracking, overwriting just the joint angles we produce
    public OVRsEMGHandModifier _realHandModifierR;

    // Raw EMG data
    List<float[]> myoDataQueue = new(MyoClassification.SEQ_LEN);
    // Placeholder for bandpassed EMG data
    List<float[]> myoDataQueueFiltered = new(MyoClassification.SEQ_LEN);
    // Input tensor for ONNX model
    // public static float[,,] inputTensor = new float[1, MyoClassification.INPUT_TENSOR_LEN, MyoClassification.INPUT_DIM];
    public static float[][] inputTensor = new float[MyoClassification.INPUT_TENSOR_LEN][];
    
    private bool inferenceActive = true;
    
    private OVRPlugin.Quatf[] _handRotationsR;

    List<float[]> csvEMGData = new();
    List<float[]> csvAngleData = new();
    private int csvEMGDataCounter = 0;
    private int csvAngleDataCounter = 0;

    enum  Mode
    {
        RealInference, // Myo EMG data
        SimInference,  // Validation data
        DataCollection // Collect data for finetuning
    }
    
    // Mode mode = Mode.DataCollection;
    Mode mode = Mode.RealInference;
    // Mode mode = Mode.SimInference;
    
    
    // Data collection fields
    public HandVisual _sourceHandL;
    public HandVisual _sourceHandR;
    
    static int fs = 100;
    float lowCut = 20.0f / fs;
    float highCut = 25.0f / fs;
    int filterOrder = 4;
    private BandPassFilter butter;

    // For simulated inference - whether we are visualising the "true" values (coming from python -> ONNX pipeline)
    // or the values produced by the Unity -> ONNX pipeline
    private bool isSiminferenceGT = false;
    private int numGTPredictions = 11967;
    private string gtPath = "finetuned_onnx_train.csv";
    // private string gtPath = "finetuned_onnx_test.csv";
    
    Stopwatch stopwatch = new Stopwatch();
    private int intervalMS = 10; // 10ms = 100Hz

    void Awake()
    {
        // Set target FPS
        OVRPlugin.systemDisplayFrequency = 120.0f;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 100;
        
        // Set up BetterStreamingAssets
        BetterStreamingAssets.Initialize();
        
        // Set up band pass filter for raw EMG filtering
        butter = new BandPassFilter(lowCut, highCut, filterOrder);
        for(int channel = 0; channel < MyoClassification.SEQ_LEN; channel++)
        {
            myoDataQueueFiltered.Add(new float[MyoClassification.INPUT_DIM]);
        }
        
        // Subscribe to button events
        ButtonControls.HandSyncButtonToggledEvent.AddListener(OnHandSyncButtonToggled);

        switch (mode)
        {
            case Mode.RealInference:
                // Subscribe to Oculus hand update event
                OVRHand.HandStateUpdatedEvent.AddListener(OnHandStateUpdated);
                break;
            case Mode.SimInference:
                // string[] csv_lines = BetterStreamingAssets.ReadAllLines("finetuned_onnx_test.csv");
                string[] csv_lines = BetterStreamingAssets.ReadAllLines(gtPath);
                int counter = 0;
                foreach (string line in csv_lines)
                {
                    string[] values = line.Split(',');
                    float[] reading = new float[MyoClassification.INPUT_DIM];
                    // Read data - if counter is less than numPredictions, we are reading the EMG data
                    // Otherwise, we are reading the angle data
                    for(int i = 0; i < MyoClassification.INPUT_DIM; i++)
                    {
                        reading[i] = float.Parse(values[i]);
                    }

                    if (counter < numGTPredictions * MyoClassification.SEQ_LEN)
                    {
                        csvEMGData.Add(reading);
                    }
                    else
                    {
                        csvAngleData.Add(reading);
                    }
                    
                    counter++;
                }
                Debug.Log("Loaded " + csvEMGData.Count + " EMG samples.");
                break;
            case Mode.DataCollection:
                // Subscribe to hand updated events from Oculus hand data
                OVRHand.HandStateUpdatedEvent.AddListener(OnHandStateUpdated);
                // Enable pose updates to the rendered virtual hands move correctly
                _sourceHandL.InjectOptionalUpdateRootPose(true);
                _sourceHandR.InjectOptionalUpdateRootPose(true);
                // Subscribe to our custom recording toggle event
                ButtonControls.RecordingButtonToggledEvent.AddListener(OnRecordingButtonToggled);
                break;
        }
    }

    private void Start()
    {
        _myoClassification = GetComponent<MyoClassification>();
        stopwatch.Start();
    }

    private int previnputQLag = 0;
    private void Update()
    {
        KeyListener();
        
        if(!inferenceActive)
            return;

        if(MyoClassification.inputQ.Count > 1)
            return;
        
        if (stopwatch.ElapsedMilliseconds < intervalMS)
        {
            Thread.Sleep(intervalMS - (int)stopwatch.ElapsedMilliseconds);  
            // return;
        } 
        
        stopwatch.Restart();
        
        switch (mode)
        {
            case Mode.RealInference:
                // Debug.Log("Got " + MyoReadWorker.myoInputQCount + " samples from MyoReadWorker");
                while(MyoReadWorker.myoInputQ.TryDequeue(out var newSample))
                {
                    EnqueueEMGReading(newSample);
                    Interlocked.Decrement(ref MyoReadWorker.myoInputQCount);
                }

                if (myoDataQueue.Count == MyoClassification.SEQ_LEN)
                {
                    QueueToTensors();
                    _myoClassification.Invoke();
                }
                break;
            case Mode.SimInference:
                if (isSiminferenceGT)
                {
                    // Take angle readings from csv
                    var csvData = csvAngleData[csvAngleDataCounter];
                    
                    var gtOutputTensor = new DenseTensor<float>(new[] { 1, MyoClassification.TGT_LEN, MyoClassification.OUTPUT_DIM });
                    for (int i = 0; i < MyoClassification.TGT_LEN; i++)
                    {
                        for (int j = 0; j < MyoClassification.OUTPUT_DIM; j++)
                        {
                            gtOutputTensor[0, i, j] = csvData[j];
                        }
                    }
                    
                    MyoClassification.outputQ.Enqueue(gtOutputTensor);

                    csvAngleDataCounter++;
                    if (csvAngleDataCounter >= csvAngleData.Count)
                    {
                        csvAngleDataCounter = 0;
                        Debug.Log("Loop - Python ONNX Inference");
                    }
                }
                else
                {
                    myoDataQueue.Clear();
                    for(int i = csvEMGDataCounter; i < csvEMGDataCounter + MyoClassification.SEQ_LEN; i++)
                    {
                        var dat = csvEMGData[i];
                        EnqueueEMGReading(dat);
                    }

                    QueueToTensors();
                    _myoClassification.Invoke();

                    csvEMGDataCounter += MyoClassification.SEQ_LEN;
                
                    if (csvEMGDataCounter + MyoClassification.SEQ_LEN >= csvEMGData.Count)
                    {
                        csvEMGDataCounter = 0;
                        Debug.Log("Loop - Unity ONNX Inference");
                    }
                }
                break;
            case Mode.DataCollection:
                if(!isRecordingData)
                    return;
                while(MyoReadWorker.myoInputQ.TryDequeue(out var newSample))
                {
                    // GetComponent<Finetuning>().AddReadings(newSample, lastOVRReading);
                    Interlocked.Decrement(ref MyoReadWorker.myoInputQCount);
                }
                break;
        }
    }

    private void LateUpdate()
    {
        HandleOutputQueue();
    }

    private void HandleOutputQueue()
    {
        while(MyoClassification.outputQ.TryDequeue(out var outputTensor))
        {
            float[] resultValues = new float[MyoClassification.OUTPUT_DIM];
            for (int i = 0; i < MyoClassification.OUTPUT_DIM; i++)
            {
                // Get last "output_dim" values of outputArray
                var value = outputTensor[0, MyoClassification.TGT_LEN - 1, i];
                resultValues[i] = value;
            }

            var filteredOutputAngles = AnglePostprocessor.PostProcessAngles(resultValues);
            if(filteredOutputAngles == null)
                continue;
            
            _visHandModifierR.UpdateJointData(filteredOutputAngles);
            
            if(_realHandModifierR.enabled)
                _realHandModifierR.UpdateJointData(filteredOutputAngles);
        }
    }

    private void EnqueueEMGReading(int[] emgReading)
    {
        // Convert emgReading to float[]
        float[] emgReadingFloat = new float[MyoClassification.INPUT_DIM];
        for (int i = 0; i < MyoClassification.INPUT_DIM; i++)
        {
            emgReadingFloat[i] = emgReading[i];
        }
        myoDataQueue.Add(emgReadingFloat);

        // If we have reached SEQ_LEN readings, remove the oldest one, but not SOS token
        if (myoDataQueue.Count > MyoClassification.SEQ_LEN)
            myoDataQueue.RemoveAt(0);
    }
    
    private void EnqueueEMGReading(float[] emgReading)
    {
        // Convert emgReading to float[]
        myoDataQueue.Add(emgReading);
        // If we have reached SEQ_LEN readings, remove the oldest one
        if (myoDataQueue.Count > MyoClassification.SEQ_LEN)
            myoDataQueue.RemoveAt(0);
    }
    
    private void QueueToTensors()
    {
        inputTensor = myoDataQueue.ToArray();
    }

    private float[] lastOVRReading = new float[MyoClassification.OUTPUT_DIM];
    private void OnHandStateUpdated(OVRPlugin.Quatf[] rotations, OVRHand.Hand hand)
    {
        if(hand != OVRHand.Hand.HandRight)
            return;
        
        float[] angleReading = new float[MyoClassification.OUTPUT_DIM];
        var counter = 0;
        for (int i = 0; i < 30; i++)
        {
            if (i is
                6 or 7 or    // Index
                9 or 10 or   // Middle
                12 or 13 or  // Ring
                16 or 17     // Pinky
               )
            {
                var quat = qovr2q(rotations[i]);
                var angle = quat.eulerAngles.z;
                if(angle > 180)
                    angle = -360 + angle;
                angleReading[counter] = angle;
                counter++;
            }
        }
        lastOVRReading = angleReading;
    }
    
    // ------------------------------------------ Button Callbacks ------------------------------------------ //
    private void OnHandSyncButtonToggled(bool shouldHandsSync)
    {
        _realHandModifierR.enabled = shouldHandsSync;
    }

    private bool isRecordingData;
    private void OnRecordingButtonToggled(bool isRecording)
    {
        isRecordingData = isRecording;
        if(isRecording)
            GetComponent<Finetuning>().StartRecording();
        else
            GetComponent<Finetuning>().StopRecording();
        
    }
    
    // ------------------------------------------ Helper Functions ------------------------------------------ //
    private Quaternion qovr2q(OVRPlugin.Quatf q)
    {
        var result = new Quaternion();
        result.x = q.x;
        result.y = q.y;
        result.z = q.z;
        result.w = q.w;
        return result;
    }
    
    private void KeyListener()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Inference " + (inferenceActive ? "deactivated" : "activated") + ".");
            inferenceActive = !inferenceActive;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            isSiminferenceGT = !isSiminferenceGT;
            
            MyoClassification.outputQ.Clear();
            AnglePostprocessor.ResetPostprocessor();

            if(isSiminferenceGT)
                csvAngleDataCounter = 0;
            else
            {
                csvEMGDataCounter = 0;
                myoDataQueue.Clear();
            }

            string newGTMode = isSiminferenceGT ? "Python ONNX" : "Unity ONNX";
            Debug.Log("Switched to " + newGTMode + ".");
        }
    }
}
