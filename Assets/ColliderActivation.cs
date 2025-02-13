using UnityEngine;

public class ColliderActivation : MonoBehaviour
{
    public GameObject objectToActivate; // GameObject, das im Inspector festgelegt wird
    private CharacterController characterController; // Character Controller

    public ParentLightSwitch parentLightSwitch;
    private bool isActivated = false;

    
    private void Start()
    {
        // Suche den Character Controller in der gesamten Szene
        characterController = FindObjectOfType<CharacterController>();
        //parentLightSwitch = GetComponent<ParentLightSwitch>();
        //parentLightSwitch = GetComponent<ParentLightSwitch>();
        if (characterController == null)
        {
            Debug.LogError("Character Controller nicht gefunden. Stelle sicher, dass er in der Szene vorhanden ist.");
        }
    }

    private void Update()
    {
        // Ausgabe der Positionen
        //Debug.Log("Character Controller Position: " + characterController.transform.position);
        //Debug.Log("Object To Activate Position: " + transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Überprüfen, ob der Collider mit dem Character Controller kollidiert ist
        if (other.gameObject == characterController.gameObject && !isActivated)
        {
            Debug.Log("Collider überschneidet sich mit Character Controller.");
            ActivateObject();
            parentLightSwitch.RandomlyDeactivateActiveLights(3);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Überprüfen, ob der Collider mit dem Character Controller verlassen wurde
        if (other.gameObject == characterController.gameObject && isActivated)
        {
            Debug.Log("Collider nicht mehr mit Character Controller überschneidend.");
            isActivated = false;
        }
    }

    private void ActivateObject()
    {
        // Aktiviere das GameObject
        objectToActivate.SetActive(true);
        isActivated = true;
        Debug.Log("GameObject aktiviert.");
    }
}
