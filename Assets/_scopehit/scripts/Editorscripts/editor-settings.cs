using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "EditorSettings", menuName = "Editor/Custom Editor Settings")]
public class EditorSettings : ScriptableObject 
{
    public static System.Action OnSettingsChanged;

    [Header("Editor Styles")]
    [SerializeField]
    public EditorStyle[] editorStyles;

    [Header("Global Settings")]
    [HideInInspector]
    public float additionalBackgroundHeight = 0f;

    private void OnEnable()
    {
        InitializeDefaultStyles();
    }

    private void InitializeDefaultStyles()
    {
        if (editorStyles == null || editorStyles.Length == 0)
        {
            editorStyles = new EditorStyle[] 
            {
new EditorStyle
{
    styleName = "DEBUG",
    backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/Debug.png",
    iconPath = "Assets/Editor/Backgroundlogos/Debug_icon.png",
    description = "Tools and utilities for debugging and development purposes"
},

new EditorStyle
{
    styleName = "Network",
    backgroundColor = new Color(1.0f, 0.0f, 0.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/Network.png",
    iconPath = "Assets/Editor/Backgroundlogos/Network_icon.png",
    description = "Network configuration and multiplayer management tools"
},

new EditorStyle
{
    styleName = "Player",
    backgroundColor = new Color(1.0f, 0.7f, 0.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/Player.png",
    iconPath = "Assets/Editor/Backgroundlogos/Player_icon.png",
    description = "Player character settings and controller configuration"
},

new EditorStyle
{
    styleName = "World",
    backgroundColor = new Color(0.0f, 1.0f, 0.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/World.png",
    iconPath = "Assets/Editor/Backgroundlogos/World_icon.png",
    description = "World building tools and environment settings"
},

new EditorStyle
{
    styleName = "VFX",
    backgroundColor = new Color(1.0f, 0.0f, 1.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/VFX.png",
    iconPath = "Assets/Editor/Backgroundlogos/VFX_icon.png",
    description = "Visual effects and particle system configuration"
},

new EditorStyle
{
    styleName = "Tasks",
    backgroundColor = new Color(0.0f, 0.7f, 1.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/Tasks.png",
    iconPath = "Assets/Editor/Backgroundlogos/Tasks_icon.png",
    description = "Task management and project workflow organization"
},

new EditorStyle
{
    styleName = "microscripts",
    backgroundColor = new Color(0.5f, 0.0f, 1.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/microscripts.png",
    iconPath = "Assets/Editor/Backgroundlogos/microscripts_icon.png",
    description = "Small utility scripts and helper functions management"
},

new EditorStyle
{
    styleName = "Tracking",
    backgroundColor = new Color(1.0f, 0.5f, 0.0f, 0.5f),
    logoPath = "Assets/Editor/Backgroundlogos/Tracking.png",
    iconPath = "Assets/Editor/Backgroundlogos/Tracking_icon.png",
    description = "Movement and object tracking system configuration"
}
            };

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            #endif
        }
    }

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
        if (editorStyles == null || editorStyles.Length == 0)
        {
            InitializeDefaultStyles();
        }

        var style = editorStyles?.FirstOrDefault(x => x != null && x.styleName == styleName);
        
        if (style == null)
        {
            style = editorStyles?.FirstOrDefault(x => x != null && x.styleName == "Default");
            
            if (style == null)
            {
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
        if (editorStyles == null || editorStyles.Length == 0)
        {
            InitializeDefaultStyles();
        }
        return editorStyles?.Where(x => x != null).Select(x => x.styleName).ToArray() ?? new string[0];
    }
}