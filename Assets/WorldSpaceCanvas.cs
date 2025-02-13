using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class WorldSpaceCanvas : MonoBehaviour
{
    public string websiteURL = "https://deine-website.de";
    public RawImage rawImage;

    void Start()
    {
        if (rawImage == null)
        {
            Debug.LogError("RawImage not assigned in the Unity Editor. Please assign it.");
            return; // Stop execution if rawImage is not assigned.
        }

        StartCoroutine(LoadWebsite());
    }

    IEnumerator LoadWebsite()
    {
        UnityWebRequest www = UnityWebRequest.Get(websiteURL);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            // Dein Code hier, um den HTML-Inhalt zu verarbeiten
        }
        else
        {
            Debug.LogError("Fehler beim Laden der Website: " + www.error);
        }

        www.Dispose();
    }
}
