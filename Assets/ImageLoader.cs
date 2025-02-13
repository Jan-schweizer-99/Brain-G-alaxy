using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class ImageLoader : MonoBehaviour
{
    public string imageUrl = "https://static-cdn.jtvnw.net/badges/v1/d1d1ad54-40a6-492b-882e-dcbdce5fa81e/3";
    public RawImage image;

    void Start()
    {
        StartCoroutine(LoadImage());
    }

    IEnumerator LoadImage()
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Laden Sie das heruntergeladene Bild in eine Texture
                Texture2D texture = DownloadHandlerTexture.GetContent(www);

                // Weisen Sie die Texture dem RawImage auf dem Canvas zu
                image.texture = texture;
            }
            else
            {
                Debug.LogError("Fehler beim Laden des Bildes: " + www.error);
            }
        }
    }
}
