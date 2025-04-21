using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Central director that listens for StoryEventChannel events and performs
/// camera zooms, SFX, and key unlocking for companions.
/// Public API = zero.  Everything is configured in the Inspector.
/// </summary>
public class StoryDirector : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private CameraSwitchManager switchManager;    // drag from scene
    [SerializeField] private AudioSource audioSource;     // optional

    [Header("Reveal Timing")]
    [SerializeField] private float revealDuration = 2f;

    [Header("Companion Reveals (add rows)")]
    [SerializeField] private CompanionRevealData[] reveals;

    /* ---------- private state ---------- */
    private readonly Dictionary<int, KeyCode> _cachedKeys = new();
    private readonly Dictionary<string, CompanionRevealData> _lookup =
        new Dictionary<string, CompanionRevealData>();

    /* ---------- life‑cycle ---------- */
    private void Awake()
    {
        // Build quick look‑up for event name → data
        foreach (var r in reveals)
            _lookup[r.eventName] = r;
    }

    private void OnEnable() => StoryEventChannel.OnStoryEvent += HandleStoryEvent;
    private void OnDisable() => StoryEventChannel.OnStoryEvent -= HandleStoryEvent;

    private void Start()
    {
        // Disable all companions listed in the table
        foreach (var data in reveals)
            DisableCompanionKey(data.playerID);
    }

    /* ---------- event handling ---------- */
    private void HandleStoryEvent(string eventName)
    {
        if (_lookup.TryGetValue(eventName, out var data))
            StartCoroutine(RevealRoutine(data));
    }

    private IEnumerator RevealRoutine(CompanionRevealData data)
    {
        // 1 – bring up the reveal camera
        int originalPriority = 0;
        if (data.revealCamera != null)
        {
            originalPriority = data.revealCamera.Priority;
            data.revealCamera.Priority = 50;           // above gameplay cams
        }

        // 2 – lock player controls
        switchManager.LockCameraInput(true);

        // 3 – SFX
        if (audioSource && data.revealClip)
            audioSource.PlayOneShot(data.revealClip);

        // 4 – small pause (or replace with Timeline)
        yield return new WaitForSeconds(revealDuration);

        // 5 – unlock companion
        EnableCompanionKey(data.playerID);

        // 6 – restore camera & input
        if (data.revealCamera != null)
            data.revealCamera.Priority = originalPriority;

        switchManager.LockCameraInput(false);
    }

    /* ---------- key management ---------- */
    private void DisableCompanionKey(int playerID)
    {
        var item = FindItem(playerID);
        if (item == null) return;

        _cachedKeys[playerID] = item.key;   // remember original
        item.key = KeyCode.None;            // block
    }

    private void EnableCompanionKey(int playerID)
    {
        var item = FindItem(playerID);
        if (item == null) return;

        if (_cachedKeys.TryGetValue(playerID, out var cached))
            item.key = cached;
    }

    private ControllerItem FindItem(int id) =>
        switchManager.controllerItemArray.FirstOrDefault(ci => ci.playerID == id);
}
