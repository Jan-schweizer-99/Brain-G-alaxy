using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

[ExecuteInEditMode]
public class EditorCameraTrigger : MonoBehaviour
{
    [Header("Prefab Settings")]
    public string prefabFolderPath = "Assets/Prefabs/IslandContent";
    private string contentPrefabName;

    public GameObject prefabToSpawn;
    private bool isInsideCollider = false;
    private GameObject spawnedInstance;
    private double lastUpdateTime;
    private const double UPDATE_INTERVAL = 0.1; // 100ms interval

    [Header("UI References")]
    public Canvas infoCanvas;
    public TextMeshProUGUI objectNameText;

    [Header("Gizmo Settings")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    public Color gizmoWireColor = Color.green;

    [Header("Debug Info")]
    [SerializeField, ReadOnly] private Vector3 targetPosition;
    [SerializeField, ReadOnly] private bool isTriggerActive;
    [SerializeField, ReadOnly] private string debugStatus;

    private void OnEnable()
    {
        #if UNITY_EDITOR
        InitializePrefab();
        if (!Application.isPlaying)
        {
            EditorApplication.update += OnEditorUpdate;
        }
        #endif

        if (objectNameText != null)
        {
            string displayName = gameObject.name;
            if (Application.isPlaying && displayName.EndsWith("(Clone)"))
            {
                displayName = displayName.Substring(0, displayName.Length - 7);
            }
            objectNameText.text = displayName;
        }
        UpdateCanvasVisibility();
    }

    private void OnDisable()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        #endif
        DestroySpawnedInstance();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            CheckMainCamera();
        }
    }

    #if UNITY_EDITOR
    private void InitializePrefab()
    {
        string objectName = gameObject.name;
        
        // Remove (Clone) if present
        if (objectName.EndsWith("(Clone)"))
        {
            objectName = objectName.Substring(0, objectName.Length - 7);
        }
        
        if (objectName.Length < 3) return;

        // Remove the first 3 characters (e.g., "01-")
        string baseNumber = objectName.Substring(3);
        contentPrefabName = baseNumber + "_content";

        // Ensure the folder exists
        if (!Directory.Exists(prefabFolderPath))
        {
            Directory.CreateDirectory(prefabFolderPath);
        }

        // Check if prefab exists
        string prefabPath = Path.Combine(prefabFolderPath, contentPrefabName + ".prefab");
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (existingPrefab == null)
        {
            // Create new empty prefab
            GameObject tempObj = new GameObject(contentPrefabName);
            
            // Create the prefab
            GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
            DestroyImmediate(tempObj);

            if (newPrefab != null)
            {
                Debug.Log($"Created new prefab: {prefabPath}");
                prefabToSpawn = newPrefab;
            }
        }
        else
        {
            prefabToSpawn = existingPrefab;
        }

        // Refresh the asset database
        AssetDatabase.Refresh();
    }
    #endif

    private void CheckMainCamera()
    {
        if (Camera.main == null) return;
        
        Collider trigger = GetComponent<Collider>();
        if (trigger == null) return;

        Vector3 cameraPosition = Camera.main.transform.position;
        bool cameraInTrigger = trigger.bounds.Contains(cameraPosition);
        
        if (cameraInTrigger != isInsideCollider)
        {
            isInsideCollider = cameraInTrigger;
            isTriggerActive = cameraInTrigger;
            targetPosition = cameraPosition;

            if (cameraInTrigger)
            {
                if (prefabToSpawn != null && spawnedInstance == null)
                {
                    Transform parentTransform = FindCorrectParent();
                    spawnedInstance = Instantiate(prefabToSpawn, parentTransform.position, parentTransform.rotation, parentTransform);
                    // Remove (Clone) from the name
                    if (spawnedInstance.name.EndsWith("(Clone)"))
                    {
                        spawnedInstance.name = spawnedInstance.name.Substring(0, spawnedInstance.name.Length - 7);
                    }
                }
            }
            else
            {
                if (spawnedInstance != null)
                {
                    Destroy(spawnedInstance);
                    spawnedInstance = null;
                }
            }

            UpdateCanvasVisibility();
        }

        debugStatus = $"Camera in trigger: {cameraInTrigger}";
    }

    private void UpdateCanvasVisibility()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(!isInsideCollider);
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Color originalColor = Gizmos.color;

        // Draw filled collider
        Gizmos.color = gizmoColor;
        if (col is BoxCollider boxCol)
        {
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(boxCol.center, boxCol.size);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            Gizmos.matrix = originalMatrix;
        }
        else if (col is SphereCollider sphereCol)
        {
            float radius = sphereCol.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Vector3 worldCenter = transform.TransformPoint(sphereCol.center);
            Gizmos.DrawSphere(worldCenter, radius);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireSphere(worldCenter, radius);
        }
        
        Gizmos.color = originalColor;
    }

    #if UNITY_EDITOR
    private void OnEditorUpdate()
    {
        // Throttle updates to reduce CPU usage
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime < UPDATE_INTERVAL)
        {
            return;
        }
        lastUpdateTime = currentTime;

        if (!Application.isPlaying && SceneView.lastActiveSceneView?.camera != null)
        {
            var camera = SceneView.lastActiveSceneView.camera;
            targetPosition = camera.transform.position;
            
            Collider collider = GetComponent<Collider>();
            if (collider == null) return;

            bool isCurrentlyInside = collider.bounds.Contains(targetPosition);
            
            if (isCurrentlyInside != isInsideCollider)
            {
                isTriggerActive = isCurrentlyInside;
                debugStatus = $"Inside: {isCurrentlyInside}, Previous: {isInsideCollider}, HasPrefab: {prefabToSpawn != null}, Instance: {spawnedInstance != null}";
                
                if (isCurrentlyInside && prefabToSpawn != null && spawnedInstance == null)
                {
                    CreatePrefabInstance();
                }
                else if (!isCurrentlyInside)
                {
                    DestroySpawnedInstance();
                }
                
                isInsideCollider = isCurrentlyInside;
                UpdateCanvasVisibility();

                // Only mark dirty when there's an actual change
                if (Selection.activeGameObject == gameObject)
                {
                    EditorUtility.SetDirty(this);
                }
            }
        }
    }

    private void CreatePrefabInstance()
    {
        try
        {
            if (prefabToSpawn == null) return;

            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabToSpawn) ?? prefabToSpawn;
            spawnedInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            
            if (spawnedInstance != null)
            {
                Transform parentTransform = FindCorrectParent();
                spawnedInstance.transform.SetParent(parentTransform, false);
                spawnedInstance.transform.position = parentTransform.position;
                spawnedInstance.transform.rotation = parentTransform.rotation;
                
                // Remove (Clone) from the spawned instance name
                if (spawnedInstance.name.EndsWith("(Clone)"))
                {
                    spawnedInstance.name = spawnedInstance.name.Substring(0, spawnedInstance.name.Length - 7);
                }
                
                Undo.RegisterCreatedObjectUndo(spawnedInstance, "Spawn Prefab in Editor");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error instantiating prefab: {e.Message}");
        }
    }

    private void DestroySpawnedInstance()
    {
        if (spawnedInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(spawnedInstance);
            }
            else
            {
                Undo.DestroyObjectImmediate(spawnedInstance);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            spawnedInstance = null;
        }
    }

    private Transform FindCorrectParent()
    {
        string currentName = gameObject.name;
        if (currentName.EndsWith("(Clone)"))
        {
            currentName = currentName.Substring(0, currentName.Length - 7);
        }

        // Get the base name (without 01-)
        string baseName = currentName.Substring(3);
        
        // Look for parent in the scene
        Transform sceneRoot = gameObject.scene.GetRootGameObjects()[0].transform;
        foreach (Transform child in sceneRoot)
        {
            string childName = child.name;
            if (childName.EndsWith("(Clone)"))
            {
                childName = childName.Substring(0, childName.Length - 7);
            }
            
            if (childName.EndsWith(baseName))
            {
                return child;
            }
        }
        
        return transform; // Fallback to current transform if no matching parent found
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (prefabToSpawn != null && PrefabUtility.GetPrefabAssetType(prefabToSpawn) == PrefabAssetType.NotAPrefab)
        {
            Debug.LogWarning($"Warning: {prefabToSpawn.name} should be a prefab!");
        }

        if (objectNameText != null)
        {
            string displayName = gameObject.name;
            if (Application.isPlaying && displayName.EndsWith("(Clone)"))
            {
                displayName = displayName.Substring(0, displayName.Length - 7);
            }
            objectNameText.text = displayName;
        }
        #endif
    }
    #endif
}

// ReadOnly attribute implementation
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif