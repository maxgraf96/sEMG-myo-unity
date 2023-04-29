using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using ONNX;
using UnityEngine;

public class WristAngleZMQServer : MonoBehaviour
{
    private ResponseSocket server;
    public Transform OculusHandR;
    private byte[] mostRecentWristAngles;
    // Store last 250 wrist angle readings
    private List<float[]> history = new();

    void Start()
    {
        AsyncIO.ForceDotNet.Force();
        NetMQConfig.Linger = new TimeSpan(0,0,1);
        
        server = new ResponseSocket();
        server.Options.Linger = new TimeSpan(0,0,1);
        server.Bind("tcp://localhost:5556");

        StartCoroutine(CoWorker());
    }

    IEnumerator CoWorker()
    {
        var wait = new WaitForSeconds(0.01f);

        while(true)
        {
            if (server.TryReceiveFrameString(out string incoming))
            {
                try
                {
                    if(history.Count < 250)
                        server.SendFrame("");
                    else
                    {
                        // Send most recent wrist angle to client
                        // Convert the float array into a byte array
                        server.SendFrame(mostRecentWristAngles);
                    }
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
        // Get most recent wrist angle
        var angleX = OculusHandR.eulerAngles.x;
        var angleY = OculusHandR.eulerAngles.y;
        var angleZ = OculusHandR.eulerAngles.z;
        if(angleX > 180)
            angleX = -360 + angleX;
        if (angleY > 180)
            angleY = -360 + angleY;
        if (angleZ > 180)
            angleZ = -360 + angleZ;
        var angles = new[] { angleX, angleY, angleZ };

        history.Add(angles);
        
        if(history.Count > 250)
            history.RemoveAt(0);
        
        mostRecentWristAngles = new byte[angles.Length * history.Count * sizeof(float)];
        for(int i = 0; i < history.Count; i++)
            Buffer.BlockCopy(history[i], 0, mostRecentWristAngles, i * angles.Length * sizeof(float), angles.Length * sizeof(float));
    }
}
