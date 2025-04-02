using UnityEngine;
using StarterAssets;
using StarterAssets.Fox;

/// <summary>
/// Detects when an active ground character (Player, Fox, or Eagle) enters the zone,
/// and optionally handles puzzle lock logic.
/// </summary>
public class CorrectZoneTrigger : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [Tooltip("Reference to a PuzzleLock for optional puzzle integration.")]
    [SerializeField] private PuzzleLock puzzleLock;

    private void Start()
    {
        // If no PuzzleLock is assigned, no additional action is taken.
    }

    private void OnTriggerEnter(Collider other)
    {
        if (PuzzleUtils.IsActiveGround(other))
        {
            // Optionally: handle logic when an active character enters.
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (PuzzleUtils.IsActiveGround(other))
        {
            // Optionally: handle logic when an active character exits.
        }
    }
}

