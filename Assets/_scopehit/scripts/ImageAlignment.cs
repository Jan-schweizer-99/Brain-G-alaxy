using UnityEngine;

public class ImageAlignment : MonoBehaviour
{
    public bool lockRotationX = false;
    public bool lockRotationY = false;
    public bool lockRotationZ = false;

    private Camera eventCamera;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Suchen Sie die Event Camera, zum Beispiel die Main Camera
        eventCamera = Camera.main;

        // Zugriff auf den SpriteRenderer-Komponente
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (eventCamera == null)
        {
            Debug.LogError("Event Camera not found. Make sure your camera is tagged as MainCamera.");
        }
        else if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on this GameObject.");
        }
    }

    void Update()
    {
        if (eventCamera != null && spriteRenderer != null)
        {
            // Richten Sie das Bild immer zur Event Camera aus
            Vector3 lookAtPoint = transform.position + eventCamera.transform.rotation * Vector3.forward;
            Vector3 upDirection = eventCamera.transform.rotation * Vector3.up;
            
            // Sperre die Rotation um bestimmte Achsen
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - spriteRenderer.transform.position, upDirection);
            if (lockRotationX)
            {
                targetRotation.eulerAngles = new Vector3(spriteRenderer.transform.rotation.eulerAngles.x, targetRotation.eulerAngles.y, targetRotation.eulerAngles.z);
            }
            if (lockRotationY)
            {
                targetRotation.eulerAngles = new Vector3(targetRotation.eulerAngles.x, spriteRenderer.transform.rotation.eulerAngles.y, targetRotation.eulerAngles.z);
            }
            if (lockRotationZ)
            {
                targetRotation.eulerAngles = new Vector3(targetRotation.eulerAngles.x, targetRotation.eulerAngles.y, spriteRenderer.transform.rotation.eulerAngles.z);
            }

            spriteRenderer.transform.rotation = targetRotation;
        }
    }
}
