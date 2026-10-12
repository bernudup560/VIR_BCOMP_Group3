using System.Collections;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using UnityEngine;
using LandmarkPoint = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;
using MpProtobuf = Mediapipe.Protobuf;

/// <summary>
/// Webcam hand tracking (MediaPipe) with no debug overlay.
/// Gestures drive SASL UI: swipe = next/previous, pinch = confirm (trainer).
/// Meta XR hand objects can remain in the scene for headset use later.
/// </summary>
public class HandUiController : MonoBehaviour
{
    const string ModelName = "hand_landmarker.bytes";
    const int Wrist = 0;
    const int ThumbTip = 4;
    const int IndexTip = 8;
    const int PinkyTip = 20;

    public enum UiTarget
    {
        Auto,
        GestureTrainer,
        Alphabet,
        Numbers
    }

    [Header("UI targets (auto-found if empty)")]
    [SerializeField] UiTarget uiTarget = UiTarget.Auto;
    [SerializeField] GestureTrainer gestureTrainer;
    [SerializeField] SASLAlphabetController alphabetController;
    [SerializeField] SASLNumberController numberController;

    [Header("Tracking")]
    [SerializeField] int preferredCameraIndex;
    [SerializeField] bool flipHorizontally = true;
    [SerializeField] bool logGestures = true;

    [Header("Visible hand")]
    [SerializeField] bool showHandMesh = true;
    [SerializeField] WebcamHandVisualizer handVisualizer;

    [Header("Gesture tuning")]
    [SerializeField] float swipeThreshold = 0.12f;
    [SerializeField] float pinchThreshold = 0.05f;
    [SerializeField] float actionCooldownSeconds = 0.7f;

    WebCamTexture _webCam;
    TextureFramePool _textureFramePool;
    HandLandmarker _landmarker;
    HandLandmarkerResult _result = HandLandmarkerResult.Alloc(2);

    bool _ready;
    float _cooldownUntil;
    float? _swipeStartX;
    bool _pinchLatched;

    void Awake()
    {
        if (gestureTrainer == null)
            gestureTrainer = FindFirstObjectByType<GestureTrainer>();
        if (alphabetController == null)
            alphabetController = FindFirstObjectByType<SASLAlphabetController>();
        if (numberController == null)
            numberController = FindFirstObjectByType<SASLNumberController>();

        if (handVisualizer == null)
            handVisualizer = GetComponent<WebcamHandVisualizer>();
        if (handVisualizer == null && showHandMesh)
            handVisualizer = gameObject.AddComponent<WebcamHandVisualizer>();
    }

    void OnEnable()
    {
        StartCoroutine(InitializeAndRun());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        Shutdown();
    }

    void OnDestroy()
    {
        Shutdown();
    }

    IEnumerator InitializeAndRun()
    {
        MpProtobuf.SetLogHandler(MpProtobuf.DefaultLogHandler);

#if UNITY_EDITOR
        IResourceManager resources = new LocalResourceManager();
#else
        IResourceManager resources = new StreamingAssetsResourceManager();
#endif
        yield return resources.PrepareAssetAsync(ModelName);

        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("HandUiController: no webcam found.");
            yield break;
        }

        var deviceIndex = Mathf.Clamp(preferredCameraIndex, 0, WebCamTexture.devices.Length - 1);
        _webCam = new WebCamTexture(WebCamTexture.devices[deviceIndex].name, 1280, 720, 30);
        _webCam.Play();

        yield return new WaitUntil(() => _webCam.width > 16);

        var options = new HandLandmarkerOptions(
            new Mediapipe.Tasks.Core.BaseOptions(Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU, modelAssetPath: ModelName),
            runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO,
            numHands: 1,
            minHandDetectionConfidence: 0.5f,
            minHandPresenceConfidence: 0.5f,
            minTrackingConfidence: 0.5f);

        _landmarker = HandLandmarker.CreateFromOptions(options);
        _textureFramePool = new TextureFramePool(_webCam.width, _webCam.height, TextureFormat.RGBA32, 8);
        _ready = true;

        if (logGestures)
            Debug.Log("HandUiController: webcam tracking ready. Swipe = next/prev, pinch = confirm.");

        var waitEndOfFrame = new WaitForEndOfFrame();
        long frameIndex = 0;

