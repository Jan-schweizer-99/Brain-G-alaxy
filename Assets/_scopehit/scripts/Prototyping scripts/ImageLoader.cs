using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class URLImageLoader : MonoBehaviour 
{
    public string imageUrl = "";
    private SpriteRenderer spriteRenderer;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void OnProjectLoadedInEditor()
    {
        EditorApplication.delayCall += () => {
            var loaders = FindObjectsOfType<URLImageLoader>();
            foreach (var loader in loaders)
            {
                if (!string.IsNullOrEmpty(loader.imageUrl))
                {
                    loader.EditorLoadImage();
                }
            }
        };
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(imageUrl))
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            EditorLoadImage();
        }
    }

    private void EditorLoadImage()
    {
        var request = UnityWebRequestTexture.GetTexture(imageUrl);
        request.SendWebRequest();

        EditorApplication.update += EditorUpdate;

        void EditorUpdate()
        {
            if (request.isDone)
            {
                EditorApplication.update -= EditorUpdate;
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    var sprite = Sprite.Create(texture, 
                        new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f));
                    spriteRenderer.sprite = sprite;
                    EditorUtility.SetDirty(gameObject);
                }
                
                request.Dispose();
            }
        }
    }
    #endif

    void Start()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(LoadImageFromURL());
        }
    }

    IEnumerator LoadImageFromURL()
    {
        if (string.IsNullOrEmpty(imageUrl)) yield break;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                var sprite = Sprite.Create(texture, 
                    new Rect(0, 0, texture.width, texture.height), 
                    new Vector2(0.5f, 0.5f));
                spriteRenderer.sprite = sprite;
            }
        }
    }
}