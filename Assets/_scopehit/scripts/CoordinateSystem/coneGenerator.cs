using UnityEngine;

public class ConeGenerator
{
    public static Mesh CreateConeMesh(int segments = 16)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Cone";

        // Vertices berechnen
        Vector3[] vertices = new Vector3[segments + 2];
        // Spitze des Kegels
        vertices[0] = Vector3.up * 0.5f;
        // Mittelpunkt der Basis
        vertices[1] = Vector3.down * 0.5f;
        
        // Vertices am Rand der Basis
        float angleStep = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * 0.5f;
            float z = Mathf.Sin(angle) * 0.5f;
            vertices[i + 2] = new Vector3(x, -0.5f, z);
        }

        // Triangles erstellen
        int[] triangles = new int[segments * 6];
        int triIndex = 0;
        
        // Seitenflächen
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = 0; // Spitze
            triangles[triIndex++] = ((i + 1) % segments) + 2;
            triangles[triIndex++] = i + 2;
        }
        
        // Basisfläche
        for (int i = 0; i < segments - 1; i++)
        {
            triangles[triIndex++] = 1; // Basismittelpunkt
            triangles[triIndex++] = i + 2;
            triangles[triIndex++] = i + 3;
        }
        // Letztes Dreieck der Basis
        triangles[triIndex++] = 1;
        triangles[triIndex++] = segments + 1;
        triangles[triIndex++] = 2;

        // UVs generieren
        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = Vector2.up;
        uvs[1] = Vector2.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float u = (Mathf.Cos(angle) + 1f) * 0.5f;
            float v = (Mathf.Sin(angle) + 1f) * 0.5f;
            uvs[i + 2] = new Vector2(u, v);
        }

        // Normalen berechnen
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i == 0) // Spitze
                normals[i] = Vector3.up;
            else if (i == 1) // Basismittelpunkt
                normals[i] = Vector3.down;
            else // Randpunkte
            {
                Vector3 dir = (vertices[i] - vertices[1]).normalized;
                normals[i] = dir;
            }
        }

        // Mesh zuweisen
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }
}

// Hilfsklasse zum einfachen Erstellen eines Cone GameObjects
public static class ConeCreator
{
    public static GameObject CreateCone(string name = "Cone", int segments = 16)
    {
        GameObject coneObject = new GameObject(name);
        
        // Mesh Filter und Renderer hinzufügen
        MeshFilter meshFilter = coneObject.AddComponent<MeshFilter>();
        meshFilter.mesh = ConeGenerator.CreateConeMesh(segments);
        
        MeshRenderer meshRenderer = coneObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Standard"));

        return coneObject;
    }
}