#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

[InitializeOnLoad]
public class CustomEditorHighlighter
{
    private static Color highlightColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    private static Color selectionColor = new Color(0.22f, 0.42f, 0.73f, 1f);
    private static Color inactiveSelectionColor = new Color(0.28f, 0.28f, 0.28f, 1f);
    private static Dictionary<Type, Color> customEditorColors = new Dictionary<Type, Color>();
    private static bool isInitialized = false;
    private static string childIndicator = "▼";
    private static SerializedObject serializedSettings;
    private static EditorSettings settings;
    private static int lastSettingsHash;
    private static bool wasPlaying = false;
    
    // Cache für EditorReference
    private static EditorReference cachedEditorReference;
    private static bool editorReferenceChecked = false;

    static CustomEditorHighlighter()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.update += OnUpdate;
            
            InitializeSettings();
            
            if (!isInitialized)
            {
                UpdateCustomEditorCache();
                isInitialized = true;
            }
        }
    }

    private static EditorReference GetEditorReference()
    {
        if (!editorReferenceChecked)
        {
            cachedEditorReference = UnityEngine.Object.FindObjectOfType<EditorReference>();
            editorReferenceChecked = true;
        }
        return cachedEditorReference;
    }

    private static bool IsCustomEditorDisabled()
    {
        var reference = GetEditorReference();
        return reference != null && reference.DisableCustomEditor;
    }

