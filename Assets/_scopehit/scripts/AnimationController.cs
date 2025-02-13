using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator[] animators;
    public AnimationClip[] animationClips;
    private bool isPlayingForward = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayAnimations();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ReverseAnimations();
        }
    }

    private void PlayAnimations()
    {
        foreach (Animator animator in animators)
        {
            foreach (AnimationClip clip in animationClips)
            {
                animator.Play(clip.name, 0, 0f); // Start playing the animation from the beginning
            }
        }
        isPlayingForward = true; // Ensure that the animations play forward initially
    }

    private void ReverseAnimations()
    {
        foreach (Animator animator in animators)
        {
            foreach (AnimationClip clip in animationClips)
            {
                animator.Play(clip.name, 0, 1f); // Start playing the animation from the end
                animator.SetFloat("speed", -1f); // Set the playback speed to reverse
            }
        }
        isPlayingForward = false; // Update the playback direction flag
    }
}
