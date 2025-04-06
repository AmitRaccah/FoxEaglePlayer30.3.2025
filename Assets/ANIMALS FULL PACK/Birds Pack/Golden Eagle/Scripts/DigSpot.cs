using UnityEngine;
using StarterAssets.Fox; // Ensure FoxController is accessible

/// <summary>
/// DigSpot handles a designated digging area where the fox can dig.
/// When the fox is inside the trigger and the Left Control key is pressed,
/// this script starts a coroutine that waits for a specified delay before triggering
/// the dig animation and optionally spawning a found item.
/// </summary>
public class DigSpot : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Animator to trigger the dig success animation.")]
    public Animator animator;

    [Tooltip("The name of the animation trigger parameter in the Animator controller. " +
             "This is used to signal the Animator to play the dig animation. " +
             "Check your Animator Controller for the exact parameter name.")]
    public string digTriggerName = "DigSuccess";

    [Header("Dig Delay Settings")]
    [Tooltip("Time delay in seconds between the dig action and triggering the animation.")]
    public float digDelay = 2f;

    [Header("Found Item Settings (Optional)")]
    [Tooltip("Prefab of the item to spawn after a successful dig.")]
    public GameObject foundItemPrefab;

    [Tooltip("Spawn point for the found item.")]
    public Transform spawnPoint;

    // Flag to prevent multiple digs.
    private bool alreadyDug = false;

    private void OnTriggerStay(Collider other)
    {
        // If this spot has already been dug, do nothing.
        if (alreadyDug)
            return;

        // Check if the collider belongs to the fox character by verifying it has a FoxController.
        if (other.GetComponent<FoxController>() != null)
        {
            // Check if the Left Control key was pressed.
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                Debug.Log("Fox dug at the designated spot!");
                alreadyDug = true;
                StartCoroutine(DoDig());
            }
        }
    }

    private System.Collections.IEnumerator DoDig()
    {
        // Wait for the specified delay before triggering the animation.
        yield return new WaitForSeconds(digDelay);

        // Trigger the dig success animation if an animator is assigned.
        if (animator != null)
        {
            animator.SetTrigger(digTriggerName);
        }
        else
        {
            Debug.LogWarning("Animator not assigned in DigSpot script.");
        }

        // Optionally spawn the found item if both prefab and spawn point are set.
        if (foundItemPrefab != null && spawnPoint != null)
        {
            Instantiate(foundItemPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.Log("Found item prefab or spawn point not set in DigSpot script.");
        }
    }
}
