using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class MarkdownFormatter : MonoBehaviour
{
    private string latexUrl = "https://latex.codecogs.com/png.latex?";
    public Vector2 position = new Vector2(0, 20); // Position über tmp
    private Texture2D formulaTexture;

    public string MarkdownToRichText(string markdown)
    {
        return markdown;
    }

    public string RemoveThinkingSections(string text)
    {
        return text;
    }

    public void ConvertFormulaToImage(string formula)
    {
        StartCoroutine(DownloadFormulaImage(formula));
    }

    private IEnumerator DownloadFormulaImage(string formula)
    {
        // URL vorbereiten
        string encodedFormula = UnityWebRequest.EscapeURL(formula);
        string fullUrl = latexUrl + encodedFormula;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Bild in Texture2D umwandeln
                formulaTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                
                // Hier können Sie das Texture auf Ihrem Canvas zeichnen
                // z.B. mit GUI.DrawTexture in OnGUI()
            }
            else
            {
                Debug.LogError("Fehler beim Laden der Formel: " + request.error);
            }
        }
    }

    void OnGUI()
    {
        if (formulaTexture != null)
        {
            // Zeichne das Formel-Bild über der tmp Komponente
            GUI.DrawTexture(
                new Rect(position.x, position.y, formulaTexture.width, formulaTexture.height),
                formulaTexture
            );
        }
    }
}