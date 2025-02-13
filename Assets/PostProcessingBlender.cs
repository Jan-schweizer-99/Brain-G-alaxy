using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingBlender : MonoBehaviour
{
    public float blendSpeed = 0.5f; // Geschwindigkeit der Gewichtsblendung
    private float targetWeight = 0.0f; // Zielgewicht für das Volume
    private float currentWeight = 0.0f; // Aktuelles Gewicht des Volume

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Überprüfung, ob der Collider mit dem Player getroffen wurde
        {
            targetWeight = 1.0f; // Setze das Zielgewicht auf voll (1.0)
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // Überprüfung, ob der Collider mit dem Player verlassen wurde
        {
            targetWeight = 0.0f; // Setze das Zielgewicht auf 0 (kein Einfluss des Volumes)
        }
    }

    private void Update()
    {
        // Smooth-Blend des aktuellen Gewichts zum Zielgewicht
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, blendSpeed * Time.deltaTime);

        // Hier kannst du das aktuelle Gewicht auf das Volume anwenden
        // Beispiel: GetComponent<Volume>().weight = currentWeight;
    }
}
