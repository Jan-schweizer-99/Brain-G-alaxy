/*
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlaneGridGenerator : MonoBehaviour
{
    public Material gridMaterial;
    public int gridSize = 10;
    public float gridWorldSize = 10f; // Total world space size of the grid
    public bool showAxisGizmo = true;
    
    // Gizmo-Farben für die Achsen
    public Color xAxisColor = Color.red;
    public Color yAxisColor = Color.green;
    public Color zAxisColor = Color.blue;

    // Offset und Rotation für Axis Gizmo
    public Vector3Int axisGizmoOffset = Vector3Int.zero;
    public Vector3 axisGizmoRotation = Vector3.zero;

    // Neue Felder für Skalierung jeder Achse
    public float xAxisLength = 1f;
    public float yAxisLength = 1f;
    public float zAxisLength = 1f;
    
    // Neue Eigenschaft für die Gizmo-Breite
    public float gizmoBaseWidth = 0.002f;
    
    [SerializeField]
    private string shaderName = "Custom/WireframeGrid";

    // Calculated spacing will be dynamically set based on gridSize and gridWorldSize
    private float spacing;

    void Start()
    {
        if (gridMaterial == null)
        {
            CreateGridMaterial();
        }
        CalculateSpacing();
        GenerateGrid();
        if (showAxisGizmo)
        {
            CreateAxisGizmo();
        }
    }

    void CalculateSpacing()
    {
        // Calculate spacing to distribute grid lines evenly across the total world size
        spacing = gridWorldSize / gridSize;
    }

    void CreateGridMaterial()
    {
        Shader gridShader = Shader.Find(shaderName);
        if (gridShader == null)
        {
            Debug.LogError($"Shader {shaderName} nicht gefunden!");
            return;
        }
        
        // Speichere die aktuelle Farbe, falls das Material bereits existiert
        Color currentColor = gridMaterial != null ? gridMaterial.GetColor("_GridColor") : new Color(1, 1, 1, 1f);
        
        // Erstelle neues Material oder update existierendes
        if (gridMaterial == null)
        {
            gridMaterial = new Material(gridShader);
        }
        
        gridMaterial.SetFloat("_GridSize", spacing);
        gridMaterial.SetColor("_GridColor", currentColor);
        // Setze die Linienbreite basierend auf der Gizmo-Breite
        gridMaterial.SetFloat("_LineWidth", gizmoBaseWidth * 0.5f);
    }

    public void UpdateCoordinateSystem()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        CalculateSpacing();
        CreateGridMaterial();
        GenerateGrid();
        if (showAxisGizmo)
        {
            CreateAxisGizmo();
        }
    }

    void GenerateGrid()
    {
        if (gridMaterial == null) return;

        GameObject gridContainer = new GameObject("Grid");
        gridContainer.transform.parent = transform;
        gridContainer.transform.localPosition = Vector3.zero;
        gridContainer.transform.localRotation = Quaternion.identity;
        
        // Erstelle XZ-Ebenen (horizontal)
        for (int y = -gridSize/2; y <= gridSize/2; y++)
        {
            GameObject horizontal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            horizontal.name = $"HorizontalPlane_Y{y}";
            horizontal.transform.parent = gridContainer.transform;
            horizontal.transform.localScale = new Vector3(gridWorldSize, gridWorldSize, 1);
            horizontal.transform.localPosition = new Vector3(0, y * spacing, 0);
            horizontal.transform.localRotation = Quaternion.Euler(90, 0, 0);
            horizontal.GetComponent<MeshRenderer>().material = gridMaterial;
        }

        // Erstelle XY-Ebenen (vertikal, Z-Achse)
        for (int z = -gridSize/2; z <= gridSize/2; z++)
        {
            GameObject verticalZ = GameObject.CreatePrimitive(PrimitiveType.Quad);
            verticalZ.name = $"VerticalPlane_Z{z}";
            verticalZ.transform.parent = gridContainer.transform;
            verticalZ.transform.localScale = new Vector3(gridWorldSize, gridWorldSize, 1);
            verticalZ.transform.localPosition = new Vector3(0, 0, z * spacing);
            verticalZ.transform.localRotation = Quaternion.Euler(0, 0, 0);
            verticalZ.GetComponent<MeshRenderer>().material = gridMaterial;
        }

        // Erstelle YZ-Ebenen (vertikal, X-Achse)
        for (int x = -gridSize/2; x <= gridSize/2; x++)
        {
            GameObject verticalX = GameObject.CreatePrimitive(PrimitiveType.Quad);
            verticalX.name = $"VerticalPlane_X{x}";
            verticalX.transform.parent = gridContainer.transform;
            verticalX.transform.localScale = new Vector3(gridWorldSize, gridWorldSize, 1);
            verticalX.transform.localPosition = new Vector3(x * spacing, 0, 0);
            verticalX.transform.localRotation = Quaternion.Euler(0, 90, 0);
            verticalX.GetComponent<MeshRenderer>().material = gridMaterial;
        }
    }

    void CreateAxisGizmo()
    {
        GameObject axisContainer = new GameObject("AxisGizmo");
        axisContainer.transform.parent = transform;
        axisContainer.transform.localPosition = new Vector3(
            axisGizmoOffset.x * spacing, 
            axisGizmoOffset.y * spacing, 
            axisGizmoOffset.z * spacing
        );
        axisContainer.transform.localRotation = Quaternion.Euler(axisGizmoRotation);

        // Verwende Unlit/Color Shader für direkte Farbgebung
        Material xMaterial = new Material(Shader.Find("Unlit/Color")) { color = xAxisColor };
        Material yMaterial = new Material(Shader.Find("Unlit/Color")) { color = yAxisColor };
        Material zMaterial = new Material(Shader.Find("Unlit/Color")) { color = zAxisColor };

        CreateAxisSet(axisContainer.transform, Vector3.right, 
            Quaternion.Euler(0, 0, -90), 
            xMaterial, "X", 
            xAxisLength);
        
        CreateAxisSet(axisContainer.transform, Vector3.up, 
            Quaternion.identity, 
            yMaterial, "Y", 
            yAxisLength);
        
        CreateAxisSet(axisContainer.transform, Vector3.forward, 
            Quaternion.Euler(90, 0, 0), 
            zMaterial, "Z", 
            zAxisLength);
    }

    void CreateAxisSet(Transform parent, Vector3 direction, Quaternion baseRotation, Material material, string axisName, float lengthMultiplier)
    {
        // Berechne die Länge einer Gridlinie
        float gridLineLength = gridWorldSize / gridSize;

        // Basis-Länge ist die Länge einer Gridlinie multipliziert mit dem Längenfaktor
        float baseLength = gridLineLength * lengthMultiplier;
        float coneHeight = baseLength * 0.2f;
        float cylinderLength = baseLength - coneHeight;
        
        // Verwende die Basis-Gizmo-Breite direkt
        float cylinderRadius = gizmoBaseWidth;
        float coneRadius = gizmoBaseWidth * 1.5f; // Kegel etwas breiter als der Zylinder

        GameObject axisContainer = new GameObject($"Axis_{axisName}");
        axisContainer.transform.parent = parent;
        axisContainer.transform.localPosition = Vector3.zero;
        axisContainer.transform.localRotation = Quaternion.identity;

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = $"Cylinder_{axisName}";
        cylinder.transform.parent = axisContainer.transform;
        cylinder.transform.localScale = new Vector3(cylinderRadius * 2, cylinderLength / 2, cylinderRadius * 2);
        cylinder.transform.localPosition = direction * (cylinderLength / 2);
        cylinder.transform.localRotation = baseRotation;
        cylinder.GetComponent<MeshRenderer>().material = material;

        GameObject cone = ConeCreator.CreateCone($"Cone_{axisName}");
        cone.transform.parent = axisContainer.transform;
        cone.transform.localScale = new Vector3(coneRadius * 2, coneHeight, coneRadius * 2);
        cone.transform.localPosition = direction * (cylinderLength + coneHeight/2);
        cone.transform.localRotation = baseRotation;
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
[CustomEditor(typeof(PlaneGridGenerator))]
public class PlaneGridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        PlaneGridGenerator grid = (PlaneGridGenerator)target;
        
        // Grid Size Field
        EditorGUI.BeginChangeCheck();
        int newGridSize = EditorGUILayout.IntField("Grid Lines", grid.gridSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Grid Lines");
            grid.gridSize = newGridSize;
            grid.UpdateCoordinateSystem();
        }

        // Grid World Size Field
        EditorGUI.BeginChangeCheck();
        float newGridWorldSize = EditorGUILayout.FloatField("Grid World Size", grid.gridWorldSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Grid World Size");
            grid.gridWorldSize = newGridWorldSize;
            grid.UpdateCoordinateSystem();
        }

        // Show Axis Gizmo Toggle
        EditorGUI.BeginChangeCheck();
        bool newShowAxisGizmo = EditorGUILayout.Toggle("Show Axis Gizmo", grid.showAxisGizmo);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Show Axis Gizmo");
            grid.showAxisGizmo = newShowAxisGizmo;
            grid.UpdateCoordinateSystem();
        }

        // Axis Gizmo Offset and Rotation Fields
        EditorGUI.BeginChangeCheck();
        Vector3Int newAxisGizmoOffset = EditorGUILayout.Vector3IntField("Axis Gizmo Offset", grid.axisGizmoOffset);
        Vector3 newAxisGizmoRotation = EditorGUILayout.Vector3Field("Axis Gizmo Rotation", grid.axisGizmoRotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Axis Gizmo Offset/Rotation");
            grid.axisGizmoOffset = newAxisGizmoOffset;
            grid.axisGizmoRotation = newAxisGizmoRotation;
            grid.UpdateCoordinateSystem();
        }

        // Gizmo-Breite Einstellung
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gizmo Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        float newGizmoWidth = EditorGUILayout.Slider("Gizmo Width", grid.gizmoBaseWidth, 0.0001f, 0.01f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Gizmo Width");
            grid.gizmoBaseWidth = newGizmoWidth;
            grid.UpdateCoordinateSystem();
        }

        // Achsenfarben-Felder hinzufügen
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axis Gizmo Colors", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        Color newXAxisColor = EditorGUILayout.ColorField("X-Axis Color", grid.xAxisColor);
        Color newYAxisColor = EditorGUILayout.ColorField("Y-Axis Color", grid.yAxisColor);
        Color newZAxisColor = EditorGUILayout.ColorField("Z-Axis Color", grid.zAxisColor);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Axis Gizmo Colors");
            grid.xAxisColor = newXAxisColor;
            grid.yAxisColor = newYAxisColor;
            grid.zAxisColor = newZAxisColor;
            grid.UpdateCoordinateSystem();
        }

        // Axis Length Felder
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axis Gizmo Lengths", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        // X-Achse
 // X-Achse
        EditorGUILayout.LabelField("X-Axis", EditorStyles.boldLabel);
        float newXLength = EditorGUILayout.FloatField("Length", grid.xAxisLength);

        // Y-Achse
        EditorGUILayout.LabelField("Y-Axis", EditorStyles.boldLabel);
        float newYLength = EditorGUILayout.FloatField("Length", grid.yAxisLength);

        // Z-Achse
        EditorGUILayout.LabelField("Z-Axis", EditorStyles.boldLabel);
        float newZLength = EditorGUILayout.FloatField("Length", grid.zAxisLength);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Axis Gizmo Lengths");
            grid.xAxisLength = newXLength;
            grid.yAxisLength = newYLength;
            grid.zAxisLength = newZLength;
            grid.UpdateCoordinateSystem();
        }

        // Material Settings
        if (grid.gridMaterial != null)
        {
            EditorGUILayout.Space();
            Material mat = grid.gridMaterial;
            
            Color gridColor = mat.GetColor("_GridColor");
            
            EditorGUI.BeginChangeCheck();
            gridColor = EditorGUILayout.ColorField("Grid Color", gridColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mat, "Modified Grid Material");
                mat.SetColor("_GridColor", gridColor);
                EditorUtility.SetDirty(mat);
            }
        }

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Update Coordinate System"))
        {
            grid.UpdateCoordinateSystem();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
*/