using UnityEngine;

/// <summary>
/// Place this on a trigger collider.  
/// When an object with the given tag enters, raises a named story event once.
/// </summary>
[RequireComponent(typeof(Collider))]
public class StoryEventTrigger : MonoBehaviour
{
    [Tooltip("Tag that can activate the trigger (usually 'Player').")]
    [SerializeField] private string activatingTag = "Player";

    [Tooltip("Exact event name to raise through StoryEventChannel.")]
    [SerializeField] private string storyEvent = "EagleReveal";

    private bool _fired = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_fired || !other.CompareTag(activatingTag)) return;

        _fired = true;
        StoryEventChannel.Raise(storyEvent);
    }
}
