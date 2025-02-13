using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class AnimateHandOnInput : MonoBehaviour
{
    public InputActionProperty pinchAnimationAction;
    public InputActionProperty gripAnimationAction;

    public InputActionProperty thumbstickTouchAction; // Neue Zeile hinzugefügt
    public Animator handAnimator;

    // Update is called once per frame
    void Update()
    {
        float triggerValue = pinchAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Trigger", triggerValue);

        float gripValue = gripAnimationAction.action.ReadValue<float>();
        handAnimator.SetFloat("Grip", gripValue);

        // Neuer Abschnitt für die Thumbstick-Berührung
//        bool isThumbstickTouched = thumbstickTouchAction.action.ReadValue<float>() > 0.1f; // Hier musst du den Threshold anpassen
//        handAnimator.SetBool("ThumbstickTouch", isThumbstickTouched);
    }
}
