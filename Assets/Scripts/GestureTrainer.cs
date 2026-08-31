using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Flashcard-style lesson: shows a letter/number and a reference hand image.
/// Call CheckGesture(id) from hand recognition later; Space marks correct while testing.
/// </summary>
public class GestureTrainer : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("e.g. \"Copy this sign\"")]
    public TextMeshProUGUI instructionText;
    [Tooltip("Large letter/number shown to the learner")]
    public TextMeshProUGUI letterText;
    [Tooltip("Reference picture of the hand sign")]
    public Image handImage;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    [Header("Lesson data")]
    public SignLibrary signLibrary;
    public float secondsPerSign = 12f;
    public float lessonTime = 60f;
    public bool randomOrder = true;

    static readonly string[] FallbackIds =
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9"
    };

    SignEntry[] signs;
    string currentSignId;
    int score;
    float timeLeft;
    float signTimeLeft;
    int nextIndex;

    void Start()
    {
        timeLeft = lessonTime;
        if (scoreText != null)
            scoreText.text = "Score: 0";

        signs = BuildSignList();
        if (signs.Length == 0)
        {
            Debug.LogError("GestureTrainer: no signs available.");
            enabled = false;
            return;
        }

        ShowNextSign();
    }

    void Update()
    {
        if (timeLeft <= 0f)
            return;

        timeLeft -= Time.deltaTime;
        signTimeLeft -= Time.deltaTime;

        if (timerText != null)
            timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(0f, timeLeft));

        if (timeLeft <= 0f)
        {
            EndLesson();
            return;
        }

        // Temporary keyboard test until real hand detection is wired
        if (Input.GetKeyDown(KeyCode.Space))
            CheckGesture(currentSignId);

        if (Input.GetKeyDown(KeyCode.N))
            ShowNextSign();

        if (signTimeLeft <= 0f)
        {
            if (feedbackText != null)
                feedbackText.text = "Skipped";
            ShowNextSign();
        }
    }

    SignEntry[] BuildSignList()
    {
        if (signLibrary != null && signLibrary.signs != null && signLibrary.signs.Length > 0)
            return signLibrary.signs;

        var list = new SignEntry[FallbackIds.Length];
        for (int i = 0; i < FallbackIds.Length; i++)
        {
            list[i] = new SignEntry
            {
                id = FallbackIds[i],
                handImage = null
            };
        }
        return list;
    }

    void ShowNextSign()
    {
        SignEntry entry = PickNextEntry();
        if (entry == null)
        {
            EndLesson();
            return;
        }

        currentSignId = entry.id;
        signTimeLeft = secondsPerSign;

        if (instructionText != null)
            instructionText.text = "Copy this sign";

        if (letterText != null)
            letterText.text = entry.id;

        if (handImage != null)
        {
            handImage.enabled = true;
            handImage.preserveAspect = true;
            if (entry.handImage != null)
            {
                handImage.sprite = entry.handImage;
                handImage.color = Color.white;
            }
            else
            {
                // Placeholder panel until you assign hand photos in the Sign Library
                handImage.color = new Color(0.25f, 0.35f, 0.5f, 0.9f);
            }
        }

        if (feedbackText != null)
            feedbackText.text = entry.handImage == null
                ? "Add hand images in Sign Library (Space = correct)"
                : "";
    }

    SignEntry PickNextEntry()
    {
        if (signs == null || signs.Length == 0)
            return null;

        if (randomOrder)
            return signs[Random.Range(0, signs.Length)];

        SignEntry entry = signs[nextIndex % signs.Length];
        nextIndex++;
        return entry;
    }

    /// <summary>
    /// Call this from hand tracking / AI when a sign is recognized.
    /// Example: CheckGesture("A");
    /// </summary>
    public void CheckGesture(string detectedSignId)
    {
        if (string.IsNullOrEmpty(currentSignId) || timeLeft <= 0f)
            return;

        if (string.Equals(detectedSignId, currentSignId, System.StringComparison.OrdinalIgnoreCase))
        {
            score++;
            if (scoreText != null)
                scoreText.text = "Score: " + score;
            if (feedbackText != null)
                feedbackText.text = "Correct!";
            CancelInvoke(nameof(ShowNextSign));
            Invoke(nameof(ShowNextSign), 1.0f);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "Try again";
        }
    }

    void EndLesson()
    {
        if (instructionText != null)
            instructionText.text = "Lesson complete!";
        if (letterText != null)
            letterText.text = "";
        if (handImage != null)
            handImage.enabled = false;
        if (feedbackText != null)
            feedbackText.text = "Final score: " + score;
        if (timerText != null)
            timerText.text = "Time: 0";
        enabled = false;
    }
}
