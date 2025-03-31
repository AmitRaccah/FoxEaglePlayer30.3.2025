/*
using System;
using System.Collections;
using UnityEngine;
using Cinemachine;
using StarterAssets;
using StarterAssets.Fox;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Manages switching between character controllers and their cameras.
/// Switches control between Player (1), Fox (2), and Eagle (0) based on key input (1, 2, 3).
/// Retains a short delay if switching from Player1, and zeroes out Player1's movement.
/// </summary>
public class CameraSwitchController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Character References")]
    [Tooltip("Reference to Player 1 GameObject.")]
    [SerializeField] private GameObject player1;

    [Tooltip("Reference to Fox GameObject.")]
    [SerializeField] private GameObject player2;

    [Tooltip("Reference to Eagle GameObject.")]
    [SerializeField] private GameObject eagle;

    [Header("Player 1 Cameras")]
    [Tooltip("The real camera used by Player 1.")]
    [SerializeField] private Camera player1RealCamera;
    [Tooltip("The Cinemachine virtual camera for Player 1.")]
    [SerializeField] private CinemachineVirtualCamera player1Camera;

    [Header("Fox Camera")]
    [Tooltip("The camera used by the Fox.")]
    [SerializeField] private Camera foxCamera;

    [Header("Eagle Camera & Follow")]
    [Tooltip("The camera used by the Eagle.")]
    [SerializeField] private Camera eagleCamera;
    [Tooltip("The CameraFollowController for the Eagle.")]
    [SerializeField] private CameraFollowController eagleFollowController;

    [Header("Eagle Vision")]
    [Tooltip("The Eagle Vision switch script (if applicable).")]
    [SerializeField] private EagleVision2CamSwitch eagleVisionSwitch;

    #endregion

    #region Private Fields

    // Player1
    private ThirdPersonController player1Controller;
    private StarterAssetsInputs player1Inputs;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput player1PlayerInput;
#endif

    // Fox
    private FoxController player2FoxController;
    private StarterAssetsInputs player2Inputs;
#if ENABLE_INPUT_SYSTEM
    private PlayerInput player2PlayerInput;
#endif
    private FoxSearchModeController player2SearchController;

    // Eagle
    private EagleAlwaysAirController eagleController;

    #endregion

    #region Public Properties

    /// <summary>
    /// Singleton instance of CameraSwitchController.
    /// </summary>
    public static CameraSwitchController Instance { get; private set; }

    /// <summary>
    /// The currently active camera.
    /// </summary>
    public static Camera CurrentActiveCamera { get; private set; }

    /// <summary>
    /// Indicates whether a player (Player1 or Fox) is currently controlled.
    /// </summary>
    public static bool IsControllingPlayer { get; private set; }

    /// <summary>
    /// Indicates whether the Eagle is under direct control.
    /// </summary>
    public static bool IsControllingEagle { get; private set; }

    /// <summary>
    /// The currently controlled character: 1 = Player1, 2 = Fox, 0 = Eagle.
    /// </summary>
    public int ActivePlayer { get; private set; }

    /// <summary>
    /// Gets the Fox GameObject (if needed).
    /// </summary>
    public GameObject Player2 => player2;

    /// <summary>
    /// Gets the Eagle camera (if needed).
    /// </summary>
    public Camera EagleCamera => eagleCamera;

    /// <summary>
    /// Returns the Player1 Cinemachine virtual camera if Player1 is active.
    /// </summary>
    public static CinemachineVirtualCamera PlayerVirtualCamera
    {
        get
        {
            if (Instance == null) return null;
            return (Instance.ActivePlayer == 1) ? Instance.player1Camera : null;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Lock or unlock camera input for the currently controlled character.
    /// </summary>
    /// <param name="lockIt">True=lock, false=unlock.</param>
    public void LockCameraInput(bool lockIt)
    {
        if (ActivePlayer == 1)
        {
            var pc = player1.GetComponent<ThirdPersonController>();
            var inp = player1.GetComponent<StarterAssetsInputs>();
            if (pc != null) pc.enabled = !lockIt;
            if (inp != null) inp.enabled = !lockIt;
        }
        else if (ActivePlayer == 2)
        {
            var fox = player2.GetComponent<FoxController>();
            var inp = player2.GetComponent<StarterAssetsInputs>();
            if (fox != null) fox.enabled = !lockIt;
            if (inp != null) inp.enabled = !lockIt;
        }
        else if (ActivePlayer == 0)
        {
            if (eagleFollowController != null)
                eagleFollowController.enabled = !lockIt;
        }
        Debug.Log($"[CameraSwitchController] Camera input locked: {lockIt}");
    }

    #endregion

    #region Private Helpers

#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Resets Player1's input to clear any stale input. 
    /// Called after enabling Player1 again.
    /// </summary>
    private IEnumerator ResetPlayer1Input()
    {
        yield return new WaitForEndOfFrame();
        if (player1PlayerInput != null)
        {
            // Temporarily disable + re-enable to flush input
            player1PlayerInput.enabled = false;
            player1PlayerInput.actions?.Disable();
            yield return null;
            player1PlayerInput.enabled = true;
            player1PlayerInput.actions?.Enable();
            player1PlayerInput.SwitchCurrentActionMap("Player");
        }
    }
#endif

    /// <summary>
    /// Updates the follow target of the active camera so that it tracks the currently controlled character.
    /// </summary>
    private void UpdateCameraFollowTarget()
    {
        if (CurrentActiveCamera == null)
        {
            Debug.LogWarning("CurrentActiveCamera is null; cannot update follow target.");
            return;
        }

        var follow = CurrentActiveCamera.GetComponent<CameraFollowController>();
        if (follow != null)
        {
            if (ActivePlayer == 1 && player1 != null)
                follow.SetTarget(player1.transform);
            else if (ActivePlayer == 2 && player2 != null)
                follow.SetTarget(player2.transform);
            else if (ActivePlayer == 0 && eagle != null)
                follow.SetTarget(eagle.transform);
            Debug.Log("Custom follow target updated via CameraFollowController.");
        }
        else
        {
            // Fallback to Cinemachine
            var vCam = CurrentActiveCamera.GetComponent<CinemachineVirtualCamera>();
            if (vCam != null)
            {
                if (ActivePlayer == 1 && player1 != null)
                    vCam.Follow = player1.transform;
                else if (ActivePlayer == 2 && player2 != null)
                    vCam.Follow = player2.transform;
                else if (ActivePlayer == 0 && eagle != null)
                    vCam.Follow = eagle.transform;
                Debug.Log("Cinemachine follow target updated.");
            }
            else
            {
                Debug.LogWarning("No follow component found on the active camera.");
            }
        }
    }

    /// <summary>
    /// If currently in Player1, wait the specified delay before calling switchMethod.
    /// Zero out movement so Player1 doesn't keep walking.
    /// </summary>
    private IEnumerator DelaySwitch(Action switchMethod, float delay)
    {
        if (ActivePlayer == 1)
        {
            if (player1Inputs != null)
                player1Inputs.move = Vector2.zero;
            yield return new WaitForSeconds(delay);
        }
        switchMethod.Invoke();
    }

    #endregion
}
*/