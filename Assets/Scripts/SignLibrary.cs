using UnityEngine;

[CreateAssetMenu(fileName = "SignLibrary", menuName = "Sign Language/Sign Library")]
public class SignLibrary : ScriptableObject
{
    [Tooltip("All letters and numbers this lesson can show")]
    public SignEntry[] signs;
}
