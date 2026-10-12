using System.Collections.Generic;
using UnityEngine;
using LandmarkPoint = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;

/// <summary>
/// Builds a simple visible hand (joints + bones) from MediaPipe landmarks
/// and places it in front of the main camera in Game view.
/// </summary>
public class WebcamHandVisualizer : MonoBehaviour
{
    static readonly int[][] Bones =
    {
        new[] { 0, 1, 2, 3, 4 },
        new[] { 0, 5, 6, 7, 8 },
        new[] { 0, 9, 10, 11, 12 },
        new[] { 0, 13, 14, 15, 16 },
        new[] { 0, 17, 18, 19, 20 },
        new[] { 5, 9, 13, 17 }
    };

    [Header("Placement")]
    [SerializeField] Camera targetCamera;
    [SerializeField] float distanceFromCamera = 0.55f;
    [SerializeField] float handWidth = 0.28f;
    [SerializeField] float handHeight = 0.28f;
    [SerializeField] float depthScale = 0.18f;

    [Header("Look")]
    [SerializeField] Color jointColor = new Color(1f, 0.82f, 0.68f, 1f);
    [SerializeField] Color boneColor = new Color(0.95f, 0.72f, 0.55f, 1f);
    [SerializeField] float jointRadius = 0.012f;
    [SerializeField] float boneRadius = 0.006f;
    [SerializeField] bool hideWhenLost = true;

    Transform _root;
    Transform[] _joints;
    Transform[] _boneTransforms;
    Vector3[] _boneScales;
    Material _jointMat;
    Material _boneMat;
    bool _built;
    bool _visible;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        EnsureBuilt();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (_jointMat != null)
            Destroy(_jointMat);
        if (_boneMat != null)
            Destroy(_boneMat);
        if (_root != null)
            Destroy(_root.gameObject);
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        if (_root != null)
            _root.gameObject.SetActive(visible);
    }

    public void UpdateFromLandmarks(IReadOnlyList<LandmarkPoint> landmarks)
    {
        if (landmarks == null || landmarks.Count < 21)
        {
            if (hideWhenLost)
                SetVisible(false);
            return;
        }

        EnsureBuilt();
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (!_visible)
            SetVisible(true);

        // Anchor the hand rig in front of the camera each frame.
        var cam = targetCamera.transform;
        _root.SetPositionAndRotation(
            cam.position + cam.forward * distanceFromCamera,
            cam.rotation);

        for (int i = 0; i < 21; i++)
            _joints[i].localPosition = LandmarkToLocal(landmarks[i]);

        int boneIndex = 0;
        foreach (var chain in Bones)
        {
            for (int i = 0; i < chain.Length - 1; i++, boneIndex++)
            {
                var a = _joints[chain[i]].localPosition;
                var b = _joints[chain[i + 1]].localPosition;
                var mid = (a + b) * 0.5f;
                var dir = b - a;
                float length = dir.magnitude;
                var bone = _boneTransforms[boneIndex];
                bone.localPosition = mid;
                if (length > 0.0001f)
                    bone.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                var scale = _boneScales[boneIndex];
                scale.y = length * 0.5f;
                bone.localScale = scale;
            }
        }
    }

    Vector3 LandmarkToLocal(LandmarkPoint landmark)
    {
        // MediaPipe: x right, y down. Flip Y so the hand is upright in Game view.
        float x = (landmark.x - 0.5f) * handWidth;
        float y = (landmark.y - 0.5f) * handHeight;
        float z = -landmark.z * depthScale;
        return new Vector3(x, y, z);
    }

    void EnsureBuilt()
    {
        if (_built)
            return;

        _root = new GameObject("WebcamHandMesh").transform;
        _root.SetParent(transform, false);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        _jointMat = CreateColoredMaterial(shader, jointColor);
        _boneMat = CreateColoredMaterial(shader, boneColor);

        _joints = new Transform[21];
        for (int i = 0; i < 21; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Joint_" + i;
            sphere.transform.SetParent(_root, false);
            sphere.transform.localScale = Vector3.one * (i == 0 ? jointRadius * 1.6f : jointRadius);
            var col = sphere.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = _jointMat;
            _joints[i] = sphere.transform;
        }

        int boneCount = 0;
        foreach (var chain in Bones)
            boneCount += chain.Length - 1;

        _boneTransforms = new Transform[boneCount];
        _boneScales = new Vector3[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            var bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bone.name = "Bone_" + i;
            bone.transform.SetParent(_root, false);
            var col = bone.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            bone.GetComponent<MeshRenderer>().sharedMaterial = _boneMat;
            _boneScales[i] = new Vector3(boneRadius, 0.01f, boneRadius);
            bone.transform.localScale = _boneScales[i];
            _boneTransforms[i] = bone.transform;
        }

        _built = true;
    }

    static Material CreateColoredMaterial(Shader shader, Color color)
    {
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        mat.color = color;
        return mat;
    }
}
