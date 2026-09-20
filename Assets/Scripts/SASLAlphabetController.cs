using UnityEngine;
using TMPro;

public class SASLAlphabetController : MonoBehaviour
{
    public TMP_Text letterText;
    public Renderer handSignRenderer;
    public Texture2D[] handSigns;

    private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private int currentLetter = 0;

    void Start()
    {
        UpdateLetter();
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
        letterText.text = alphabet[currentLetter].ToString();

        if (handSigns.Length > currentLetter)
        {
            handSignRenderer.material.mainTexture = handSigns[currentLetter];
        }
    }
}