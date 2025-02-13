using UnityEngine;

public class ClockWithSeconds : MonoBehaviour
{
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;
    public float smoothSpeed = 5f; // Geschwindigkeit des Lerpens

    private Quaternion targetHourRotation;
    private Quaternion targetMinuteRotation;
    private Quaternion targetSecondRotation;

    void Start()
    {
        UpdateClock(true);
    }

    void Update()
    {
        UpdateClock(false);
        SmoothRotateHands();
    }

    void UpdateClock(bool instant)
    {
        // Hole die aktuelle Uhrzeit
        System.DateTime currentTime = System.DateTime.Now;

        // Berechne den Winkel für die Stunden-, Minuten- und Sekundenzeiger
        float hourAngle = currentTime.Hour * 30f + currentTime.Minute * 0.5f; // 360 Grad / 12 Stunden = 30 Grad pro Stunde
        float minuteAngle = currentTime.Minute * 6f + currentTime.Second * 0.1f; // 360 Grad / 60 Minuten = 6 Grad pro Minute
        float secondAngle = currentTime.Second * 6f; // 360 Grad / 60 Sekunden = 6 Grad pro Sekunde

        // Berechne die Zielrotationen
        targetHourRotation = Quaternion.Euler(0, 0, -hourAngle);
        targetMinuteRotation = Quaternion.Euler(0, 0, -minuteAngle);
        targetSecondRotation = Quaternion.Euler(0, 0, -secondAngle);

        if (instant)
        {
            // Setze die Zeiger sofort auf die Zielrotationen, ohne Lerp
            hourHand.localRotation = targetHourRotation;
            minuteHand.localRotation = targetMinuteRotation;
            secondHand.localRotation = targetSecondRotation;
        }
    }

    void SmoothRotateHands()
    {
        // Interpoliere die aktuellen Rotationen zu den Zielrotationen
        hourHand.localRotation = Quaternion.Lerp(hourHand.localRotation, targetHourRotation, Time.deltaTime * smoothSpeed);
        minuteHand.localRotation = Quaternion.Lerp(minuteHand.localRotation, targetMinuteRotation, Time.deltaTime * smoothSpeed);
        secondHand.localRotation = Quaternion.Lerp(secondHand.localRotation, targetSecondRotation, Time.deltaTime * smoothSpeed);
    }
}
