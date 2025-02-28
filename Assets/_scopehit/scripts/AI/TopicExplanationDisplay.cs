using UnityEngine;
using TMPro;
using UnityEditor;

public class TopicExplanationDisplay : MonoBehaviour
{
    [SerializeField] private TopicDatabase topicDatabase;
    [SerializeField] private TextMeshProUGUI explanationText;
    
    private Island parentIsland;
    
    private void Awake()
    {
        // Get parent Island component
        parentIsland = GetComponentInParent<Island>();
        
        if (explanationText == null)
        {
            explanationText = GetComponent<TextMeshProUGUI>();
        }
        
        UpdateExplanationText();
    }
    
    private void OnEnable()
    {
        UpdateExplanationText();
    }
    
    public void UpdateExplanationText()
    {
        if (parentIsland == null)
        {
            parentIsland = GetComponentInParent<Island>();
            if (parentIsland == null) return;
        }
        
        if (explanationText == null) return;
        if (topicDatabase == null) return;
        
        int topicId = parentIsland.topicId;
        if (topicId < 0) 
        {
            explanationText.text = "";
            return;
        }
        
        TopicEntry topic = topicDatabase.GetTopicById(topicId);
        if (topic != null)
        {
            explanationText.text = topic.explanation;
        }
        else
        {
            explanationText.text = "";
        }
    }
    
#if UNITY_EDITOR
    // This will make it update in the editor
    private void OnValidate()
    {
        // Use EditorApplication.delayCall to avoid errors when called during import or scene loading
        EditorApplication.delayCall += () =>
        {
            if (this == null) return; // The object might have been deleted
            UpdateExplanationText();
        };
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TopicExplanationDisplay))]
public class TopicExplanationDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TopicExplanationDisplay display = (TopicExplanationDisplay)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Update Explanation"))
        {
            display.UpdateExplanationText();
            EditorUtility.SetDirty(target);
        }
        
        // Show info about parent Island if available
        Island parentIsland = display.GetComponentInParent<Island>();
        if (parentIsland != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parent Island Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Topic ID", parentIsland.topicId);
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            EditorGUILayout.HelpBox("No parent Island component found!", MessageType.Warning);
        }
    }
}
#endif