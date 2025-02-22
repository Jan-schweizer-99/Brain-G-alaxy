using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AxisGizmoGenerator : MonoBehaviour
{
    public bool showAxisGizmo = true;
    public bool showPrefab = true;  // Neue Option für Prefab-Sichtbarkeit
    
    public Color xAxisColor = Color.red;
    public Color yAxisColor = Color.green;
    public Color zAxisColor = Color.blue;

    public Vector3 axisGizmoOffset = Vector3.zero;
    public Vector3 axisGizmoRotation = Vector3.zero;

    public float xAxisLength = 1f;
    public float yAxisLength = 1f;
    public float zAxisLength = 1f;
    
    public float gridLineWidth;

    public GameObject positionPrefab;
    private GameObject instantiatedPrefab;

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
        public bool showPrefab = true;  // Neue Option für Child-Prefab-Sichtbarkeit
        public GameObject positionPrefab;
    }

    public List<ChildGizmoSettings> childGizmos = new List<ChildGizmoSettings>();
    private List<GameObject> instantiatedChildPrefabs = new List<GameObject>();

    public void UpdateGizmo()
    {
        CleanupExistingObjects();

        // Gizmos erstellen wenn aktiviert
        if (showAxisGizmo)
        {
            CreateAxisGizmo();
        }

        // Prefabs immer erstellen, unabhängig von showAxisGizmo
        if (showPrefab)
        {
            CreatePositionPrefab();
        }

        // Child-Gizmos und deren Prefabs erstellen
        CreateChildGizmos();
    }

    private void CleanupExistingObjects()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (instantiatedPrefab != null)
        {
            DestroyImmediate(instantiatedPrefab);
            instantiatedPrefab = null;
        }

        foreach (var childPrefab in instantiatedChildPrefabs)
        {
            if (childPrefab != null)
            {
                DestroyImmediate(childPrefab);
            }
        }
        instantiatedChildPrefabs.Clear();
    }

    private void CreatePositionPrefab()
    {
        if (positionPrefab != null)
        {
            instantiatedPrefab = Instantiate(positionPrefab, transform.position + axisGizmoOffset, Quaternion.Euler(axisGizmoRotation));
            instantiatedPrefab.transform.SetParent(transform);
            instantiatedPrefab.name = "Position_Prefab";
        }
    }

    void CreateChildGizmos()
    {
        foreach (var childSettings in childGizmos)
        {
            GameObject childGizmoObj = new GameObject(childSettings.name);
            childGizmoObj.transform.SetParent(transform, false);
            
            AxisGizmoGenerator childGizmo = childGizmoObj.AddComponent<AxisGizmoGenerator>();
            
            // Berechne den skalierten Offset des Childs basierend auf Parent-Achsenlängen
            Vector3 scaledChildOffset = new Vector3(
                childSettings.relativeOffset.x * xAxisLength,
                childSettings.relativeOffset.y * yAxisLength,
                childSettings.relativeOffset.z * zAxisLength
            );

            Quaternion parentRotation = Quaternion.Euler(axisGizmoRotation);
            Vector3 rotatedOffset = parentRotation * scaledChildOffset;
            
            // Setup Child Gizmo
            childGizmo.showAxisGizmo = childSettings.isVisible;
            childGizmo.showPrefab = childSettings.showPrefab;
            childGizmo.axisGizmoOffset = axisGizmoOffset + rotatedOffset;
            childGizmo.axisGizmoRotation = axisGizmoRotation + childSettings.rotation;
            childGizmo.xAxisLength = xAxisLength * childSettings.xAxisLength;
            childGizmo.yAxisLength = yAxisLength * childSettings.yAxisLength;
            childGizmo.zAxisLength = zAxisLength * childSettings.zAxisLength;
            childGizmo.xAxisColor = childSettings.xAxisColor;
            childGizmo.yAxisColor = childSettings.yAxisColor;
            childGizmo.zAxisColor = childSettings.zAxisColor;
            childGizmo.gridLineWidth = gridLineWidth;
            childGizmo.positionPrefab = childSettings.positionPrefab;

            // Erstelle Gizmo und Prefab unabhängig voneinander
            if (childSettings.isVisible)
            {
                childGizmo.UpdateGizmo();
            }

            // Prefab erstellen, unabhängig von der Gizmo-Sichtbarkeit
            if (childSettings.showPrefab && childSettings.positionPrefab != null)
            {
                Vector3 prefabPosition = transform.position + axisGizmoOffset + rotatedOffset;
                Quaternion prefabRotation = Quaternion.Euler(axisGizmoRotation + childSettings.rotation);
                
                GameObject childPrefab = Instantiate(childSettings.positionPrefab, prefabPosition, prefabRotation);
                childPrefab.transform.SetParent(childGizmoObj.transform);
                childPrefab.name = $"Position_Prefab_{childSettings.name}";
                instantiatedChildPrefabs.Add(childPrefab);
            }
        }
    }

    void CreateAxisGizmo()
    {
        GameObject axisContainer = new GameObject("AxisGizmo");
        axisContainer.transform.SetParent(transform, false);
        axisContainer.transform.localPosition = axisGizmoOffset;
        Quaternion baseRotation = Quaternion.Euler(axisGizmoRotation);

        Material xMaterial = new Material(Shader.Find("Unlit/Color")) { color = xAxisColor };
        Material yMaterial = new Material(Shader.Find("Unlit/Color")) { color = yAxisColor };
        Material zMaterial = new Material(Shader.Find("Unlit/Color")) { color = zAxisColor };

        CreateSingleAxis(axisContainer.transform, Vector3.right, 
            baseRotation * Quaternion.Euler(0, 0, -90), 
            xMaterial, "X", xAxisLength);
        
        CreateSingleAxis(axisContainer.transform, Vector3.up, 
            baseRotation * Quaternion.identity, 
            yMaterial, "Y", yAxisLength);
        
        CreateSingleAxis(axisContainer.transform, Vector3.forward, 
            baseRotation * Quaternion.Euler(90, 0, 0), 
            zMaterial, "Z", zAxisLength);
    }

    void CreateSingleAxis(Transform parent, Vector3 direction, Quaternion rotation, Material material, string axisName, float length)
    {
        float coneHeight = length * 0.2f;
        float cylinderLength = length - coneHeight;
        
        float cylinderRadius = gridLineWidth;
        float coneRadius = gridLineWidth * 1.5f;

        GameObject axis = new GameObject($"Axis_{axisName}");
        axis.transform.parent = parent;
        axis.transform.localPosition = Vector3.zero;
        axis.transform.localRotation = rotation;

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = $"Cylinder_{axisName}";
        cylinder.transform.parent = axis.transform;
        cylinder.transform.localScale = new Vector3(cylinderRadius * 2, cylinderLength / 2, cylinderRadius * 2);
        cylinder.transform.localPosition = Vector3.up * (cylinderLength / 2);
        cylinder.transform.localRotation = Quaternion.identity;
        cylinder.GetComponent<MeshRenderer>().material = material;

        GameObject cone = ConeCreator.CreateCone($"Cone_{axisName}");
        cone.transform.parent = axis.transform;
        cone.transform.localScale = new Vector3(coneRadius * 2, coneHeight, coneRadius * 2);
        cone.transform.localPosition = Vector3.up * (cylinderLength + coneHeight/2);
        cone.transform.localRotation = Quaternion.identity;
        cone.GetComponent<MeshRenderer>().material = material;
    }

    void OnDestroy()
    {
        CleanupExistingObjects();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AxisGizmoGenerator))]
