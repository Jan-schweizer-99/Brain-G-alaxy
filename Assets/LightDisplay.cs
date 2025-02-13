using UnityEngine;
using TMPro;

public class LightDisplay : MonoBehaviour
{
    public ParentLightSwitch parentLightSwitch; // Referenz auf das ParentLightSwitch-Skript

    private TextMeshProUGUI lightsText;

    private EventSystem eventSystem;
    private float normalizedPosition;

    void Start()
    {
        // Finde das TextMeshPro-Objekt auf dem Canvas
        lightsText = GetComponentInChildren<TextMeshProUGUI>();
        eventSystem = EventSystem.Instance;
        normalizedPosition = eventSystem.LastNormalizedPosition;

        if (parentLightSwitch == null)
        {
            // Falls die Referenz nicht zugewiesen ist, versuche sie zu finden
            parentLightSwitch = FindObjectOfType<ParentLightSwitch>();
        }

        if (parentLightSwitch != null)
        {
            // Registriere den Listener für Änderungen der Lichterzahl
            parentLightSwitch.OnLightsChanged += UpdateLightsText;
            // Aktualisiere den Text zu Beginn
            UpdateLightsText();
        }
        else
        {
            Debug.LogError("ParentLightSwitch reference not assigned or found.");
        }
    }

    // Methode zur Aktualisierung des Texts basierend auf der aktuellen Lichterzahl
    void UpdateLightsText()
    {
        if (parentLightSwitch.NumberOfActiveLights == 0 && normalizedPosition >= 1) {
            lightsText.text = "Search for Cukoo";
        }
        else 
        {
            lightsText.text = parentLightSwitch.NumberOfActiveLights + "/46";
        }
    }
    void Update()
    {
        normalizedPosition = eventSystem.LastNormalizedPosition;
    }

    void OnDestroy()
    {
        // Wichtig: Den Listener beim Zerstören des Objekts entfernen, um Lecks zu vermeiden
        if (parentLightSwitch != null)
        {
            parentLightSwitch.OnLightsChanged -= UpdateLightsText;
        }
    }
}
