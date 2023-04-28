using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace ONNX
{
    public class MyoReadWorker : MonoBehaviour
    {
        // To store data coming from the myo interface
        public static ConcurrentQueue<int[]> myoInputQ = new();
        public static int myoInputQCount = 0;
        static readonly int intervalMS = 10;
        
        private Thread readThread;
        
        private void Start()
        {
            readThread = new Thread(MyoReadWork.ReadMyoData);
            readThread.Start();
        }

        private class MyoReadWork
        {
            public static bool isRunning = true;

            public static void ReadMyoData()
            {
                while (isRunning)
                {
                    var latestReading = ThalmicMyo.emg;
                    if (latestReading is { Length: 8 })
                    {
                        myoInputQ.Enqueue(latestReading);
                        Interlocked.Increment(ref myoInputQCount);
                    }
                    
                    Thread.Sleep(intervalMS);
                }
            }
        }
    }
    
     

}