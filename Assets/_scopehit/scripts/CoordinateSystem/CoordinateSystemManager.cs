using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CoordinateSystemManager : MonoBehaviour
{
    [System.Serializable]
    public class ChildGizmoSettings
    {
        public string name = "Child Gizmo";
        public Vector3 relativeOffset;
        public Vector3 rotation;
        public float xAxisLength = 1f;
        public float yAxisLength = 1f;
        public float zAxisLength = 1f;
        public Color xAxisColor = Color.red;
        public Color yAxisColor = Color.green;
        public Color zAxisColor = Color.blue;
        public bool isVisible = true;
        public bool showPrefab = true;  // Neue Option für Prefab-Sichtbarkeit
        public GameObject positionPrefab;
    }

    [System.Serializable]
    public class GizmoSettings
    {
        public string name = "Gizmo";
        public Vector3 offset;
        public Vector3 rotation;
        public float xAxisLength = 1f;
        public float yAxisLength = 1f;
        public float zAxisLength = 1f;
        public Color xAxisColor = Color.red;
        public Color yAxisColor = Color.green;
        public Color zAxisColor = Color.blue;
        public bool isVisible = true;
        public bool showPrefab = true;  // Neue Option für Prefab-Sichtbarkeit
        public GameObject positionPrefab;
        public List<ChildGizmoSettings> childGizmos = new List<ChildGizmoSettings>();
    }

    // Grid Settings
    public int gridSize = 10;
    public float gridWorldSize = 10f;
    public Material gridMaterial;
    public Color wireframeColor = Color.white;
    
    // List of Gizmo Settings
    public List<GizmoSettings> gizmoSettings = new List<GizmoSettings>();
    
    private PlaneGridGenerator gridGenerator;
    private List<AxisGizmoGenerator> gizmoGenerators = new List<AxisGizmoGenerator>();
    private List<GameObject> instantiatedPrefabs = new List<GameObject>();

    void Start()
    {
        if (!Application.isPlaying)
        {
            InitializeSystem();
            UpdateSystem();
        }
    }

    void InitializeSystem()
    {
        CleanupSystem();
        CreateGrid();
        
        foreach (var settings in gizmoSettings)
        {
            CreateGizmo(settings);
        }
    }

    void CleanupSystem()
    {
        // Cleanup all children
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        // Cleanup prefabs
        foreach (var prefab in instantiatedPrefabs)
        {
            if (prefab != null)
            {
                DestroyImmediate(prefab);
            }
        }
        instantiatedPrefabs.Clear();

        gizmoGenerators.Clear();
        gridGenerator = null;
    }

    void CreateGrid()
    {
        if (gridGenerator == null)
        {
            GameObject gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(transform, false);
            gridGenerator = gridObj.AddComponent<PlaneGridGenerator>();

            gridGenerator.gridSize = gridSize;
            gridGenerator.gridWorldSize = gridWorldSize;
            gridGenerator.gridMaterial = gridMaterial;
            gridGenerator.UpdateGrid(wireframeColor);
        }
    }

    void CreateGizmo(GizmoSettings settings)
    {
        GameObject gizmoObj = new GameObject(settings.name);
        gizmoObj.transform.SetParent(transform, false);
        
        AxisGizmoGenerator gizmo = gizmoObj.AddComponent<AxisGizmoGenerator>();
        
        float spacing = gridWorldSize / gridSize;
        float gridLineWidth = gridMaterial != null ? gridMaterial.GetFloat("_LineWidth") : 0.1f;
        
        // Calculate parent position
        Vector3 parentPosition = new Vector3(
            settings.offset.x * spacing,
            settings.offset.y * spacing,
            settings.offset.z * spacing
        );
        
        // Setup parent gizmo with new prefab visibility
        gizmo.showAxisGizmo = settings.isVisible;
        gizmo.showPrefab = settings.showPrefab;
        gizmo.axisGizmoOffset = parentPosition;
        gizmo.axisGizmoRotation = settings.rotation;
        gizmo.xAxisLength = settings.xAxisLength * spacing;
        gizmo.yAxisLength = settings.yAxisLength * spacing;
        gizmo.zAxisLength = settings.zAxisLength * spacing;
        gizmo.xAxisColor = settings.xAxisColor;
        gizmo.yAxisColor = settings.yAxisColor;
        gizmo.zAxisColor = settings.zAxisColor;
        gizmo.gridLineWidth = gridLineWidth;
        gizmo.positionPrefab = settings.positionPrefab;

        // Create child gizmos
        foreach (var childSettings in settings.childGizmos)
        {
            var childGizmoSettings = new AxisGizmoGenerator.ChildGizmoSettings
            {
                name = childSettings.name,
                relativeOffset = childSettings.relativeOffset,
                rotation = childSettings.rotation,
                xAxisLength = childSettings.xAxisLength,
                yAxisLength = childSettings.yAxisLength,
                zAxisLength = childSettings.zAxisLength,
                xAxisColor = childSettings.xAxisColor,
                yAxisColor = childSettings.yAxisColor,
                zAxisColor = childSettings.zAxisColor,
                isVisible = childSettings.isVisible,
                showPrefab = childSettings.showPrefab,  // Neue Prefab-Sichtbarkeit
                positionPrefab = childSettings.positionPrefab
            };
            gizmo.childGizmos.Add(childGizmoSettings);
        }
        
        gizmoGenerators.Add(gizmo);
        gizmo.UpdateGizmo();

        // If there's a prefab for the parent gizmo, instantiate it if showPrefab is true
        if (settings.showPrefab && settings.positionPrefab != null)
        {
            GameObject prefabInstance = Instantiate(settings.positionPrefab, 
                transform.position + parentPosition, 
                Quaternion.Euler(settings.rotation));
            prefabInstance.transform.SetParent(gizmoObj.transform);
            prefabInstance.name = $"Position_Prefab_{settings.name}";
            instantiatedPrefabs.Add(prefabInstance);
        }
    }

    public void UpdateSystem()
    {
        CleanupSystem();
        CreateGrid();
        
        if (gizmoSettings != null && gizmoSettings.Count > 0)
        {
            foreach (var settings in gizmoSettings)
            {
                CreateGizmo(settings);
            }
        }
    }

    public void AddGizmo()
    {
        gizmoSettings.Add(new GizmoSettings());
        UpdateSystem();
    }

    public void AddChildGizmo(int parentIndex)
    {
        if (parentIndex >= 0 && parentIndex < gizmoSettings.Count)
        {
            gizmoSettings[parentIndex].childGizmos.Add(new ChildGizmoSettings());
            UpdateSystem();
        }
    }

    public void RemoveGizmo(int index)
    {
        if (index >= 0 && index < gizmoSettings.Count)
        {
            gizmoSettings.RemoveAt(index);
            UpdateSystem();
        }
    }

    public void RemoveChildGizmo(int parentIndex, int childIndex)
    {
        if (parentIndex >= 0 && parentIndex < gizmoSettings.Count &&
            childIndex >= 0 && childIndex < gizmoSettings[parentIndex].childGizmos.Count)
        {
            gizmoSettings[parentIndex].childGizmos.RemoveAt(childIndex);
            UpdateSystem();
        }
    }

    void OnDestroy()
    {
        CleanupSystem();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CoordinateSystemManager))]
