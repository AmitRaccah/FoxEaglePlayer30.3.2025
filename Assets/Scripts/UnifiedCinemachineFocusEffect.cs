using UnityEngine;
using Cinemachine;

/// <summary>
/// Allows each player's Cinemachine cameras to switch to a "zoom cam" on right-click,
/// without modifying the existing CameraSwitchManager.
/// 
/// Usage:
/// 1) In the Inspector, fill 'zoomConfigs' with entries for each player ID:
///    - playerID = 1 (Player?), normalCam = vCamA, zoomCam = vCamB
///    - playerID = 2 (Fox?),    normalCam = vCamC, zoomCam = vCamD
///    etc.
/// 2) Press the key to switch to Player/Fox/Eagle as usual via CameraSwitchManager.
/// 3) Hold right-click: This script disables that player's normalCam and enables zoomCam.
///    Release: Re-enable normalCam, disable zoomCam.
/// </summary>
public class RightClickZoomSwitch : MonoBehaviour
{
    [System.Serializable]
    public class ZoomConfig
    {
        public int playerID;                     // 1 = Player, 2 = Fox, 3 = Eagle, etc.
        public CinemachineVirtualCamera normalCam;
        public CinemachineVirtualCamera zoomCam;
        public AudioSource searchModeAudioSource;

    }

    [Tooltip("List of each player's normal & zoom cameras, mapped by playerID.")]
    [SerializeField] private ZoomConfig[] zoomConfigs;

    private void Start()
    {
        // Make sure all players start with normalCam ON, zoomCam OFF
        foreach (var config in zoomConfigs)
        {
            if (config.normalCam != null)
                config.normalCam.gameObject.SetActive(true);
            if (config.zoomCam != null)
                config.zoomCam.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (CameraSwitchManager.Instance == null) return;

        // Check which player is currently active:
        int activeID = CameraSwitchManager.Instance.ActivePlayer;

        // Find the matching ZoomConfig for this active ID
        ZoomConfig activeConfig = null;
        foreach (var config in zoomConfigs)
        {
            if (config.playerID == activeID)
            {
                activeConfig = config;
                break;
            }
        }

        if (activeConfig == null)
        {
            // No config for the active player
            return;
        }

        // If right-click is held, switch to the zoom camera
        if (Input.GetMouseButton(1))
        {
            if (activeConfig.normalCam != null)
                activeConfig.normalCam.gameObject.SetActive(false);

            if (activeConfig.zoomCam != null)
                activeConfig.zoomCam.gameObject.SetActive(true);

            //Sounds
            if (activeConfig.searchModeAudioSource != null && !activeConfig.searchModeAudioSource.isPlaying)
            {
                activeConfig.searchModeAudioSource.Play();
            }
        }
        else
        {
            // Otherwise, revert to normal camera
            if (activeConfig.zoomCam != null)
                activeConfig.zoomCam.gameObject.SetActive(false);

            if (activeConfig.normalCam != null)
                activeConfig.normalCam.gameObject.SetActive(true);

            //Sounds Off
            if (activeConfig.searchModeAudioSource != null && activeConfig.searchModeAudioSource.isPlaying)
            {
                activeConfig.searchModeAudioSource.Stop();
            }

        }
    }
}
