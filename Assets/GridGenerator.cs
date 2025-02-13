using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridGenerator : MonoBehaviour
{
    public float gridSize = 10f;
    public float gridSpacing = 1f;
    
    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int linesPerAxis = Mathf.CeilToInt(gridSize / gridSpacing) * 2 + 1;
        int vertexCount = linesPerAxis * 4 * 3; // 4 Vertices pro Linie, 3 Achsen
        int lineCount = linesPerAxis * 3;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] indices = new int[vertexCount];

        int vertexIndex = 0;

        // Erstelle das Grid für jede Achse
        for (int axis = 0; axis < 3; axis++)
        {
            float start = -gridSize;
            float end = gridSize;

            for (int i = 0; i < linesPerAxis; i++)
            {
                float offset = -gridSize + i * gridSpacing;

                Vector3 startPoint = Vector3.zero;
                Vector3 endPoint = Vector3.zero;

                switch (axis)
                {
                    case 0: // X-Achse Linien
                        startPoint = new Vector3(start, offset, 0);
                        endPoint = new Vector3(end, offset, 0);
                        vertices[vertexIndex] = startPoint;
                        vertices[vertexIndex + 1] = endPoint;
                        
                        startPoint = new Vector3(start, 0, offset);
                        endPoint = new Vector3(end, 0, offset);
                        vertices[vertexIndex + 2] = startPoint;
                        vertices[vertexIndex + 3] = endPoint;
                        break;

                    case 1: // Y-Achse Linien
                        startPoint = new Vector3(offset, start, 0);
                        endPoint = new Vector3(offset, end, 0);
                        vertices[vertexIndex] = startPoint;
                        vertices[vertexIndex + 1] = endPoint;
                        
                        startPoint = new Vector3(0, start, offset);
                        endPoint = new Vector3(0, end, offset);
                        vertices[vertexIndex + 2] = startPoint;
                        vertices[vertexIndex + 3] = endPoint;
                        break;

                    case 2: // Z-Achse Linien
                        startPoint = new Vector3(offset, 0, start);
                        endPoint = new Vector3(offset, 0, end);
                        vertices[vertexIndex] = startPoint;
                        vertices[vertexIndex + 1] = endPoint;
                        
                        startPoint = new Vector3(0, offset, start);
                        endPoint = new Vector3(0, offset, end);
                        vertices[vertexIndex + 2] = startPoint;
                        vertices[vertexIndex + 3] = endPoint;
                        break;
                }

                indices[vertexIndex] = vertexIndex;
                indices[vertexIndex + 1] = vertexIndex + 1;
                indices[vertexIndex + 2] = vertexIndex + 2;
                indices[vertexIndex + 3] = vertexIndex + 3;

                vertexIndex += 4;
            }
        }

        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }
}