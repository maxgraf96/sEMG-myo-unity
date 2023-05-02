using System.Collections;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using UnityEngine;

public class LeapMotionReceiver : MonoBehaviour
{
    public LeapProvider leapProvider;
    private static float[] lastLeapReading = new float[8];

    private void OnEnable()
    {
        leapProvider.OnUpdateFrame += OnUpdateFrame;
    }
    private void OnDisable()
    {
        leapProvider.OnUpdateFrame -= OnUpdateFrame;
    }

    void OnUpdateFrame(Frame frame)
    {
        //Use a helpul utility function to get the first hand that matches the Chirality
        Hand _rightHand = frame.GetHand(Chirality.Right);
        if(_rightHand != null)
        {
            Quaternion handRotation = _rightHand.Rotation;

            foreach (var finger in _rightHand.Fingers)
            {
                if (finger.Type is not (Finger.FingerType.TYPE_INDEX or Finger.FingerType.TYPE_MIDDLE
                    or Finger.FingerType.TYPE_RING or Finger.FingerType.TYPE_PINKY))
                    continue;
                
                var proximal = finger.bones[1];
                var intermediate = finger.bones[2];
                
                Quaternion proximalBoneRotation = proximal.Rotation;
                Quaternion proximalBoneRelativeToHand = Quaternion.Inverse(handRotation) * proximalBoneRotation;
                Quaternion intermediateBoneRotation = intermediate.Rotation;
                Quaternion intermediateBoneRelativeToHand = Quaternion.Inverse(handRotation) * intermediateBoneRotation;
                
                // This is super confusing - leap motion uses a different coordinate system than Unity
                // So to get the equivalent of the z rotation in Unity, we need to use the x rotation in leap motion
                var proxRot = proximalBoneRelativeToHand.eulerAngles.x;
                var interRot = intermediateBoneRelativeToHand.eulerAngles.x;
                if(proxRot > 180)
                    proxRot = -360 + proxRot;
                if(interRot > 180)
                    interRot = -360 + interRot;
                
                switch (finger.Type)
                {
                    case Finger.FingerType.TYPE_INDEX:
                        lastLeapReading[0] = proxRot;
                        lastLeapReading[1] = interRot;
                        break;
                    case Finger.FingerType.TYPE_MIDDLE:
                        lastLeapReading[2] = proxRot;
                        lastLeapReading[3] = interRot;
                        break;
                    case Finger.FingerType.TYPE_RING:
                        lastLeapReading[4] = proxRot;
                        lastLeapReading[5] = interRot;
                        break;
                    case Finger.FingerType.TYPE_PINKY:
                        lastLeapReading[6] = proxRot;
                        lastLeapReading[7] = interRot;
                        break;
                }
            }
            
            // print("Last leap reading is: " + lastLeapReading[0] + ", " + lastLeapReading[1] + ", " + lastLeapReading[2] + ", " + lastLeapReading[3] + ", " + lastLeapReading[4] + ", " + lastLeapReading[5] + ", " + lastLeapReading[6] + ", " + lastLeapReading[7]);
            
        }
    }

    public static float[] GetLastLeapReading()
    {
        var reading = new[] {lastLeapReading[0], lastLeapReading[1], lastLeapReading[2], lastLeapReading[3], lastLeapReading[4], lastLeapReading[5], lastLeapReading[6], lastLeapReading[7]};
        return reading;
    }
}
