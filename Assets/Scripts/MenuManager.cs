
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cinemachine;
using StarterAssets;

public class MenuManager : MonoBehaviour
{
    /* ------------ Inspector ------------ */

    [Header("UI")]
    [SerializeField] private CanvasGroup menuCanvas;
    [SerializeField] private Button startButton;

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera mainMenuCam;
    [SerializeField] private CinemachineVirtualCamera playerCam;

    [Header("Audio")]
    [SerializeField] private AudioSource startMusic;
    [SerializeField] private AudioSource levelMusic;
    [SerializeField] private float musicFadeTime = 2f;

    [Header("Player")]
    [SerializeField] private Animator playerAnimator;    
    [SerializeField] private float wakeClipLength = 3f;

    [Header("Optional – crosshair canvas")]
    [SerializeField] private GameObject reticleCanvas;

    /* ------------ Private refs ------------ */

    private ThirdPersonController tpc;
    private StarterAssetsInputs[] inputScripts;
    private CameraSwitchManager camManager;
    private RightClickZoomSwitch[] zoomScripts;
    private SearchReticleUI[] reticleScripts;

    /* ------------ Awake ------------ */

    void Awake()
    {
        /* UI hook */
        startButton.onClick.AddListener(StartGame);

        /* collect gameplay systems */
        camManager = CameraSwitchManager.Instance;
        zoomScripts = FindObjectsOfType<RightClickZoomSwitch>(true);
        reticleScripts = FindObjectsOfType<SearchReticleUI>(true);
        inputScripts = FindObjectsOfType<StarterAssetsInputs>(true);
        tpc = playerAnimator.GetComponent<ThirdPersonController>();

        /* disable everything until START is pressed */
        if (camManager) camManager.enabled = false;
        foreach (var z in zoomScripts) z.enabled = false;
        foreach (var r in reticleScripts) r.enabled = false;
        foreach (var inp in inputScripts) inp.enabled = false;
        if (tpc) tpc.enabled = false;
        playerAnimator.enabled = false;               // prevents auto‑playing WakeUp
        if (reticleCanvas) reticleCanvas.SetActive(false);

        /* camera priorities */
        if (mainMenuCam) mainMenuCam.Priority = 50;
        if (playerCam) playerCam.Priority = 10;

        /* menu music */
        startMusic.volume = 1f; startMusic.Play();
        levelMusic.volume = 0f; levelMusic.Play();
    }

    /* ------------ Start ------------ */

    IEnumerator Start()
    {
        yield return null;                            // wait one frame
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    /* ------------ Button entry ------------ */

    public void StartGame() => StartCoroutine(EnterGame());

    /* ------------ Intro sequence ------------ */

    IEnumerator EnterGame()
    {
        startButton.interactable = false;

        /* fade UI */
        yield return FadeCanvas(1f, 0f, 0.8f);

        /* camera & music */
        mainMenuCam.Priority = 5;
        playerCam.Priority = 25;
        StartCoroutine(FadeAudio(startMusic, 1f, 0f));
        StartCoroutine(FadeAudio(levelMusic, 0f, 1f));

        /* wake‑up clip */
        playerAnimator.enabled = true;           // now the default state plays
        if (tpc) tpc.enabled = true;           // enables root‑motion handling
        playerAnimator.applyRootMotion = true;
        yield return new WaitForSeconds(wakeClipLength);
        playerAnimator.applyRootMotion = false;

        /* enable gameplay systems */
        foreach (var inp in inputScripts) inp.enabled = true;
        if (camManager) camManager.enabled = true;
        foreach (var z in zoomScripts) z.enabled = true;
        foreach (var r in reticleScripts) r.enabled = true;
        if (reticleCanvas) reticleCanvas.SetActive(true);

        /* lock cursor for gameplay */
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        menuCanvas.gameObject.SetActive(false);
    }

    /* ------------ Helpers ------------ */

    IEnumerator FadeCanvas(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            menuCanvas.alpha = Mathf.Lerp(from, to, t / time);
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
            yield return null;
        }
        menuCanvas.alpha = to;
    }

    IEnumerator FadeAudio(AudioSource src, float from, float to)
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
