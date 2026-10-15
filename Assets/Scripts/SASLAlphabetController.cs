using UnityEngine;
using TMPro;

public class SASLAlphabetController : MonoBehaviour
{
    public TMP_Text letterText;
    public Renderer handSignRenderer;

    private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int currentLetter = 0;

    public string CurrentLetter =>
        alphabet[currentLetter].ToString();

    void Start()
    {
        UpdateLetter();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            NextLetter();

        if (Input.GetKeyDown(KeyCode.P))
            PreviousLetter();
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

    void UpdateLetter()
    {
        string letter = alphabet[currentLetter].ToString();

        if (letterText != null)
            letterText.text = letter;

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
}

