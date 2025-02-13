using UnityEngine;
using System.Collections;

public class OpacityController : MonoBehaviour
{
    public float nearDistance = 1f;
    public float farDistance = 5f;
    public float maxOpacity = 1f;
    public float minOpacity = 0f;
    public float transitionDuration = 1f;
    public Material targetMaterial;

    private Camera mainCamera;
    private Collider colliderToCheck;
    private bool isInCollider = false;
    private bool isFadingOut = false;

    void Start()
    {
        if (targetMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                targetMaterial = renderer.material;
            }
            else
            {
                Debug.LogError("Target Material not assigned in the Inspector and no renderer found on the object!");
                return;
            }
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found in the scene!");
        }

        colliderToCheck = GetComponentInParent<Collider>();

        if (colliderToCheck != null)
        {
            colliderToCheck.isTrigger = true;
        }
    }

    void Update()
    {
        if (!isInCollider || isFadingOut)
        {
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Please make sure the script is attached to the correct object.");
            return;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        float distance = Vector3.Distance(transform.position, cameraPosition);
        float opacity = Mathf.InverseLerp(nearDistance, farDistance, distance);
        opacity = Mathf.Clamp(opacity, minOpacity, maxOpacity);

        Color color = targetMaterial.color;
        color.a = opacity;
        targetMaterial.color = color;
    }

    void OnTriggerEnter(Collider other)
    {
        CharacterController characterController = other.GetComponent<CharacterController>();
        if (characterController != null)
        {
            isInCollider = true;
            if (isFadingOut)
            {
                return;
            }
            StopCoroutine("FadeOut");
        }
    }

    void OnTriggerExit(Collider other)
    {
        CharacterController characterController = other.GetComponent<CharacterController>();
        if (characterController != null)
        {
            isInCollider = false;
            if (!isFadingOut)
            {
                StartCoroutine("FadeOut");
            }
        }
    }

    IEnumerator FadeOut()
    {
        isFadingOut = true;

        float elapsedTime = 0f;
        Color startColor = targetMaterial.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < transitionDuration)
        {
            targetMaterial.color = Color.Lerp(startColor, endColor, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        targetMaterial.color = endColor;

        isFadingOut = false;
    }
}
