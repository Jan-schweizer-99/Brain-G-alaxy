using UnityEngine;
using UnityEditor;
using System.Linq;
#if UNITY_EDITOR
public class CustomBaseEditor : Editor
{
    private static EditorSettings editorSettings;
    protected Texture2D logoTexture;
    protected Texture2D tintedIconTexture;
    protected const float LOGO_SIZE = 240f;
    protected const float PADDING = 10f;

    private string currentStyleName = "Default";
    protected EditorStyle currentStyle;

    private int instanceId;
    private static System.Collections.Generic.Dictionary<int, CustomBaseEditor> activeEditors = 
        new System.Collections.Generic.Dictionary<int, CustomBaseEditor>();
        
    // Cache für den EditorReference
    private EditorReference cachedEditorReference;
    private bool editorReferenceChecked = false;

    protected EditorReference GetEditorReference()
    {
        if (!editorReferenceChecked)
        {
            cachedEditorReference = Object.FindObjectOfType<EditorReference>();
            editorReferenceChecked = true;
        }
        return cachedEditorReference;
    }

    protected bool IsCustomEditorDisabled()
    {
        var reference = GetEditorReference();
        return reference != null && reference.DisableCustomEditor;
    }

protected static EditorSettings LoadEditorSettings()
{
    if (editorSettings == null)
    {
        string settingsPath = "Assets/Editor/Settings/EditorSettings.asset";
        editorSettings = AssetDatabase.LoadAssetAtPath<EditorSettings>(settingsPath);
        
        if (editorSettings == null)
        {
            Debug.LogWarning($"EditorSettings not found at {settingsPath}. Creating new one...");
            
            // Ensure directories exist
            CreateDirectoryIfNotExists("Assets/Editor");
            CreateDirectoryIfNotExists("Assets/Editor/Settings");
            
            // Create new settings asset
            editorSettings = ScriptableObject.CreateInstance<EditorSettings>();
            AssetDatabase.CreateAsset(editorSettings, settingsPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Created new EditorSettings asset.");
        }
    }
    return editorSettings;
}

    private static void CreateDirectoryIfNotExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parentPath = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parentPath, folderName);
            Debug.Log($"Created directory: {path}");
        }
    }
    
    private bool ValidateAssetPath(string assetPath)
    {
        // Convert to system path
        string fullPath = Application.dataPath + "/" + assetPath.Replace("Assets/", "");
        
        if (!System.IO.File.Exists(fullPath))
        {
            return false;
        }
        
        // Verify it's a valid Unity asset
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
        {
            return false;
        }
        
        return true;
    }

    protected virtual void SetEditorStyle(string styleName)
    {
        // Wenn der Editor deaktiviert ist, keine Styles setzen
        if (IsCustomEditorDisabled())
        {
            ClearCustomEditorIcon();
            return;
        }

        var settings = LoadEditorSettings();
        if (settings == null)
        {
            Debug.LogError("Failed to load EditorSettings!");
            return;
        }

        currentStyleName = styleName;
        currentStyle = settings.GetStyle(styleName);
        
        if (currentStyle == null)
        {
            Debug.LogWarning($"Style '{styleName}' not found, falling back to default paths");
            currentStyle = new EditorStyle
            {
                logoPath = LogoPath,
                iconPath = CustomIconPath,
                backgroundColor = Color.white
            };
        }

        UpdateEditorStyleAssets();
    }

    private void UpdateEditorStyleAssets()
    {
        // Wenn der Editor deaktiviert ist, keine Assets updaten
        if (IsCustomEditorDisabled())
        {
            ClearCustomEditorIcon();
            return;
        }

        try 
        {
            string logoPath = currentStyle?.logoPath;
            string iconPath = currentStyle?.iconPath;

            if (!ValidateAssetPath(logoPath) || !ValidateAssetPath(iconPath))
            {
                Debug.LogError("Asset path validation failed!");
                return;
            }

            logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);
            if (logoTexture == null)
            {
                Debug.LogError($"Failed to load logo texture from: {logoPath}");
                return;
            }
            
            Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (iconTexture == null)
            {
                Debug.LogError($"Failed to load icon texture from: {iconPath}");
                return;
            }

            tintedIconTexture = CreateTintedTexture(iconTexture, currentStyle?.backgroundColor ?? Color.white);
            SetCustomEditorIcon();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateEditorStyleAssets: {e.Message}\n{e.StackTrace}");
        }

        Repaint();
    }

    protected virtual Color BackgroundColor => currentStyle?.backgroundColor ?? Color.white;
    protected virtual float AdditionalBackgroundHeight => editorSettings != null ? editorSettings.additionalBackgroundHeight : 0f;
    protected virtual string LogoPath => "Assets/Editor/CustomEditorIcons/HFULOGO.png";
    protected virtual string CustomIconPath => "Assets/Editor/CustomEditorIcons/HFU.png";

    [InitializeOnLoad]
    private class CustomEditorUpdateWindow : EditorWindow
    {
        static CustomEditorUpdateWindow()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (editorSettings != null)
            {
                foreach (var editor in activeEditors.Values.Where(e => e != null))
                {
                    editor.Repaint();
                }
            }
        }
    }

    protected virtual void OnEnable()
    {
        instanceId = GetHashCode();
        LoadEditorSettings();
        activeEditors[instanceId] = this;
        EditorSettings.OnSettingsChanged += OnSettingsChanged;
        editorReferenceChecked = false; // Reset the cache when enabled
        SetEditorStyle(currentStyleName);
    }

    protected virtual void OnDisable()
    {
        EditorSettings.OnSettingsChanged -= OnSettingsChanged;
        activeEditors.Remove(instanceId);
        
        ClearCustomEditorIcon();
        
        if (tintedIconTexture != null)
        {
            DestroyImmediate(tintedIconTexture);
            tintedIconTexture = null;
        }
    }

    private void OnSettingsChanged()
    {
        editorReferenceChecked = false; // Reset the cache when settings change
        if (!string.IsNullOrEmpty(currentStyleName))
        {
            SetEditorStyle(currentStyleName);
        }
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject == null) return;

        serializedObject.Update();

        if (IsCustomEditorDisabled() || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DrawDefaultInspector();
        }
        else
        {
            ApplyCustomStyles(() =>
            {
                DrawCustomBackground();
                DrawLogo();
                DrawDefaultInspector();
            });
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ApplyCustomStyles(System.Action drawContent)
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !IsCustomEditorDisabled())
        {
            var originalFoldoutColor = EditorStyles.foldout.normal.textColor;
            var originalBackgroundColor = GUI.backgroundColor;

            try
            {
                EditorStyles.foldout.normal.textColor = Color.white;
                GUI.backgroundColor = BackgroundColor;
                EditorStyles.label.normal.textColor = Color.white;
                EditorStyles.label.focused.textColor = Color.white;
                EditorStyles.numberField.normal.textColor = Color.white;
                EditorStyles.numberField.focused.textColor = Color.white;

                drawContent?.Invoke();
            }
            finally
            {
                // Restore original colors
                EditorStyles.foldout.normal.textColor = originalFoldoutColor;
                GUI.backgroundColor = originalBackgroundColor;
                EditorStyles.label.normal.textColor = Color.white;
                EditorStyles.label.focused.textColor = Color.white;
                EditorStyles.numberField.normal.textColor = Color.white;
                EditorStyles.numberField.focused.textColor = Color.black;
            }
        }
        else 
        {
            drawContent?.Invoke();
        }
    }

    protected virtual void DrawCustomBackground()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        float startY = EditorGUIUtility.singleLineHeight;
        Rect backgroundRect = EditorGUILayout.GetControlRect(false, 0);
        backgroundRect.y = startY - 20;
        backgroundRect.height = GetInspectorHeight() - startY + 30 + AdditionalBackgroundHeight;
        backgroundRect.x = 0;
        backgroundRect.width = EditorGUIUtility.currentViewWidth;

        EditorGUI.DrawRect(backgroundRect, BackgroundColor);
    }

    protected virtual void DrawLogo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || logoTexture == null)
            return;

        float startY = EditorGUIUtility.singleLineHeight;
        Rect backgroundRect = new Rect(0, startY - 20, EditorGUIUtility.currentViewWidth, 
            GetInspectorHeight() - startY + 30 + AdditionalBackgroundHeight);

        // Center the logo
        float centerX = (backgroundRect.width - LOGO_SIZE) * 0.5f;
        float centerY = PADDING;

        Rect logoRect = new Rect(centerX, centerY, LOGO_SIZE, LOGO_SIZE);

        GUI.BeginClip(backgroundRect);
        GUI.DrawTexture(
            new Rect(
                logoRect.x - backgroundRect.x,
                logoRect.y - backgroundRect.y,
                logoRect.width,
                logoRect.height
            ),
            logoTexture
        );
        GUI.EndClip();
    }

    protected virtual void SetCustomEditorIcon()
    {
        if (IsCustomEditorDisabled() || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ClearCustomEditorIcon();
            return;
        }

        if (tintedIconTexture != null && target != null)
        {
            EditorGUIUtility.SetIconForObject(target, tintedIconTexture);
        }
    }

    protected virtual void ClearCustomEditorIcon()
    {
        if (target != null)
        {
            EditorGUIUtility.SetIconForObject(target, null);
        }
    }

    protected float GetInspectorHeight()
    {
        if (serializedObject == null) return 0f;

        float totalHeight = EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            totalHeight += EditorGUI.GetPropertyHeight(iterator, true);
            totalHeight += EditorGUIUtility.standardVerticalSpacing;
        }

        return totalHeight;
    }

    private Texture2D CreateTintedTexture(Texture2D original, Color tintColor)
    {
        if (original == null) return null;

        try
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                original.width,
                original.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            Graphics.Blit(original, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D readableTexture = new Texture2D(original.width, original.height);
            readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = readableTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0)
                {
                    pixels[i] = new Color(
                        tintColor.r,
                        tintColor.g,
                        tintColor.b,
                        pixels[i].a
                    );
                }
            }

            readableTexture.SetPixels(pixels);
            readableTexture.Apply();
            return readableTexture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating tinted texture: {e.Message}");
            return null;
        }
    }
}
#endif