using UnityEngine;
using TMPro;

public class SASLAlphabetController : MonoBehaviour
{
    public TMP_Text letterText;
    public Renderer handSignRenderer;

    private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int currentLetter = 0;

    private string feedbackMessage = "";
    private float feedbackUntil = 0f;

    public string CurrentLetter =>
        alphabet[currentLetter].ToString();

    void Start()
    {
        UpdateLetter();
    }

    void Update()
    {
        // Alphabet navigation
        if (Input.GetKeyDown(KeyCode.N))
            NextLetter();

        if (Input.GetKeyDown(KeyCode.P))
            PreviousLetter();

        // Temporary fake recogniser
        if (Input.GetKeyDown(KeyCode.Alpha1))
            CheckPrediction("A");

        if (Input.GetKeyDown(KeyCode.Alpha2))
            CheckPrediction("B");

        if (Input.GetKeyDown(KeyCode.Alpha3))
            CheckPrediction("C");

        if (Time.time > feedbackUntil)
            feedbackMessage = "";
    }

    public void NextLetter()
    {
        if (currentLetter < alphabet.Length - 1)
        {
            currentLetter++;
            UpdateLetter();
        }
    }

    public void PreviousLetter()
    {
        if (currentLetter > 0)
        {
            currentLetter--;
            UpdateLetter();
        }
    }

    public void CheckPrediction(string predictedLetter)
    {
        if (string.IsNullOrWhiteSpace(predictedLetter))
            return;

        predictedLetter =
            predictedLetter.Trim().ToUpperInvariant();

        bool correct =
            predictedLetter == CurrentLetter;

        feedbackMessage = correct
            ? "Correct!"
            : $"Try again - detected {predictedLetter}";

        feedbackUntil = Time.time + 2f;

        Debug.Log(
            $"Recognition: expected {CurrentLetter}, " +
            $"detected {predictedLetter}, correct={correct}"
        );
    }

    void UpdateLetter()
    {
        string letter = CurrentLetter;

        if (letterText != null)
            letterText.text = letter;

        feedbackMessage = "";

        Texture2D texture =
            Resources.Load<Texture2D>($"Signs/{letter}");

        if (texture != null && handSignRenderer != null)
        {
            handSignRenderer.material.mainTexture = texture;

            Debug.Log(
                $"SASLAlphabetController: showing sign {letter}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"SASLAlphabetController: no image found for {letter}"
            );
        }
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(feedbackMessage))
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 28;
        style.alignment = TextAnchor.MiddleCenter;

        float width = 420f;
        float height = 70f;

        Rect box = new Rect(
            (Screen.width - width) / 2f,
            Screen.height - 120f,
            width,
            height
        );

        GUI.Box(box, feedbackMessage, style);
    }
}
