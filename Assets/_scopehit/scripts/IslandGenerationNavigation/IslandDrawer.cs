using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Linq;
using System.Collections.Generic;

public class InvertedIslandDrawer : MonoBehaviour
{
    [Header("VR Settings")]
    [SerializeField] private XRDirectInteractor directInteractor;
    [SerializeField] private bool useRightHand = true;
    private LineRenderer drawingLine;
    private LineRenderer rayLine;
    public float rayDistance = 10f;
    
    [Header("Ray Settings")]
    public Color rayColor = Color.blue;
    public float rayStartWidth = 0.01f;
    public float rayEndWidth = 0.001f;
    
    [Header("Crater Generation")]
    public Material groundMaterial;
    public float maxDepth = 3f;
    public int gridSize = 32;
    public float simplificationTolerance = 0.1f;
    public float noiseScale = 0.5f;
    public float edgeBuffer = 0.1f;

    [Header("Top Surface")]
    public Material topMaterial;
    public float surfaceHeight = 0.1f;

    [Header("Drawing Surface")]
    public float surfaceSize = 10f;
    public Material drawingSurfaceMaterial;
    public float drawingSurfaceHeight = 0f;
    private GameObject drawingSurface;
    public LayerMask drawingSurfaceLayer;
    
    [Header("Drawing Settings")]
    public bool isDrawingEnabled = false;
    private bool isCurrentlyDrawing = false;
    private List<Vector3> drawPoints = new List<Vector3>();
    private List<Vector3> simplifiedPoints = new List<Vector3>();

    // Referenzen für kombinierte Meshes
    private GameObject combinedIsland;
    private MeshFilter topMeshFilter;
    private MeshFilter bottomMeshFilter;
    private Mesh topMesh;
    private Mesh bottomMesh;
    
    // Reference to the current drawing surface hit
    private GameObject currentDrawingSurface;
    private float currentDrawingHeight;

