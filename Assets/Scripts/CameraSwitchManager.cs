using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;
using StarterAssets.Fox;

public class CameraSwitchManager : MonoBehaviour
{
    public static CameraSwitchManager Instance;

    [Tooltip("List of controller items. Each item has a playerID, a Cinemachine virtual camera, a normal camera, a controller, etc.")]
    public ControllerItem[] controllerItemArray;

    [Tooltip("The ID of the currently active player (1 = Player1, 2 = Fox, 0 = Eagle).")]
    public int ActivePlayer { get; private set; }

    // Cached reference to Player1's StarterAssetsInputs (used to reset movement)

    private bool initialized;




    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Cache Player1's StarterAssetsInputs for quick access.

        initialized = true;
        TurnOffAllItems();

        // Switch to Player1 by default.
        ControllerItem playerController = controllerItemArray.FirstOrDefault(item => item.playerID == 1);
        SwitchToPlayer(playerController);
    }

    private void Update()
    {
        if (!initialized) return;
        if (!Input.anyKeyDown) return;

        // Loop through all controller items and check for a key press.
        foreach (ControllerItem controllerItem in controllerItemArray)
        {
            if (Input.GetKeyDown(controllerItem.key))
            {
                TurnOffAllItems();
                // No delay on switch – immediate switch.
                SwitchToPlayer(controllerItem);
                break;
            }
        }
    }


    
    /// <summary>
    /// Returns the currently active normal camera (n_camera) from the active ControllerItem.
    /// If none is active, returns null.
    /// </summary>
    public static Camera CurrentActiveCamera
    {
        get
        {
            if (Instance == null) return null;
            foreach (var item in Instance.controllerItemArray)
            {
                if (item.n_camera != null && item.n_camera.gameObject.activeInHierarchy)
                {
                    return item.n_camera;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Temporarily locks or unlocks input for the current active character.
    /// </summary>
    /// 
    
    public void LockCameraInput(bool lockIt)
    {
        Debug.Log($"[CameraSwitchManager] LockCameraInput({lockIt}) called. ActivePlayer = {ActivePlayer}");
        // Find the ControllerItem matching the current active player.
        foreach (var item in controllerItemArray)
        {
            if (item.playerID != ActivePlayer)
                continue;

            switch (ActivePlayer)
            {
                case 1: // Player
                    if (item.controller is ThirdPersonController tpc)
                        tpc.enabled = !lockIt;
                    break;
                case 2: // Fox
                    if (item.controller is FoxController fc)
                        fc.enabled = !lockIt;
                    break;
                case 3: // Eagle
                    if (item.controller is EagleAlwaysAirController eagle)
                        eagle.enabled = !lockIt;
                    break;
                default:
                    Debug.LogWarning($"[CameraSwitchManager] Unknown ActivePlayer ID: {ActivePlayer}");
                    break;
            }

#if ENABLE_INPUT_SYSTEM
            if (item.playerInput != null)
            {
                if (lockIt)
                {
                    item.playerInput.actions?.Disable();
                    item.playerInput.enabled = false;
                }
                else
                {
                    item.playerInput.enabled = true;
                    item.playerInput.actions?.Enable();
                    item.playerInput.SwitchCurrentActionMap("Player");
                }
            }
#endif
            break; // Found the active item, exit loop.
        }
    }

    /// <summary>
    /// Disables all controller items (sets their virtual camera priority to 10 and disables controllers and input).
    /// Note: The main camera (n_camera) is left active to avoid "no camera rendering" issues.
    /// </summary>
    private void TurnOffAllItems()
    {
        foreach (ControllerItem item in controllerItemArray)
        {
            if (item.camera != null)
                item.camera.Priority = 10;
            // We do not disable n_camera to keep the main camera active.
            if (item.controller != null)
                item.controller.enabled = false;
#if ENABLE_INPUT_SYSTEM
            if (item.playerInput != null)
            {
                item.playerInput.actions?.Disable();
                item.playerInput.enabled = false;
            }
#endif
            // Re-enable follow behavior on inactive characters.
            var foxFollow = item.controller.GetComponent<FoxFollowController>();
            if (foxFollow != null)
                foxFollow.enabled = true;
            var eagleFollow = item.controller.GetComponent<EagleFollowController>();
            if (eagleFollow != null)
                eagleFollow.enabled = true;
        }
    }

    /// <summary>
    /// Switches control to the specified controller item.
    /// </summary>
    private void SwitchToPlayer(ControllerItem item)
    {
        if (item.camera != null)
            item.camera.Priority = 15;
        if (item.n_camera != null)
            item.n_camera.gameObject.SetActive(true);
        if (item.controller != null)
            item.controller.enabled = true;
#if ENABLE_INPUT_SYSTEM
        if (item.playerInput != null)
        {
            item.playerInput.enabled = true;
            item.playerInput.actions?.Enable();
            item.playerInput.SwitchCurrentActionMap("Player");
        }
#endif
        // Disable follow controllers on the newly activated character.
        var foxFollow = item.controller.GetComponent<FoxFollowController>();
        if (foxFollow != null)
            foxFollow.enabled = false;
        var eagleFollow = item.controller.GetComponent<EagleFollowController>();
        if (eagleFollow != null)
            eagleFollow.enabled = false;
        // Clear any residual movement input.
        var input = item.controller.GetComponent<StarterAssetsInputs>();
        if (input != null)
        {
            input.move = Vector2.zero;
            input.sprint = false;
        }
        ActivePlayer = item.playerID;
        Debug.Log($"[CameraSwitchManager] Switched to playerID = {item.playerID}, controller = {item.controller}");
    }
}

[System.Serializable]
public class ControllerItem
{
    public int playerID;
    public CinemachineVirtualCamera camera;
    public Camera n_camera;
    public MonoBehaviour controller;
#if ENABLE_INPUT_SYSTEM
    public PlayerInput playerInput;
#endif
    public KeyCode key;
}
