using UnityEngine;

public class HideMeshOnCollision : MonoBehaviour
{
    public GameObject meshToHide;
    public BoxCollider collisionCollider;

    private void Start()
    {
        if (meshToHide == null)
        {
            Debug.LogError("Mesh nicht zugewiesen!");
            return;
        }

        if (collisionCollider == null)
        {
            Debug.LogError("Box Collider nicht zugewiesen!");
            return;
        }
    }

    private void Update()
    {
        // Überprüfe im Update, ob das Mesh mit dem Collider kollidiert
        CheckCollision();
    }

    private void CheckCollision()
    {
        // Ignoriere den Collider des leeren GameObjects
        if (collisionCollider == null)
        {
            Debug.LogError("Box Collider nicht gefunden!");
            return;
        }

        // Ignoriere den Collider des zu versteckenden Meshes
        Collider meshCollider = meshToHide.GetComponent<Collider>();
        if (meshCollider == null)
        {
            Debug.LogError("Mesh Collider nicht gefunden!");
            return;
        }

        // Überprüfe, ob das Mesh mit dem Collider kollidiert
        if (Physics.CheckBox(collisionCollider.bounds.center, collisionCollider.bounds.extents, collisionCollider.transform.rotation, LayerMask.GetMask("Default")))
        {
            // Deaktiviere das Rendering des Meshes, wenn es mit dem Collider kollidiert
            meshToHide.GetComponent<Renderer>().enabled = false;
        }
        else
        {
            // Aktiviere das Rendering des Meshes, wenn es den Collider verlässt
            meshToHide.GetComponent<Renderer>().enabled = true;
        }
    }
}
