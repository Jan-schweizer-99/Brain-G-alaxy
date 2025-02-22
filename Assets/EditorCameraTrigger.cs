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
    public GameObject templatePrefab;
    private string contentPrefabName;

    public GameObject prefabToSpawn;
    private bool isInsideCollider = false;
    private GameObject spawnedInstance;
    private double lastUpdateTime;
    private const double UPDATE_INTERVAL = 0.2f; // Increased to 200ms

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
    private Vector3 lastCheckedPosition;

    private void OnEnable()
    {
        #if UNITY_EDITOR
        LoadPrefab();
        if (!Application.isPlaying)
        {
            EditorApplication.update += OnEditorUpdate;
        }
        #endif

        UpdateInfoText();
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
    private void LoadPrefab()
    {
        // Skip if already initialized or invalid name
        if (prefabToSpawn != null || gameObject.name.Length < 3) return;

        // First ensure we have a template
        if (templatePrefab == null)
        {
            Debug.LogError($"No template prefab assigned for {gameObject.name}! Please assign a template prefab first.");
            return;
        }

        string objectName = gameObject.name;
        if (objectName.EndsWith("(Clone)"))
        {
            objectName = objectName.Substring(0, objectName.Length - 7);
        }
        
        string baseNumber = objectName.Substring(3);
        string prefabPath = Path.Combine(prefabFolderPath, baseNumber + "_content.prefab");

        // First try to load an existing prefab
        prefabToSpawn = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabToSpawn != null)
        {
            return; // Use existing prefab
        }

        // Create directory if needed
        string directory = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Get the template path
        string templatePath = AssetDatabase.GetAssetPath(templatePrefab);
        if (string.IsNullOrEmpty(templatePath))
        {
            Debug.LogError($"Template prefab has no valid path: {templatePrefab.name}");
            return;
        }

        // Copy the template prefab directly
        bool success = AssetDatabase.CopyAsset(templatePath, prefabPath);
        if (!success)
        {
            Debug.LogError($"Failed to copy template prefab from {templatePath} to {prefabPath}");
            return;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        prefabToSpawn = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Failed to load copied prefab at {prefabPath}");
            return;
        }

        Debug.Log($"Successfully created new prefab from template: {prefabPath}");
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
                    spawnedInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn, parentTransform);
                    if (spawnedInstance != null)
                    {
                        spawnedInstance.transform.position = parentTransform.position;
                        spawnedInstance.transform.rotation = parentTransform.rotation;
                    }
                }
            }
            else
            {
                DestroySpawnedInstance();
            }

            UpdateCanvasVisibility();
        }
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
    // Frühe Ausführungsabbrüche für offensichtliche Fälle
    if (Application.isPlaying || !EditorWindow.focusedWindow) return;

    // Cache den Window-Typ-Check
    bool isSceneViewFocused = EditorWindow.focusedWindow.GetType().Name == "SceneView";
    if (!isSceneViewFocused) return;

    // Throttle updates
    double currentTime = EditorApplication.timeSinceStartup;
    if (currentTime - lastUpdateTime < UPDATE_INTERVAL)
    {
        return;
    }

    // Cache SceneView reference
    var sceneView = SceneView.lastActiveSceneView;
    if (sceneView?.camera == null) return;

    // Cache Collider reference
    var collider = GetComponent<Collider>();
    if (collider == null) return;

    lastUpdateTime = currentTime;
    targetPosition = sceneView.camera.transform.position;
    
    // Führe Bounds-Check nur durch, wenn sich die Position signifikant geändert hat
    if (Vector3.Distance(targetPosition, lastCheckedPosition) > 0.1f)
    {
        lastCheckedPosition = targetPosition;
        bool isCurrentlyInside = collider.bounds.Contains(targetPosition);
        
        if (isCurrentlyInside != isInsideCollider)
        {
            isTriggerActive = isCurrentlyInside;
            
            if (isCurrentlyInside && prefabToSpawn != null && spawnedInstance == null)
            {
                CreatePrefabInstance();
            }
            else if (!isCurrentlyInside && spawnedInstance != null)
            {
                DestroySpawnedInstance();
            }
            
            isInsideCollider = isCurrentlyInside;
            UpdateCanvasVisibility();
        }
    }
}

    private void CreatePrefabInstance()
    {
        if (prefabToSpawn == null) return;

        Transform parentTransform = FindCorrectParent();
        spawnedInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn, parentTransform);
        if (spawnedInstance != null)
        {
            spawnedInstance.transform.position = parentTransform.position;
            spawnedInstance.transform.rotation = parentTransform.rotation;
            Undo.RegisterCreatedObjectUndo(spawnedInstance, "Spawn Prefab in Editor");
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

        string baseName = currentName.Substring(3);
        
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
        
        return transform;
    }

    private void UpdateInfoText()
    {
        if (objectNameText != null)
        {
            string displayName = gameObject.name;
            if (displayName.EndsWith("(Clone)"))
            {
                displayName = displayName.Substring(0, displayName.Length - 7);
            }
            objectNameText.text = displayName;
        }
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        UpdateInfoText();
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