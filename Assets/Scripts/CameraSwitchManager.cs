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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip companionRecallClip;
    [SerializeField] private AudioClip companionStayClip;

    [Header("Hot‑keys (can be changed in Inspector)")]
    [SerializeField] private KeyCode recallKey = KeyCode.Alpha4;
    [SerializeField] private KeyCode stayKey = KeyCode.Alpha1;

    private bool _initialised;

    /* ------------------------------------------------------------------ */
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
        _initialised = true;
        TurnOffAllItems();

        // Default to Player 1
        ControllerItem playerItem = controllerItemArray.FirstOrDefault(i => i.playerID == 1);
        if (playerItem != null)
            SwitchToPlayer(playerItem);
    }

    private void Update()
    {
        if (!_initialised) return;

        /* ---------- Recall / Stay (only allowed if active character is Player) ---------- */
        if (ActivePlayer == 1)
        {
            // Recall – key configurable
            if (Input.GetKeyDown(recallKey))
            {
                foreach (var item in controllerItemArray)
                {
                    // Only companions whose key != None are considered UNLOCKED
                    if (item.playerID == 2) // Fox
                    {
                        var foxFollow = item.controller.GetComponent<FoxFollowController>();
                        if (foxFollow != null && item.key != KeyCode.None)
                            foxFollow.enabled = true;   // if far it will teleport itself
                    }
                    else if (item.playerID == 3) // Eagle
                    {
                        var eagleFollow = item.controller.GetComponent<EagleFollowController>();
                        if (eagleFollow != null && item.key != KeyCode.None)
                            eagleFollow.enabled = true;
                    }
                }

                if (audioSource && companionRecallClip)
                    audioSource.PlayOneShot(companionRecallClip);
            }

            // Stay – key configurable
            if (Input.GetKeyDown(stayKey))
            {
                foreach (var item in controllerItemArray)
                {
                    if (item.playerID == 2)
                    {
                        var foxFollow = item.controller.GetComponent<FoxFollowController>();
                        if (foxFollow != null && item.key != KeyCode.None)
                            foxFollow.enabled = false;
                    }
                    else if (item.playerID == 3)
                    {
                        var eagleFollow = item.controller.GetComponent<EagleFollowController>();
                        if (eagleFollow != null && item.key != KeyCode.None)
                            eagleFollow.enabled = false;
                    }
                }

                if (audioSource && companionStayClip)
                    audioSource.PlayOneShot(companionStayClip);
            }
        }

        /* ---------- Character switching ---------- */
        if (!Input.anyKeyDown) return;

        foreach (ControllerItem item in controllerItemArray)
        {
            if (item.key == KeyCode.None) continue;          // not unlocked yet
            if (Input.GetKeyDown(item.key))
            {
                if (item.playerID == ActivePlayer) return;  // already active

                TurnOffAllItems();
                SwitchToPlayer(item);
                return;
            }
        }
    }

    /* ------------------------------------------------------------------ */
    public static Camera CurrentActiveCamera
    {
        get
        {
            if (Instance == null) return null;
            foreach (var item in Instance.controllerItemArray)
            {
                if (item.n_camera != null && item.n_camera.gameObject.activeInHierarchy)
                    return item.n_camera;
            }
            return null;
        }
    }

    public void LockCameraInput(bool lockIt)
    {
        foreach (var item in controllerItemArray)
        {
            if (item.playerID != ActivePlayer) continue;

            switch (ActivePlayer)
            {
                case 1:
                    if (item.controller is ThirdPersonController tpc) tpc.enabled = !lockIt;
                    break;
                case 2:
                    if (item.controller is FoxController fc) fc.enabled = !lockIt;
                    break;
                case 3:
                    if (item.controller is EagleController ec) ec.enabled = !lockIt;
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
            break;
        }
    }

    /* ------------------------------------------------------------------ */
    private void TurnOffAllItems()
    {
        foreach (ControllerItem item in controllerItemArray)
        {
            if (item.camera != null)
                item.camera.Priority = 10;

            if (item.controller != null)
                item.controller.enabled = false;

#if ENABLE_INPUT_SYSTEM
            if (item.playerInput != null)
            {
                item.playerInput.actions?.Disable();
                item.playerInput.enabled = false;
            }
#endif
            if (item.controller != null)
            {
                var ff = item.controller.GetComponent<FoxFollowController>();
                if (ff) ff.enabled = false;

                var ef = item.controller.GetComponent<EagleFollowController>();
                if (ef) ef.enabled = false;

                var pi = item.controller.GetComponent<PickupInput>();
                if (pi) pi.enabled = false;
            }
        }
    }

    private void SwitchToPlayer(ControllerItem item)
    {
        if (item.camera != null)
            item.camera.Priority = 15;

        if (item.n_camera != null)
            item.n_camera.gameObject.SetActive(true);

        if (item.controller != null)
            item.controller.enabled = true;

        // Enable pickup & disable follow on the active character
        if (item.controller != null)
        {
            var pi = item.controller.GetComponent<PickupInput>();
            if (pi) pi.enabled = true;

            var ff = item.controller.GetComponent<FoxFollowController>();
            if (ff) ff.enabled = false;

            var ef = item.controller.GetComponent<EagleFollowController>();
            if (ef) ef.enabled = false;

            var input = item.controller.GetComponent<StarterAssetsInputs>();
            if (input != null)
            {
                input.move = Vector2.zero;
                input.sprint = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (item.playerInput != null)
        {
            item.playerInput.enabled = true;
            item.playerInput.actions?.Enable();
            item.playerInput.SwitchCurrentActionMap("Player");
        }
#endif
        ActivePlayer = item.playerID;

        if (audioSource != null && item.switchSound != null)
            audioSource.PlayOneShot(item.switchSound);
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
    public AudioClip switchSound;
}
