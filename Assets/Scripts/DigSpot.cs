using UnityEngine;
using StarterAssets.Fox; // Ensure FoxController is accessible
using System.Collections;

/// <summary>
/// DigSpotLegacy handles a designated dig area using the Legacy Animation system.
/// When a fox (with FoxController) is inside the trigger and presses Left Control,
/// the script waits for a delay, repositions an existing found item at a specified spawn point,
/// and plays a legacy animation (e.g., a pop-up) on that item.
/// </summary>
public class DigSpotLegacy : MonoBehaviour
{
    [Header("Dig Settings")]
    [Tooltip("Time delay (in seconds) between digging and triggering the found item animation.")]
    public float digDelay = 2f;

    [Header("Found Item Settings")]
    [Tooltip("The found item in the scene that will be animated. (Do not use a prefab; assign the actual instance.)")]
    public GameObject foundItem;

    [Tooltip("The transform that defines the spawn position and rotation for the found item.")]
    public Transform spawnPoint;

    [Header("Legacy Animation Settings")]
    [Tooltip("Name of the legacy animation clip to play on the found item.")]
    public string popUpAnimationName = "PopUp";

    // Flag to prevent multiple digs.
    private bool alreadyDug = false;

    private void OnTriggerStay(Collider other)
    {
        if (alreadyDug)
            return;

        // Check if the collider belongs to the fox (using FoxController as identifier).
        if (other.GetComponent<FoxController>() != null)
        {
            // Check if the Left Control key was pressed.
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                alreadyDug = true;
                StartCoroutine(DoDig());
            }
        }
    }

    private IEnumerator DoDig()
    {
        // Wait for the specified delay.
        yield return new WaitForSeconds(digDelay);

        if (foundItem == null || spawnPoint == null)
        {
            yield break;
        }

        // Reposition the found item at the spawn point.
        foundItem.transform.position = spawnPoint.position;
        foundItem.transform.rotation = spawnPoint.rotation;

        // Optionally, activate the found item if it was inactive.
        if (!foundItem.activeSelf)
        {
            foundItem.SetActive(true);
        }

        // Retrieve the Legacy Animation component from the found item.
        Animation legacyAnimation = foundItem.GetComponent<Animation>();
        if (legacyAnimation != null)
        {
            Debug.Log("Found Animation component, attempting to play: " + popUpAnimationName);
            if (legacyAnimation.GetClip(popUpAnimationName) != null)
            {
                legacyAnimation.Play(popUpAnimationName);
                Debug.Log("Playing legacy animation: " + popUpAnimationName);
            }
            else
            {
                Debug.LogWarning("Animation clip '" + popUpAnimationName + "' not found in the Animation component. " +
                                   "Ensure you have assigned the clip in the Animation component on your found item.");
            }
        }
        else
        {
            Debug.LogWarning("No Animation component found on the found item. " +
                             "Ensure the found item has a Legacy Animation component with the proper clip assigned.");
        }
    }
}
