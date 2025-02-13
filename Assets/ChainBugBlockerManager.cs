using UnityEngine;

public class ChainBugBlockerManager : MonoBehaviour
{
    public static ChainBugBlockerManager Instance;

    public delegate void ControllerTrackingChangedDelegate(bool enabled);
    public static event ControllerTrackingChangedDelegate OnControllerTrackingChanged;

    void Awake()
    {
        // Singleton-Pattern, um sicherzustellen, dass nur eine Instanz existiert
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetControllerTrackingEnabled(bool enabled)
    {
        // Rufe diese Methode auf, um den Tracking-Status aller Controller zu aktualisieren
        OnControllerTrackingChanged?.Invoke(enabled);
    }
}
