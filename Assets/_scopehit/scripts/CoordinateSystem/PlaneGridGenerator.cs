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

    private GameObject CreatePlaneWithoutCollider(string name, Vector3 scale, Vector3 position, Quaternion rotation)
    {
        GameObject plane = new GameObject(name);
        plane.AddComponent<MeshFilter>().mesh = CreateQuadMesh();
        plane.AddComponent<MeshRenderer>().material = gridMaterial;
        
        plane.transform.localScale = scale;
        plane.transform.position = transform.TransformPoint(position);
        plane.transform.rotation = transform.rotation * rotation;
        
        return plane;
    }

    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    void GenerateGrid()
    {
        if (gridMaterial == null) return;

        GameObject gridContainer = new GameObject("GridPlanes");
        gridContainer.transform.SetParent(transform);
        gridContainer.transform.position = transform.position;
        gridContainer.transform.rotation = transform.rotation;
        
        // Create XZ planes (horizontal)
        for (int y = -gridSize/2; y <= gridSize/2; y++)
        {
            GameObject horizontal = CreatePlaneWithoutCollider(
                $"HorizontalPlane_Y{y}",
                new Vector3(gridWorldSize, gridWorldSize, 1),
                new Vector3(0, y * spacing, 0),
                Quaternion.Euler(90, 0, 0)
            );
            horizontal.transform.parent = gridContainer.transform;
        }

        // Create XY planes (vertical, Z-axis)
        for (int z = -gridSize/2; z <= gridSize/2; z++)
        {
            GameObject verticalZ = CreatePlaneWithoutCollider(
                $"VerticalPlane_Z{z}",
                new Vector3(gridWorldSize, gridWorldSize, 1),
                new Vector3(0, 0, z * spacing),
                Quaternion.Euler(0, 0, 0)
            );
            verticalZ.transform.parent = gridContainer.transform;
        }

        // Create YZ planes (vertical, X-axis)
        for (int x = -gridSize/2; x <= gridSize/2; x++)
        {
            GameObject verticalX = CreatePlaneWithoutCollider(
                $"VerticalPlane_X{x}",
                new Vector3(gridWorldSize, gridWorldSize, 1),
                new Vector3(x * spacing, 0, 0),
                Quaternion.Euler(0, 90, 0)
            );
            verticalX.transform.parent = gridContainer.transform;
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