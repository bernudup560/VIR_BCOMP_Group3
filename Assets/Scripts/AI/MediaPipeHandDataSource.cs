using System.Collections.Generic;
using UnityEngine;
using LandmarkPoint = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;

public class MediaPipeHandDataSource : HandDataSource
{
    public void UpdateFromLandmarks(IReadOnlyList<LandmarkPoint> landmarks)
    {
        var frame = new HandFrameData
        {
            timestamp = Time.timeAsDouble,
            leftHand = new HandData { isTracked = false },
            rightHand = new HandData { isTracked = false }
        };

        if (landmarks == null || landmarks.Count < 21)
        {
            CurrentFrame = frame;
            return;
        }

        var hand = new HandData
        {
            isTracked = true,
            joints = new List<HandJointData>()
        };

        for (int i = 0; i < 21; i++)
        {
            var landmark = landmarks[i];

            hand.joints.Add(new HandJointData
            {
                jointName = "MediaPipe_" + i,
                tracked = true,
                position = new Vector3(
                    landmark.x,
                    landmark.y,
                    landmark.z
                ),
                rotation = Quaternion.identity
            });
        }

        // For now MediaPipe is configured to track one hand.
        // Store it as rightHand until handedness support is added.
        frame.rightHand = hand;

        CurrentFrame = frame;
    }

    public void ClearTracking()
    {
        CurrentFrame = new HandFrameData
        {
            timestamp = Time.timeAsDouble,
            leftHand = new HandData { isTracked = false },
            rightHand = new HandData { isTracked = false }
        };
    }
}