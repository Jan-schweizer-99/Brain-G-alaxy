// CoordinateSystemManager.cs

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CoordinateSystemManager : MonoBehaviour
{
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
    // Nur initialisieren, wenn nicht im Playmode
    if (!Application.isPlaying)
    {
        InitializeSystem();
        UpdateSystem();
    }
}

void InitializeSystem()
{
    // Lösche vorhandene Objekte
    foreach (Transform child in transform)
    {
        DestroyImmediate(child.gameObject);
    }

    // Liste leeren
    gizmoGenerators.Clear();
    gridGenerator = null;

    // Neu erstellen
    CreateGrid();
    
    foreach (var settings in gizmoSettings)
    {
        CreateGizmo(settings);
    }
}

void CreateGrid()
{
    // Stelle sicher, dass nur ein Grid erstellt wird
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
    // Stelle sicher, dass Gizmo-Objekte korrekt erstellt werden
    GameObject gizmoObj = new GameObject(settings.name);
    gizmoObj.transform.SetParent(transform, false);
    
    AxisGizmoGenerator gizmo = gizmoObj.AddComponent<AxisGizmoGenerator>();
    
    float spacing = gridWorldSize / gridSize;
    float gridLineWidth = gridMaterial != null ? gridMaterial.GetFloat("_LineWidth") : 0.1f;
    
    gizmo.showAxisGizmo = settings.isVisible;
    gizmo.axisGizmoOffset = new Vector3(
        settings.offset.x * spacing,
        settings.offset.y * spacing,
        settings.offset.z * spacing
    );
    gizmo.axisGizmoRotation = settings.rotation;
    gizmo.xAxisLength = settings.xAxisLength * spacing;
    gizmo.yAxisLength = settings.yAxisLength * spacing;
    gizmo.zAxisLength = settings.zAxisLength * spacing;
    gizmo.xAxisColor = settings.xAxisColor;
    gizmo.yAxisColor = settings.yAxisColor;
    gizmo.zAxisColor = settings.zAxisColor;
    gizmo.gridLineWidth = gridLineWidth;
    
    gizmoGenerators.Add(gizmo);
    gizmo.UpdateGizmo();
}

public void UpdateSystem()
{
    // Lösche Kinder rückwärts, um Probleme mit sich verändernden Indizes zu vermeiden
    for (int i = transform.childCount - 1; i >= 0; i--)
    {
        Transform child = transform.GetChild(i);
        
        // Sicherstellen, dass das Objekt wirklich zerstört wird
        if (child != null)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    // Listen komplett leeren
    gizmoGenerators.Clear();
    gridGenerator = null;

    // Nur wenn Gizmo-Einstellungen vorhanden sind
    CreateGrid();
    
    // Gizmos nur erstellen, wenn tatsächlich Einstellungen existieren
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

    public void RemoveGizmo(int index)
    {
        if (index >= 0 && index < gizmoSettings.Count)
        {
            gizmoSettings.RemoveAt(index);
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

// Update the existing CoordinateSystemManagerEditor class
#if UNITY_EDITOR
[CustomEditor(typeof(CoordinateSystemManager))]
public class CoordinateSystemManagerEditor : Editor
{
    private bool showGridSettings = true;
    private bool showGizmoList = true;
    
    // Create a dictionary to track the foldout state of each gizmo
    private Dictionary<int, bool> gizmoFoldouts = new Dictionary<int, bool>();

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
                // Ensure the dictionary has an entry for this gizmo
                if (!gizmoFoldouts.ContainsKey(i))
                {
                    gizmoFoldouts[i] = false;
                }

                EditorGUILayout.BeginHorizontal();
                
                var gizmo = manager.gizmoSettings[i];
                
                // Use the stored foldout state for this specific gizmo
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