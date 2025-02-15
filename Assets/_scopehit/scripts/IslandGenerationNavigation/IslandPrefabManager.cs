using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;

#if UNITY_EDITOR
public class IslandPrefabManager : MonoBehaviour
{
    [Header("Prefab Einstellungen")]
    public GameObject sourcePrefab;
    public string targetFolderPath = "Assets/Prefabs/Islands";

    [Header("Spiral Generator")]
    public ConicalSpiralIslandPlacer spiralPlacer;

    [Header("Neue Insel Einstellungen")]
    [SerializeField] private string newIslandName = "unknownIsland";
    [SerializeField] private int prefabNumber = 1;

    // Event das bei Änderungen ausgelöst wird
    public System.Action OnIslandsChanged;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(newIslandName))
            newIslandName = "unknownIsland";
    }

    public void CopyPrefabToFolder()
    {
        if (sourcePrefab == null)
        {
            Debug.LogError("Kein Quell-Prefab ausgewählt!");
            return;
        }

        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);
            AssetDatabase.Refresh();
        }

        // Prüfe ob bereits eine Insel mit diesem Namen existiert
        string[] existingFiles = Directory.GetFiles(targetFolderPath, "*.prefab");
        foreach (string file in existingFiles)
        {
            string fileName = Path.GetFileName(file);
            // Extrahiere den Inselnamen (ohne Nummer und .prefab)
            string existingIslandName = fileName.Substring(3, fileName.Length - 3 - 7);
            
            if (existingIslandName.Equals(newIslandName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"Eine Insel mit dem Namen '{newIslandName}' existiert bereits!");
                return;
            }
        }

        string formattedNumber = prefabNumber.ToString("00");
        string newPrefabName = $"{formattedNumber}-{newIslandName}.prefab";
        string fullPath = Path.Combine(targetFolderPath, newPrefabName).Replace("\\", "/");

        if (File.Exists(fullPath))
        {
            Debug.LogWarning($"Ein Prefab mit dem Namen {newPrefabName} existiert bereits!");
            return;
        }

        bool success = AssetDatabase.CopyAsset(
            AssetDatabase.GetAssetPath(sourcePrefab),
            fullPath
        );

        if (success)
        {
            Debug.Log($"Prefab erfolgreich kopiert: {fullPath}");
            prefabNumber++;
            EditorUtility.SetDirty(this);
            OnIslandsChanged?.Invoke();
            
            // Aktualisiere den Spiral Placer
            if (spiralPlacer != null)
            {
                EditorApplication.delayCall += () => {
                    spiralPlacer.RegenerateAll();
                };
            }
        }
        else
        {
            Debug.LogError($"Fehler beim Kopieren des Prefabs nach {fullPath}");
        }

        AssetDatabase.Refresh();
    }

    public void DeleteIsland(string assetPath)
    {
        if (EditorUtility.DisplayDialog("Insel löschen",
            $"Möchten Sie die Insel wirklich löschen?",
            "Ja", "Abbrechen"))
        {
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();
            RenumberIslands();
            OnIslandsChanged?.Invoke();
            
            // Aktualisiere den Spiral Placer
            if (spiralPlacer != null)
            {
                EditorApplication.delayCall += () => {
                    spiralPlacer.RegenerateAll();
                };
            }
        }
    }

    public void RenumberIslands()
    {
        string[] allFiles = Directory.GetFiles(targetFolderPath, "*.prefab")
            .Select(f => f.Replace("\\", "/"))
            .OrderBy(f => f)
            .ToArray();

        List<(string oldPath, string newPath)> renamingQueue = new List<(string, string)>();

        for (int i = 0; i < allFiles.Length; i++)
        {
            string currentFile = allFiles[i];
            string fileName = Path.GetFileName(currentFile);
            string islandName = fileName.Substring(3, fileName.Length - 3 - 7);

            string newFileName = $"{(i + 1).ToString("00")}-{islandName}.prefab";
            string newPath = Path.Combine(targetFolderPath, newFileName).Replace("\\", "/");

            if (currentFile != newPath)
            {
                renamingQueue.Add((currentFile, newPath));
            }
        }

        foreach (var (oldPath, newPath) in renamingQueue)
        {
            AssetDatabase.MoveAsset(oldPath, newPath);
        }

        if (renamingQueue.Count > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log("Inseln erfolgreich neu nummeriert!");
            OnIslandsChanged?.Invoke();
            
            // Aktualisiere den Spiral Placer
            if (spiralPlacer != null)
            {
                EditorApplication.delayCall += () => {
                    spiralPlacer.RegenerateAll();
                };
            }
        }

        prefabNumber = allFiles.Length + 1;
        EditorUtility.SetDirty(this);
    }

    public void RenameIsland(string oldPath, string newName)
    {
        string directory = Path.GetDirectoryName(oldPath);
        string currentFileName = Path.GetFileName(oldPath);
        string prefix = currentFileName.Substring(0, 3); // Behält die Nummer bei
        string newFileName = $"{prefix}{newName}.prefab";
        string newPath = Path.Combine(directory, newFileName).Replace("\\", "/");

        if (oldPath != newPath)
        {
            // Prüfe ob der neue Name bereits existiert
            string[] existingFiles = Directory.GetFiles(targetFolderPath, "*.prefab");
            foreach (string file in existingFiles)
            {
                string fileName = Path.GetFileName(file);
                if (fileName != currentFileName) // Ignoriere die aktuelle Datei
                {
                    string existingIslandName = fileName.Substring(3, fileName.Length - 3 - 7);
                    if (existingIslandName.Equals(newName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning($"Eine Insel mit dem Namen '{newName}' existiert bereits!");
                        return;
                    }
                }
            }

            AssetDatabase.MoveAsset(oldPath, newPath);
            AssetDatabase.Refresh();
            
            // Aktualisiere den Spiral Placer
            if (spiralPlacer != null)
            {
                EditorApplication.delayCall += () => {
                    spiralPlacer.RegenerateAll();
                };
            }
            
            OnIslandsChanged?.Invoke();
        }
    }
}

