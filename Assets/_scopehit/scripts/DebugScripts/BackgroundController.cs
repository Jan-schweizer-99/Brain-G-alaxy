using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
public class BackgroundController : MonoBehaviour 
{
    [Header("Colors")]
    public Color backgroundColor = Color.red;
    public Color cat = Color.red;
    public Color water = Color.red;

    [Header("Images")]
    public Sprite image;

    [Header("Test Data")]
    public string[] testArray = new string[5];
}
#if UNITY_EDITOR
[CustomEditor(typeof(BackgroundController))]
[CanEditMultipleObjects]
public class BackgroundControllerEditor : CustomBaseEditor
{

}
#endif