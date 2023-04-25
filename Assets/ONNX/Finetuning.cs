using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Finetuning : MonoBehaviour
{
    private List<int[]> emgReadings = new();
    private List<float[]> fingerJointReadings = new();
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartRecording()
    {
        emgReadings.Clear();
        fingerJointReadings.Clear();
    }

    public void StopRecording()
    {
        // Make sure we have the same number of readings emg and finger joint readings
        if (emgReadings.Count != fingerJointReadings.Count)
        {
            Debug.LogError("Number of EMG readings and finger joint angle readings do not match!");
            return;
        }
        // Save the data to a csv file
        StringBuilder sb = new StringBuilder("EMG1,EMG2,EMG3,EMG4,EMG5,EMG6,EMG7,EMG8,FingerJoint1,FingerJoint2,FingerJoint3,FingerJoint4,FingerJoint5,FingerJoint6,FingerJoint7,FingerJoint8\n");
        for (int i = 0; i < emgReadings.Count; i++)
        {
            for (int j = 0; j < emgReadings[i].Length; j++)
            {
                sb.Append(emgReadings[i][j] + ",");
            }
            for (int j = 0; j < fingerJointReadings[i].Length; j++)
            {
                sb.Append(fingerJointReadings[i][j] + ",");
            }
            sb.Append("\n");
        }
        
        System.IO.File.WriteAllText("Assets/ONNX/finetuning_data.csv", sb.ToString());
    }

    public void AddReadings(int[] emg, float[] fingerJoints)
    {
        emgReadings.Add(emg);
        fingerJointReadings.Add(fingerJoints);
    }
}
