using UnityEngine;

public class CrossPlaneGenerator : MonoBehaviour
{
    [Header("Cross Settings")]
    public Material planeMaterial;
    public float planeSize = 1f;
    public Vector3 gridSize = new Vector3(3, 3, 3);
    public float spacing = 1.1f;

    void Start()
    {
        GenerateCrossGrid();
    }

    void GenerateCrossGrid()
    {
        Vector3 offset = new Vector3(
            (gridSize.x - 1) * spacing * 0.5f,
            (gridSize.y - 1) * spacing * 0.5f,
            (gridSize.z - 1) * spacing * 0.5f
        );

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3 position = new Vector3(
                        x * spacing - offset.x,
                        y * spacing - offset.y,
                        z * spacing - offset.z
                    );
                    CreateCrossPlanes(position);
                }
            }
        }
    }

    void CreateCrossPlanes(Vector3 position)
    {
        // Erstelle Parent Object für das Kreuz
        GameObject crossParent = new GameObject("Cross");
        crossParent.transform.parent = transform;
        crossParent.transform.position = position;

        // XY Plane
        CreatePlane("XY_Plane", crossParent.transform, new Vector3(0, 0, 90), Vector3.forward);
        
        // XZ Plane
        CreatePlane("XZ_Plane", crossParent.transform, new Vector3(0, 0, 0), Vector3.up);
        
        // YZ Plane
        CreatePlane("YZ_Plane", crossParent.transform, new Vector3(0, 90, 0), Vector3.right);
    }

    void CreatePlane(string name, Transform parent, Vector3 rotation, Vector3 normal)
    {
        GameObject plane = new GameObject(name);
        plane.transform.parent = parent;
        plane.transform.localPosition = Vector3.zero;
        plane.transform.localRotation = Quaternion.Euler(rotation);
        plane.transform.localScale = Vector3.one * planeSize;

        MeshFilter meshFilter = plane.AddComponent<MeshFilter>();
        meshFilter.mesh = CreatePlaneMesh();

        MeshRenderer meshRenderer = plane.AddComponent<MeshRenderer>();
        meshRenderer.material = planeMaterial;
    }

    Mesh CreatePlaneMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };

        int[] triangles = new int[]
        {
            0, 2, 1,
            0, 3, 2
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}
