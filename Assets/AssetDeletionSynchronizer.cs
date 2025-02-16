using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AssetDeletionSynchronizer : AssetModificationProcessor
{
    private const string ISLANDS_PATH = "Assets/Prefabs/Islands";
    private const string CONTENT_PATH = "Assets/Prefabs/IslandContent";

    private static string GetBaseNameWithoutPrefix(string fileName)
    {
        // Remove .prefab extension if present
        if (fileName.EndsWith(".prefab"))
        {
            fileName = fileName.Substring(0, fileName.Length - 7);
        }
        
        // Remove XX- prefix if present
        if (fileName.Length > 3 && fileName[2] == '-')
        {
            fileName = fileName.Substring(3);
        }
        
        // Handle potential numbered suffixes
        if (fileName.Length > 1)
        {
            char lastChar = fileName[fileName.Length - 1];
            if (char.IsDigit(lastChar) && fileName[fileName.Length - 2] == 'n')
            {
                // Keep the number at the end
                return fileName;
            }
        }
        return fileName;
    }

    private static string GetContentName(string islandName)
    {
        string baseName = GetBaseNameWithoutPrefix(islandName);
        return baseName + "_content";
    }

    // Diese Methode wird nach jeder Asset-Änderung aufgerufen
    private class AssetPostprocessor : UnityEditor.AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Synchronisiere nach jeder Änderung
            SynchronizeFolders();
        }
    }

    [MenuItem("Tools/Synchronize Island Content")]
    private static void SynchronizeFolders()
    {
        if (!Directory.Exists(ISLANDS_PATH) || !Directory.Exists(CONTENT_PATH))
        {
            Debug.LogError("Required folders do not exist!");
            return;
        }

        // Hole alle Prefabs aus beiden Ordnern
        string[] islandPrefabs = Directory.GetFiles(ISLANDS_PATH, "*.prefab");
        string[] contentPrefabs = Directory.GetFiles(CONTENT_PATH, "*.prefab");

        // Erstelle eine Map der erwarteten Content-Prefabs
        Dictionary<string, bool> expectedContent = new Dictionary<string, bool>();
        
        // Verarbeite Island Prefabs und erstelle/aktualisiere entsprechende Content Prefabs
        foreach (string islandPath in islandPrefabs)
        {
            string islandName = Path.GetFileNameWithoutExtension(islandPath);
            string expectedContentName = GetContentName(islandName);
            string expectedContentPath = Path.Combine(CONTENT_PATH, expectedContentName + ".prefab");
            
            expectedContent[expectedContentPath.ToLower()] = true;

            // Erstelle Content Prefab falls es nicht existiert
            if (!File.Exists(expectedContentPath))
            {
                CreateEmptyContentPrefab(expectedContentPath);
            }
        }

        // Optional: Warnungen für nicht zugeordnete Content Prefabs ausgeben
        foreach (string contentPath in contentPrefabs)
        {
            if (!expectedContent.ContainsKey(contentPath.ToLower()))
            {
                Debug.LogWarning($"Found unmatched content prefab: {contentPath}");
            }
        }

        AssetDatabase.Refresh();
    }

    private static void CreateEmptyContentPrefab(string path)
    {
        // Erstelle ein leeres GameObject
        GameObject tempObj = new GameObject(Path.GetFileNameWithoutExtension(path));
        
        // Speichere es als Prefab
        bool success = false;
        try
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, path);
            if (prefab != null)
            {
                success = true;
                Debug.Log($"Created new content prefab: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating prefab: {e.Message}");
        }
        finally
        {
            // Cleanup
            Object.DestroyImmediate(tempObj);
            if (!success)
            {
                Debug.LogError($"Failed to create prefab: {path}");
            }
        }
    }

    private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
    {
        if (!sourcePath.StartsWith(ISLANDS_PATH) || !sourcePath.EndsWith(".prefab"))
            return AssetMoveResult.DidNotMove;

        string sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
        string destFileName = Path.GetFileNameWithoutExtension(destinationPath);

        string sourceContentPath = Path.Combine(CONTENT_PATH, GetContentName(sourceFileName) + ".prefab");
        string destContentPath = Path.Combine(CONTENT_PATH, GetContentName(destFileName) + ".prefab");

        // Wenn die Quelldatei existiert
        if (File.Exists(sourceContentPath))
        {
            try 
            {
                // Überprüfe ob Quelle und Ziel identisch sind
                if (sourceContentPath.ToLower() == destContentPath.ToLower())
                {
                    // Ignoriere den Vorgang wenn es der gleiche Pfad ist
                    return AssetMoveResult.DidNotMove;
                }

                Debug.Log($"Renaming content prefab from {sourceContentPath} to {destContentPath}");
                
                // Erst verschieben/kopieren
                string error = AssetDatabase.MoveAsset(sourceContentPath, destContentPath);
                
                // Wenn die Verschiebung erfolgreich war
                if (string.IsNullOrEmpty(error))
                {
                    // Warte einen Frame um sicherzustellen, dass die Verschiebung abgeschlossen ist
                    EditorApplication.delayCall += () =>
                    {
                        // Prüfe ob die alte Datei noch existiert und die neue erfolgreich erstellt wurde
                        if (File.Exists(sourceContentPath) && File.Exists(destContentPath))
                        {
                            Debug.Log($"Deleting old content prefab: {sourceContentPath}");
                            AssetDatabase.DeleteAsset(sourceContentPath);
                            AssetDatabase.Refresh();
                        }
                    };
                }
                else
                {
                    // Ignoriere den spezifischen Fehler für gleiche Pfade
                    if (!error.Contains("Trying to move asset to location it came from"))
                    {
                        Debug.LogError($"Error moving asset: {error}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to move asset: {e.Message}");
            }
        }

        return AssetMoveResult.DidNotMove;
    }

    private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        if (!assetPath.StartsWith(ISLANDS_PATH) || !assetPath.EndsWith(".prefab"))
            return AssetDeleteResult.DidNotDelete;

        try
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string baseNameWithoutPrefix = GetBaseNameWithoutPrefix(fileName);
            string contentPath = Path.Combine(CONTENT_PATH, baseNameWithoutPrefix + "_content.prefab");

            if (File.Exists(contentPath))
            {
                Debug.Log($"Deleting corresponding content prefab: {contentPath}");
                AssetDatabase.DeleteAsset(contentPath);
            }

            // Führe eine vollständige Synchronisation durch
            EditorApplication.delayCall += () => 
            {
                FullSynchronization();
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to delete content asset: {e.Message}");
        }

        return AssetDeleteResult.DidNotDelete;
    }

    private static void FullSynchronization()
    {
        try
        {
            // Hole alle existierenden Prefabs
            string[] islandPrefabs = Directory.GetFiles(ISLANDS_PATH, "*.prefab");
            string[] contentPrefabs = Directory.GetFiles(CONTENT_PATH, "*.prefab");

            // Erstelle einen Hash-Set der erwarteten Content-Namen
            HashSet<string> validContentNames = new HashSet<string>();
            foreach (string islandPath in islandPrefabs)
            {
                string islandName = Path.GetFileNameWithoutExtension(islandPath);
                string baseNameWithoutPrefix = GetBaseNameWithoutPrefix(islandName);
                validContentNames.Add(baseNameWithoutPrefix.ToLower() + "_content");
            }

            // Lösche alle Content Prefabs die kein entsprechendes Island haben
            foreach (string contentPath in contentPrefabs)
            {
                string contentName = Path.GetFileNameWithoutExtension(contentPath).ToLower();
                if (!validContentNames.Contains(contentName))
                {
                    Debug.Log($"Cleaning up orphaned content prefab: {contentPath}");
                    AssetDatabase.DeleteAsset(contentPath);
                }
            }

            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during full synchronization: {e.Message}");
        }
    }
}