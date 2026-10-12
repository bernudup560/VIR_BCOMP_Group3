using System.Collections.Generic;
using UnityEngine;

public class MockHandDataSource : HandDataSource
{
    [Header("Mock movement")]
    public float movementSpeed = 1f;
    public float movementAmount = 0.05f;

    private void Update()
    {
        CurrentFrame = new HandFrameData
        {
            timestamp = Time.timeAsDouble,
            leftHand = CreateMockHand(-0.2f),
            rightHand = CreateMockHand(0.2f)
        };
    }

    private HandData CreateMockHand(float xOffset)
    {
        HandData hand = new HandData
        {
            isTracked = true,
            joints = new List<HandJointData>()
        };

        float movement =
            Mathf.Sin(Time.time * movementSpeed) * movementAmount;

        for (int i = 0; i < 26; i++)
        {
            float fingerOffset = i * 0.005f;

            hand.joints.Add(new HandJointData
            {
                jointName = "MockJoint_" + i,
                tracked = true,
                position = new Vector3(
                    xOffset + fingerOffset,
                    1.2f + movement,
                    0.5f
                ),
                rotation = Quaternion.identity
            });
        }

        return hand;
    }
}
