using UnityEngine;

[System.Serializable]
public class EditorStyle
{
    public string styleName;
    public Color backgroundColor = Color.white;
    public string logoPath = "Assets/Editor/Backgroundlogos/DefaultLogo.png";
    public string iconPath = "Assets/Editor/Icons/DefaultIcon.png";
    
    [Tooltip("Optionale Beschreibung für diesen Stil")]
    public string description;
}