using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Finetuning : MonoBehaviour
{
    private List<float[]> emgReadings = new();
    private List<float[]> fingerJointReadings = new();
    private bool isRecording = false;
    private float[] lastOVRReading;
    public Transform OculusHandR;

    private void Start()
    {
        OVRHand.HandStateUpdatedEvent.AddListener(OnHandStateUpdated);
    }

    public void ToggleRecording()
    {
        isRecording = !isRecording;
        if (isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
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
        StringBuilder sb = new StringBuilder("EMG1,EMG2,EMG3,EMG4,EMG5,EMG6,EMG7,EMG8,FingerJoint1,FingerJoint2,FingerJoint3,FingerJoint4,FingerJoint5,FingerJoint6,FingerJoint7,FingerJoint8,wrist1,wrist2,wrist3\n");
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

    public void AddReadings(float[] emg)
    {
        emgReadings.Add(emg);
        fingerJointReadings.Add(lastOVRReading);
    }
    
    
    private void OnHandStateUpdated(OVRPlugin.Quatf[] rotations, OVRHand.Hand hand)
    {
        if(hand != OVRHand.Hand.HandRight)
            return;
        
        // 8 angle readings we care about + xyz of wrist
        float[] angleReading = new float[MyoClassification.OUTPUT_DIM + 3];

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
                print(angle);
            }
        }
        
        // Get hand rotation
        var angleX = OculusHandR.eulerAngles.x;
        var angleY = OculusHandR.eulerAngles.y;
        var angleZ = OculusHandR.eulerAngles.z;
        if(angleX > 180)
            angleX = -360 + angleX;
        if (angleY > 180)
            angleY = -360 + angleY;
        if (angleZ > 180)
            angleZ = -360 + angleZ;
        angleReading[counter++] = angleX;
        angleReading[counter++] = angleY;
        angleReading[counter++] = angleZ;
            
        lastOVRReading = angleReading;
    }
    
    private Quaternion qovr2q(OVRPlugin.Quatf q)
    {
        var result = new Quaternion();
        result.x = q.x;
        result.y = q.y;
        result.z = q.z;
        result.w = q.w;
        return result;
    }

    public bool IsRecording()
    {
        return isRecording;
    }
}
