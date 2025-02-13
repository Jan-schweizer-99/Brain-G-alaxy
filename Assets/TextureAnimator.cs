using UnityEngine;

public class SeamlessSpriteAnimator : MonoBehaviour
{
    public float scrollSpeed = 0.5f; // Geschwindigkeit der Sprite-Bewegung

    private SpriteRenderer[] spriteRenderers;
    private float spriteWidth;

    void Start()
    {
        spriteRenderers = new SpriteRenderer[2];
        spriteRenderers[0] = CreateSpriteCopy();
        spriteRenderers[1] = CreateSpriteCopy();

        spriteWidth = spriteRenderers[0].bounds.size.x;

        // Setze den Wrap-Modus der Textur auf Repeat für beide Kopien
        foreach (var spriteRenderer in spriteRenderers)
        {
            spriteRenderer.material.mainTexture.wrapMode = TextureWrapMode.Repeat;
        }
    }

    SpriteRenderer CreateSpriteCopy()
    {
        GameObject copyObject = new GameObject("SpriteCopy");
        copyObject.transform.parent = transform;
        copyObject.transform.localPosition = new Vector3(spriteWidth, 0, 0);

        SpriteRenderer copyRenderer = copyObject.AddComponent<SpriteRenderer>();
        copyRenderer.sprite = GetComponent<SpriteRenderer>().sprite;

        return copyRenderer;
    }

    void Update()
    {
        // Texturverschiebung basierend auf der Zeit und der scrollSpeed
        float offset = Time.time * scrollSpeed;

        foreach (var spriteRenderer in spriteRenderers)
        {
            spriteRenderer.material.mainTextureOffset = new Vector2(offset, 0);
        }

        // Überprüfe, ob die beiden Sprites den sichtbaren Bereich verlassen
        float visibleLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        float visibleRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        if (transform.position.x + spriteWidth < visibleLeft)
        {
            // Verschiebe das erste Sprite nach rechts, um eine nahtlose Endlos-Animation zu erstellen
            transform.position = new Vector3(transform.position.x + spriteWidth, transform.position.y, transform.position.z);
        }
        else if (transform.position.x - spriteWidth > visibleRight)
        {
            // Verschiebe das zweite Sprite nach rechts, um eine nahtlose Endlos-Animation zu erstellen
            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.transform.position = new Vector3(spriteRenderer.transform.position.x + spriteWidth * 2, spriteRenderer.transform.position.y, spriteRenderer.transform.position.z);
            }
        }
    }
}
