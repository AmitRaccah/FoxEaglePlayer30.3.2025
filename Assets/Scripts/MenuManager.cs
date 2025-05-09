// MenuManager.cs
// Controls the main‑menu overlay, camera swap, music cross‑fade
// and one‑shot “wake up” animation of the player.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using StarterAssets;

public class MenuManager : MonoBehaviour
{
    /* -------------------- inspector -------------------- */

    [Header("UI")]
    [SerializeField] private CanvasGroup menuCanvas;     // CanvasGroup on the menu canvas
    [SerializeField] private Button playButton;    // “Start / Play” button

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera mainMenuCam;   // looks at the island
    [SerializeField] private CinemachineVirtualCamera playerCam;     // follows the player

    [Header("Audio")]
    [SerializeField] private AudioSource startMusic;     // menu music (starts at full volume)
    [SerializeField] private AudioSource levelMusic;     // level loop (starts muted)
    [SerializeField] private float musicFadeTime = 2f;

    [Header("Player")]
    [SerializeField] private Animator playerAnimator;    // Animator on PlayerArmature
    [SerializeField] private string wakeTrigger = "Wake";
    [SerializeField] private float wakeAnimLength = 3f; // seconds

    /* -------------------- life‑cycle -------------------- */

    private void Awake()
    {
        playButton.onClick.AddListener(() => StartCoroutine(EnterGameRoutine()));

        // ensure initial audio state
        startMusic.volume = 1f;
        levelMusic.volume = 0f;
        if (!startMusic.isPlaying) startMusic.Play();
        if (!levelMusic.isPlaying) levelMusic.Play();
    }

    /* -------------------- core coroutine -------------------- */

    private IEnumerator EnterGameRoutine()
    {
        playButton.interactable = false;

        /* 1 – fade out UI */
        yield return StartCoroutine(FadeCanvas(1f, 0f, 0.8f));

        /* 2 – camera priority swap (Cinemachine handles blend) */
        if (mainMenuCam) mainMenuCam.Priority = 5;
        if (playerCam) playerCam.Priority = 25;

        /* 3 – cross‑fade music */
        StartCoroutine(FadeAudio(startMusic, 1f, 0f));
        StartCoroutine(FadeAudio(levelMusic, 0f, 1f));

        /* 4 – play wake‑up animation without controller interference */
        var tpController = playerAnimator.GetComponent<ThirdPersonController>();
        if (tpController) tpController.enabled = false;

        playerAnimator.applyRootMotion = true;          // if clip contains root motion
        playerAnimator.SetTrigger(wakeTrigger);

        yield return new WaitForSeconds(wakeAnimLength);

        playerAnimator.applyRootMotion = false;
        if (tpController) tpController.enabled = true;  // restore player control

        /* 5 – disable menu canvas completely */
        menuCanvas.gameObject.SetActive(false);
    }

    /* -------------------- helpers -------------------- */

    private IEnumerator FadeCanvas(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            menuCanvas.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        menuCanvas.alpha = to;
    }

    private IEnumerator FadeAudio(AudioSource src, float from, float to)
    {
        float t = 0f;
        while (t < musicFadeTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / musicFadeTime);
            yield return null;
        }
        src.volume = to;
    }
}