public class CoordinateSystemManagerEditor : Editor
{
    private bool showGridSettings = true;
    private bool showGizmoList = true;
    private Dictionary<int, bool> gizmoFoldouts = new Dictionary<int, bool>();
    private Dictionary<(int, int), bool> childGizmoFoldouts = new Dictionary<(int, int), bool>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        CoordinateSystemManager manager = (CoordinateSystemManager)target;

        // Grid Settings
        showGridSettings = EditorGUILayout.Foldout(showGridSettings, "Grid Settings", true);
        if (showGridSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUI.BeginChangeCheck();
            int newGridSize = EditorGUILayout.IntField("Grid Lines", manager.gridSize);
            float newGridWorldSize = EditorGUILayout.FloatField("Grid World Size", manager.gridWorldSize);
            Material newGridMaterial = (Material)EditorGUILayout.ObjectField("Grid Material", manager.gridMaterial, typeof(Material), false);
            Color newWireframeColor = EditorGUILayout.ColorField("Wireframe Color", manager.wireframeColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Modified Grid Settings");
                manager.gridSize = newGridSize;
                manager.gridWorldSize = newGridWorldSize;
                manager.gridMaterial = newGridMaterial;
                manager.wireframeColor = newWireframeColor;
                manager.UpdateSystem();
            }
            
            EditorGUI.indentLevel--;
        }

