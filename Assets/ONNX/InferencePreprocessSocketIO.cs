using System;
using System.Collections.Generic;
using System.Threading;
using SocketIOClient;
using UnityEngine;

namespace ONNX
{
    public class InferencePreprocessSocketIO : MonoBehaviour
    {
        // public static SocketIO client;
        
        static Queue<float[]> responseQueue = new Queue<float[]>();


    }
    
    
}