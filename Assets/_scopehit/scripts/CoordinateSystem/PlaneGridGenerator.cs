// PlaneGridGenerator.cs

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlaneGridGenerator : MonoBehaviour
{
    public Material gridMaterial;
    public int gridSize = 10;
    public float gridWorldSize = 10f;
    
    [SerializeField]
    private string shaderName = "Custom/WireframeGrid";

    private float spacing;
    private Color currentWireframeColor = Color.white;

    void Start()
    {
        if (gridMaterial == null)
        {
            CreateGridMaterial(currentWireframeColor);
        }
        CalculateSpacing();
        //GenerateGrid();
    }

    void CalculateSpacing()
    {
        spacing = gridWorldSize / gridSize;
    }

    void CreateGridMaterial(Color wireframeColor)
    {
        Shader gridShader = Shader.Find(shaderName);
        if (gridShader == null)
        {
            Debug.LogError($"Shader {shaderName} not found!");
            return;
        }
        
        if (gridMaterial == null)
        {
            gridMaterial = new Material(gridShader);
        }
        
        gridMaterial.SetFloat("_GridSize", spacing);
        gridMaterial.SetColor("_GridColor", wireframeColor);
    }

    public void UpdateGrid(Color wireframeColor)
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        currentWireframeColor = wireframeColor;
        CalculateSpacing();
        CreateGridMaterial(wireframeColor);
        GenerateGrid();
    }

    public void UpdateGrid()
    {
        UpdateGrid(currentWireframeColor);
    }

    void GenerateGrid()
    {
        if (gridMaterial == null) return;

        GameObject gridContainer = new GameObject("GridPlanes");
        gridContainer.transform.SetParent(transform, false);
        gridContainer.transform.localPosition = Vector3.zero;
        gridContainer.transform.localRotation = Quaternion.identity;
        
        // Create XZ planes (horizontal)
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

        // Create XY planes (vertical, Z-axis)
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

        // Create YZ planes (vertical, X-axis)
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
        
        EditorGUI.BeginChangeCheck();
        int newGridSize = EditorGUILayout.IntField("Grid Lines", grid.gridSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Grid Lines");
            grid.gridSize = newGridSize;
            grid.UpdateGrid();
        }

        EditorGUI.BeginChangeCheck();
        float newGridWorldSize = EditorGUILayout.FloatField("Grid World Size", grid.gridWorldSize);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(grid, "Changed Grid World Size");
            grid.gridWorldSize = newGridWorldSize;
            grid.UpdateGrid();
        }

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
        
        if (GUILayout.Button("Update Grid"))
        {
            grid.UpdateGrid();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif