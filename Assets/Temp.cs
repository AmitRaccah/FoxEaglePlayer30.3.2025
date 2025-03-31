using UnityEngine;

/// <summary>
/// Ensures that there is only one active AudioListener in the scene.
/// Attach this script to the primary GameObject (e.g., your Main Camera)
/// that should have the active AudioListener. It will automatically disable any additional AudioListeners.
/// </summary>
public class UniqueAudioListener : MonoBehaviour
{
    private void Awake()
    {
        // Get all AudioListener components in the scene.
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();

        // If there is more than one, disable all but the one attached to this GameObject.
        if (listeners.Length > 1)
        {
            foreach (AudioListener listener in listeners)
            {
                // Skip the AudioListener on this GameObject.
                if (listener.gameObject != gameObject)
                {
                    Debug.LogWarning("Disabling extra AudioListener on: " + listener.gameObject.name);
                    listener.enabled = false;
                }
            }
        }
    }
}
