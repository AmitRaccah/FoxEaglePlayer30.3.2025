using StarterAssets.Fox;
using StarterAssets;
using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a parallax puzzle where each puzzle piece moves based on the active character’s
/// distance from a designated "correct zone." 
/// When the player is outside the zone, a broken static object remains visible.
/// Upon entering the zone, the broken object hides, multiple entry particle effects play once,
/// and the dynamic parallax pieces appear and interpolate from their disassembled offsets
/// back toward their assembled positions. Once fully centered, the puzzle auto‑solves,
/// snapping pieces into place, destroying any remaining entry effects, and disabling further updates.
/// </summary>
public class ParallaxPuzzleController : MonoBehaviour
{
    [Header("Broken Object")]
    [Tooltip("Root GameObject whose children are the broken pieces on the floor.")]
    [SerializeField] private GameObject brokenRoot;

    [Header("Parallax Pieces")]
    [Tooltip("Each individual parallax‑driven piece (not children of brokenRoot).")]
    [SerializeField] private Transform[] puzzlePieces;
    [Tooltip("Offsets for each piece when disassembled.")]
    [SerializeField] private Vector3[] disassembledOffsets;

    [Header("Zone Settings")]
    [Tooltip("Transform marking the center of the correct zone.")]
    [SerializeField] private Transform correctZoneCenter;
    [Tooltip("Radius within which puzzle pieces respond.")]
    [SerializeField] private float influenceRadius = 10f;

    [Header("Transition Settings")]
    [Tooltip("Speed of interpolation towards assembled positions.")]
    [SerializeField] private float reassembleSpeed = 5f;

    [Header("Entry Effects")]
    [Tooltip("ParticleSystems to play once when entering the parallax zone.")]
    [SerializeField] private ParticleSystem[] entryEffects;

    // Cached final local positions for each piece
    private Vector3[] assembledPositions;

    // State flags
    private bool inZone = false;
    private bool puzzleSolved = false;

    // Remember which character was last active (for snapping on switch)
    private string lastActiveTag = "";

    private void Start()
    {
        // Show broken object and hide all parallax pieces initially
        if (brokenRoot != null) brokenRoot.SetActive(true);
        foreach (var piece in puzzlePieces)
            piece.gameObject.SetActive(false);

        // Validate arrays
        if (puzzlePieces.Length == 0 || disassembledOffsets.Length != puzzlePieces.Length)
        {
            Debug.LogError("ParallaxPuzzleController: puzzlePieces and disassembledOffsets must match.");
            enabled = false;
            return;
        }

        // Cache assembled positions
        assembledPositions = new Vector3[puzzlePieces.Length];
        for (int i = 0; i < puzzlePieces.Length; i++)
            assembledPositions[i] = puzzlePieces[i].localPosition;
    }

    private void Update()
    {
        if (puzzleSolved) return;

        GameObject activeObj = GetActiveGround();
        if (activeObj == null) return;

        float distance = Vector3.Distance(activeObj.transform.position, correctZoneCenter.position);
        bool nowInZone = distance <= influenceRadius;

        // Enter zone
        if (nowInZone && !inZone)
        {
            inZone = true;
            PlayEntryEffects();

            // Hide broken object, show parallax pieces
            if (brokenRoot != null) brokenRoot.SetActive(false);
            foreach (var piece in puzzlePieces)
                piece.gameObject.SetActive(true);
        }
        // Exit zone
        else if (!nowInZone && inZone)
        {
            inZone = false;

            // Show broken object, hide parallax pieces
            if (brokenRoot != null) brokenRoot.SetActive(true);
            foreach (var piece in puzzlePieces)
                piece.gameObject.SetActive(false);
            return;
        }

        // Parallax movement while in zone
        if (inZone)
        {
            // Solve when very close
            if (distance <= 0.01f)
            {
                AssemblePuzzlePieces();
                puzzleSolved = true;
                DestroyEntryEffects();
                return;
            }

            // Compute progress (0 at edge, 1 at center)
            float progress = 1f - Mathf.Clamp01(distance / influenceRadius);
            Vector3 offset = activeObj.transform.position - correctZoneCenter.position;

            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                Vector3 baseOffset = Vector3.Lerp(disassembledOffsets[i], Vector3.zero, progress);
                Vector3 parallaxOffset = offset * (1f - progress);
                Vector3 targetPos = assembledPositions[i] + baseOffset + parallaxOffset;

                if (activeObj.tag != lastActiveTag)
                    puzzlePieces[i].localPosition = targetPos;
                else
                    puzzlePieces[i].localPosition = Vector3.Lerp(
                        puzzlePieces[i].localPosition,
                        targetPos,
                        Time.deltaTime * reassembleSpeed
                    );
            }

            lastActiveTag = activeObj.tag;
        }
    }

    /// <summary>
    /// Plays each entry effect once and schedules its disable.
    /// </summary>
    private void PlayEntryEffects()
    {
        foreach (var effect in entryEffects)
        {
            if (effect == null) continue;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play();
            StartCoroutine(DisableEffectAfterDelay(effect, effect.main.duration + effect.main.startLifetime.constantMax));
        }
    }

    /// <summary>
    /// Snaps all pieces to their assembled positions immediately.
    /// </summary>
    private void AssemblePuzzlePieces()
    {
        for (int i = 0; i < puzzlePieces.Length; i++)
            puzzlePieces[i].localPosition = assembledPositions[i];
    }

    /// <summary>
    /// Destroys all entry effect GameObjects to ensure no particles remain.
    /// </summary>
    private void DestroyEntryEffects()
    {
        foreach (var effect in entryEffects)
        {
            if (effect != null)
                Destroy(effect.gameObject);
        }
    }

    /// <summary>
    /// External call to force solve, hide broken object, show pieces,
    /// snap into place, and destroy any remaining entry effects.
    /// </summary>
    public void EnterCorrectZone()
    {
        inZone = true;
        puzzleSolved = true;
        PlayEntryEffects();

        if (brokenRoot != null) brokenRoot.SetActive(false);

        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            puzzlePieces[i].gameObject.SetActive(true);
            puzzlePieces[i].localPosition = assembledPositions[i];
        }

        DestroyEntryEffects();
    }

    /// <summary>
    /// Disables the given ParticleSystem after the specified delay.
    /// </summary>
    private IEnumerator DisableEffectAfterDelay(ParticleSystem effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>
    /// Finds the currently active ground-based character.
    /// </summary>
    private GameObject GetActiveGround()
    {
        if (CameraSwitchManager.Instance == null) return null;

        switch (CameraSwitchManager.Instance.ActivePlayer)
        {
            case 1:
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player?.GetComponentInParent<ThirdPersonController>()?.enabled == true)
                    return player;
                break;
            case 2:
                var fox = GameObject.FindGameObjectWithTag("Fox");
                if (fox?.GetComponentInParent<FoxController>()?.enabled == true)
                    return fox;
                break;
            case 3:
                var eagle = GameObject.FindGameObjectWithTag("Eagle");
                if (eagle?.GetComponentInParent<EagleController>()?.enabled == true)
                    return eagle;
                break;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (correctZoneCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(correctZoneCenter.position, influenceRadius);
        }
    }
}
