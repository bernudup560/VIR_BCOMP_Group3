using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GestureRecording
{
    public string label;
    public string signerId;
    public string dataSource;
    public string recordedAt;
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

    private void Start()
    {
        ReportDataSource();
    }

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

    private void ReportDataSource()
    {
        if (handDataSource == null)
        {
            Debug.LogError(
                "XRHandDataRecorder: NO HAND DATA SOURCE ASSIGNED."
            );
            return;
        }

        string sourceName = handDataSource.GetType().Name;

        if (handDataSource is MockHandDataSource)
        {
            Debug.LogWarning(
                "XRHandDataRecorder: USING MOCK HAND DATA."
            );
        }
        else if (handDataSource is XRHandDataReader)
        {
            Debug.Log(
                "XRHandDataRecorder: USING LIVE XR HAND DATA."
            );
        }
        else
        {
            Debug.Log(
                $"XRHandDataRecorder: Using source {sourceName}."
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

        if (handDataSource == null)
        {
            Debug.LogError(
                "XRHandDataRecorder: Cannot record because no Hand Data Source is assigned."
            );
            return;
        }

        string sourceName = handDataSource.GetType().Name;

        currentRecording = new GestureRecording
        {
            label = gestureLabel,
            signerId = signerId,
            dataSource = sourceName,
            recordedAt = DateTime.Now.ToString("o"),
            frames = new List<HandFrameData>()
        };

        isRecording = true;

        Debug.Log(
            $"XRHandDataRecorder: START '{gestureLabel}' | signer={signerId} | source={sourceName}"
        );
    }

    public void StopRecording()
    {
        if (!isRecording || currentRecording == null)
            return;

        isRecording = false;

        int leftTrackedFrames = 0;
        int rightTrackedFrames = 0;

        foreach (HandFrameData frame in currentRecording.frames)
        {
            if (frame.leftHand != null && frame.leftHand.isTracked)
                leftTrackedFrames++;

            if (frame.rightHand != null && frame.rightHand.isTracked)
                rightTrackedFrames++;
        }

        int totalFrames = currentRecording.frames.Count;

        if (totalFrames == 0)
        {
            Debug.LogError(
                "XRHandDataRecorder: No frames were captured. Recording not saved."
            );
            return;
        }

        string json = JsonUtility.ToJson(currentRecording, true);

        string recordingsFolder = Path.Combine(
            Application.persistentDataPath,
            "HandRecordings"
        );

        Directory.CreateDirectory(recordingsFolder);

        string safeLabel = MakeSafeFilePart(gestureLabel);
        string safeSigner = MakeSafeFilePart(signerId);

        string fileName =
            $"{safeLabel}_{safeSigner}_{DateTime.Now:yyyyMMdd_HHmmss}.json";

        string filePath = Path.Combine(
            recordingsFolder,
            fileName
        );

        File.WriteAllText(filePath, json);

        Debug.Log(
            $"XRHandDataRecorder: SAVED '{gestureLabel}' | " +
            $"frames={totalFrames} | " +
            $"leftTracked={leftTrackedFrames}/{totalFrames} | " +
            $"rightTracked={rightTrackedFrames}/{totalFrames} | " +
            $"source={currentRecording.dataSource} | " +
            $"file={filePath}"
        );

        if (leftTrackedFrames == 0 && rightTrackedFrames == 0)
        {
            Debug.LogWarning(
                "XRHandDataRecorder: WARNING - recording contains NO tracked hands."
            );
        }
    }

    private string MakeSafeFilePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "UNKNOWN";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(" ", "_");
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
