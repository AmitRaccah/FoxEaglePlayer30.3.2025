using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the visibility of the search reticle UI element based on right-click input.
/// </summary>
public class SearchReticleUI : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Reference to the reticle image to display when searching.")]
    [SerializeField] private Image reticleImage;

    /// <summary>
    /// Initializes the reticle state at startup.
    /// </summary>
    private void Start()
    {
        if (reticleImage != null)
        {
            // Hide the reticle initially.
            reticleImage.enabled = false;
        }
        else
        {
            Debug.LogWarning("Reticle image is not assigned.", this);
        }
    }

    /// <summary>
    /// Checks for right-click input each frame and updates the reticle visibility.
    /// </summary>
    private void Update()
    {
        if (reticleImage == null)
            return;

        // The right mouse button (index 1) is held down.
        bool isRightClickHeld = Input.GetMouseButton(1);

        // Set the reticle's visibility based on the input.
        reticleImage.enabled = isRightClickHeld;
    }
}
