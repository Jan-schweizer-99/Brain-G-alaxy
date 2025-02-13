using UnityEngine;

public class LampeBlitz : MonoBehaviour
{
    public Light lampe;
    public float blitzDauer = 0.1f;
    public float erhoeheIntensitaet = 2.0f;
    public AnimationCurve anfangsKurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve abflussKurve = AnimationCurve.Linear(0, 1, 1, 0);
    private float urspruenglicheIntensitaet;

    void Start()
    {
        if (lampe == null)
        {
            lampe = GetComponent<Light>();
        }

        if (lampe != null)
        {
            urspruenglicheIntensitaet = lampe.intensity;
            InvokeRepeating("Blitzeffekt", 5f, 5f);
        }
        else
        {
            Debug.LogError("Keine Lampe gefunden! Weise eine Lampe dem Skript zu.");
        }
    }

    void Blitzeffekt()
    {
        StartCoroutine(BlitzRoutine());
    }

    System.Collections.IEnumerator BlitzRoutine()
    {
        float time = 0f;

        while (time < blitzDauer)
        {
            float progress = time / blitzDauer;
            float intensityMultiplier = erhoeheIntensitaet * anfangsKurve.Evaluate(progress);
            lampe.intensity = urspruenglicheIntensitaet + intensityMultiplier;

            yield return null;
            time += Time.deltaTime;
        }

        // Warte kurz am Ende der Blitzdauer, bevor die Intensität wieder abnimmt
        yield return new WaitForSeconds(0.1f);

        time = 0f;

        while (time < blitzDauer)
        {
            float progress = time / blitzDauer;
            float intensityMultiplier = erhoeheIntensitaet * abflussKurve.Evaluate(progress);
            lampe.intensity = urspruenglicheIntensitaet + intensityMultiplier;

            yield return null;
            time += Time.deltaTime;
        }

        // Setze die Intensität auf die ursprüngliche Intensität zurück
        lampe.intensity = urspruenglicheIntensitaet;
    }
}
