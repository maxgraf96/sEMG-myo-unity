using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

public class PythonInteropDemo
{
    public PythonInteropDemo()
    {
        AsyncIO.ForceDotNet.Force();
        NetMQConfig.Linger = new TimeSpan(0,0,1);
        
        for(int i = 0; i < MyoClassification.INPUT_TENSOR_LEN; i++)
        {
            outputHolder.Add(new float[MyoClassification.INPUT_DIM]);
        }
        
        // Create a socket and connect it to the server
        try
        {
            socket = new RequestSocket();
            socket.Connect("tcp://localhost:5555");
        }
        catch (Exception e)
        {
            socket = null;
        }
    }

    StringBuilder sb = new();
    List<float[]> outputHolder = new();
    private RequestSocket socket;

    public List<float[]> ProcessEMGData(float[] emg1D)
    {
        sb.Clear();
        
        for (int i = 0; i < emg1D.Length; i++)
        {
            sb.Append(emg1D[i].ToString());
            if (i < emg1D.Length - 1)
            {
                sb.Append(", ");
            }
        }
        
        socket.SendFrame(sb.ToString());
        string output = socket.ReceiveFrameString();

        // Parse the string
        output = output.Replace("[", "").Replace("]", "");
        string[] splitInput = output.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
        
        int index = 0;
        int channel = 0;
        for (int i = 0; i < splitInput.Length; i++)
        {
            if (float.TryParse(splitInput[i], out float floatValue))
            {
                outputHolder[index][channel] = floatValue;
                channel++;
                if (channel == MyoClassification.INPUT_DIM)
                {
                    channel = 0;
                    index++;
                }
            }
        }
        
        return outputHolder;
    }

    public void Quit()
    {
        if(socket == null) return;
        // socket.SendFrame("exit");
        socket.Dispose();
        NetMQConfig.Cleanup(false);
    }
}
