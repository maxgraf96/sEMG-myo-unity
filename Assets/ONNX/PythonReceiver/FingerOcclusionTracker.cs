using System;
using System.Collections.Generic;
using UnityEngine;

namespace ONNX.PythonReceiver
{
    public class FingerOcclusionTracker : MonoBehaviour
    {
        public GameObject _handR;
        public Transform source;
        // Set in Unity Editor. Will contain the proximal and intermediate phalanges positions for index, middle, ring, and pinky fingers
        public List<Transform> targets;
        // Corresponds to the targets list. Will contain a bool for each target, indicating whether it is occluded or not
        public static List<bool> occluded = new();
        
        private void Start() 
        {
            AttachCollidersRecursively(_handR.transform);
            foreach (var target in targets)
            {
                occluded.Add(false);
            }
        }

        private void Update()
        {
            var counter = 0;
            foreach (var target in targets)
            {
                occluded[counter] = false;
                // Get the direction vector from the source to the target
                Vector3 direction = target.position - source.position;

                // Perform a raycast from the source towards the target
                RaycastHit hit;
                if (Physics.Raycast(source.position, direction, out hit))
                {
                    if(hit.collider.gameObject.name == target.gameObject.name)
                        Debug.DrawLine(source.position, target.position, Color.green);
                    else
                    {
                        occluded[counter] = true;
                        // If the raycast hits something, print the name of the object and the distance
                        Debug.Log("Occluded by " + hit.collider.gameObject.name);
                        // Draw a red line in the Scene view for visualization
                        Debug.DrawLine(source.position, hit.point, Color.red);
                    }
                }
                else
                {
                    // If the raycast doesn't hit anything, draw a green line in the Scene view for visualization
                    Debug.DrawLine(source.position, target.position, Color.green);
                }
                
                counter++;
            }
        }
        
        private void AttachCollidersRecursively(Transform currentTransform)
        {
            string currentName = currentTransform.gameObject.name;
            if (currentName.Contains("0") || currentName.Contains("1") || currentName.Contains("2") || currentName.Contains("3") || currentName.Contains("null"))
            {
                // Add a Capsule Collider to the current bone
                CapsuleCollider collider = currentTransform.gameObject.AddComponent<CapsuleCollider>();

                // Configure the Capsule Collider as needed, e.g. set the radius, height, and direction
                collider.radius = 0.008f;
                collider.height = 0.005f;
                collider.direction = 2; // Set the direction to Z-axis (0 = X-axis, 1 = Y-axis, 2 = Z-axis)
            }

            // Iterate through all children of the current bone and call this method recursively
            for (int i = 0; i < currentTransform.childCount; i++)
            {
                AttachCollidersRecursively(currentTransform.GetChild(i));
            }
        }

        public static int[] GetOccludedArray()
        {
            int[] occludedArray = new int[occluded.Count];
            for (int i = 0; i < occluded.Count; i++)
            {
                occludedArray[i] = occluded[i] ? 1 : 0;
            }
            return occludedArray;
        }
    }
}