[CustomEditor(typeof(IslandPrefabManager))]
public class IslandPrefabManagerEditor : CustomBaseEditor
{
    private ReorderableList islandList;
    private List<IslandData> islands = new List<IslandData>();
    private IslandPrefabManager manager;
    private int? editingIndex = null;
    private string editingName = "";

    private class IslandData
    {
        public string Path;
        public string DisplayName;
        public string Name;
    }

    private void OnEnable()
    {
        SetEditorStyle("IslandSystem");
        manager = (IslandPrefabManager)target;
        RefreshIslandLists();
        SetupReorderableList();
    }

    private void RefreshIslandLists()
    {
        islands.Clear();

        if (Directory.Exists(manager.targetFolderPath))
        {
            string[] files = Directory.GetFiles(manager.targetFolderPath, "*.prefab")
                .Select(f => f.Replace("\\", "/"))
                .OrderBy(f => f)
                .ToArray();

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string islandName = fileName.Substring(3, fileName.Length - 3 - 7);
                islands.Add(new IslandData 
                { 
                    Path = file,
                    DisplayName = $"{fileName.Substring(0, 2)}: {islandName}",
                    Name = islandName
                });
            }
        }

        if (islandList != null)
        {
            islandList.list = islands;
        }
    }

    private void ReorderIslands()
    {
        for (int i = 0; i < islands.Count; i++)
        {
            string currentPath = islands[i].Path;
            string fileName = Path.GetFileName(currentPath);
            string newFileName = $"{(i + 1).ToString("00")}{fileName.Substring(2)}";
            string newPath = Path.Combine(manager.targetFolderPath, newFileName).Replace("\\", "/");
            
            if (currentPath != newPath)
            {
                AssetDatabase.MoveAsset(currentPath, newPath);
            }
        }

        AssetDatabase.Refresh();
        RefreshIslandLists();

        // Aktualisiere den Spiral Placer nach der Neuordnung
        if (manager.spiralPlacer != null)
        {
            EditorApplication.delayCall += () => {
                manager.spiralPlacer.RegenerateAll();
            };
        }
        
        // Benachrichtige über die Änderung
        manager.OnIslandsChanged?.Invoke();
    }

    private void SetupReorderableList()
    {
        islandList = new ReorderableList(islands, typeof(IslandData), true, true, false, false)
        {
            drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Inseln (Drag & Drop zum Neuordnen)");
            },

            onReorderCallback = (ReorderableList list) =>
            {
                ReorderIslands();
            },

            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                rect.height -= 4;

                if (index < islands.Count)
                {
                    float buttonWidth = 55;
                    float spacing = 5;
                    float textFieldWidth = rect.width - buttonWidth * 2 - spacing * 2;

                    if (editingIndex == index)
                    {
                        // Zeige Textfeld für die Bearbeitung
                        string newName = EditorGUI.TextField(
                            new Rect(rect.x, rect.y, textFieldWidth, rect.height),
                            editingName
                        );

                        if (newName != editingName)
                        {
                            editingName = newName;
                        }

                        // Bestätigen Button
                        if (GUI.Button(
                            new Rect(rect.x + textFieldWidth + spacing, rect.y, buttonWidth, rect.height),
                            "OK"))
                        {
                            if (!string.IsNullOrWhiteSpace(editingName))
                            {
                                manager.RenameIsland(islands[index].Path, editingName);
                                editingIndex = null;
                                RefreshIslandLists();
                            }
                        }
                    }
                    else
                    {
                        // Normaler Anzeigemodus
                        EditorGUI.LabelField(
                            new Rect(rect.x, rect.y, textFieldWidth, rect.height),
                            islands[index].DisplayName
                        );

                        // Umbenennen Button
                        if (GUI.Button(
                            new Rect(rect.x + textFieldWidth + spacing, rect.y, buttonWidth, rect.height),
                            "Umbenennen"))
                        {
                            editingIndex = index;
                            editingName = islands[index].Name;
                        }
                    }

                    // Löschen Button (immer sichtbar)
                    if (GUI.Button(
                        new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height),
                        "Löschen"))
                    {
                        manager.DeleteIsland(islands[index].Path);
                        RefreshIslandLists();
                    }
                }
            }
        };
    }

    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        base.OnInspectorGUI(); // Dies ist wichtig für den Hintergrund und das Logo

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Neue Insel erstellen"))
        {
            manager.CopyPrefabToFolder();
            RefreshIslandLists();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Vorhandene Inseln", EditorStyles.boldLabel);
        islandList?.DoLayoutList();
    }
}
#endif