using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChainBugBlocker : MonoBehaviour
{
    private Transform[] chainLinks;
    private float comparisonValue = 1.7f;

    void Start()
    {
        // Sucht nach allen ChainLink-Elementen als Kinder dieses GameObjects
        chainLinks = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            chainLinks[i] = transform.GetChild(i);
        }

        ChainBugBlockerManager.OnControllerTrackingChanged += HandleControllerTrackingChanged;
        ChainBugBlockerManager.Instance.SetControllerTrackingEnabled(true); // Aktiviere das Tracking zu Beginn
    }

    void OnDestroy()
    {
        ChainBugBlockerManager.OnControllerTrackingChanged -= HandleControllerTrackingChanged;
    }

    void HandleControllerTrackingChanged(bool enabled)
    {
        // Aktiviere/Deaktiviere das Tracking der Controller
        ActionBasedController leftControllerScript = GameObject.FindGameObjectWithTag("LeftControllerTag")?.GetComponent<ActionBasedController>();
        ActionBasedController rightControllerScript = GameObject.FindGameObjectWithTag("RightControllerTag")?.GetComponent<ActionBasedController>();

        if (leftControllerScript != null)
        {
            leftControllerScript.enableInputTracking = enabled;
        }

        if (rightControllerScript != null)
        {
            rightControllerScript.enableInputTracking = enabled;
        }

        // Überprüft die Kettenlänge nur, wenn das Tracking aktiviert ist
        if (enabled)
        {
            UpdateChainLength();
        }
    }

    void UpdateChainLength()
    {
        // Berechnet die Live-Gesamtlänge der Kette
        float totalLength = CalculateTotalLength();
        Debug.Log("Live-Gesamtlänge der Kette: " + totalLength);

        // Überprüft, ob die Kettenlänge den Vergleichswert erreicht oder überschreitet und aktualisiert den Manager
        if (totalLength >= comparisonValue)
        {
            ChainBugBlockerManager.Instance.SetControllerTrackingEnabled(false);
            Debug.Log("Controller deaktiviert!");
        }
        else
        {
            ChainBugBlockerManager.Instance.SetControllerTrackingEnabled(true);
            //Debug.Log("Controller aktiviert!");
        }
    }

    float CalculateTotalLength()
    {
        float totalLength = 0f;

        for (int i = 0; i < chainLinks.Length - 1; i++)
        {
            // Berücksichtigt die Verschiebung des Aufhängungspunkts in der Y-Richtung
            totalLength += Vector3.Distance(chainLinks[i].position, chainLinks[i + 1].position);
        }

        return totalLength;
    }
}
