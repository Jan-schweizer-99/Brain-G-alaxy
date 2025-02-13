using UnityEngine;
using UnityEngine.Events;

public class ColliderMonitor : MonoBehaviour
{
    public UnityEvent onEnterCollision; // Event, das ausgelöst wird, wenn der Collider in die Kollision eintritt
    public UnityEvent onExitCollision; // Event, das ausgelöst wird, wenn der Collider die Kollision verlässt

    private void OnTriggerEnter(Collider other)
    {
        GameObject parentObject = other.transform.parent != null ? other.transform.parent.gameObject : null;

        if (parentObject != null && parentObject.name == "ChainLink" && other.gameObject.name == "Sphere")
        {
            onEnterCollision?.Invoke();
            SetMeshAndLightActive(parentObject, false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject parentObject = other.transform.parent != null ? other.transform.parent.gameObject : null;

        if (parentObject != null && parentObject.name == "ChainLink" && other.gameObject.name == "Sphere")
        {
            onExitCollision?.Invoke();
            SetMeshAndLightActive(parentObject, true);
        }
    }

    private void SetMeshAndLightActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = active;
            }

            Light[] lights = obj.GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                light.enabled = active;
            }
        }
    }
}
