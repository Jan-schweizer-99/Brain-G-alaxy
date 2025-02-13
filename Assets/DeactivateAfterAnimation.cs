using UnityEngine;

public class DeactivateAfterAnimation : MonoBehaviour
{
    private Animation animationComponent;

    void Start()
    {
        // Holen Sie sich die Referenz auf die Animation-Komponente
        animationComponent = GetComponent<Animation>();

        // Fügen Sie einen Event-Listener hinzu, der aufgerufen wird, wenn die Animation beendet ist
        animationComponent.clip.events = new AnimationEvent[] { new AnimationEvent { time = animationComponent.clip.length, functionName = "OnAnimationEnded" } };
    }

    void OnAnimationEnded()
    {
        // Deaktivieren Sie das GameObject
        gameObject.SetActive(false);
    }
}
