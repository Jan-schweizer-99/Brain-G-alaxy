using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
public class VRPrefabGeometrySaver : MonoBehaviour
{
    [SerializeField]
    private string prefabFolderPath = "Assets/Prefabs";
    
    [SerializeField]
    private string geometryFolderPath = "Assets/GeometryData";

    [SerializeField]
    private InputActionReference saveButton;

    private bool isInSavingZone = false;
    private XROrigin xrOrigin;
    private BoxCollider triggerZone;

    private string GetCleanIslandName(string originalName)
    {
        // Remove the first 3 characters if the name is long enough
        if (originalName.Length > 3)
        {
            return originalName.Substring(3);
        }
        return originalName;
    }

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // BoxCollider am selben GameObject initialisieren
        triggerZone = gameObject.GetComponent<BoxCollider>();
        if (triggerZone == null)
        {
            triggerZone = gameObject.AddComponent<BoxCollider>();
            triggerZone.isTrigger = true;
            triggerZone.size = new Vector3(3f, 3f, 3f);
        }

        // XR Origin finden
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("Kein XR Origin gefunden!");
            return;
        }

        // CharacterController am XR Origin GameObject hinzufügen/initialisieren
        var characterController = xrOrigin.gameObject.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = xrOrigin.gameObject.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.3f;
            characterController.minMoveDistance = 0.001f;
            characterController.center = new Vector3(0, 1f, 0);
        }
    }

    private void OnEnable()
    {
        if (saveButton != null)
        {
            saveButton.action.Enable();
            saveButton.action.performed += OnSaveButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (saveButton != null)
        {
            saveButton.action.performed -= OnSaveButtonPressed;
            saveButton.action.Disable();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (xrOrigin != null && other.gameObject == xrOrigin.gameObject)
        {
            isInSavingZone = true;
            Debug.Log("XR Rig entered saving zone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (xrOrigin != null && other.gameObject == xrOrigin.gameObject)
        {
            isInSavingZone = false;
            Debug.Log("XR Rig left saving zone");
        }
    }

    private void OnSaveButtonPressed(InputAction.CallbackContext context)
    {
        if (isInSavingZone)
        {
            SavePrefabWithGeometry();
        }
        else
        {
            Debug.Log("Cannot save: XR Rig is not in the saving zone");
        }
    }

    public void SavePrefabWithGeometry()
    {
        if (!isInSavingZone)
        {
            Debug.LogWarning("Cannot save: Not in saving zone");
            return;
        }

        CreateDirectoryIfNotExists(prefabFolderPath);
        CreateDirectoryIfNotExists(geometryFolderPath);

        string prefabName = gameObject.name;
        // Clean the geometry folder name by removing the first 3 characters
        string cleanGeometryName = GetCleanIslandName(prefabName);
        string prefabGeometryPath = Path.Combine(geometryFolderPath, cleanGeometryName);
        CreateDirectoryIfNotExists(prefabGeometryPath);

        string prefabPath = Path.Combine(prefabFolderPath, $"{prefabName}.prefab");

        AssetDatabase.StartAssetEditing();

        try
        {
            var currentIslands = FindCombinedIslands();
            if (currentIslands.Count == 0)
            {
                Debug.LogWarning("No CombinedIslands found to save");
                return;
            }

            CleanupUnusedGeometries(prefabGeometryPath, currentIslands);
            
            for (int i = 0; i < currentIslands.Count; i++)
            {
                string groupName = i == 0 ? "CombinedIsland" : $"CombinedIsland ({i})";
                ProcessIslandGroup(currentIslands[i], prefabGeometryPath, groupName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefabRoot = gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            }

            if (File.Exists(prefabPath))
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"Prefab aktualisiert: {prefabPath}");
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"Neues Prefab erstellt: {prefabPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fehler beim Speichern: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    private List<Transform> FindCombinedIslands()
    {
        List<Transform> islands = new List<Transform>();
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains("CombinedIsland"))
            {
                islands.Add(child);
            }
        }
        return islands;
    }

    private void ProcessIslandGroup(Transform islandGroup, string basePath, string groupName)
    {
        // Clean the island name by removing the first 3 characters
        string cleanGroupName = GetCleanIslandName(groupName);
        string groupPath = Path.Combine(basePath, cleanGroupName);
        CreateDirectoryIfNotExists(groupPath);

        foreach (Transform child in islandGroup)
        {
            if (child.name.Contains("Surface"))
            {
                var meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Mesh meshCopy = new Mesh();
                    meshCopy.vertices = meshFilter.sharedMesh.vertices;
                    meshCopy.triangles = meshFilter.sharedMesh.triangles;
                    meshCopy.normals = meshFilter.sharedMesh.normals;
                    meshCopy.uv = meshFilter.sharedMesh.uv;
                    meshCopy.bounds = meshFilter.sharedMesh.bounds;

                    string meshPath = Path.Combine(groupPath, $"{child.name}.asset");
                    SaveMeshAsset(meshCopy, meshPath);

                    meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                }
            }
        }
    }

    private void CleanupUnusedGeometries(string prefabGeometryPath, List<Transform> currentIslands)
    {
        if (!Directory.Exists(prefabGeometryPath)) return;

        HashSet<string> currentIslandNames = new HashSet<string>();
        for (int i = 0; i < currentIslands.Count; i++)
        {
            string groupName = i == 0 ? "CombinedIsland" : $"CombinedIsland ({i})";
            currentIslandNames.Add(GetCleanIslandName(groupName));
        }

        string[] existingDirectories = Directory.GetDirectories(prefabGeometryPath);
        foreach (string dir in existingDirectories)
        {
            string dirName = Path.GetFileName(dir);
            if (!currentIslandNames.Contains(dirName))
            {
                try
                {
                    string[] assetFiles = Directory.GetFiles(dir, "*.asset");
                    foreach (string assetFile in assetFiles)
                    {
                        AssetDatabase.DeleteAsset(GetUnityPath(assetFile));
                    }

                    Directory.Delete(dir, true);
                    Debug.Log($"Gelöschter ungenutzter Geometrie-Ordner: {dir}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Fehler beim Löschen von {dir}: {e.Message}");
                }
            }
        }

        foreach (Transform island in currentIslands)
        {
            string cleanIslandName = GetCleanIslandName(island.name);
            string islandPath = Path.Combine(prefabGeometryPath, cleanIslandName);
            if (Directory.Exists(islandPath))
            {
                CleanupUnusedSurfaceMeshes(islandPath, island);
            }
        }

        AssetDatabase.Refresh();
    }

    private void CleanupUnusedSurfaceMeshes(string islandPath, Transform island)
    {
        HashSet<string> currentSurfaceNames = new HashSet<string>();
        foreach (Transform child in island)
        {
            if (child.name.Contains("Surface"))
            {
                currentSurfaceNames.Add($"{child.name}.asset");
            }
        }

        string[] existingMeshes = Directory.GetFiles(islandPath, "*.asset");
        foreach (string meshPath in existingMeshes)
        {
            string meshFileName = Path.GetFileName(meshPath);
            if (!currentSurfaceNames.Contains(meshFileName))
            {
                AssetDatabase.DeleteAsset(GetUnityPath(meshPath));
                Debug.Log($"Gelöschtes ungenutztes Mesh-Asset: {meshPath}");
            }
        }
    }

    private string GetUnityPath(string fullPath)
    {
        return fullPath.Substring(fullPath.IndexOf("Assets"));
    }

    private void SaveMeshAsset(Mesh mesh, string path)
    {
        try
        {
            // Wenn ein bestehendes Mesh existiert, lösche es zuerst
            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existingMesh != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            // Erstelle eine neue Kopie des Meshes
            Mesh meshToSave = new Mesh();
            meshToSave.vertices = mesh.vertices;
            meshToSave.triangles = mesh.triangles;
            meshToSave.normals = mesh.normals;
            meshToSave.uv = mesh.uv;
            meshToSave.bounds = mesh.bounds;
            
            // Speichere das neue Mesh als Asset
            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fehler beim Speichern des Mesh-Assets: {e.Message}");
        }
    }

    private void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}

[CustomEditor(typeof(VRPrefabGeometrySaver))]
public class VRPrefabGeometrySaverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        
        VRPrefabGeometrySaver saver = (VRPrefabGeometrySaver)target;
        
        GUILayout.Space(10);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
        if(GUILayout.Button("Prefab mit Geometrie Speichern"))
        {
            saver.SavePrefabWithGeometry();
        }
        EditorGUI.EndDisabledGroup();
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif