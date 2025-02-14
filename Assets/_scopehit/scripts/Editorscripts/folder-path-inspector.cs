using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

[InitializeOnLoad]
public static class ProjectTabRenamer
{
    private static Dictionary<int, string> windowTitles = new Dictionary<int, string>();
    private static EditorWindow currentProjectWindow;
    private static GUIContent folderIcon;
    private static string savePath;

    static ProjectTabRenamer()
    {
        EditorApplication.update += UpdateProjectTab;
        Selection.selectionChanged += OnSelectionChanged;
        
        folderIcon = EditorGUIUtility.IconContent("Folder Icon");
        savePath = Path.Combine(Application.dataPath, "../Library/ProjectTabTitles.json");
        
        // Use delayCall to ensure everything is initialized
        EditorApplication.delayCall += () => {
            LoadTitles();
            RestoreAllWindowTitles();
        };
    }

    [Serializable]
    private class WindowTitle
    {
        public int windowId;
        public string title;
    }

    [Serializable]
    private class SaveData
    {
        public List<WindowTitle> titles = new List<WindowTitle>();
    }

    static void SaveTitles()
    {
        try
        {
            var saveData = new SaveData
            {
                titles = windowTitles.Select(kvp => new WindowTitle 
                { 
                    windowId = kvp.Key, 
                    title = kvp.Value 
                }).ToList()
            };

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Saved {windowTitles.Count} titles");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save window titles: {e.Message}");
        }
    }

    static void LoadTitles()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);
                
                windowTitles.Clear();
                foreach (var item in saveData.titles)
                {
                    windowTitles[item.windowId] = item.title;
                }
                Debug.Log($"Loaded {windowTitles.Count} titles");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load window titles: {e.Message}");
        }
    }

    static void RestoreAllWindowTitles()
    {
        var windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
        foreach (EditorWindow window in windows)
        {
            if (window.GetType().Name == "ProjectBrowser")
            {
                int id = window.GetInstanceID();
                if (windowTitles.ContainsKey(id))
                {
                    window.titleContent = new GUIContent(windowTitles[id], folderIcon.image);
                    Debug.Log($"Restored title for window {id}: {windowTitles[id]}");
                }
            }
        }
    }

    static void OnSelectionChanged()
    {
        if (EditorWindow.focusedWindow != null && 
            EditorWindow.focusedWindow.GetType().Name == "ProjectBrowser")
        {
            currentProjectWindow = EditorWindow.focusedWindow;
            UpdateWindowTitle(currentProjectWindow);
        }
    }

    static string GetCurrentFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Project";

        // Normalize path separators
        path = path.Replace('\\', '/');
        
        if (Directory.Exists(path))
        {
            // For directories, show the parent folder
            string parentPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentPath))
            {
                return Path.GetFileName(parentPath);
            }
        }
        else if (File.Exists(path))
        {
            // For files, show their direct containing folder
            return Path.GetFileName(Path.GetDirectoryName(path));
        }

        return "Project";
    }

    static void UpdateWindowTitle(EditorWindow window)
    {
        if (window == null) return;

        int windowID = window.GetInstanceID();
        
        UnityEngine.Object[] selection = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
        if (selection.Length > 0)
        {
            string assetPath = AssetDatabase.GetAssetPath(selection[0]);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string displayPath = GetCurrentFolder(assetPath);
                windowTitles[windowID] = displayPath;
                window.titleContent = new GUIContent(displayPath, folderIcon.image);
                SaveTitles();
                Debug.Log($"Updated window {windowID} title to: {displayPath}");
            }
        }
    }

    static void UpdateProjectTab()
    {
        if (currentProjectWindow != null)
        {
            int windowID = currentProjectWindow.GetInstanceID();
            if (windowTitles.ContainsKey(windowID))
            {
                currentProjectWindow.titleContent = new GUIContent(
                    windowTitles[windowID],
                    folderIcon.image
                );
            }
        }
    }
}