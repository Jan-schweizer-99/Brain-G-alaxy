// AxisGizmoGenerator.cs

using UnityEngine;
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

    void Start()
    {
        if (showAxisGizmo)
        {
            //CreateAxisGizmo();
        }
    }

    public void UpdateGizmo()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (showAxisGizmo)
        {
            CreateAxisGizmo();
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
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        AxisGizmoGenerator gizmo = (AxisGizmoGenerator)target;

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
        // AxisGizmoGenerator.cs continuation:

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
        
        EditorGUILayout.LabelField("X-Axis", EditorStyles.boldLabel);
        float newXLength = EditorGUILayout.FloatField("Length", gizmo.xAxisLength);

        EditorGUILayout.LabelField("Y-Axis", EditorStyles.boldLabel);
        float newYLength = EditorGUILayout.FloatField("Length", gizmo.yAxisLength);

        EditorGUILayout.LabelField("Z-Axis", EditorStyles.boldLabel);
        float newZLength = EditorGUILayout.FloatField("Length", gizmo.zAxisLength);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gizmo, "Changed Axis Gizmo Lengths");
            gizmo.xAxisLength = newXLength;
            gizmo.yAxisLength = newYLength;
            gizmo.zAxisLength = newZLength;
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