using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimpleSASLRecognizer : MonoBehaviour
{
    [Serializable]
    public class PoseSample
    {
        public float[] values;
    }

    [Serializable]
    public class LetterTemplate
    {
        public string letter;
        public List<PoseSample> samples = new();
    }

    [Serializable]
    public class TemplateCollection
    {
        public List<LetterTemplate> templates = new();
    }

    [SerializeField] private float rejectionThreshold = 0.12f;

    private MediaPipeHandDataSource handSource;
    private SASLAlphabetController alphabetController;

    private TemplateCollection templateData = new();

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "SASLTemplatesMulti.json"
        );

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindFirstObjectByType<SimpleSASLRecognizer>() == null)
        {
            GameObject obj = new GameObject(
                "Simple SASL Recognizer"
            );

            obj.AddComponent<SimpleSASLRecognizer>();
        }
    }

    void Start()
    {
        handSource =
            FindFirstObjectByType<MediaPipeHandDataSource>();

        alphabetController =
            FindFirstObjectByType<SASLAlphabetController>();

        LoadTemplates();

        Debug.Log(
            "SimpleSASLRecognizer ready. " +
            "T = add training sample, G = recognise."
        );
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            TrainCurrentLetter();

        if (Input.GetKeyDown(KeyCode.G))
            RecogniseCurrentPose();

        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("UNKNOWN TRAIN REQUEST");
            TrainUnknownPose();
        }
    }

    void TrainCurrentLetter()
    {
        if (alphabetController == null)
        {
            Debug.LogWarning(
                "SASLAlphabetController not found."
            );
            return;
        }

        float[] pose = GetNormalisedPose();

        if (pose == null)
        {
            Debug.LogWarning(
                "No valid hand detected."
            );
            return;
        }

        string letter =
            alphabetController.CurrentLetter;

        LetterTemplate template =
            templateData.templates.Find(
                x => string.Equals(
                    x.letter,
                    letter,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (template == null)
        {
            template = new LetterTemplate
            {
                letter = letter
            };

            templateData.templates.Add(template);
        }

        template.samples.Add(
            new PoseSample
            {
                values = pose
            }
        );

        SaveTemplates();

        Debug.Log(
            $"TRAINED {letter} | samples={template.samples.Count}"
        );
    }

    void TrainUnknownPose()
    {
        float[] pose = GetNormalisedPose();

        if (pose == null)
        {
            Debug.LogWarning("No valid hand detected for UNKNOWN training.");
            return;
        }

        const string unknownLabel = "__UNKNOWN__";

        LetterTemplate template =
            templateData.templates.Find(
                x => string.Equals(
                    x.letter,
                    unknownLabel,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (template == null)
        {
            template = new LetterTemplate
            {
                letter = unknownLabel
            };

            templateData.templates.Add(template);
        }

        template.samples.Add(
            new PoseSample
            {
                values = pose
            }
        );

        SaveTemplates();

        Debug.Log(
            $"TRAINED UNKNOWN | samples={template.samples.Count}"
        );
    }
    void RecogniseCurrentPose()
    {
        float[] pose = GetNormalisedPose();

        if (pose == null)
        {
            Debug.LogWarning("No valid hand detected for UNKNOWN training.");
            return;
        }

        if (templateData.templates.Count == 0)
        {
            Debug.LogWarning("No SASL templates trained yet.");
            return;
        }

        string bestLetter = "";
        float bestAverage = float.MaxValue;

        foreach (LetterTemplate template in templateData.templates)
        {
            if (template.samples == null ||
                template.samples.Count == 0)
                continue;

            List<float> distances = new();

            foreach (PoseSample sample in template.samples)
            {
                if (sample.values == null ||
                    sample.values.Length != pose.Length)
                    continue;

                distances.Add(
                    CalculateDistance(
                        pose,
                        sample.values
                    )
                );
            }

            if (distances.Count == 0)
                continue;

            distances.Sort();

            int count = Mathf.Min(3, distances.Count);

            float total = 0f;

            for (int i = 0; i < count; i++)
                total += distances[i];

            float average = total / count;

            Debug.Log(
                $"MATCH {template.letter} | avgBest3={average:F4}"
            );

            if (average < bestAverage)
            {
                bestAverage = average;
                bestLetter = template.letter;
            }
        }

        if (string.IsNullOrEmpty(bestLetter))
            return;

        if (bestLetter == "__UNKNOWN__")
        {
            Debug.Log(
                $"RECOGNISED: UNKNOWN | negative-template={bestAverage:F4}"
            );

            if (alphabetController != null)
                alphabetController.CheckPrediction("UNKNOWN");

            return;
        }

        if (bestAverage > rejectionThreshold)
        {
            Debug.Log(
                $"RECOGNISED: UNKNOWN | avgBest3={bestAverage:F4}"
            );

            if (alphabetController != null)
                alphabetController.CheckPrediction("UNKNOWN");

            return;
        }

        Debug.Log(
            $"RECOGNISED: {bestLetter} | avgBest3={bestAverage:F4}"
        );

        if (alphabetController != null)
            alphabetController.CheckPrediction(bestLetter);
    }
    float[] GetNormalisedPose()
    {
        if (handSource == null ||
            handSource.CurrentFrame == null)
            return null;

        HandData hand = null;

        if (handSource.CurrentFrame.rightHand != null &&
            handSource.CurrentFrame.rightHand.isTracked)
        {
            hand =
                handSource.CurrentFrame.rightHand;
        }
        else if (
            handSource.CurrentFrame.leftHand != null &&
            handSource.CurrentFrame.leftHand.isTracked)
        {
            hand =
                handSource.CurrentFrame.leftHand;
        }

        if (hand == null ||
            hand.joints == null ||
            hand.joints.Count < 21)
            return null;

        Vector3 wrist =
            hand.joints[0].position;

        Vector3[] points =
            new Vector3[21];

        float maxDistance = 0f;

        for (int i = 0; i < 21; i++)
        {
            points[i] =
                hand.joints[i].position - wrist;

            float d = points[i].magnitude;

            if (d > maxDistance)
                maxDistance = d;
        }

        if (maxDistance < 0.000001f)
            return null;

        float[] result =
            new float[63];

        for (int i = 0; i < 21; i++)
        {
            Vector3 p =
                points[i] / maxDistance;

            result[i * 3] = p.x;
            result[i * 3 + 1] = p.y;
            result[i * 3 + 2] = p.z;
        }

        return result;
    }

    float CalculateDistance(
        float[] a,
        float[] b)
    {
        float total = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            float diff =
                a[i] - b[i];

            total += diff * diff;
        }

        return Mathf.Sqrt(
            total / a.Length
        );
    }

    void SaveTemplates()
    {
        string json =
            JsonUtility.ToJson(
                templateData,
                true
            );

        File.WriteAllText(
            SavePath,
            json
        );

        Debug.Log(
            $"Templates saved to: {SavePath}"
        );
    }

    void LoadTemplates()
    {
        if (!File.Exists(SavePath))
            return;

        string json =
            File.ReadAllText(SavePath);

        TemplateCollection loaded =
            JsonUtility.FromJson
            <TemplateCollection>(json);

        if (loaded != null)
            templateData = loaded;

        int totalSamples = 0;

        foreach (LetterTemplate template
                 in templateData.templates)
        {
            if (template.samples != null)
                totalSamples +=
                    template.samples.Count;
        }

        Debug.Log(
            $"Loaded {templateData.templates.Count} " +
            $"letters with {totalSamples} samples."
        );
    }
}