public class AxisGizmoGeneratorEditor : Editor
{
    private Dictionary<int, bool> childGizmoFoldouts = new Dictionary<int, bool>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        AxisGizmoGenerator gizmo = (AxisGizmoGenerator)target;

        EditorGUI.BeginChangeCheck();
        
        // Haupteinstellungen
        bool newShowAxisGizmo = EditorGUILayout.Toggle("Show Axis Gizmo", gizmo.showAxisGizmo);
        bool newShowPrefab = EditorGUILayout.Toggle("Show Prefab", gizmo.showPrefab);
        
        Vector3 newAxisGizmoOffset = EditorGUILayout.Vector3Field("Axis Gizmo Offset", gizmo.axisGizmoOffset);
        Vector3 newAxisGizmoRotation = EditorGUILayout.Vector3Field("Axis Gizmo Rotation", gizmo.axisGizmoRotation);
        GameObject newPositionPrefab = (GameObject)EditorGUILayout.ObjectField("Position Prefab", gizmo.positionPrefab, typeof(GameObject), false);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Axis Gizmo Settings");
            gizmo.showAxisGizmo = newShowAxisGizmo;
            gizmo.showPrefab = newShowPrefab;
            gizmo.axisGizmoOffset = newAxisGizmoOffset;
            gizmo.axisGizmoRotation = newAxisGizmoRotation;
            gizmo.positionPrefab = newPositionPrefab;
            gizmo.UpdateGizmo();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axis Gizmo Colors", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        Color newXAxisColor = EditorGUILayout.ColorField("X-Axis Color", gizmo.xAxisColor);
        Color newYAxisColor = EditorGUILayout.ColorField("Y-Axis Color", gizmo.yAxisColor);
        Color newZAxisColor = EditorGUILayout.ColorField("Z-Axis Color", gizmo.zAxisColor);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Axis Colors");
            gizmo.xAxisColor = newXAxisColor;
            gizmo.yAxisColor = newYAxisColor;
            gizmo.zAxisColor = newZAxisColor;
            gizmo.UpdateGizmo();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axis Lengths", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float newXLength = EditorGUILayout.FloatField("X-Axis Length", gizmo.xAxisLength);
        float newYLength = EditorGUILayout.FloatField("Y-Axis Length", gizmo.yAxisLength);
        float newZLength = EditorGUILayout.FloatField("Z-Axis Length", gizmo.zAxisLength);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Axis Lengths");
            gizmo.xAxisLength = newXLength;
            gizmo.yAxisLength = newYLength;
            gizmo.zAxisLength = newZLength;
            gizmo.UpdateGizmo();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Child Gizmos", EditorStyles.boldLabel);