    private void Awake()
    {
        if (directInteractor == null)
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                string interactorName = useRightHand ? "Right_Direct Interactor" : "Left_Direct Interactor";
                directInteractor = xrOrigin.GetComponentsInChildren<XRDirectInteractor>()
                    .FirstOrDefault(i => i.gameObject.name == interactorName);
            }
        }

        if (directInteractor == null)
        {
            Debug.LogError($"No {(useRightHand ? "Right" : "Left")}_Direct Interactor found!");
            return;
        }

        CreateRayLine();
        CreateDrawingLine();
        CreateDrawingSurface();
    }

    private void CreateDrawingSurface()
    {
        drawingSurface = GameObject.CreatePrimitive(PrimitiveType.Plane);
        drawingSurface.name = "DrawingSurface";
        
        float scale = surfaceSize / 10f;
        drawingSurface.transform.localScale = new Vector3(scale, 1f, scale);
        drawingSurface.transform.position = new Vector3(0f, drawingSurfaceHeight, 0f);
        
        if (drawingSurfaceMaterial != null)
        {
            drawingSurface.GetComponent<MeshRenderer>().material = drawingSurfaceMaterial;
        }
        
        drawingSurface.layer = Mathf.RoundToInt(Mathf.Log(drawingSurfaceLayer.value, 2));
        
        Color surfaceColor = drawingSurface.GetComponent<MeshRenderer>().material.color;
        surfaceColor.a = 0.5f;
        drawingSurface.GetComponent<MeshRenderer>().material.color = surfaceColor;
    }

    private void CreateRayLine()
    {
        GameObject rayObject = new GameObject("HandRay");
        rayObject.transform.SetParent(directInteractor.transform, false);
        
        rayLine = rayObject.AddComponent<LineRenderer>();
        rayLine.useWorldSpace = false;
        rayLine.positionCount = 2;
        rayLine.startWidth = rayStartWidth;
        rayLine.endWidth = rayEndWidth;
        
        rayLine.material = new Material(Shader.Find("Sprites/Default"));
        rayLine.startColor = rayColor;
        rayLine.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.5f);

        rayLine.SetPosition(0, Vector3.zero);
        rayLine.SetPosition(1, new Vector3(0, 0, rayDistance));
    }

    private void CreateDrawingLine()
    {
        GameObject lineObj = new GameObject("DrawingLine");
        lineObj.transform.SetParent(directInteractor.transform, false);

        drawingLine = lineObj.AddComponent<LineRenderer>();
        drawingLine.positionCount = 0;
        drawingLine.startWidth = 0.02f;
        drawingLine.endWidth = 0.02f;
        drawingLine.useWorldSpace = true;
        drawingLine.material = new Material(Shader.Find("Sprites/Default"));
        drawingLine.startColor = Color.white;
        drawingLine.endColor = Color.white;
    }

    private void Update()
    {
        UpdateRayVisualization();

        if (!isDrawingEnabled || directInteractor == null) return;

        var controller = directInteractor.GetComponentInParent<ActionBasedController>();
        if (controller == null) return;

        bool triggerValue = controller.selectAction.action.ReadValue<float>() > 0.5f;
        
        if (triggerValue && !isCurrentlyDrawing)
        {
            StartDrawing();
        }
        else if (triggerValue && isCurrentlyDrawing)
        {
            ContinueDrawing();
        }
        else if (!triggerValue && isCurrentlyDrawing)
        {
            FinishDrawing();
        }
    }

    private void UpdateRayVisualization()
    {
        if (Physics.Raycast(directInteractor.transform.position, directInteractor.transform.forward, 
            out RaycastHit hit, rayDistance, drawingSurfaceLayer))
        {
            rayLine.SetPosition(1, new Vector3(0, 0, hit.distance));
        }
        else
        {
            rayLine.SetPosition(1, new Vector3(0, 0, rayDistance));
        }
    }

    private void StartDrawing()
    {
        if (Physics.Raycast(directInteractor.transform.position, directInteractor.transform.forward, 
            out RaycastHit hit, rayDistance, drawingSurfaceLayer))
        {
            currentDrawingSurface = hit.collider.gameObject;
            // Speichere die Y-Position der getroffenen Oberfläche
            currentDrawingHeight = hit.point.y;
            isCurrentlyDrawing = true;
            drawPoints.Clear();
            simplifiedPoints.Clear();
            drawingLine.positionCount = 0;
            
            InitializeCombinedMesh();
        }
    }


    private void InitializeCombinedMesh()
    {
        if (currentDrawingSurface == null) return;
        
        // Create new island object
        combinedIsland = new GameObject("CombinedIsland");
        
        // Set the parent to the current drawing surface
        combinedIsland.transform.SetParent(currentDrawingSurface.transform, true);
        
        // Top Surface Setup
        GameObject topSurface = new GameObject("TopSurface");
        topSurface.transform.SetParent(combinedIsland.transform);
        topMeshFilter = topSurface.AddComponent<MeshFilter>();
        MeshRenderer topRenderer = topSurface.AddComponent<MeshRenderer>();
        topRenderer.material = topMaterial;
        topMesh = new Mesh();
        topMeshFilter.mesh = topMesh;
        topSurface.AddComponent<MeshCollider>();

        // Bottom Surface Setup
        GameObject bottomSurface = new GameObject("BottomSurface");
        bottomSurface.transform.SetParent(combinedIsland.transform);
        bottomMeshFilter = bottomSurface.AddComponent<MeshFilter>();
        MeshRenderer bottomRenderer = bottomSurface.AddComponent<MeshRenderer>();
        bottomRenderer.material = groundMaterial;
        bottomMesh = new Mesh();
        bottomMeshFilter.mesh = bottomMesh;
        bottomSurface.AddComponent<MeshCollider>();
    }

    private void ContinueDrawing()
    {
        if (Physics.Raycast(directInteractor.transform.position, directInteractor.transform.forward, 
            out RaycastHit hit, rayDistance, drawingSurfaceLayer))
        {
            // Only continue drawing if we hit the same surface we started on
            if (hit.collider.gameObject == currentDrawingSurface)
            {
                drawPoints.Add(hit.point);
                drawingLine.positionCount = drawPoints.Count;
                drawingLine.SetPositions(drawPoints.ToArray());
            }
        }
    }

    private void FinishDrawing()
    {
        isCurrentlyDrawing = false;
        
        if (drawPoints.Count >= 3)
        {
            if (Vector3.Distance(drawPoints[0], drawPoints[drawPoints.Count-1]) > 0.1f)
            {
                drawPoints.Add(drawPoints[0]);
            }
            
            simplifiedPoints = SimplifyLine(drawPoints, simplificationTolerance);
            
            if (simplifiedPoints.Count >= 3)
            {
                simplifiedPoints = EnsureClockwiseOrder(simplifiedPoints);
                CreateInvertedIsland();
            }
        }
    }

    private void CreateInvertedIsland()
    {
        if (simplifiedPoints.Count < 3) return;

        // Erstelle temporäre Meshes für die neue Form
        Mesh newTopMesh = CreateTopSurface(simplifiedPoints);
        Mesh newBottomMesh = CreateNoisyMesh(simplifiedPoints);

        // Kombiniere mit existierenden Meshes
        if (topMesh.vertexCount == 0)
        {
            // Erste Form
            topMesh.Clear();
            bottomMesh.Clear();
            CopyMeshData(newTopMesh, topMesh);
            CopyMeshData(newBottomMesh, bottomMesh);
        }
        else
        {
            // Kombiniere mit existierenden Meshes
            CombineMeshes(topMesh, newTopMesh, true);
            CombineMeshes(bottomMesh, newBottomMesh, true);
        }

        // Update Collider
        UpdateColliders();
    }

   private Mesh CreateTopSurface(List<Vector3> points)
    {
        List<Vector2> flatPoints = points.Select(p => new Vector2(p.x, p.z)).ToList();
        Vector2 center = GetCentroid(flatPoints);
        
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Füge Zentrum-Vertex mit aktueller Höhe + surfaceHeight hinzu
        vertices.Add(new Vector3(center.x, currentDrawingHeight + surfaceHeight, center.y));

        // Füge Umriss-Vertices mit aktueller Höhe + surfaceHeight hinzu
        foreach (Vector2 point in flatPoints)
        {
            vertices.Add(new Vector3(point.x, currentDrawingHeight + surfaceHeight, point.y));
        }

        // Erstelle Dreiecke für die flache Oberfläche
        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        
        return mesh;
    }

    private Mesh CreateNoisyMesh(List<Vector3> points)
    {
        List<Vector2> flatPoints = points.Select(p => new Vector2(p.x, p.z)).ToList();
        Mesh mesh = new Mesh();
        Vector2 center = GetCentroid(flatPoints);
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        float maxDistance = flatPoints.Max(p => Vector2.Distance(p, center));
        
        // Zentraler Punkt mit Noise und aktueller Höhe
        float centerNoise = Mathf.PerlinNoise(
            center.x * noiseScale, 
            center.y * noiseScale
        );
        vertices.Add(new Vector3(center.x, currentDrawingHeight - maxDepth * centerNoise, center.y));

        int innerRingPoints = 16;
        float innerRingRadius = maxDistance * 0.3f;
        
        // Innerer Ring mit Noise und aktueller Höhe
        for (int i = 0; i < innerRingPoints; i++)
        {
            float angle = (i / (float)innerRingPoints) * Mathf.PI * 2;
            Vector2 innerPoint = center + new Vector2(
                Mathf.Cos(angle) * innerRingRadius,
                Mathf.Sin(angle) * innerRingRadius
            );
            
            float noise = Mathf.PerlinNoise(
                innerPoint.x * noiseScale,
                innerPoint.y * noiseScale
            );
            
            vertices.Add(new Vector3(
                innerPoint.x,
                currentDrawingHeight - maxDepth * noise * 0.7f,
                innerPoint.y
            ));
        }
        
        // Äußere Punkte auf aktueller Höhe
        foreach (Vector2 point in flatPoints)
        {
            vertices.Add(new Vector3(point.x, currentDrawingHeight, point.y));
        }
        
        int innerRingStart = 1;
        int outlineStart = innerRingStart + innerRingPoints;
        
        for (int i = 0; i < innerRingPoints; i++)
        {
            int current = innerRingStart + i;
            int next = innerRingStart + ((i + 1) % innerRingPoints);
            
            triangles.Add(0);
            triangles.Add(current);
            triangles.Add(next);
        }
        
        for (int i = 0; i < flatPoints.Count - 1; i++)
        {
            int outlineCurrent = outlineStart + i;
            int outlineNext = outlineStart + i + 1;
            
            Vector2 currentPoint = flatPoints[i];
            Vector2 nextPoint = flatPoints[i + 1];
            
int innerCurrent = GetClosestInnerRingPoint(i, innerRingPoints, currentPoint, center, vertices);
            int innerNext = GetClosestInnerRingPoint(i + 1, innerRingPoints, nextPoint, center, vertices);
            
            if (innerCurrent == innerNext)
            {
                triangles.Add(outlineCurrent);
                triangles.Add(outlineNext);
                triangles.Add(innerRingStart + innerCurrent);
            }
            else
            {
                triangles.Add(outlineCurrent);
                triangles.Add(outlineNext);
                triangles.Add(innerRingStart + innerCurrent);
                
                triangles.Add(innerRingStart + innerCurrent);
                triangles.Add(outlineNext);
                triangles.Add(innerRingStart + innerNext);
                
                int innerDiff = (innerNext - innerCurrent + innerRingPoints) % innerRingPoints;
                if (innerDiff > 1)
                {
                    for (int j = 1; j < innerDiff; j++)
                    {
                        triangles.Add(innerRingStart + innerCurrent);
                        triangles.Add(innerRingStart + ((innerCurrent + j) % innerRingPoints));
                        triangles.Add(innerRingStart + ((innerCurrent + j + 1) % innerRingPoints));
                    }
                }
            }
        }
        
        // Verbinde letzten und ersten Punkt
        int lastOutline = outlineStart + flatPoints.Count - 1;
        int firstOutline = outlineStart;
        int lastInner = GetClosestInnerRingPoint(flatPoints.Count - 1, innerRingPoints, flatPoints[flatPoints.Count - 1], center, vertices);
        int firstInner = GetClosestInnerRingPoint(0, innerRingPoints, flatPoints[0], center, vertices);
        
        triangles.Add(lastOutline);
        triangles.Add(firstOutline);
        triangles.Add(innerRingStart + lastInner);
        
        triangles.Add(innerRingStart + lastInner);
        triangles.Add(firstOutline);
        triangles.Add(innerRingStart + firstInner);
        
        int lastInnerDiff = (firstInner - lastInner + innerRingPoints) % innerRingPoints;
        if (lastInnerDiff > 1)
        {
            for (int j = 1; j < lastInnerDiff; j++)
            {
                triangles.Add(innerRingStart + lastInner);
                triangles.Add(innerRingStart + ((lastInner + j) % innerRingPoints));
                triangles.Add(innerRingStart + ((lastInner + j + 1) % innerRingPoints));
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        
        return mesh;
    }

    private void UpdateColliders()
    {
        var topCollider = topMeshFilter.GetComponent<MeshCollider>();
        var bottomCollider = bottomMeshFilter.GetComponent<MeshCollider>();
        
        topCollider.sharedMesh = null;
        bottomCollider.sharedMesh = null;
        
        topCollider.sharedMesh = topMesh;
        bottomCollider.sharedMesh = bottomMesh;
    }

    private void CopyMeshData(Mesh source, Mesh destination)
    {
        destination.vertices = source.vertices;
        destination.triangles = source.triangles;
        destination.normals = source.normals;
        destination.RecalculateBounds();
    }

    private void CombineMeshes(Mesh targetMesh, Mesh newMesh, bool union)
    {
        Vector3[] originalVertices = targetMesh.vertices;
        int[] originalTriangles = targetMesh.triangles;
        
        Vector3[] newVertices = newMesh.vertices;
        int[] newTriangles = newMesh.triangles;

        Vector3[] combinedVertices = new Vector3[originalVertices.Length + newVertices.Length];
        originalVertices.CopyTo(combinedVertices, 0);
        newVertices.CopyTo(combinedVertices, originalVertices.Length);

        int[] combinedTriangles = new int[originalTriangles.Length + newTriangles.Length];
        originalTriangles.CopyTo(combinedTriangles, 0);
        
        for (int i = 0; i < newTriangles.Length; i++)
        {
            combinedTriangles[originalTriangles.Length + i] = newTriangles[i] + originalVertices.Length;
        }

        targetMesh.Clear();
        targetMesh.vertices = combinedVertices;
        targetMesh.triangles = combinedTriangles;
        targetMesh.RecalculateNormals();
        targetMesh.RecalculateBounds();
    }

    private Vector2 GetCentroid(List<Vector2> points)
    {
        float sumX = points.Sum(p => p.x);
        float sumZ = points.Sum(p => p.y);
        return new Vector2(sumX / points.Count, sumZ / points.Count);
    }

    private List<Vector3> SimplifyLine(List<Vector3> points, float tolerance)
    {
        if (points.Count <= 3) return new List<Vector3>(points);

        List<Vector3> simplified = new List<Vector3>();
        simplified.Add(points[0]);
        
        float maxDistanceThreshold = tolerance;
        
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 current = points[i];
            Vector3 prev = simplified[simplified.Count - 1];
            float distance = Vector3.Distance(current, prev);
            
            if (distance > maxDistanceThreshold)
            {
                simplified.Add(current);
            }
        }
        
        if (points.Count > 1 && (simplified.Count == 1 || Vector3.Distance(simplified[simplified.Count - 1], points[points.Count - 1]) > maxDistanceThreshold))
        {
            simplified.Add(points[points.Count - 1]);
        }

        return simplified;
    }

    private bool IsClockwise(List<Vector2> points)
    {
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 current = points[i];
            Vector2 next = points[(i + 1) % points.Count];
            area += (next.x - current.x) * (next.y + current.y);
        }
        return area < 0;
    }

    private List<Vector3> EnsureClockwiseOrder(List<Vector3> points)
    {
        List<Vector2> flatPoints = points.Select(p => new Vector2(p.x, p.z)).ToList();
        if (!IsClockwise(flatPoints))
        {
            points.Reverse();
        }
        return points;
    }

    private float PointLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Cross(lineEnd - lineStart, point - lineStart).magnitude / (lineEnd - lineStart).magnitude;
    }
    
    private int GetClosestInnerRingPoint(int outlineIndex, int innerRingPoints, Vector2 outlinePoint, Vector2 center, List<Vector3> vertices)
    {
        float angle = Mathf.Atan2(outlinePoint.y - center.y, outlinePoint.x - center.x);
        if (angle < 0) angle += Mathf.PI * 2;
        
        float indexF = (angle / (Mathf.PI * 2)) * innerRingPoints;
        int baseIndex = Mathf.RoundToInt(indexF) % innerRingPoints;
        
        float minDistance = float.MaxValue;
        int bestIndex = baseIndex;
        
        for (int offset = -1; offset <= 1; offset++)
        {
            int testIndex = (baseIndex + offset + innerRingPoints) % innerRingPoints;
            Vector3 innerPoint = vertices[1 + testIndex];
            float distance = Vector2.Distance(
                new Vector2(innerPoint.x, innerPoint.z),
                outlinePoint
            );
            
            if (distance < minDistance)
            {
                minDistance = distance;
                bestIndex = testIndex;
            }
        }
        
        return bestIndex;
    }

    public void ToggleDrawingSurfaceVisibility(bool visible)
    {
        if (drawingSurface != null)
        {
            drawingSurface.SetActive(visible);
        }
    }

    public void SetDrawingSurfaceHeight(float height)
    {
        drawingSurfaceHeight = height;
        if (drawingSurface != null)
        {
            Vector3 pos = drawingSurface.transform.position;
            drawingSurface.transform.position = new Vector3(pos.x, height, pos.z);
        }
    }
}