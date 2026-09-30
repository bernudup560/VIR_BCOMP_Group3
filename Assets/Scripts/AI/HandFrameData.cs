using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HandJointData
{
    public string jointName;
    public Vector3 position;
    public Quaternion rotation;
    public bool tracked;
}

[Serializable]
public class HandData
{
    public bool isTracked;
    public List<HandJointData> joints = new();
}

[Serializable]
public class HandFrameData
{
    public double timestamp;
    public HandData leftHand = new();
    public HandData rightHand = new();
}
