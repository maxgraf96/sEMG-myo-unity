using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ONNX.PythonReceiver
{
    public class ComparisonExperiment : MonoBehaviour
    {
        private static bool isRecording = false;
        private static List<float[]> oculusReadings = new();
        private static List<float[]> modelReadings = new();
        private static List<float[]> leapReadings = new();
        private static List<int[]> occluded = new();
        
        private void Start()
        {
            ButtonControls.ExperimentButtonToggledEvent.AddListener(ExperimentButtonToggled);
        }

        public static void StartExperiment()
        {
            // Clear all the lists
            oculusReadings.Clear();
            modelReadings.Clear();
            leapReadings.Clear();
            
            isRecording = true;
        }

        public static void StopExperiment()
        {
            isRecording = false;
            
            // Assert that all readings have the same length
            if (oculusReadings.Count != modelReadings.Count || oculusReadings.Count != leapReadings.Count)
            {
                Debug.LogError("Number of readings do not match!");
                return;
            }
            
            // Export the data to a csv file
            StringBuilder sb = new StringBuilder("Oculus1,Oculus2,Oculus3,Oculus4,Oculus5,Oculus6,Oculus7,Oculus8,Model1,Model2,Model3,Model4,Model5,Model6,Model7,Model8,Leap1,Leap2,Leap3,Leap4,Leap5,Leap6,Leap7,Leap8,Occluded1,Occluded2,Occluded3,Occluded4,Occluded5,Occluded6,Occluded7,Occluded8,\n");
            for (int i = 0; i < oculusReadings.Count; i++)
            {
                for (int j = 0; j < oculusReadings[i].Length; j++)
                {
                    sb.Append(oculusReadings[i][j] + ",");
                }
                for (int j = 0; j < modelReadings[i].Length; j++)
                {
                    sb.Append(modelReadings[i][j] + ",");
                }
                for (int j = 0; j < leapReadings[i].Length; j++)
                {
                    sb.Append(leapReadings[i][j] + ",");
                }
                for (int j = 0; j < occluded[i].Length; j++)
                {
                    sb.Append(occluded[i][j] + ",");
                }
                sb.Append("\n");
            }
            System.IO.File.WriteAllText("Assets/ONNX/ComparisonExperimentResults.csv", sb.ToString());
        }
        
        public static void AddReadings(float[] model)
        {
            // This guard is for when the ZMQServer mode is set to experiment but we haven't started the experiment yet
            // In this case readings will come in but we don't want to record them yet.
            if(!isRecording)
                return;
            
            oculusReadings.Add(Finetuning.GetLastOVRReadingForExperiment());
            modelReadings.Add(model);
            leapReadings.Add(LeapMotionReceiver.GetLastLeapReading());
            occluded.Add(FingerOcclusionTracker.GetOccludedArray());
        }
        
        private void ExperimentButtonToggled(bool isToggled)
        {
            if(isToggled)
                StartExperiment();
            else
                StopExperiment();
        }
    }
}