private static bool previousDisableState = false;

    private static void OnUpdate()
    {
        // Check for EditorReference changes
        var reference = GetEditorReference();
        bool currentDisableState = reference != null && reference.DisableCustomEditor;
        
        // Wenn sich der Zustand geändert hat
        if (currentDisableState != previousDisableState)
        {
            previousDisableState = currentDisableState;
            editorReferenceChecked = false; // Reset cache
            UpdateCustomEditorCache();
            EditorApplication.RepaintHierarchyWindow();
        }

        // Check for play mode changes
        if (wasPlaying != Application.isPlaying)
        {
            wasPlaying = Application.isPlaying;
            editorReferenceChecked = false; // Reset cache on play mode change
            
            if (Application.isPlaying)
            {
                EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
                EditorApplication.update -= CheckSettingsChanges;
            }
            else
            {
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
                EditorApplication.update += CheckSettingsChanges;
                UpdateCustomEditorCache();
            }
        }

        if (!Application.isPlaying)
        {
            CheckSettingsChanges();
        }
    }

    private static void InitializeSettings()
    {
        if (Application.isPlaying) return;

        editorReferenceChecked = false; // Reset cache when initializing settings

        if (settings == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:EditorSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<EditorSettings>(path);
                if (settings != null)
                {
                    serializedSettings = new SerializedObject(settings);
                    lastSettingsHash = settings.GetHashCode();
                }
            }
        }
    }

    private static void CheckSettingsChanges()
    {
        if (Application.isPlaying) return;

        if (settings == null)
        {
            InitializeSettings();
            return;
        }

        serializedSettings.Update();
        int currentHash = settings.GetHashCode();
        
        if (currentHash != lastSettingsHash)
        {
            lastSettingsHash = currentHash;
            editorReferenceChecked = false; // Reset cache when settings change
            UpdateCustomEditorCache();
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (Application.isPlaying) return;

        editorReferenceChecked = false; // Reset cache when scripts reload
        InitializeSettings();
        UpdateCustomEditorCache();
        EditorApplication.RepaintHierarchyWindow();
    }

    private static void UpdateCustomEditorCache()
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) 
        {
            customEditorColors.Clear();
            return;
        }

        customEditorColors.Clear();
            
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (assembly.GetName().Name.StartsWith("Unity") ||
                    assembly.GetName().Name.StartsWith("System") ||
                    assembly.GetName().Name.StartsWith("Microsoft") ||
                    assembly.GetName().Name.StartsWith("Mono"))
                {
                    continue;
                }

                foreach (var type in assembly.GetTypes())
                {
                    // Skip problematic types
                    if (type == null || !type.IsSubclassOf(typeof(Editor)) || 
                        type.Namespace != null && (
                        type.Namespace.StartsWith("UnityEditor") || 
                        type.Namespace.StartsWith("Unity.") ||
                        type.Namespace.StartsWith("TMPro")))
                    {
                        continue;
                    }

                    var attributes = type.GetCustomAttributes(typeof(CustomEditor), true);
                    if (attributes != null && attributes.Length > 0)
                    {
                        var attr = attributes[0] as CustomEditor;
                        if (attr != null)
                        {
                            var inspectedType = GetInspectedType(attr);
                            if (inspectedType != null && !IsUnityInternalType(inspectedType))
                            {
                                try
                                {
                                    // Handle MonoBehaviours
                                    if (typeof(MonoBehaviour).IsAssignableFrom(inspectedType))
                                    {
                                        var dummyGO = new GameObject("_EditorTemp");
                                        dummyGO.hideFlags = HideFlags.HideAndDontSave;
                                        
                                        try
                                        {
                                            var component = dummyGO.AddComponent(inspectedType);
                                            if (component != null)
                                            {
                                                ProcessEditorForTarget(component, type, inspectedType);
                                            }
                                        }
                                        finally
                                        {
                                            UnityEngine.Object.DestroyImmediate(dummyGO);
                                        }
                                    }
                                    // Handle ScriptableObjects
                                    else if (typeof(ScriptableObject).IsAssignableFrom(inspectedType) && 
                                            !typeof(Editor).IsAssignableFrom(inspectedType))
                                    {
                                        var scriptableObject = ScriptableObject.CreateInstance(inspectedType);
                                        if (scriptableObject != null)
                                        {
                                            try
                                            {
                                                ProcessEditorForTarget(scriptableObject, type, inspectedType);
                                            }
                                            finally
                                            {
                                                UnityEngine.Object.DestroyImmediate(scriptableObject);
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    #if UNITY_EDITOR && DEBUG
                                    Debug.LogWarning($"Error processing editor for type {type.Name}: {e.Message}");
                                    #endif
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                #if UNITY_EDITOR && DEBUG
                Debug.LogWarning($"Error processing assembly {assembly.GetName().Name}: {e.Message}");
                #endif
                continue;
            }
        }
    }

    private static void ProcessEditorForTarget(UnityEngine.Object target, Type editorType, Type inspectedType)
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) return;

        var editor = Editor.CreateEditor(target);
        if (editor != null)
        {
            try
            {
                var colorProperty = editorType.GetProperty("BackgroundColor",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.GetProperty);

                if (colorProperty != null)
                {
                    var color = (Color)colorProperty.GetValue(editor);
                    color.a = 1f;
                    customEditorColors[inspectedType] = color;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }
    }

    private static bool IsUnityInternalType(Type type)
    {
        if (type == null) return true;
        
        return type.Namespace != null && (
            type.Namespace.StartsWith("UnityEngine.") ||
            type.Namespace.StartsWith("UnityEditor.") ||
            type.Namespace.StartsWith("Unity.") ||
            type.Namespace == "UnityEngine" ||
            type.Namespace == "UnityEditor" ||
            type.Assembly.GetName().Name.StartsWith("Unity"));
    }

    private static List<Color> GetCustomEditorColorsForObject(GameObject obj)
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) return new List<Color>();

        var colors = new List<Color>();
        
        foreach (var comp in obj.GetComponents<MonoBehaviour>())
        {
            if (comp != null && customEditorColors.ContainsKey(comp.GetType()))
            {
                colors.Add(customEditorColors[comp.GetType()]);
            }
        }
        
        return colors;
    }

    private static List<(GameObject, List<Color>)> GetCustomEditorObjectsInChildren(GameObject obj)
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) return new List<(GameObject, List<Color>)>();

        var childObjects = new List<(GameObject, List<Color>)>();
        
        foreach (Transform child in obj.transform)
        {
            var childObj = child.gameObject;
            
            var childComponentColors = GetCustomEditorColorsForObject(childObj);
            if (childComponentColors.Any())
            {
                childObjects.Add((childObj, childComponentColors));
            }

            var deeperChildren = GetCustomEditorObjectsInChildren(childObj);
            if (deeperChildren.Any())
            {
                childObjects.AddRange(deeperChildren);
            }
        }
        
        return childObjects;
    }

    private static bool ColorsAreEqual(Color a, Color b)
    {
        return Mathf.Approximately(a.r, b.r) &&
               Mathf.Approximately(a.g, b.g) &&
               Mathf.Approximately(a.b, b.b);
    }

    private static Dictionary<Color, int> GroupChildrenByColor(List<(GameObject, List<Color>)> children)
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) return new Dictionary<Color, int>();

        var colorGroups = new Dictionary<Color, int>();
        
        foreach (var (childObj, childColors) in children)
        {
            foreach (var childColor in childColors)
            {
                var existingColor = colorGroups.Keys.FirstOrDefault(c => ColorsAreEqual(c, childColor));
                
                if (existingColor != default(Color))
                {
                    colorGroups[existingColor]++;
                }
                else
                {
                    colorGroups[childColor] = 1;
                }
            }
        }
        
        return colorGroups;
    }

    private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) return;

        var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        var componentColors = GetCustomEditorColorsForObject(obj);
        bool hasCustomEditor = componentColors.Any();
        
        var childObjectsWithColors = GetCustomEditorObjectsInChildren(obj);
        bool hasCustomEditorInChildren = childObjectsWithColors.Any();

        bool isSelected = Selection.activeGameObject == obj;
        bool isHierarchyFocused = EditorWindow.focusedWindow != null && 
                                 EditorWindow.focusedWindow.GetType().Name == "SceneHierarchyWindow";

        if (hasCustomEditor || hasCustomEditorInChildren)
        {
            if (isSelected)
            {
                Color backgroundColor = isHierarchyFocused ? selectionColor : inactiveSelectionColor;
                EditorGUI.DrawRect(selectionRect, backgroundColor);
            }
            else
            {
                EditorGUI.DrawRect(selectionRect, highlightColor);
            }

            Color textColor = hasCustomEditor ? componentColors[0] : Color.white;
            textColor.a = obj.activeSelf ? 1f : 0.5f;

            Rect nameRect = new Rect(selectionRect);
            EditorGUI.LabelField(nameRect, obj.name, 
                new GUIStyle(EditorStyles.label) { 
                    normal = { textColor = textColor } 
                });

            if (hasCustomEditorInChildren)
            {
                float textWidth = EditorStyles.label.CalcSize(new GUIContent(obj.name)).x;
                float currentX = selectionRect.x + textWidth;
                float padding = 2f;

                var colorGroups = GroupChildrenByColor(childObjectsWithColors);
                
                foreach (var group in colorGroups)
                {
                    Color groupColor = group.Key;
                    int count = group.Value;
                    groupColor.a = obj.activeSelf ? 1f : 0.5f;

                    string countText = count.ToString();
                    float numberWidth = EditorStyles.label.CalcSize(new GUIContent(countText)).x;
                    Rect numberRect = new Rect(currentX, selectionRect.y, numberWidth, selectionRect.height);
                    
                    EditorGUI.LabelField(numberRect, countText, 
                        new GUIStyle(EditorStyles.label) { 
                            normal = { textColor = groupColor },
                            fontSize = 10,
                            alignment = TextAnchor.MiddleLeft
                        });
                    
                    currentX += numberWidth + padding;

                    float indicatorWidth = 10f;
                    Rect indicatorRect = new Rect(currentX, selectionRect.y, indicatorWidth, selectionRect.height);
                    
                    EditorGUI.LabelField(indicatorRect, childIndicator, 
                        new GUIStyle(EditorStyles.label) { 
                            normal = { textColor = groupColor },
                            fontSize = 8,
                            alignment = TextAnchor.MiddleLeft
                        });
                    
                    currentX += indicatorWidth + padding * 2;
                }
            }

            if (hasCustomEditor && componentColors.Any())
            {
                float dotSize = 8f;
                float dotPadding = 2f;
                
                for (int i = componentColors.Count - 1; i >= 0; i--)
                {
                    Color dotColor = componentColors[i];
                    dotColor.a = obj.activeSelf ? 1f : 0.5f;
                    
                    Rect dotRect = new Rect(
                        selectionRect.xMax - (dotSize + dotPadding) * (componentColors.Count - i), 
                        selectionRect.y + (selectionRect.height - dotSize) / 2, 
                        dotSize, 
                        dotSize
                    );
                    
                    Handles.color = dotColor;
                    Handles.DrawSolidDisc(dotRect.center, Vector3.forward, dotRect.width / 2);
                }
            }
        }
    }

    private static Type GetInspectedType(CustomEditor attr)
    {
        if (Application.isPlaying || IsCustomEditorDisabled()) return null;

        try
        {
            var field = attr.GetType().GetField("m_InspectedType",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
                return field.GetValue(attr) as Type;

            var property = attr.GetType().GetProperty("inspectedType",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            
            if (property != null)
                return property.GetValue(attr) as Type;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error getting inspected type: {e.Message}");
        }
        return null;
    }

    [MenuItem("Tools/Update Custom Editor Highlighting")]
    private static void RefreshCache()
    {
        if (Application.isPlaying) return;

        editorReferenceChecked = false; // Reset cache when manually refreshing
        customEditorColors.Clear();
        UpdateCustomEditorCache();
        EditorApplication.RepaintHierarchyWindow();
    }
}
#endif