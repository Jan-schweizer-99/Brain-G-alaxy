using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class CubeGenerator : MonoBehaviour
{
   public float insetAmount = 0.2f;
   
   public float width = 1f;
   public float height = 1f; 
   public float length = 1f;

   public bool flipFront = false;
   public bool flipBack = false;
   public bool flipTop = false;
   public bool flipBottom = false;
   public bool flipRight = false;
   public bool flipLeft = false;

   public bool flipFrontTopRight = false;
   public bool flipFrontTopLeft = false;
   public bool flipFrontBottomRight = false;
   public bool flipFrontBottomLeft = false;
   public bool flipBackTopRight = false;
   public bool flipBackTopLeft = false;
   public bool flipBackBottomRight = false;
   public bool flipBackBottomLeft = false;

   public bool flipEdgeNorthEast = false;
   public bool flipEdgeNorthWest = false;
   public bool flipEdgeNorthFront = false;
   public bool flipEdgeNorthBack = false;
   public bool flipEdgeSouthEast = false; 
   public bool flipEdgeSouthWest = false;
   public bool flipEdgeSouthFront = false;
   public bool flipEdgeSouthBack = false;
   public bool flipEdgeEastFront = false;
   public bool flipEdgeEastBack = false;
   public bool flipEdgeWestFront = false;
   public bool flipEdgeWestBack = false;

   private Mesh mesh;
   private bool initialized = false;

   void OnEnable()
   {
       if (!initialized)
       {
           mesh = new Mesh();
           mesh.name = "GeneratedCube";
           GetComponent<MeshFilter>().sharedMesh = mesh;
           initialized = true;
       }
       GenerateCube();
   }

   void OnValidate()
   {
       GenerateCube();
   }

   void OnDrawGizmos()
   {
       if (mesh == null) return;

       Vector3[] vertices = mesh.vertices;
       for (int i = 0; i < vertices.Length; i++)
       {
           Vector3 worldPos = transform.TransformPoint(vertices[i]);
           Vector3 labelOffset = Vector3.up * 0.02f;
           Handles.Label(worldPos + labelOffset, i.ToString());
           Gizmos.DrawSphere(worldPos, 0.01f);
       }
   }

   void GenerateCube()
   {
       if (mesh == null) return;

       float w = width / 2f;
       float h = height / 2f;
       float l = length / 2f;

       Vector3[] vertices = new Vector3[48];
       
       // Front face
       vertices[0] = new Vector3(-w + insetAmount, -h + insetAmount, l);
       vertices[1] = new Vector3(-w + insetAmount, h - insetAmount, l);
       vertices[2] = new Vector3(w - insetAmount, h - insetAmount, l);
       vertices[3] = new Vector3(w - insetAmount, -h + insetAmount, l);

       // Back face  
       vertices[4] = new Vector3(w - insetAmount, -h + insetAmount, -l);
       vertices[5] = new Vector3(-w + insetAmount, -h + insetAmount, -l);
       vertices[6] = new Vector3(-w + insetAmount, h - insetAmount, -l);
       vertices[7] = new Vector3(w - insetAmount, h - insetAmount, -l);

       // Top face
       vertices[8] = new Vector3(-w + insetAmount, h, -l + insetAmount);
       vertices[9] = new Vector3(-w + insetAmount, h, l - insetAmount);
       vertices[10] = new Vector3(w - insetAmount, h, l - insetAmount);
       vertices[11] = new Vector3(w - insetAmount, h, -l + insetAmount);

       // Bottom face
       vertices[12] = new Vector3(w - insetAmount, -h, -l + insetAmount);
       vertices[13] = new Vector3(w - insetAmount, -h, l - insetAmount);
       vertices[14] = new Vector3(-w + insetAmount, -h, l - insetAmount);
       vertices[15] = new Vector3(-w + insetAmount, -h, -l + insetAmount);

       // Right face
       vertices[16] = new Vector3(w, -h + insetAmount, l - insetAmount);
       vertices[17] = new Vector3(w, -h + insetAmount, -l + insetAmount);
       vertices[18] = new Vector3(w, h - insetAmount, -l + insetAmount);
       vertices[19] = new Vector3(w, h - insetAmount, l - insetAmount);

       // Left face
       vertices[20] = new Vector3(-w, -h + insetAmount, -l + insetAmount);
       vertices[21] = new Vector3(-w, -h + insetAmount, l - insetAmount);
       vertices[22] = new Vector3(-w, h - insetAmount, l - insetAmount);
       vertices[23] = new Vector3(-w, h - insetAmount, -l + insetAmount);

       // Corner vertices
       // Front Top Right
       vertices[24] = new Vector3(w - insetAmount, h - insetAmount, l);
       vertices[25] = new Vector3(w, h - insetAmount, l - insetAmount);
       vertices[26] = new Vector3(w - insetAmount, h, l - insetAmount);

       // Front Top Left
       vertices[27] = new Vector3(-w + insetAmount, h - insetAmount, l);
       vertices[28] = new Vector3(-w + insetAmount, h, l - insetAmount);
       vertices[29] = new Vector3(-w, h - insetAmount, l - insetAmount);

       // Front Bottom Right
       vertices[30] = new Vector3(w - insetAmount, -h + insetAmount, l);
       vertices[31] = new Vector3(w - insetAmount, -h, l - insetAmount);
       vertices[32] = new Vector3(w, -h + insetAmount, l - insetAmount);

       // Front Bottom Left
       vertices[33] = new Vector3(-w + insetAmount, -h + insetAmount, l);
       vertices[34] = new Vector3(-w, -h + insetAmount, l - insetAmount);
       vertices[35] = new Vector3(-w + insetAmount, -h, l - insetAmount);

       // Back Top Right
       vertices[36] = new Vector3(w - insetAmount, h - insetAmount, -l);
       vertices[37] = new Vector3(w - insetAmount, h, -l + insetAmount);
       vertices[38] = new Vector3(w, h - insetAmount, -l + insetAmount);

       // Back Top Left
       vertices[39] = new Vector3(-w + insetAmount, h - insetAmount, -l);
       vertices[40] = new Vector3(-w, h - insetAmount, -l + insetAmount);
       vertices[41] = new Vector3(-w + insetAmount, h, -l + insetAmount);

       // Back Bottom Right
       vertices[42] = new Vector3(w - insetAmount, -h + insetAmount, -l);
       vertices[43] = new Vector3(w, -h + insetAmount, -l + insetAmount);
       vertices[44] = new Vector3(w - insetAmount, -h, -l + insetAmount);

       // Back Bottom Left
       vertices[45] = new Vector3(-w + insetAmount, -h + insetAmount, -l);
       vertices[46] = new Vector3(-w + insetAmount, -h, -l + insetAmount);
       vertices[47] = new Vector3(-w, -h + insetAmount, -l + insetAmount);

       int[] triangles = new int[132];
       
       // Main faces
       for (int i = 0; i < 6; i++)
       {
           int offset = i * 4;
           int tOffset = i * 6;
           
           bool flip = false;
           switch(i) {
               case 0: flip = flipFront; break;
               case 1: flip = flipBack; break;
               case 2: flip = flipTop; break;
               case 3: flip = flipBottom; break;
               case 4: flip = flipRight; break;
               case 5: flip = flipLeft; break;
           }

if ((i == 0 && !flip) || (i != 0 && flip)) {
    triangles[tOffset] = offset;
    triangles[tOffset + 1] = offset + 2;
    triangles[tOffset + 2] = offset + 1;
    triangles[tOffset + 3] = offset;
    triangles[tOffset + 4] = offset + 3;
    triangles[tOffset + 5] = offset + 2;
} else {
    triangles[tOffset] = offset;
    triangles[tOffset + 1] = offset + 1;
    triangles[tOffset + 2] = offset + 2;
    triangles[tOffset + 3] = offset;
    triangles[tOffset + 4] = offset + 2;
    triangles[tOffset + 5] = offset + 3;
}
       }

       // Corner triangles
       for (int i = 0; i < 8; i++)
       {
           int vOffset = 24 + (i * 3);
           int tOffset = 36 + (i * 3);
           
           bool flip = false;
           switch(i) {
               case 0: flip = flipFrontTopRight; break;
               case 1: flip = flipFrontTopLeft; break;
               case 2: flip = flipFrontBottomRight; break;
               case 3: flip = flipFrontBottomLeft; break;
               case 4: flip = flipBackTopRight; break;
               case 5: flip = flipBackTopLeft; break;
               case 6: flip = flipBackBottomRight; break;
               case 7: flip = flipBackBottomLeft; break;
           }

           if (flip) {
               triangles[tOffset] = vOffset;
               triangles[tOffset + 1] = vOffset + 2;
               triangles[tOffset + 2] = vOffset + 1;
           } else {
               triangles[tOffset] = vOffset;
               triangles[tOffset + 1] = vOffset + 1;
               triangles[tOffset + 2] = vOffset + 2;
           }
       }

       // Inner edges
       int[][] edgeFaces = new int[][] {
           new int[] {9, 22, 8, 23},   // NorthEast
           new int[] {8, 6, 11, 7},    // NorthWest
           new int[] {11, 10, 18, 19}, // NorthFront
           new int[] {10, 2, 9, 1},    // NorthBack
           new int[] {1, 22, 0, 21},   // SouthEast
           new int[] {5, 20, 6, 23},   // SouthWest
           new int[] {7, 18, 4, 17},   // SouthFront
           new int[] {3, 16, 2, 19},   // SouthBack 
           new int[] {16, 13, 17, 12}, // EastFront
           new int[] {4, 12, 5, 15},   // EastBack
           new int[] {20, 15, 21, 14}, // WestFront
           new int[] {0, 14, 3, 13}    // WestBack
       };

       bool[] edgeFlips = new bool[] {
           flipEdgeNorthEast,
           flipEdgeNorthWest,
           flipEdgeNorthFront,
           flipEdgeNorthBack,
           flipEdgeSouthEast,
           flipEdgeSouthWest,
           flipEdgeSouthFront,
           flipEdgeSouthBack,
           flipEdgeEastFront,
           flipEdgeEastBack,
           flipEdgeWestFront,
           flipEdgeWestBack
       };

       for (int i = 0; i < edgeFaces.Length; i++)
       {
           int tOffset = 60 + (i * 6);
           int[] face = edgeFaces[i];
           
           if (edgeFlips[i]) {
               triangles[tOffset] = face[0];
               triangles[tOffset + 1] = face[2];
               triangles[tOffset + 2] = face[1];
               triangles[tOffset + 3] = face[2];
               triangles[tOffset + 4] = face[3]; 
               triangles[tOffset + 5] = face[1];
           } else {
               triangles[tOffset] = face[0];
               triangles[tOffset + 1] = face[1];
               triangles[tOffset + 2] = face[2];
               triangles[tOffset + 3] = face[2];
               triangles[tOffset + 4] = face[1];
               triangles[tOffset + 5] = face[3];
           }
       }

       Vector2[] uvs = new Vector2[48];
       for (int i = 0; i < 24; i++)
       {
           if (i % 4 == 0) uvs[i] = new Vector2(0, 0);
           if (i % 4 == 1) uvs[i] = new Vector2(1, 0);
           if (i % 4 == 2) uvs[i] = new Vector2(1, 1);
           if (i % 4 == 3) uvs[i] = new Vector2(0, 1);
       }
       
       for (int i = 24; i < 48; i++)
       {
           uvs[i] = new Vector2(0, 0);
       }

       // Merge vertices
       float mergeDistance = 0.0001f;
       List<Vector3> mergedVertices = new List<Vector3>();
       List<Vector2> mergedUVs = new List<Vector2>();
       int[] newTriangles = new int[triangles.Length];
       
       Dictionary<int, int> vertexMap = new Dictionary<int, int>();
       
       for (int i = 0; i < vertices.Length; i++)
       {
           if (vertexMap.ContainsKey(i)) continue;
           
           int newIndex = mergedVertices.Count;
           mergedVertices.Add(vertices[i]);
           mergedUVs.Add(uvs[i]);
           vertexMap[i] = newIndex;
           
           for (int j = i + 1; j < vertices.Length; j++)
           {
               if (!vertexMap.ContainsKey(j) && Vector3.Distance(vertices[i], vertices[j]) < mergeDistance)
               {
                   vertexMap[j] = newIndex;
               }
           }
       }
       
       for (int i = 0; i < triangles.Length; i++)
       {
           newTriangles[i] = vertexMap[triangles[i]];
       }
       
       mesh.Clear();
       mesh.vertices = mergedVertices.ToArray();
       mesh.triangles = newTriangles;
       mesh.uv = mergedUVs.ToArray();
       mesh.RecalculateNormals();
   }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CubeGenerator))]
public class CubeGeneratorEditor : Editor
{
   public override void OnInspectorGUI()
   {
       CubeGenerator generator = (CubeGenerator)target;
       
       EditorGUI.BeginChangeCheck();
       DrawDefaultInspector();
       
       if (EditorGUI.EndChangeCheck())
       {
           EditorUtility.SetDirty(generator);
       }
   }
}
#endif