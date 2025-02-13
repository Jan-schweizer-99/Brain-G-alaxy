using UnityEngine;
using UnityEditor;

[ExecuteInEditMode] // Diese Attribute ermöglicht die Ausführung im Editor
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpacePointsGenerator : MonoBehaviour
{
    public float spacing = 1f;           
    public float renderDistance = 20f;   
    public float pointSize = 0.05f;      

    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh pointMesh;
    private Material material;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private bool isInitialized = false;
    private int totalPoints;
    private int pointsPerAxis;
    private Vector3 gridOrigin;

    // Speichern der vorherigen Werte für Aktualisierungsprüfung
    private float previousSpacing;
    private float previousRenderDistance;
    private float previousPointSize;
    private Vector3 previousPosition;

    void OnEnable()
    {
        // Initialisierung beim Aktivieren der Komponente
        Initialize();
    }

    void Initialize()
    {
        // Speichere aktuelle Werte
        previousSpacing = spacing;
        previousRenderDistance = renderDistance;
        previousPointSize = pointSize;
        previousPosition = transform.position;

        if (isInitialized)
        {
            CleanupBuffers();
        }

        gridOrigin = transform.position;
        
        pointMesh = new Mesh();
        pointMesh.vertices = new Vector3[] { Vector3.zero };
        pointMesh.SetIndices(new int[] { 0 }, MeshTopology.Points, 0);

        pointsPerAxis = Mathf.CeilToInt(renderDistance * 2f / spacing) + 1;
        totalPoints = pointsPerAxis * pointsPerAxis * pointsPerAxis;
        
        positionBuffer = new ComputeBuffer(totalPoints, sizeof(float) * 4);
        
        material = new Material(Shader.Find("Custom/SpacePointShader"));
        material.SetBuffer("_PositionBuffer", positionBuffer);
        material.SetFloat("_PointSize", pointSize);
        GetComponent<MeshRenderer>().material = material;

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = pointMesh.GetIndexCount(0);
        args[1] = (uint)totalPoints;
        argsBuffer.SetData(args);

        UpdatePositions();
        isInitialized = true;
    }

    void UpdatePositions()
    {
        Vector4[] positions = new Vector4[totalPoints];
        int index = 0;
        float halfSize = renderDistance;

        for (int x = 0; x < pointsPerAxis; x++)
        {
            float xPos = -halfSize + (x * spacing);
            for (int y = 0; y < pointsPerAxis; y++)
            {
                float yPos = -halfSize + (y * spacing);
                for (int z = 0; z < pointsPerAxis; z++)
                {
                    float zPos = -halfSize + (z * spacing);
                    Vector3 localPos = new Vector3(xPos, yPos, zPos);
                    Vector3 worldPos = gridOrigin + localPos;
                    positions[index++] = new Vector4(worldPos.x, worldPos.y, worldPos.z, 1f);
                }
            }
        }

        if (positionBuffer != null)
        {
            positionBuffer.SetData(positions);
        }
    }

    void Update()
    {
        // Prüfe auf Änderungen der Parameter
        if (!isInitialized ||
            previousSpacing != spacing ||
            previousRenderDistance != renderDistance ||
            previousPointSize != pointSize ||
            previousPosition != transform.position)
        {
            Initialize();
        }

        if (!isInitialized) return;

        // Stelle sicher, dass das Grid an seiner Position bleibt
        if (transform.position != gridOrigin)
        {
            transform.position = gridOrigin;
        }

        Graphics.DrawMeshInstancedIndirect(
            pointMesh,
            0,
            material,
            new Bounds(gridOrigin, Vector3.one * renderDistance * 2f),
            argsBuffer,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            gameObject.layer
        );
    }

    void CleanupBuffers()
    {
        if (positionBuffer != null)
        {
            positionBuffer.Release();
            positionBuffer = null;
        }
        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }
    }

    void OnDisable()
    {
        CleanupBuffers();
        isInitialized = false;
    }

    void OnDestroy()
    {
        CleanupBuffers();
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? gridOrigin : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, Vector3.one * (renderDistance * 2f));
    }
}