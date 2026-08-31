using UnityEngine;

[System.Serializable]
public class SignEntry
{
    [Tooltip("Letter or number, e.g. A or 5")]
    public string id;

    [Tooltip("Picture of the hand sign the learner should copy")]
    public Sprite handImage;
}
