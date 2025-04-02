using UnityEngine;
using StarterAssets;
using StarterAssets.Fox;

/// <summary>
/// Detects when an active ground character (Player, Fox, or Eagle) enters the zone,
/// and allows puzzle locks to be unlocked via a right-click raycast.
/// Only unlocks a lock if it is associated with this StandZone.
/// </summary>
public class StandZone : MonoBehaviour
{
    [Header("Puzzle Lock Settings")]
    [Tooltip("Layer mask for detecting puzzle locks.")]
    [SerializeField] private LayerMask puzzleLockLayer = ~0;

    [Header("Display Options")]
    [SerializeField] private bool showRay = true;
    [SerializeField] private Color rayColor = Color.red;
    [SerializeField] private float rayLength = 100f;

    private bool characterInZone = false;
    private PuzzleLock lastHighlightedLock = null;

    private void OnTriggerEnter(Collider other)
    {
        if (PuzzleUtils.IsActiveGround(other))
            characterInZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (PuzzleUtils.IsActiveGround(other))
        {
            characterInZone = false;
            ClearHighlight();
        }
    }

    private void Update()
    {
        if (!characterInZone)
            return;

        Camera activeCam = CameraSwitchManager.CurrentActiveCamera;
        if (activeCam == null)
            return;

        if (showRay)
            Debug.DrawRay(activeCam.transform.position, activeCam.transform.forward * rayLength, rayColor);

        if (Input.GetMouseButton(1)) // Right-click.
            ProcessRaycast(activeCam);
        else
            ClearHighlight();
    }

    /// <summary>
    /// Casts a ray from the screen center to detect PuzzleLock objects.
    /// Only processes a lock if its associated StandZone matches this StandZone.
    /// </summary>
    /// <param name="activeCam">The active camera used for raycasting.</param>
    private void ProcessRaycast(Camera activeCam)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = activeCam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, puzzleLockLayer))
        {
            PuzzleLock pLock = hit.collider.GetComponentInParent<PuzzleLock>();
            if (pLock != null)
            {
                // Only process the lock if its associated StandZone is this one.
                if (pLock.AssociatedStandZone != this)
                    return;

                pLock.SetHighlight(true);
                if (lastHighlightedLock != null && lastHighlightedLock != pLock)
                    lastHighlightedLock.SetHighlight(false);
                lastHighlightedLock = pLock;

                if (!pLock.IsUnlocked)
                    pLock.Unlock();
            }
        }
        else
        {
            ClearHighlight();
        }
    }

    private void ClearHighlight()
    {
        if (lastHighlightedLock != null)
        {
            lastHighlightedLock.SetHighlight(false);
            lastHighlightedLock = null;
        }
    }
}