        while (isActiveAndEnabled && _ready)
        {
            yield return waitEndOfFrame;

            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                continue;

            textureFrame.ReadTextureOnCPU(_webCam, flipHorizontally, false);
            var image = textureFrame.BuildCPUImage();
            textureFrame.Release();

            frameIndex++;
            var timestampMs = frameIndex * 33;
            if (!_landmarker.TryDetectForVideo(image, timestampMs, null, ref _result))
            {
                HideHandMesh();
                _swipeStartX = null;
                _pinchLatched = false;
                continue;
            }

            if (_result.handLandmarks == null || _result.handLandmarks.Count == 0)
            {
                HideHandMesh();
                _swipeStartX = null;
                _pinchLatched = false;
                continue;
            }

            var landmarks = _result.handLandmarks[0].landmarks;
            UpdateHandMesh(landmarks);
            ProcessHandGestures(landmarks);
        }
    }

    void UpdateHandMesh(List<LandmarkPoint> landmarks)
    {
        if (!showHandMesh || handVisualizer == null)
            return;
        handVisualizer.UpdateFromLandmarks(landmarks);
    }

    void HideHandMesh()
    {
        if (handVisualizer != null)
            handVisualizer.SetVisible(false);
    }

    void ProcessHandGestures(List<LandmarkPoint> landmarks)
    {
        if (landmarks == null || landmarks.Count <= PinkyTip)
            return;

        if (Time.unscaledTime < _cooldownUntil)
            return;

        var wrist = landmarks[Wrist];
        var thumb = landmarks[ThumbTip];
        var index = landmarks[IndexTip];

        float pinchDist = Distance2D(thumb, index);
        bool isPinching = pinchDist < pinchThreshold;

        if (isPinching)
        {
            if (!_pinchLatched)
            {
                _pinchLatched = true;
                FireConfirm();
            }
            _swipeStartX = null;
            return;
        }

        _pinchLatched = false;

        if (_swipeStartX == null)
        {
            _swipeStartX = wrist.x;
            return;
        }

        float delta = wrist.x - _swipeStartX.Value;
        if (Mathf.Abs(delta) < swipeThreshold)
            return;

        // Mirrored webcam: moving hand to your physical right decreases x.
        if (delta < 0f)
            FireNext();
        else
            FirePrevious();

        _swipeStartX = wrist.x;
    }

    UiTarget ResolveTarget()
    {
        if (uiTarget != UiTarget.Auto)
            return uiTarget;

        if (gestureTrainer != null && gestureTrainer.isActiveAndEnabled)
            return UiTarget.GestureTrainer;
        if (alphabetController != null && alphabetController.isActiveAndEnabled)
            return UiTarget.Alphabet;
        if (numberController != null && numberController.isActiveAndEnabled)
            return UiTarget.Numbers;
        return UiTarget.Auto;
    }

    void FireNext()
    {
        _cooldownUntil = Time.unscaledTime + actionCooldownSeconds;
        if (logGestures)
            Debug.Log("Hand UI: Next");

        switch (ResolveTarget())
        {
            case UiTarget.GestureTrainer:
                gestureTrainer.AdvanceToNextSign();
                break;
            case UiTarget.Alphabet:
                alphabetController.NextLetter();
                break;
            case UiTarget.Numbers:
                numberController.NextNumber();
                break;
        }
    }

    void FirePrevious()
    {
        _cooldownUntil = Time.unscaledTime + actionCooldownSeconds;
        if (logGestures)
            Debug.Log("Hand UI: Previous");

        switch (ResolveTarget())
        {
            case UiTarget.Alphabet:
                alphabetController.PreviousLetter();
                break;
            case UiTarget.Numbers:
                numberController.PreviousNumber();
                break;
            case UiTarget.GestureTrainer:
                gestureTrainer.AdvanceToNextSign();
                break;
        }
    }

    void FireConfirm()
    {
        _cooldownUntil = Time.unscaledTime + actionCooldownSeconds;
        if (logGestures)
            Debug.Log("Hand UI: Confirm (pinch)");

        if (ResolveTarget() != UiTarget.GestureTrainer)
            return;

        var id = gestureTrainer.CurrentSignId;
        if (!string.IsNullOrEmpty(id))
            gestureTrainer.CheckGesture(id);
    }

    static float Distance2D(LandmarkPoint a, LandmarkPoint b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    void Shutdown()
    {
        _ready = false;

        _landmarker?.Close();
        _landmarker = null;

        _textureFramePool?.Dispose();
        _textureFramePool = null;

        if (_webCam != null)
        {
            _webCam.Stop();
            Destroy(_webCam);
            _webCam = null;
        }

        HideHandMesh();
        MpProtobuf.ResetLogHandler();
    }
}
