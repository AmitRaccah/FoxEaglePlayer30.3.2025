using UnityEngine;

/// <summary>
/// A specialized PuzzleLock that unlocks when a specified pickup object is placed
/// close enough to its stand (or socket).
/// </summary>
public class SwitchLock : PuzzleLock
{
    [Header("Switch Lock Settings")]
    [Tooltip("Transform indicating the exact position/orientation on the stand where the switch should snap.")]
    [SerializeField] private Transform standPoint;

    [Tooltip("The movable switch object that the player can pick up and drop.")]
    [SerializeField] private GameObject switchPickup;

    [Tooltip("Defines how close the switchPickup must be to standPoint to snap in place.")]
    [SerializeField] private float snapRadius = 1.0f;

    [Tooltip("Optional: a visual representation of the switch after it's placed in the stand (e.g., a 'locked-in' version).")]
    [SerializeField] private GameObject standSwitchVisual;

    [Tooltip("If true, the switch lock can only snap if the item is no longer held by the player. " +
             "This checks if the pickup's Rigidbody is not isKinematic.")]
    [SerializeField] private bool requireDropBeforeSnap = true;

    // Internal flag to avoid multiple unlock calls
    private bool isPlaced = false;

    private void Awake()
    {
        // Optionally, if you want the stand's visual to be hidden initially
        // you can ensure standSwitchVisual is disabled at start:
        if (standSwitchVisual != null)
        {
            standSwitchVisual.SetActive(false);
        }
    }

    private void Update()
    {
        // If this lock has already been unlocked or we've already placed the switch, do nothing
        if (IsUnlocked || isPlaced) return;

        // Basic validation checks
        if (switchPickup == null || standPoint == null) return;

        // Retrieve the Rigidbody to check if the item is dropped (non-kinematic)
        Rigidbody rb = switchPickup.GetComponent<Rigidbody>();
        if (rb == null) return;

        // If we require the item to be dropped, ensure rb.isKinematic == false 
        // or handle the logic in your own way
        if (requireDropBeforeSnap && rb.isKinematic)
        {
            return; // The item is still being held, skip snapping
        }

        // Check distance from the pickup to the stand point
        float distanceToStand = Vector3.Distance(switchPickup.transform.position, standPoint.position);
        if (distanceToStand <= snapRadius)
        {
            // Snap the switchPickup to the stand
            SnapSwitchToStand();

            // Mark this puzzle lock as solved
            Unlock();  // inherited from PuzzleLock, calls puzzleManager.OnLockUnlocked(this)
        }
    }

    /// <summary>
    /// Moves the switchPickup to the standPoint and optionally disables it, 
    /// showing an alternative 'locked-in' visual instead.
    /// </summary>
    private void SnapSwitchToStand()
    {
        // Mark the local state
        isPlaced = true;

        // Position and rotate the switchPickup at the standPoint
        switchPickup.transform.SetParent(standPoint);
        switchPickup.transform.localPosition = Vector3.zero;
        switchPickup.transform.localRotation = Quaternion.identity;

        // Optionally, make it kinematic again, or disable its collider, etc.
        Rigidbody rb = switchPickup.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Hide the original pickup object if desired
        // (In some cases, you might want to keep it visible; adapt as needed)
        switchPickup.SetActive(false);

        // Show the stand's locked-in version, if provided
        if (standSwitchVisual != null)
        {
            standSwitchVisual.SetActive(true);
        }

        // Optionally play an animation or SFX here (e.g., "SwitchDown" animation)
        // Animator anim = standSwitchVisual?.GetComponent<Animator>();
        // if (anim != null) anim.SetTrigger("SwitchDown");
    }
}
