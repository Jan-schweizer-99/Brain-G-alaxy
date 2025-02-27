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
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        Island island = (Island)target;
        
        
        // We won't duplicate the Topic ID field here since it's already shown by base.OnInspectorGUI()
        // Instead, we can add other information about the topic if needed
        // EditorGUI.BeginDisabledGroup(true);
        // EditorGUILayout.IntField("Topic ID", island.topicId);
        // EditorGUI.EndDisabledGroup();
    }
}
#endif

public class Island : MonoBehaviour 
{
    [SerializeField] private int _topicId = -1;
    
    // Public property for the Topic-ID
    public int topicId 
    {
        get { return _topicId; }
        set { _topicId = value; }
    }
    
    // Start is called before the first frame update
    void Start()
    {
         
    }
    
    // Update is called once per frame
    void Update()
    {
         
    }
}