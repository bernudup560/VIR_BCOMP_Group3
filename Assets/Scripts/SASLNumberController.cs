using UnityEngine;
using TMPro;

public class SASLNumberController : MonoBehaviour
{
    public TMP_Text numberText;
    public Renderer handSignRenderer;
    public Texture2D[] numberSigns;

    private int currentNumber = 0;

    void Start()
    {
        UpdateNumber();
    }

    public void NextNumber()
    {
        if (currentNumber < numberSigns.Length - 1)
        {
            currentNumber++;
            UpdateNumber();
        }
    }

    public void PreviousNumber()
    {
        if (currentNumber > 0)
        {
            currentNumber--;
            UpdateNumber();
        }
    }

    void UpdateNumber()
    {
        numberText.text = currentNumber.ToString();

        if (numberSigns.Length > currentNumber)
        {
            handSignRenderer.material.mainTexture = numberSigns[currentNumber];
        }
    }
}