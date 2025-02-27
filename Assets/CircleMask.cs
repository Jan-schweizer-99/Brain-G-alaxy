using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class CircleMask : MonoBehaviour
{
    private Material maskMaterial;
    private SpriteRenderer spriteRenderer;
    
    [Range(0f, 1f)]
    public float radius = 0.5f; // Radius der Maske (0-1)
    
    [Range(0f, 1f)]
    public float smoothness = 0.01f; // Weichzeichnung der Kanten
    
    void OnEnable()
    {
        // Erstelle das Material, wenn es noch nicht existiert
        if (maskMaterial == null)
        {
            // Erstelle ein neues Material mit dem Custom Shader
            maskMaterial = new Material(Shader.Find("Custom/CircleMask"));
        }

        // Hole die Referenz zum SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Setze das Material
        if (spriteRenderer != null)
        {
            spriteRenderer.material = maskMaterial;
        }
    }

    void Update()
    {
        if (maskMaterial != null)
        {
            // Setze die Shader Properties
            maskMaterial.SetFloat("_CircleRadius", radius);
            maskMaterial.SetFloat("_Smoothness", smoothness);
        }
    }
}