        for (int i = 0; i < gizmo.childGizmos.Count; i++)
        {
            if (!childGizmoFoldouts.ContainsKey(i))
            {
                childGizmoFoldouts[i] = false;
            }

            EditorGUILayout.BeginHorizontal();
            var childGizmo = gizmo.childGizmos[i];
            childGizmoFoldouts[i] = EditorGUILayout.Foldout(childGizmoFoldouts[i], childGizmo.name, true);
            
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                Undo.RecordObject(gizmo, "Removed Child Gizmo");
                gizmo.childGizmos.RemoveAt(i);
                childGizmoFoldouts.Remove(i);
                gizmo.UpdateGizmo();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (childGizmoFoldouts[i])
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                childGizmo.name = EditorGUILayout.TextField("Name", childGizmo.name);
                childGizmo.isVisible = EditorGUILayout.Toggle("Show Axis Gizmo", childGizmo.isVisible);
                childGizmo.showPrefab = EditorGUILayout.Toggle("Show Prefab", childGizmo.showPrefab);
                childGizmo.positionPrefab = (GameObject)EditorGUILayout.ObjectField("Position Prefab", childGizmo.positionPrefab, typeof(GameObject), false);
                childGizmo.relativeOffset = EditorGUILayout.Vector3Field("Relative Offset", childGizmo.relativeOffset);
                childGizmo.rotation = EditorGUILayout.Vector3Field("Rotation", childGizmo.rotation);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Axis Lengths", EditorStyles.boldLabel);
                childGizmo.xAxisLength = EditorGUILayout.FloatField("X-Axis Length", childGizmo.xAxisLength);
                childGizmo.yAxisLength = EditorGUILayout.FloatField("Y-Axis Length", childGizmo.yAxisLength);
                childGizmo.zAxisLength = EditorGUILayout.FloatField("Z-Axis Length", childGizmo.zAxisLength);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                childGizmo.xAxisColor = EditorGUILayout.ColorField("X-Axis Color", childGizmo.xAxisColor);
                childGizmo.yAxisColor = EditorGUILayout.ColorField("Y-Axis Color", childGizmo.yAxisColor);
                childGizmo.zAxisColor = EditorGUILayout.ColorField("Z-Axis Color", childGizmo.zAxisColor);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(gizmo, "Modified Child Gizmo Settings");
                    gizmo.UpdateGizmo();
                }
                EditorGUI.indentLevel--;
            }
        }

        if (GUILayout.Button("Add Child Gizmo"))
        {
            Undo.RecordObject(gizmo, "Added Child Gizmo");
            gizmo.childGizmos.Add(new AxisGizmoGenerator.ChildGizmoSettings());
            gizmo.UpdateGizmo();
        }

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Update Gizmo"))
        {
            gizmo.UpdateGizmo();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif