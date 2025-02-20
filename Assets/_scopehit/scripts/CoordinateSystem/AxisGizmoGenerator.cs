using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AxisGizmoGenerator : MonoBehaviour
{
    public bool showAxisGizmo = true;
    
    public Color xAxisColor = Color.red;
    public Color yAxisColor = Color.green;
    public Color zAxisColor = Color.blue;

    public Vector3 axisGizmoOffset = Vector3.zero;
    public Vector3 axisGizmoRotation = Vector3.zero;

    public float xAxisLength = 1f;
    public float yAxisLength = 1f;
    public float zAxisLength = 1f;
    
    public float gridLineWidth;

    // New: List of child gizmos
    [System.Serializable]
    public class ChildGizmoSettings
    {
        public string name = "Child Gizmo";
        public Vector3 relativeOffset; // Offset relative to parent axis lengths
        public Vector3 rotation;
        public float xAxisLength = 1f;
        public float yAxisLength = 1f;
        public float zAxisLength = 1f;
        public Color xAxisColor = Color.red;
        public Color yAxisColor = Color.green;
        public Color zAxisColor = Color.blue;
        public bool isVisible = true;
    }

    public List<ChildGizmoSettings> childGizmos = new List<ChildGizmoSettings>();

    public void UpdateGizmo()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (showAxisGizmo)
        {
            CreateAxisGizmo();
            CreateChildGizmos();
        }
    }

void CreateChildGizmos()
{
    foreach (var childSettings in childGizmos)
    {
        if (childSettings.isVisible)
        {
            GameObject childGizmoObj = new GameObject(childSettings.name);
            childGizmoObj.transform.SetParent(transform, false);
            
            AxisGizmoGenerator childGizmo = childGizmoObj.AddComponent<AxisGizmoGenerator>();
            childGizmo.showAxisGizmo = true;
            
            // Berechne den skalierten Offset des Childs basierend auf Parent-Achsenlängen
            Vector3 scaledChildOffset = new Vector3(
                childSettings.relativeOffset.x * xAxisLength,
                childSettings.relativeOffset.y * yAxisLength,
                childSettings.relativeOffset.z * zAxisLength
            );

            // Rotiere den Offset um den Parent-Ursprung basierend auf der Parent-Rotation
            Quaternion parentRotation = Quaternion.Euler(axisGizmoRotation);
            Vector3 rotatedOffset = parentRotation * scaledChildOffset;
            
            // Setze die finale Position des Child-Gizmos
            childGizmo.axisGizmoOffset = axisGizmoOffset + rotatedOffset;
            
            // Kombiniere Parent- und Child-Rotation
            childGizmo.axisGizmoRotation = axisGizmoRotation + childSettings.rotation;
            
            // Übernehme die Parent-Achsenlängen als Basis und multipliziere mit Child-Skalierung
            childGizmo.xAxisLength = xAxisLength * childSettings.xAxisLength;
            childGizmo.yAxisLength = yAxisLength * childSettings.yAxisLength;
            childGizmo.zAxisLength = zAxisLength * childSettings.zAxisLength;
            
            // Übernehme die Farben vom Child
            childGizmo.xAxisColor = childSettings.xAxisColor;
            childGizmo.yAxisColor = childSettings.yAxisColor;
            childGizmo.zAxisColor = childSettings.zAxisColor;
            
            childGizmo.gridLineWidth = gridLineWidth;
            
            childGizmo.UpdateGizmo();
        }
    }
}

    // Rest of the existing methods remain the same
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
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
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

        // Main gizmo settings
        EditorGUI.BeginChangeCheck();
        bool newShowAxisGizmo = EditorGUILayout.Toggle("Show Axis Gizmo", gizmo.showAxisGizmo);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Show Axis Gizmo");
            gizmo.showAxisGizmo = newShowAxisGizmo;
            gizmo.UpdateGizmo();
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newAxisGizmoOffset = EditorGUILayout.Vector3Field("Axis Gizmo Offset", gizmo.axisGizmoOffset);
        Vector3 newAxisGizmoRotation = EditorGUILayout.Vector3Field("Axis Gizmo Rotation", gizmo.axisGizmoRotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Axis Gizmo Offset/Rotation");
            gizmo.axisGizmoOffset = newAxisGizmoOffset;
            gizmo.axisGizmoRotation = newAxisGizmoRotation;
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
            Undo.RecordObject(gizmo, "Changed Axis Gizmo Colors");
            gizmo.xAxisColor = newXAxisColor;
            gizmo.yAxisColor = newYAxisColor;
            gizmo.zAxisColor = newZAxisColor;
            gizmo.UpdateGizmo();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axis Gizmo Lengths", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float newXLength = EditorGUILayout.FloatField("X-Axis Length", gizmo.xAxisLength);
        float newYLength = EditorGUILayout.FloatField("Y-Axis Length", gizmo.yAxisLength);
        float newZLength = EditorGUILayout.FloatField("Z-Axis Length", gizmo.zAxisLength);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Axis Gizmo Lengths");
            gizmo.xAxisLength = newXLength;
            gizmo.yAxisLength = newYLength;
            gizmo.zAxisLength = newZLength;
            gizmo.UpdateGizmo();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Child Gizmos", EditorStyles.boldLabel);

        // Child gizmos list
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
                childGizmo.isVisible = EditorGUILayout.Toggle("Visible", childGizmo.isVisible);
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