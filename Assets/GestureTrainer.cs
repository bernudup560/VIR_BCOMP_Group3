using UnityEngine;
using TMPro;

public class GestureTrainer : MonoBehaviour
{
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    private string currentGesture;
    private int score = 0;
    private float timeLeft = 30f;

    void Start()
    {
        NextGesture();
    }

    void Update()
    {
        HandleInput();
        HandleTimer();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            CheckGesture("HELLO");
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            CheckGesture("THANK YOU");
        }
    }

    void HandleTimer()
    {
        timeLeft -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.Ceil(timeLeft);

        if (timeLeft <= 0)
        {
            instructionText.text = "Game Over!";
            feedbackText.text = "Final Score: " + score;
            enabled = false;
        }
    }

    void NextGesture()
    {
        string[] gestures = { "HELLO", "THANK YOU" };
        currentGesture = gestures[Random.Range(0, gestures.Length)];

        instructionText.text = "Make the sign for: " + currentGesture;
        feedbackText.text = "";
    }

    void CheckGesture(string inputGesture)
    {
        if (inputGesture == currentGesture)
        {
            score++;
            scoreText.text = "Score: " + score;

            feedbackText.text = "Correct! ✅";
            Invoke("NextGesture", 1.5f);
        }
        else
        {
            feedbackText.text = "Wrong ❌";
        }
    }
}