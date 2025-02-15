using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR
[CustomEditor(typeof(Island))]
public class IslandEditor : CustomBaseEditor
{
    protected override void OnEnable()
    {
        SetEditorStyle("Island");
    }

}
#endif
public class Island : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
