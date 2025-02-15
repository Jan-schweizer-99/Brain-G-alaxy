using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[ExecuteInEditMode]
public class EditorCameraTrigger : MonoBehaviour
{
    public GameObject prefabToSpawn;
    private bool isInsideCollider = false;
    private GameObject spawnedInstance;

    [Header("Gizmo Settings")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);  // Halbtransparentes Grün
    public Color gizmoWireColor = Color.green;           // Grüner Rahmen

    [Header("Editor Camera Info")]
    [SerializeField, ReadOnly] private Vector3 editorCameraPosition;
    [SerializeField, ReadOnly] private bool isCameraInTrigger;
    [SerializeField, ReadOnly] private string debugStatus;

    private void OnEnable()
    {
        #if UNITY_EDITOR
        EditorApplication.update += OnEditorUpdate;
        SceneView.duringSceneGui += OnSceneGUI;
        #endif
    }

    private void OnDisable()
    {
        #if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroySpawnedInstance();
        #endif
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Color originalColor = Gizmos.color;

            // Zeichnen des gefüllten Colliders
            Gizmos.color = gizmoColor;
            if (col is BoxCollider)
            {
                BoxCollider boxCol = (BoxCollider)col;
                Matrix4x4 originalMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(boxCol.center, boxCol.size);
                // Zeichnen des Drahtgitter-Rahmens
                Gizmos.color = gizmoWireColor;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
                Gizmos.matrix = originalMatrix;
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphereCol = (SphereCollider)col;
                float radius = sphereCol.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
                Gizmos.DrawSphere(transform.TransformPoint(sphereCol.center), radius);
                // Zeichnen des Drahtgitter-Rahmens
                Gizmos.color = gizmoWireColor;
                Gizmos.DrawWireSphere(transform.TransformPoint(sphereCol.center), radius);
            }
            Gizmos.color = originalColor;
        }
    }

    #if UNITY_EDITOR
    private void OnSceneGUI(SceneView sceneView)
    {
        if (Event.current.type == EventType.Layout)
        {
            OnEditorUpdate();
        }
    }
    #endif

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying && SceneView.lastActiveSceneView != null)
        {
            editorCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;
            
            bool isCurrentlyInside = GetComponent<Collider>().bounds.Contains(editorCameraPosition);
            isCameraInTrigger = isCurrentlyInside;
            
            debugStatus = $"Inside: {isCurrentlyInside}, Previous: {isInsideCollider}, HasPrefab: {prefabToSpawn != null}, Instance: {spawnedInstance != null}";
            
            if (isCurrentlyInside && !isInsideCollider && prefabToSpawn != null && spawnedInstance == null)
            {
                CreatePrefabInstance();
            }
            else if (!isCurrentlyInside && isInsideCollider)
            {
                DestroySpawnedInstance();
            }
            
            isInsideCollider = isCurrentlyInside;

            if (Selection.activeGameObject == gameObject)
            {
                EditorUtility.SetDirty(this);
                SceneView.RepaintAll();
            }
        }
    }

    private void CreatePrefabInstance()
    {
        #if UNITY_EDITOR
        try
        {
            if (prefabToSpawn == null)
            {
                Debug.LogError("Kein Prefab ausgewählt!");
                return;
            }

            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabToSpawn);
            if (prefabAsset == null)
            {
                prefabAsset = prefabToSpawn;
            }

            spawnedInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            if (spawnedInstance != null)
            {
                spawnedInstance.transform.SetParent(transform, false);
                spawnedInstance.transform.position = transform.position;
                spawnedInstance.transform.rotation = transform.rotation;

                Debug.Log("Prefab erfolgreich instanziiert: " + spawnedInstance.name);
                
                Undo.RegisterCreatedObjectUndo(spawnedInstance, "Spawn Prefab in Editor");
                
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            else
            {
                Debug.LogError("Prefab konnte nicht instanziiert werden!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fehler beim Instanziieren des Prefabs: {e.Message}\nStacktrace: {e.StackTrace}");
        }
        #endif
    }

    private void DestroySpawnedInstance()
    {
        #if UNITY_EDITOR
        if (spawnedInstance != null)
        {
            Undo.DestroyObjectImmediate(spawnedInstance);
            spawnedInstance = null;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Prefab Instance destroyed");
        }
        #endif
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (prefabToSpawn != null)
        {
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(prefabToSpawn);
            if (prefabType == PrefabAssetType.NotAPrefab)
            {
                Debug.LogWarning("Warnung: Das zugewiesene GameObject sollte ein Prefab sein!");
            }
        }
        #endif
    }
}

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