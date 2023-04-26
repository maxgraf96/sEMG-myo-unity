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
	private float[] jointData = new float[MyoClassification.OUTPUT_DIM];
	
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

	private void Start()
	{
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

		// foreach (var mapping in distalMappings)
		// {
		// 	var oculusSDKIdx = mapping.Key;
		// 	var hu2022IntermediateIdx = mapping.Value;
		// 	var intermediateRotationValue = Mathf.Abs(jointData[hu2022IntermediateIdx]);
		// 	var distalRotationValue = 2f / 3f * intermediateRotationValue;
		// 	GetComponent<HandVisual>().Joints[oculusSDKIdx].localRotation = Quaternion.Euler(0, 0, -distalRotationValue);
		// }
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