using UnityEngine;

public class FogTransition : MonoBehaviour
{
    public Color targetFogColor = Color.gray; // Ziel-Nebelfarbe
    public float transitionDuration = 2f; // Übergangsdauer in Sekunden

    private Color initialFogColor; // Anfangs-Nebelfarbe
    private Color previousTargetFogColor; // Vorherige Ziel-Nebelfarbe
    private bool isTransitioning = false; // Variable um zu verfolgen, ob bereits ein Übergang läuft
    private float transitionTimer = 0f; // Timer für den Übergang

    void Start()
    {
        initialFogColor = RenderSettings.fogColor; // Speichere die aktuelle Nebelfarbe zu Beginn
        previousTargetFogColor = targetFogColor; // Setze die vorherige Ziel-Nebelfarbe auf die aktuelle Ziel-Nebelfarbe
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTransitioning) // Wenn der Spieler den Collider betritt und kein Übergang läuft
        {
            initialFogColor = RenderSettings.fogColor; // Speichere die aktuelle Nebelfarbe zu Beginn
            //Debug.Log("Player entered the collider!"); // Konsolenausgabe
            StartTransition(); // Starte den Übergang
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isTransitioning) // Wenn der Spieler den Collider verlässt und ein Übergang läuft
        {
            //Debug.Log("Player exited the collider!"); // Konsolenausgabe
            StartTransition(); // Starte den Übergang zurück zur Anfangs-Nebelfarbe
        }
    }

    void Update()
    {
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime; // Aktualisiere den Übergangstimer

            // Berechne den Anteil des Übergangs, der abgeschlossen ist (0 bis 1)
            float transitionProgress = Mathf.Clamp01(transitionTimer / transitionDuration);

            // Lerp (lineares Interpolieren) zwischen der Anfangs- und der Ziel-Nebelfarbe basierend auf dem Fortschritt des Übergangs
            RenderSettings.fogColor = Color.Lerp(initialFogColor, targetFogColor, transitionProgress);

            // Wenn der Übergang abgeschlossen ist
            if (transitionProgress >= 1f)
            {
                isTransitioning = false; // Setze den Übergangsstatus auf "nicht mehr laufend"
            }
        }
    }

    void StartTransition()
    {
        if (targetFogColor != previousTargetFogColor)
        {
            initialFogColor = RenderSettings.fogColor; // Setze die aktuelle Nebelfarbe als Anfangs-Nebelfarbe
            previousTargetFogColor = targetFogColor; // Aktualisiere die vorherige Ziel-Nebelfarbe
        }

        isTransitioning = true; // Setze den Übergangsstatus auf "läuft"
        transitionTimer = 0f; // Setze den Timer auf Null zurück
    }
}
