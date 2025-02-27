using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldAlignRotation : MonoBehaviour
{
    private Quaternion initialRotation;
    private bool hasAligned = false;

#if UNITY_EDITOR
    // Diese Methode wird im Editor aufgerufen
    private void OnValidate()
    {
        if (!hasAligned)
        {
            AlignToWorldY();
            hasAligned = true;
        }
    }

    // Diese Methode wird im Editor aufgerufen, wenn das Objekt ausgewählt ist
    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // Sperre die Y-Rotation im Editor
        if (transform.rotation.eulerAngles.y != 0)
        {
            Vector3 currentEulerAngles = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentEulerAngles.x, 0f, currentEulerAngles.z);
        }
    }
#endif

    // Wird aufgerufen, bevor der erste Frame aktualisiert wird im Play-Modus
    void Start()
    {
        // Speichere die aktuelle Rotation des GameObjects
        initialRotation = transform.rotation;
        
        // Richte die Y-Achse an der Weltausrichtung aus
        AlignToWorldY();
        
        Debug.Log("GameObject wurde an der Weltausrichtung auf der Y-Achse ausgerichtet.");
    }
    
    // Methode zum Ausrichten der Y-Achse an der Weltausrichtung
    void AlignToWorldY()
    {
        // Erstelle eine neue Rotation, die nur die Y-Achse auf weltausrichtung setzt
        // und die X und Z Rotation beibehält
        Vector3 currentEulerAngles = transform.rotation.eulerAngles;
        
        // Setze die Y-Rotation auf 0 (Weltausrichtung)
        Vector3 newRotation = new Vector3(currentEulerAngles.x, 0f, currentEulerAngles.z);
        
        // Wende die neue Rotation an
        transform.rotation = Quaternion.Euler(newRotation);
    }
    
    // Optional: Methode zum Zurücksetzen der ursprünglichen Rotation
    public void ResetRotation()
    {
        transform.rotation = initialRotation;
        Debug.Log("Rotation wurde zurückgesetzt.");
        hasAligned = false;
    }
}