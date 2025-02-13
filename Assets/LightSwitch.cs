using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    public Light myLight;
    public bool isLightOn = false; // Public boolean variable to control the light state

    // Reference to the parent script
    private ParentLightSwitch parentLightSwitch;
    private bool isRegistered = false; // Flag to track registration status

    void Start()
    {
        // Find the parent script in the hierarchy only if not already registered
        if (!isRegistered)
        {
            parentLightSwitch = GetComponentInParent<ParentLightSwitch>();

            try
            {
                // Register this LightSwitch with the parent script
                parentLightSwitch.RegisterChildSwitch(this);
                isRegistered = true; // Set the flag to true after successful registration
            }
            catch (System.NullReferenceException)
            {
                // Handle the null reference exception without displaying an error message
                Debug.Log("ParentLightSwitch reference is null in the LightSwitch script.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null)
        {
            SetLightState(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null)
        {
            // Here, the light is no longer automatically turned off
            // myLight.enabled = false;
        }
    }

    public void ResetLight()
    {
        SetLightState(false);
    }

    public void SetLightState(bool newState)
    {
        isLightOn = newState;
        myLight.enabled = isLightOn;

        try
        {
            // Inform the parent script about the state change
            if (parentLightSwitch != null)
            {
                parentLightSwitch.ChildSwitchStateChanged(this, newState);
            }
        }
        catch (System.NullReferenceException)
        {
            // Handle the null reference exception without displaying an error message
            Debug.Log("ParentLightSwitch reference is null in the LightSwitch script.");
        }
    }
}
