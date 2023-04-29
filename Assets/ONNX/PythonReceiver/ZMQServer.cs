using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using ONNX;
using UnityEngine;

public class ZMQServer : MonoBehaviour
{
    private ResponseSocket server;
    static ConcurrentQueue<int> queue = new();
    
    public bool isModeRecording;
    
    private Finetuning _finetuning;
    public OVRsEMGHandModifier _visHandModifierR;
    public OVRsEMGHandModifier _realHandModifierR;
    
    void Start()
    {
        _finetuning = GetComponent<Finetuning>();
        
        AsyncIO.ForceDotNet.Force();
        NetMQConfig.Linger = new TimeSpan(0,0,1);
        
        server = new ResponseSocket();
        server.Options.Linger = new TimeSpan(0,0,1);
        server.Bind("tcp://localhost:5555");

        StartCoroutine(CoWorker());
        
        // Subscribe to button events
        ButtonControls.HandSyncButtonToggledEvent.AddListener(OnHandSyncButtonToggled);
    }

    IEnumerator CoWorker()
    {
        var wait = new WaitForSeconds(0.001f);

        while(true)
        {
            // Proper data
            if(server.TryReceiveFrameBytes(out byte[] packedData) )
            {
                try
                {
                    // Send an response frame back to the client
                    server.SendFrameEmpty();
                    
                    // Unpack the bytes into a float array
                    var floatArray = UnpackFloatArray(packedData);
                    
                    // -------------------------------- Recording mode --------------------------------
                    if (isModeRecording && _finetuning.IsRecording())
                    {
                        _finetuning.AddReadings(floatArray);
                    }
                    // -------------------------------- Inference mode --------------------------------
                    else
                    {
                        // Inference mode
                        // Convert the float array into a list of lists
                        var data = floatArray;
                        
                        var filteredOutputAngles = AnglePostprocessor.PostProcessAngles(data);
                        if (filteredOutputAngles != null)
                        {
                            // Send the data to OVR Hand Tracking
                            _visHandModifierR.UpdateJointData(filteredOutputAngles);
                            if(_realHandModifierR.enabled)
                                _realHandModifierR.UpdateJointData(filteredOutputAngles);
                        }
                    }
                    continue;
                    
                } catch (Exception e)
                {
                    Debug.Log(e);
                    server.SendFrame("Error");
                }
            }

            yield return wait;
        }
    }

    private void OnApplicationQuit()
    {
        server.Dispose();
        NetMQConfig.Cleanup(false);
    }

    // Update is called once per frame
    void Update()
    {
        while (queue.TryDequeue(out int i))
        {
            Debug.Log(i);
        }
        
    }
    
    // --------------------------------------------- Helper functions ---------------------------------------------
    // Unpack the bytes into a float array
    static float[] UnpackFloatArray(byte[] packedData)
    {
        var floatArray = new float[packedData.Length / sizeof(float)];
        Buffer.BlockCopy(packedData, 0, floatArray, 0, packedData.Length);
        return floatArray;
    }

    // Convert the flat float array into a nested list
    static float[][] ConvertToNestedList(float[] flatArray, int sublistLength)
    {
        var nestedList = new float[flatArray.Length / sublistLength][];
        for (int i = 0; i < nestedList.Length; i++)
        {
            nestedList[i] = new float[sublistLength];
            Array.Copy(flatArray, i * sublistLength, nestedList[i], 0, sublistLength);
        }
        return nestedList;
    }
    
    // ------------------------------------------ Button Callbacks ------------------------------------------ //
    private void OnHandSyncButtonToggled(bool shouldHandsSync)
    {
        _realHandModifierR.enabled = shouldHandsSync;
    }
}

