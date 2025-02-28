using UnityEngine;

public class CanvasAlignment : MonoBehaviour
{
    private Camera eventCamera;
    private Canvas canvas;

    void Start()
    {
        // Suchen Sie die Event Camera, zum Beispiel die Main Camera
        eventCamera = Camera.main;

        // Zugriff auf das Canvas-Komponente
        canvas = GetComponent<Canvas>();

        if (eventCamera == null)
        {
            Debug.LogError("Event Camera not found. Make sure your camera is tagged as MainCamera.");
        }
        else if (canvas == null)
        {
            Debug.LogError("Canvas component not found on this GameObject.");
        }
    }

    void Update()
    {
        if (eventCamera != null && canvas != null)
        {
            // Richten Sie das Canvas immer zur Event Camera aus
            Vector3 lookAtPoint = transform.position + eventCamera.transform.rotation * Vector3.forward;
            Vector3 upDirection = eventCamera.transform.rotation * Vector3.up;
            canvas.transform.LookAt(lookAtPoint, upDirection);
        }
    }
}
