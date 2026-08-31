using UnityEngine;

public class HandTrackerDebug : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    void Update()
    {
        Debug.Log("LEFT: " + leftHand.position);
        Debug.Log("RIGHT: " + rightHand.position);
    }
}