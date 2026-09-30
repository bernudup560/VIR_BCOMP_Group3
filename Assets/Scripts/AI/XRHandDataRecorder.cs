using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GestureRecording
{
    public string label;
    public string signerId;
    public List<HandFrameData> frames = new();
}

public class XRHandDataRecorder : MonoBehaviour
{
    [Header("References")]
    public HandDataSource handDataSource;

    [Header("Recording")]
    public string gestureLabel = "TEST";
    public string signerId = "signer_01";

    private GestureRecording currentRecording;
    private bool isRecording;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartRecording();

        if (Input.GetKeyDown(KeyCode.S))
            StopRecording();

        if (isRecording &&
            handDataSource != null &&
            handDataSource.CurrentFrame != null)
        {
            currentRecording.frames.Add(
                CopyFrame(handDataSource.CurrentFrame)
            );
        }
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning(
                "XRHandDataRecorder: Recording is already active."
            );
            return;
        }

        currentRecording = new GestureRecording
        {
            label = gestureLabel,
            signerId = signerId,
            frames = new List<HandFrameData>()
        };

        isRecording = true;

        Debug.Log(
            $"XRHandDataRecorder: Recording started for '{gestureLabel}'."
        );
    }

    public void StopRecording()
    {
        if (!isRecording || currentRecording == null)
            return;

        isRecording = false;

        string json = JsonUtility.ToJson(currentRecording, true);

        string recordingsFolder = Path.Combine(
            Application.persistentDataPath,
            "HandRecordings"
        );

        Directory.CreateDirectory(recordingsFolder);

        string safeLabel = string.IsNullOrWhiteSpace(gestureLabel)
            ? "UNLABELLED"
            : gestureLabel.Replace(" ", "_");

        string fileName =
            $"{safeLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        string filePath = Path.Combine(
            recordingsFolder,
            fileName
        );

        File.WriteAllText(filePath, json);

        Debug.Log(
            $"XRHandDataRecorder: Saved {currentRecording.frames.Count} frames to {filePath}"
        );
    }

    private HandFrameData CopyFrame(HandFrameData source)
    {
        return new HandFrameData
        {
            timestamp = source.timestamp,
            leftHand = CopyHand(source.leftHand),
            rightHand = CopyHand(source.rightHand)
        };
    }

    private HandData CopyHand(HandData source)
    {
        HandData copy = new HandData
        {
            isTracked = source.isTracked,
            joints = new List<HandJointData>()
        };

        foreach (HandJointData joint in source.joints)
        {
            copy.joints.Add(new HandJointData
            {
                jointName = joint.jointName,
                position = joint.position,
                rotation = joint.rotation,
                tracked = joint.tracked
            });
        }

        return copy;
    }
}