        // Gizmo List
        EditorGUILayout.Space();
        showGizmoList = EditorGUILayout.Foldout(showGizmoList, "Gizmo List", true);
        if (showGizmoList)
        {
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < manager.gizmoSettings.Count; i++)
            {
                if (!gizmoFoldouts.ContainsKey(i))
                {
                    gizmoFoldouts[i] = false;
                }

                EditorGUILayout.BeginHorizontal();
                var gizmo = manager.gizmoSettings[i];
                gizmoFoldouts[i] = EditorGUILayout.Foldout(gizmoFoldouts[i], gizmo.name, true);
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    manager.RemoveGizmo(i);
                    gizmoFoldouts.Remove(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();

                if (gizmoFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    
                    // Main gizmo settings
                    gizmo.name = EditorGUILayout.TextField("Name", gizmo.name);
                    gizmo.isVisible = EditorGUILayout.Toggle("Show Axis Gizmo", gizmo.isVisible);
                    gizmo.showPrefab = EditorGUILayout.Toggle("Show Prefab", gizmo.showPrefab);  // Neue Option
                    gizmo.positionPrefab = (GameObject)EditorGUILayout.ObjectField(
                        "Position Prefab", 
                        gizmo.positionPrefab, 
                        typeof(GameObject), 
                        false);
                    gizmo.offset = EditorGUILayout.Vector3Field("Offset", gizmo.offset);
                    gizmo.rotation = EditorGUILayout.Vector3Field("Rotation", gizmo.rotation);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Axis Lengths", EditorStyles.boldLabel);
                    gizmo.xAxisLength = EditorGUILayout.FloatField("X Axis Length", gizmo.xAxisLength);
                    gizmo.yAxisLength = EditorGUILayout.FloatField("Y Axis Length", gizmo.yAxisLength);
                    gizmo.zAxisLength = EditorGUILayout.FloatField("Z Axis Length", gizmo.zAxisLength);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                    gizmo.xAxisColor = EditorGUILayout.ColorField("X Axis Color", gizmo.xAxisColor);
                    gizmo.yAxisColor = EditorGUILayout.ColorField("Y Axis Color", gizmo.yAxisColor);
                    gizmo.zAxisColor = EditorGUILayout.ColorField("Z Axis Color", gizmo.zAxisColor);

                    // Child Gizmos Section
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Child Gizmos", EditorStyles.boldLabel);

                    for (int j = 0; j < gizmo.childGizmos.Count; j++)
                    {
                        var key = (i, j);
                        if (!childGizmoFoldouts.ContainsKey(key))
                        {
                            childGizmoFoldouts[key] = false;
                        }

                        EditorGUILayout.BeginHorizontal();
                        var childGizmo = gizmo.childGizmos[j];
                        childGizmoFoldouts[key] = EditorGUILayout.Foldout(childGizmoFoldouts[key], childGizmo.name, true);

                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            Undo.RecordObject(manager, "Remove Child Gizmo");
                            manager.RemoveChildGizmo(i, j);
                            childGizmoFoldouts.Remove(key);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();

                        if (childGizmoFoldouts[key])
                        {
                            EditorGUI.indentLevel++;
                            EditorGUI.BeginChangeCheck();

                            // Child gizmo settings
                            childGizmo.name = EditorGUILayout.TextField("Name", childGizmo.name);
                            childGizmo.isVisible = EditorGUILayout.Toggle("Show Axis Gizmo", childGizmo.isVisible);
                            childGizmo.showPrefab = EditorGUILayout.Toggle("Show Prefab", childGizmo.showPrefab);  // Neue Option
                            childGizmo.positionPrefab = (GameObject)EditorGUILayout.ObjectField(
                                "Position Prefab", 
                                childGizmo.positionPrefab, 
                                typeof(GameObject), 
                                false);
                            childGizmo.relativeOffset = EditorGUILayout.Vector3Field("Relative Offset", childGizmo.relativeOffset);
                            childGizmo.rotation = EditorGUILayout.Vector3Field("Rotation", childGizmo.rotation);

                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Axis Lengths", EditorStyles.boldLabel);
                            childGizmo.xAxisLength = EditorGUILayout.FloatField("X Axis Length", childGizmo.xAxisLength);
                            childGizmo.yAxisLength = EditorGUILayout.FloatField("Y Axis Length", childGizmo.yAxisLength);
                            childGizmo.zAxisLength = EditorGUILayout.FloatField("Z Axis Length", childGizmo.zAxisLength);

                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                            childGizmo.xAxisColor = EditorGUILayout.ColorField("X Axis Color", childGizmo.xAxisColor);
                            childGizmo.yAxisColor = EditorGUILayout.ColorField("Y Axis Color", childGizmo.yAxisColor);
                            childGizmo.zAxisColor = EditorGUILayout.ColorField("Z Axis Color", childGizmo.zAxisColor);

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(manager, "Modified Child Gizmo Settings");
                                manager.UpdateSystem();
                            }

                            EditorGUI.indentLevel--;
                        }
                    }

                    if (GUILayout.Button("Add Child Gizmo"))
                    {
                        Undo.RecordObject(manager, "Add Child Gizmo");
                        manager.AddChildGizmo(i);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(manager, "Modified Gizmo Settings");
                        manager.UpdateSystem();
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            if (GUILayout.Button("Add New Gizmo"))
            {
                manager.AddGizmo();
            }
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Update Coordinate System"))
        {
            manager.UpdateSystem();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif