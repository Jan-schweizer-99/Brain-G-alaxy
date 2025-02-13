using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TMPro;

[ExecuteInEditMode]
public class MarkdownMathRenderer : MonoBehaviour
{
    [SerializeField]
    [TextArea(10, 20)]
    private string markdownInput = "";

    [SerializeField]
    private TMP_Text outputText;

    private Dictionary<string, string> markdownPatterns = new Dictionary<string, string>()
    {
        // Headers
        {@"###\s+(.+)", "<size=150%><b>$1</b></size>"},
        {@"##\s+(.+)", "<size=175%><b>$1</b></size>"},
        {@"#\s+(.+)", "<size=200%><b>$1</b></size>"},
        
        // Bold
        {@"\*\*(.+?)\*\*", "<b>$1</b>"},
        
        // Italic
        {@"\*(.+?)\*", "<i>$1</i>"},
        
        // Lists
        {@"^\s*-\s+(.+)", "• $1"},
        {@"^\s*\d+\.\s+(.+)", "$1. $2"}
    };

    private Dictionary<string, string> mathPatterns = new Dictionary<string, string>()
    {
        // Inline math
        {@"\\\((.*?)\\\)", "<math>$1</math>"},
        
        // Display math
        {@"\\\[(.*?)\\\]", "\n<math>$1</math>\n"},
        
        // Common math symbols
        {@"\\times", "×"},
        {@"\\left\(", "("},
        {@"\\right\)", ")"},
        {@"\\frac{(.*?)}{(.*?)}", "$1/$2"},
        {@"\\boxed{(.*?)}", "┌─$1─┐"},
        {@"\\text{(.*?)}", "$1"}
    };

    private void OnValidate()
    {
        if (outputText == null) return;
        RenderText();
    }

    public void RenderText()
    {
        string processedText = markdownInput;

        // Process Markdown
        foreach (var pattern in markdownPatterns)
        {
            processedText = Regex.Replace(processedText, pattern.Key, pattern.Value, 
                RegexOptions.Multiline);
        }

        // Process Math
        foreach (var pattern in mathPatterns)
        {
            processedText = Regex.Replace(processedText, pattern.Key, pattern.Value);
        }

        // Apply final formatting
        outputText.text = processedText;
    }

    [CustomEditor(typeof(MarkdownMathRenderer))]
    public class MarkdownMathRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MarkdownMathRenderer renderer = (MarkdownMathRenderer)target;

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("markdownInput"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outputText"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                renderer.RenderText();
            }

            if (GUILayout.Button("Refresh Rendering"))
            {
                renderer.RenderText();
            }
        }
    }
}

[CustomPropertyDrawer(typeof(MarkdownMathRenderer))]
public class MarkdownMathRendererDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}