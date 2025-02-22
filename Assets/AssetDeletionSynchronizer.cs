using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AssetDeletionSynchronizer : AssetModificationProcessor
{
    private const string ISLANDS_PATH = "Assets/Prefabs/Islands";
    private const string CONTENT_PATH = "Assets/Prefabs/IslandContent";
    private const string TEMPLATE_PATH = "Assets/Prefabs/IslandContent/_template.prefab"; // Pfad zum Template

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

    private class AssetPostprocessor : UnityEditor.AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
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

        // Check if template exists
        GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEMPLATE_PATH);
        if (templatePrefab == null)
        {
            Debug.LogError($"Template prefab not found at {TEMPLATE_PATH}! Please ensure it exists.");
            return;
        }

        string[] islandPrefabs = Directory.GetFiles(ISLANDS_PATH, "*.prefab");
        string[] contentPrefabs = Directory.GetFiles(CONTENT_PATH, "*.prefab");
        Dictionary<string, bool> expectedContent = new Dictionary<string, bool>();
        
        foreach (string islandPath in islandPrefabs)
        {
            string islandName = Path.GetFileNameWithoutExtension(islandPath);
            string expectedContentName = GetContentName(islandName);
            string expectedContentPath = Path.Combine(CONTENT_PATH, expectedContentName + ".prefab");
            
            expectedContent[expectedContentPath.ToLower()] = true;

            // Create content prefab from template if it doesn't exist
            if (!File.Exists(expectedContentPath))
            {
                CreateContentPrefabFromTemplate(expectedContentPath, templatePrefab);
            }
        }

        // Optional: Output warnings for unmatched content prefabs
        foreach (string contentPath in contentPrefabs)
        {
            if (!expectedContent.ContainsKey(contentPath.ToLower()))
            {
                Debug.LogWarning($"Found unmatched content prefab: {contentPath}");
            }
        }
    }

    private static void CreateContentPrefabFromTemplate(string path, GameObject templatePrefab)
    {
        if (templatePrefab == null)
        {
            Debug.LogError("Template prefab is null!");
            return;
        }

        string templatePath = AssetDatabase.GetAssetPath(templatePrefab);
        if (string.IsNullOrEmpty(templatePath))
        {
            Debug.LogError("Template prefab has no valid path!");
            return;
        }

        // Create a copy of the template
        bool success = AssetDatabase.CopyAsset(templatePath, path);
        if (success)
        {
            Debug.Log($"Created new content prefab from template: {path}");
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError($"Failed to create content prefab at: {path}");
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

        if (File.Exists(sourceContentPath))
        {
            try 
            {
                if (sourceContentPath.ToLower() == destContentPath.ToLower())
                {
                    return AssetMoveResult.DidNotMove;
                }

                Debug.Log($"Renaming content prefab from {sourceContentPath} to {destContentPath}");
                
                string error = AssetDatabase.MoveAsset(sourceContentPath, destContentPath);
                
                if (string.IsNullOrEmpty(error))
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (File.Exists(sourceContentPath) && File.Exists(destContentPath))
                        {
                            AssetDatabase.DeleteAsset(sourceContentPath);
                            AssetDatabase.Refresh();
                        }
                    };
                }
                else if (!error.Contains("Trying to move asset to location it came from"))
                {
                    Debug.LogError($"Error moving asset: {error}");
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
            string[] islandPrefabs = Directory.GetFiles(ISLANDS_PATH, "*.prefab");
            string[] contentPrefabs = Directory.GetFiles(CONTENT_PATH, "*.prefab");

            HashSet<string> validContentNames = new HashSet<string>();
            foreach (string islandPath in islandPrefabs)
            {
                string islandName = Path.GetFileNameWithoutExtension(islandPath);
                string baseNameWithoutPrefix = GetBaseNameWithoutPrefix(islandName);
                validContentNames.Add(baseNameWithoutPrefix.ToLower() + "_content");
            }

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