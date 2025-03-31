using UnityEngine;
using StarterAssets;
using StarterAssets.Fox;

/// <summary>
/// Utility class for common puzzle-related helper methods.
/// </summary>
public static class PuzzleUtils
{
    /// <summary>
    /// Determines if the collider belongs to an active ground character (Player, Fox, or Eagle).
    /// </summary>
    public static bool IsActiveGround(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponentInParent<ThirdPersonController>();
            return (controller != null && controller.enabled);
        }
        if (other.CompareTag("Fox"))
        {
            var controller = other.GetComponentInParent<FoxController>();
            return (controller != null && controller.enabled);
        }
        if (other.CompareTag("Eagle"))
        {
            var controller = other.GetComponentInParent<EagleAlwaysAirController>();
            return (controller != null && controller.enabled);
        }
        return false;
    }
}
