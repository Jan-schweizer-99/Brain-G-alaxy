using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorReference : MonoBehaviour
{
    [SerializeField] 
    private EditorSettings settings;
    
    [SerializeField]
    private bool disableCustomEditor = false;

    public EditorSettings Settings => settings;
    public bool DisableCustomEditor => disableCustomEditor;

    #if UNITY_EDITOR
    [CustomEditor(typeof(EditorReference))]
    public class EditorReferenceInspector : CustomBaseEditor
    {
        private SerializedObject settingsObject;
        private Editor settingsEditor;
        private SerializedProperty disableCustomEditorProperty;

        protected override float AdditionalBackgroundHeight => 450f;
        
        // Layout Constants
        private const float CIRCLE_SIZE = 32f;
        private const float SPACING_HORIZONTAL = 40f;
        private const float SPACING_VERTICAL = 35f;
        private const float LABEL_OFFSET = 12f;
        private const float LABEL_HEIGHT = 14f;
        private const float SIDE_PADDING = 16f;
        private const float ICON_SIZE = 20f;

        protected override void OnEnable()
        {
            base.OnEnable();
            SetEditorStyle("DEBUG");

            if (settingsEditor != null)
                DestroyImmediate(settingsEditor);
                
            disableCustomEditorProperty = serializedObject.FindProperty("disableCustomEditor");
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;
            serializedObject.Update();
            
            EditorReference reference = (EditorReference)target;
            
            // Bool-Property vor dem Rest zeichnen
            EditorGUILayout.PropertyField(disableCustomEditorProperty, new GUIContent("Disable Custom Editor"));
            
            if (!reference.DisableCustomEditor && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DrawCustomBackground();
                DrawLogo();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings"));

                if (reference.Settings != null)
                {
                    GUILayout.Space(8);
                    DrawColorPalette(reference);
                    EditorGUILayout.LabelField("Settings Konfiguration", EditorStyles.boldLabel);
                    
                    if (settingsEditor == null)
                        settingsEditor = CreateEditor(reference.Settings);

                    settingsEditor.OnInspectorGUI();
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawColorPalette(EditorReference reference)
        {
            if (reference.Settings?.editorStyles == null || reference.Settings.editorStyles.Length == 0) 
                return;

            EditorGUILayout.LabelField("Style Übersicht", EditorStyles.boldLabel);
            GUILayout.Space(8);
            
            var styles = reference.Settings.editorStyles;
            int styleCount = styles.Length;
            
            float availableWidth = EditorGUIUtility.currentViewWidth - SIDE_PADDING * 2;
            int circlesPerRow = Mathf.Max(1, Mathf.FloorToInt((availableWidth + SPACING_HORIZONTAL) / (CIRCLE_SIZE + SPACING_HORIZONTAL)));
            
            int rows = Mathf.CeilToInt((float)styleCount / circlesPerRow);
            
            float totalWidth = circlesPerRow * (CIRCLE_SIZE + SPACING_HORIZONTAL) - SPACING_HORIZONTAL;
            float totalHeight = rows * (CIRCLE_SIZE + LABEL_HEIGHT + LABEL_OFFSET + SPACING_VERTICAL) - SPACING_VERTICAL;
            
            Rect gridArea = EditorGUILayout.GetControlRect(false, totalHeight);
            float currentY = gridArea.y;

            for (int row = 0; row < rows; row++)
            {
                float rowY = currentY + row * (CIRCLE_SIZE + LABEL_HEIGHT + LABEL_OFFSET + SPACING_VERTICAL);
                
                int circlesInThisRow = Mathf.Min(circlesPerRow, styleCount - row * circlesPerRow);
                
                float rowWidth = circlesInThisRow * (CIRCLE_SIZE + SPACING_HORIZONTAL) - SPACING_HORIZONTAL;
                float rowStartX = gridArea.x + (EditorGUIUtility.currentViewWidth - rowWidth) / 2 - 8f;
                
                for (int col = 0; col < circlesInThisRow; col++)
                {
                    int index = row * circlesPerRow + col;
                    if (index >= styleCount) break;
                    
                    float posX = rowStartX + col * (CIRCLE_SIZE + SPACING_HORIZONTAL);
                    
                    // Draw the circle
                    Rect circleRect = new Rect(posX, rowY, CIRCLE_SIZE, CIRCLE_SIZE);
                    DrawCircle(circleRect, styles[index].backgroundColor, styles[index].styleName.Contains("Debug"));
                    
                    // Draw the icon
                    if (styles[index].iconPath != null)
                    {
                        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(styles[index].iconPath);
                        if (icon != null)
                        {
                            float iconX = posX + (CIRCLE_SIZE - ICON_SIZE) / 2;
                            float iconY = rowY + (CIRCLE_SIZE - ICON_SIZE) / 2;
                            Rect iconRect = new Rect(iconX, iconY, ICON_SIZE, ICON_SIZE);
                            
                            Color originalColor = GUI.color;
                            GUI.color = Color.white;
                            GUI.DrawTexture(iconRect, icon);
                            GUI.color = originalColor;
                        }
                    }
                    
                    // Draw the label
                    Rect labelRect = new Rect(
                        posX - SPACING_HORIZONTAL/4, 
                        rowY + CIRCLE_SIZE + LABEL_OFFSET,
                        CIRCLE_SIZE + SPACING_HORIZONTAL/2,
                        LABEL_HEIGHT
                    );
                    
                    var labelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.UpperCenter,
                        fontSize = 11,
                        fixedHeight = LABEL_HEIGHT
                    };
                    
                    GUI.Label(labelRect, styles[index].styleName, labelStyle);
                }
            }
            
            GUILayout.Space(4);
        }

        private void DrawCircle(Rect position, Color color, bool isDebug)
        {
            float baseRadius = position.width/2;
            
            GUI.BeginClip(position);
            Vector2 center = new Vector2(position.width/2, position.height/2);
            
            Color circleColor = isDebug ? Color.white : color;
            Handles.color = circleColor;
            Handles.DrawSolidDisc(center, Vector3.forward, baseRadius);
            
            GUI.EndClip();
        }
    }
    #endif
}