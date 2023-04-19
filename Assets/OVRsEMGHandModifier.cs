using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

public class OVRsEMGHandModifier : MonoBehaviour
{
	// For which hand this component is used
	private OVRHand.Hand HandType { get; set; }
	private OVRHand _hand;
	private HandVisual _handVisual;
	private List<Transform> _jointTransforms;
	
	// Make a float array to hold the Hu 2022 joint data
	private float[] jointData = new float[MyoClassificationTF.OUTPUT_DIM];
	
	private int counter = 0;
	// Joint mappings from Hu 2022 dataset to Oculus SDK
	private Dictionary<int, int> jointMappings = new()
	{
		// { 0, 6 },   // Index MCP
		{ 0, 6 },   // Index PIP 
		{ 1, 7 },   // Index IP
		// { 3, 9 },   // Middle MCP
		{ 2, 9 },  // Middle PIP
		{ 3, 10 },  // Middle IP
		// { 7, 12 },  // Ring MCP
		{ 4, 12 },  // Ring PIP
		{ 5, 13 },  // Ring IP
		// { 11, 16 }, // Pinky MCP
		{ 6, 16 }, // Pinky PIP
		{ 7, 17 }  // Pinky IP
	};
	// Maps from Oculus SDK joint indices to INTERMEDIATE Hu 2022 joint indices to apply the formula theta = 2/3 * theta_dip
	private Dictionary<int, int> distalMappings = new()
	{
		{ 8, 1 },
		{ 11, 3 },
		{ 14, 5 },
		{ 18, 7 },
	};

	// Map from Hu 2022 abduction values (angle between index-middle, middle-ring and ring-pinky) to Oculus SDK
	private Dictionary<int, (int, int)> abductionMappings = new()
	{
		{ 6, (6, 9) }, // Index-Middle
		{ 10, (9, 12) }, // Middle-Ring
		{ 14, (12, 16) } // Ring-Pinky
	};

	private void Start()
	{
		// Get hand and hand state from OVRHand component
		// _hand = GetComponent<OVRHand>();
		// HandType = _hand.HandType;
		// Subscribe to OVRHand state updated event
		// -------------------------------- IMPORTANT --------------------------------
		// When the Oculus SDK is updated, this event in OVRHand.cs needs to be added in again -.-
		// _hand.HandStateUpdatedEvent.AddListener(OnHandStateUpdated);
		_handVisual = GetComponent<HandVisual>();
		_jointTransforms = (List<Transform>)_handVisual.Joints;

		GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
	}

	private void LateUpdate()
	{
		GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;
		
		// Loop over all values from joint mappings
		foreach (var mapping in jointMappings)
		{
			var rotationValue = Mathf.Abs(jointData[mapping.Key]);

			// _hand._handState.BoneRotations[mapping.Value] = q2ovrq(Quaternion.Euler(0, 0, rotationValue));
			GetComponent<HandVisual>().Joints[mapping.Value].localRotation = Quaternion.Euler(0, 0, -rotationValue);
		}

		foreach (var mapping in distalMappings)
		{
			var oculusSDKIdx = mapping.Key;
			var hu2022IntermediateIdx = mapping.Value;
			var intermediateRotationValue = Mathf.Abs(jointData[hu2022IntermediateIdx]);
			var distalRotationValue = 2f / 3f * intermediateRotationValue;
			GetComponent<HandVisual>().Joints[oculusSDKIdx].localRotation = Quaternion.Euler(0, 0, -distalRotationValue);
		}

		// foreach (var mapping in abductionMappings)
		// {
		// 	// Get the abduction value from the Hu 2022 dataset
		// 	var abductionValue = jointData[counter, mapping.Key];
		// 	// Split the abduction value into two abduction values for the two joints
		// 	abductionValue /= 2f;
		// 	// Convert the abduction value to a quaternion for each finger
		// 	var abductionJointRotation1 = Quaternion.Euler(0, -abductionValue, 0);
		// 	var abductionJointRotation2 = Quaternion.Euler(0, abductionValue, 0);
		//
		// 	// Set the rotation of the abduction joint
		// 	_hand._handState.BoneRotations[mapping.Value.Item1] = q2ovrq(qovr2q(_hand._handState.BoneRotations[mapping.Value.Item1]) * abductionJointRotation1);
		// 	_hand._handState.BoneRotations[mapping.Value.Item2] = q2ovrq(qovr2q(_hand._handState.BoneRotations[mapping.Value.Item2]) * abductionJointRotation2);
		// 		
		// }
			
		// counter++;
		// if(counter > 999)
		// {
		// 	counter = 0;
		// }
	}

	/// <summary>
	/// Callback from OVRHand.cs when hand state is updated
	/// </summary>
	private void OnHandStateUpdated()
	{
		// If hand is left, rotate middle finger bone 180 degrees around the x axis
		if (HandType == OVRHand.Hand.HandRight)
		{
			// Loop over all values from joint mappings
			foreach (var mapping in jointMappings)
			{
				var rotationValue = Mathf.Abs(jointData[mapping.Key]);
				// If we're dealing with a DIP joint, use the theta = 2/3 * theta_dip formula
				if (mapping.Key is 2 or 5 or 9 or 13)
				{
					rotationValue = 2f / 3f * Mathf.Abs(jointData[mapping.Key - 1]);
				}
				
				// if (rotationValue < 0)
					// rotationValue = 360 - rotationValue;
				
				_hand._handState.BoneRotations[mapping.Value] = q2ovrq(Quaternion.Euler(0, 0, rotationValue));
			}

			counter++;
			if(counter > 999)
			{
				counter = 0;
			}
		}
	}

	/// <summary>
	/// Convert Quaternion to OVRPlugin.Quatf
	/// </summary>
	/// <param name="q"></param>
	/// <returns></returns>
	private OVRPlugin.Quatf q2ovrq(Quaternion q)
	{
		var result = new OVRPlugin.Quatf();
		result.x = q.x;
		result.y = q.y;
		result.z = q.z;
		result.w = q.w;
		return result;
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

	public void UpdateJointData(float[] resultValues)
	{
		for(var i = 0; i < resultValues.Length; i++)
		{
			jointData[i] = resultValues[i];
		}
	}
}