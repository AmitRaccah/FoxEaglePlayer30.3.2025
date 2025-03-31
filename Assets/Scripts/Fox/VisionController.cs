using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;
using StarterAssets.Fox;

public class UnifiedVisionController : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("Unique player ID for this character (e.g., 2 for Fox, 3 for Eagle).")]
    public int playerID = 2;

    [Header("Vision Overlay References")]
    [Tooltip("Overlay camera for vision effects (e.g., black & white effect).")]
    public Camera visionOverlayCamera;
    [Tooltip("Overlay camera for highlighting objects.")]
    public Camera highlightOverlayCamera;
    [Tooltip("Post‑processing volume GameObject for vision effects.")]
    public GameObject postProcessingVolume;

    [Header("Main Camera Reference")]
    [Tooltip("Reference to the shared main camera (with your Cinemachine Virtual Camera).")]
    public Camera mainCamera;

    [Header("Activation Settings")]
    [Tooltip("Time (in seconds) to hold F to activate vision mode.")]
    public float holdTime = 2f;

    [Header("Debug Options")]
    public bool enableDebugLogs = true;

    // (Optional) Vision toggle event that other scripts (like HighlightEmissionController) can subscribe to.
    public static event Action<bool> OnVisionToggle;

    private bool visionActive = false;
    private float holdTimer = 0f;

    private void Start()
    {
        // Ensure overlay components start off.
        if (visionOverlayCamera != null)
            visionOverlayCamera.gameObject.SetActive(false);
        if (highlightOverlayCamera != null)
            highlightOverlayCamera.gameObject.SetActive(false);
        if (postProcessingVolume != null)
            postProcessingVolume.SetActive(false);
    }

    private void Update()
    {
        HandleVisionInput();

        // While vision mode is active, update the overlay cameras to follow the main camera.
        if (visionActive && mainCamera != null)
        {
            if (visionOverlayCamera != null)
            {
                visionOverlayCamera.transform.position = mainCamera.transform.position;
                visionOverlayCamera.transform.rotation = mainCamera.transform.rotation;
            }
            if (highlightOverlayCamera != null)
            {
                highlightOverlayCamera.transform.position = mainCamera.transform.position;
                highlightOverlayCamera.transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private void HandleVisionInput()
    {
        // Process input only if this character is active.
        if (CameraSwitchManager.Instance == null || CameraSwitchManager.Instance.ActivePlayer != playerID)
        {
            if (visionActive)
                DeactivateVision();
            holdTimer = 0f;
            return;
        }

        if (!visionActive)
        {
            if (Input.GetKey(KeyCode.F))
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdTime)
                {
                    ActivateVision();
                }
            }
            else
            {
                holdTimer = 0f;
            }
        }
        else
        {
            // When vision is active, pressing F once toggles it off.
            if (Input.GetKeyDown(KeyCode.F))
                DeactivateVision();
        }
    }

    private void ActivateVision()
    {
        visionActive = true;
        holdTimer = 0f;
        if (visionOverlayCamera != null)
            visionOverlayCamera.gameObject.SetActive(true);
        if (highlightOverlayCamera != null)
            highlightOverlayCamera.gameObject.SetActive(true);
        if (postProcessingVolume != null)
            postProcessingVolume.SetActive(true);

        LogDebug("Vision mode ACTIVATED.");
        OnVisionToggle?.Invoke(true);
    }

    private void DeactivateVision()
    {
        visionActive = false;
        if (visionOverlayCamera != null)
            visionOverlayCamera.gameObject.SetActive(false);
        if (highlightOverlayCamera != null)
            highlightOverlayCamera.gameObject.SetActive(false);
        if (postProcessingVolume != null)
            postProcessingVolume.SetActive(false);

        LogDebug("Vision mode DEACTIVATED.");
        OnVisionToggle?.Invoke(false);
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log(message);
    }
}
