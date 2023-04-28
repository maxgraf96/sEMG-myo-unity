using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class ZMQServer : MonoBehaviour
{
    private ResponseSocket server;
    static ConcurrentQueue<int> queue = new();
    void Start()
    {
        AsyncIO.ForceDotNet.Force();
        NetMQConfig.Linger = new TimeSpan(0,0,1);
        
        server = new ResponseSocket();
        server.Options.Linger = new TimeSpan(0,0,1);
        server.Bind("tcp://localhost:5555");

        StartCoroutine(CoWorker());
    }

    IEnumerator CoWorker()
    {
        var wait = new WaitForSeconds(0.01f);

        while(true)
        {
            // Proper data
            if(server.TryReceiveFrameBytes(out byte[] packedData) )
            {
                try
                {
                    // Unpack the bytes into a float array
                    var floatArray = UnpackFloatArray(packedData);

                    // Convert the float array into a list of lists
                    var data = ConvertToNestedList(floatArray, MyoClassification.INPUT_DIM);

                    // Process the data (you can add your own logic here)
                    var response = $"Received {data.Length} lists of floats";

                    // Send the response back to the Python client
                    server.SendFrame(response);
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
}
