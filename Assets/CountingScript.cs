using UnityEngine;
using TMPro;

public class CountingScript : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public AnimationCurve countingCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float countingDuration = 2f;
    public int maxCountValue = 100;

    private float timer;
    private bool countingUp = true;

    void Update()
    {
        // Prüfe, ob der Timer das Ende der Animation erreicht hat
        if (timer >= countingDuration)
        {
            countingUp = !countingUp; // Ändere die Richtung des Zählens
            timer = 0f;
        }

        // Berechne den Fortschritt basierend auf der AnimationCurve
        float progress = countingCurve.Evaluate(timer / countingDuration);

        // Zähle basierend auf der AnimationCurve hoch oder runter
        int countValue = countingUp ? Mathf.RoundToInt(progress * maxCountValue) : Mathf.RoundToInt((1 - progress) * maxCountValue);

        // Aktualisiere den TextMesh-Wert
        textMesh.text = countValue.ToString();

        // Aktualisiere den Timer
        timer += Time.deltaTime;
    }
}
