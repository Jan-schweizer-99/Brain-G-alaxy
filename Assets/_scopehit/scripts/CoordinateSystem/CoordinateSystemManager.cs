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
        // New: List of child gizmos
        public List<ChildGizmoSettings> childGizmos = new List<ChildGizmoSettings>();
    }

    // Grid Settings
    public int gridSize = 10;
    public float gridWorldSize = 10f;
    public Material gridMaterial;
    
    // List of Gizmo Settings
    public List<GizmoSettings> gizmoSettings = new List<GizmoSettings>();
    
    private PlaneGridGenerator gridGenerator;
    private List<AxisGizmoGenerator> gizmoGenerators = new List<AxisGizmoGenerator>();
    
    // Wireframe Color
    public Color wireframeColor = Color.white;

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
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        gizmoGenerators.Clear();
        gridGenerator = null;

        CreateGrid();
        
        foreach (var settings in gizmoSettings)
        {
            CreateGizmo(settings);
        }
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

    void ClearGizmos()
    {
        foreach (var gizmo in gizmoGenerators)
        {
            if (gizmo != null)
            {
                DestroyImmediate(gizmo.gameObject);
            }
        }
        gizmoGenerators.Clear();
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
    
    gizmo.showAxisGizmo = settings.isVisible;
    gizmo.axisGizmoOffset = parentPosition;
    gizmo.axisGizmoRotation = settings.rotation;
    gizmo.xAxisLength = settings.xAxisLength * spacing;
    gizmo.yAxisLength = settings.yAxisLength * spacing;
    gizmo.zAxisLength = settings.zAxisLength * spacing;
    gizmo.xAxisColor = settings.xAxisColor;
    gizmo.yAxisColor = settings.yAxisColor;
    gizmo.zAxisColor = settings.zAxisColor;
    gizmo.gridLineWidth = gridLineWidth;

    // Add child gizmos with corrected positioning
    foreach (var childSettings in settings.childGizmos)
    {
        var childGizmoSettings = new AxisGizmoGenerator.ChildGizmoSettings
        {
            name = childSettings.name,
            // Set relative offset directly - the parent position is already handled by the parent gizmo
            relativeOffset = childSettings.relativeOffset,
            rotation = childSettings.rotation, // Child rotation is local to parent
            xAxisLength = childSettings.xAxisLength,
            yAxisLength = childSettings.yAxisLength,
            zAxisLength = childSettings.zAxisLength,
            xAxisColor = childSettings.xAxisColor,
            yAxisColor = childSettings.yAxisColor,
            zAxisColor = childSettings.zAxisColor,
            isVisible = childSettings.isVisible
        };
        gizmo.childGizmos.Add(childGizmoSettings);
    }
    
    gizmoGenerators.Add(gizmo);
    gizmo.UpdateGizmo();
}

    public void UpdateSystem()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        gizmoGenerators.Clear();
        gridGenerator = null;

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
        if (gridGenerator != null)
        {
            DestroyImmediate(gridGenerator.gameObject);
        }
        ClearGizmos();
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
                    
                    gizmo.name = EditorGUILayout.TextField("Name", gizmo.name);
                    gizmo.isVisible = EditorGUILayout.Toggle("Visible", gizmo.isVisible);
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

                            childGizmo.name = EditorGUILayout.TextField("Name", childGizmo.name);
                            childGizmo.isVisible = EditorGUILayout.Toggle("Visible", childGizmo.isVisible);
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