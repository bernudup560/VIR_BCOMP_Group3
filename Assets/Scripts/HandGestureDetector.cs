using UnityEngine;

public class HandGestureDetector : MonoBehaviour
{
    public Transform rightHand;

    void Update()
    {
        DetectGesture();
    }

    void DetectGesture()
    {
        // Example placeholder detection
        float handHeight = rightHand.position.y;

        if (handHeight > 1.5f)
        {
            Debug.Log("Hand Raised ✋");
        }
    }
}