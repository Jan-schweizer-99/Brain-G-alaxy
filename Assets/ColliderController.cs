using UnityEngine;

public class ColliderController : MonoBehaviour
{
    public Animator[] animators; // Array von Animator-Objekten.
    public string targetTag; // Der String, der im Unity-Editor festgelegt werden kann.

    private void OnTriggerEnter(Collider other)
    {
        // Überprüfe, ob der Collider den gewünschten Tag hat.
        if (other.CompareTag(targetTag))
        {
            foreach (Animator animator in animators)
            {
                animator.SetBool("ColliderEntered", true); // Setze den Bool-Wert auf true für jeden Animator.
            }
            Debug.Log(targetTag + " entered the collider!"); // Konsolenausgabe.
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            foreach (Animator animator in animators)
            {
                animator.SetBool("ColliderEntered", false); // Setze den Bool-Wert auf false für jeden Animator.
            }
            Debug.Log(targetTag + " exited the collider!"); // Konsolenausgabe.
        }
    }
}
