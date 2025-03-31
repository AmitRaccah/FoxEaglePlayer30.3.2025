using UnityEngine;

/// <summary>
/// Manages the state of a puzzle lock, including highlight and unlock logic.
/// Notifies the PuzzleManager and (optionally) a ParallaxPuzzleController when unlocked.
/// </summary>
public class PuzzleLock : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("Indicates whether this lock is unlocked.")]
    [SerializeField] private bool isUnlocked = false;
    [Tooltip("Reference to the PuzzleManager that handles this lock.")]
    [SerializeField] private PuzzleManager puzzleManager;

    [Header("Association")]
    [Tooltip("The StandZone that this lock belongs to.")]
    [SerializeField] private StandZone associatedStandZone;

    [Header("Parallax Controller (Optional)")]
    [SerializeField] private ParallaxPuzzleController parallaxPuzzleController;

    [Header("Highlight Effects")]
    [SerializeField] private ParticleSystem highlightParticles;

    [Header("Door Shape Settings")]
    [SerializeField] private GameObject doorShapeUnsolved;
    [SerializeField] private GameObject doorShapeSolved;

    /// <summary>
    /// Indicates whether this lock is unlocked.
    /// </summary>
    public bool IsUnlocked => isUnlocked;

    /// <summary>
    /// Gets the unsolved door shape.
    /// </summary>
    public GameObject DoorShapeUnsolved => doorShapeUnsolved;

    /// <summary>
    /// Gets the solved door shape.
    /// </summary>
    public GameObject DoorShapeSolved => doorShapeSolved;

    /// <summary>
    /// Gets the associated StandZone.
    /// </summary>
    public StandZone AssociatedStandZone => associatedStandZone;

    private void Awake()
    {
        // If a parallax controller wasn't assigned, try to auto-assign from the parent.
        if (parallaxPuzzleController == null)
        {
            parallaxPuzzleController = GetComponentInParent<ParallaxPuzzleController>();
        }
    }

    /// <summary>
    /// Resets the lock to its locked state.
    /// </summary>
    public void ResetLock()
    {
        isUnlocked = false;
    }

    /// <summary>
    /// Unlocks the lock and notifies the PuzzleManager and ParallaxPuzzleController if assigned.
    /// </summary>
    public void Unlock()
    {
        if (isUnlocked)
            return;

        isUnlocked = true;
        SetHighlight(false);

        if (puzzleManager != null)
            puzzleManager.OnLockUnlocked(this);
        else
            Debug.LogWarning($"{name} has no PuzzleManager assigned.");

        if (parallaxPuzzleController != null)
            parallaxPuzzleController.EnterCorrectZone();
    }

    /// <summary>
    /// Sets the highlight state of this lock.
    /// </summary>
    /// <param name="highlight">True to enable highlight; false to disable.</param>
    public void SetHighlight(bool highlight)
    {
        if (highlightParticles != null)
        {
            if (highlight)
            {
                if (!highlightParticles.gameObject.activeSelf)
                    highlightParticles.gameObject.SetActive(true);
                highlightParticles.Play();
            }
            else
            {
                highlightParticles.Stop();
            }
        }
        else
        {
            Debug.LogWarning($"No ParticleSystem assigned on {name}");
        }
    }
}
