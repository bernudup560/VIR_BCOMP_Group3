using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class XRHandDataReader : HandDataSource
{
    private XRHandSubsystem handSubsystem;
    private readonly List<XRHandSubsystem> handSubsystems = new();

    private void Start()
    {
        FindHandSubsystem();
    }

    private void FindHandSubsystem()
    {
        handSubsystems.Clear();
        SubsystemManager.GetSubsystems(handSubsystems);

        if (handSubsystems.Count == 0)
        {
            Debug.LogWarning("XRHandDataReader: No XRHandSubsystem found.");
            return;
        }

        handSubsystem = handSubsystems[0];

        Debug.Log(
            "XRHandDataReader: XR Hand subsystem found. Running: "
            + handSubsystem.running
        );
    }

    private void Update()
    {
        if (handSubsystem == null)
        {
            FindHandSubsystem();
            return;
        }

        CurrentFrame = new HandFrameData
        {
            timestamp = Time.timeAsDouble,
            leftHand = BuildHandData(handSubsystem.leftHand),
            rightHand = BuildHandData(handSubsystem.rightHand)
        };
    }

    private HandData BuildHandData(XRHand hand)
    {
        HandData handData = new HandData
        {
            isTracked = hand.isTracked
        };

        if (!hand.isTracked)
            return handData;

        for (int i = (int)XRHandJointID.BeginMarker + 1;
             i < (int)XRHandJointID.EndMarker;
             i++)
        {
            XRHandJointID jointId = (XRHandJointID)i;
            XRHandJoint joint = hand.GetJoint(jointId);

            HandJointData jointData = new HandJointData
            {
                jointName = jointId.ToString(),
                tracked = false,
                position = Vector3.zero,
                rotation = Quaternion.identity
            };

            if (joint.TryGetPose(out Pose pose))
            {
                jointData.tracked = true;
                jointData.position = pose.position;
                jointData.rotation = pose.rotation;
            }

            handData.joints.Add(jointData);
        }

        return handData;
    }
}
