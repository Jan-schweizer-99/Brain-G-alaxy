using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Kein CreateAssetMenu Attribut mehr, damit keine automatische Asset-Erstellung möglich ist
public class EditorSettings : ScriptableObject 
{
    public static System.Action OnSettingsChanged;

    [Header("Editor Styles")]
    [SerializeField]
    public EditorStyle[] editorStyles;

    [Header("Global Settings")]
    [HideInInspector]
    public float additionalBackgroundHeight = 0f;

    // Keine OnEnable-Methode mehr, die irgendwas initialisiert

    public override int GetHashCode()
    {
        int hash = base.GetHashCode();
        if (editorStyles != null)
        {
            foreach (var style in editorStyles)
            {
                if (style != null)
                {
                    hash = hash * 17 + style.backgroundColor.GetHashCode();
                }
            }
        }
        return hash;
    }

    public EditorStyle GetStyle(string styleName)
    {
        var style = editorStyles?.FirstOrDefault(x => x != null && x.styleName == styleName);
        
        if (style == null)
        {
            Debug.LogWarning($"EditorSettings: No style found with name '{styleName}', returning fallback style.");
            
            // Suche nach einem Default-Stil
            style = editorStyles?.FirstOrDefault(x => x != null && x.styleName == "Default");
            
            // Wenn auch kein Default-Stil existiert, erzeuge einen Notfall-Stil
            if (style == null)
            {
                Debug.LogWarning("EditorSettings: No 'Default' style found either, creating emergency style.");
                style = new EditorStyle 
                {
                    styleName = "Default",
                    backgroundColor = new Color(0.00f, 0.53f, 0.33f, 0.5f),
                    logoPath = "Assets/Editor/Backgroundlogos/HFULOGO.png",
                    iconPath = "Assets/Editor/Icons/HFU.png",
                    description = "Emergency Default Style"
                };
            }
        }

        return style;
    }

    public string[] GetStyleNames()
    {
        return editorStyles?.Where(x => x != null).Select(x => x.styleName).ToArray() ?? new string[0];
    }
}