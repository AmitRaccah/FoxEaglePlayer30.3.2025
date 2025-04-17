using StarterAssets.Fox;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Controls a parallax puzzle where each puzzle piece moves based on the active character’s
/// distance from a designated "correct zone." When the character is near the zone, pieces are displaced
/// based on a mix of their disassembled offsets and a parallax offset derived from the character’s position.
/// Once the puzzle is solved, the pieces snap into their final positions and the effect is disabled.
/// </summary>
public class ParallaxPuzzleController : MonoBehaviour
{
    [Header("Puzzle Pieces Settings")]
    [Tooltip("Transforms for all puzzle pieces.")]
    [SerializeField] private Transform[] puzzlePieces;
    [Tooltip("Offsets for each piece when the puzzle is disassembled.")]
    [SerializeField] private Vector3[] disassembledOffsets;
    [Tooltip("Parallax multipliers for each piece (higher values indicate more movement).")]
    [SerializeField] private float[] parallaxMultipliers;

    [Header("Zone Settings")]
    [Tooltip("Transform marking the center of the correct zone.")]
    [SerializeField] private Transform correctZoneCenter;
    [Tooltip("Radius within which puzzle pieces respond to character movement.")]
    [SerializeField] private float influenceRadius = 10f;

    [Header("Transition Settings")]
    [Tooltip("Speed at which puzzle pieces interpolate toward their assembled positions.")]
    [SerializeField] private float reassembleSpeed = 5f;

    [HideInInspector] public bool inCorrectZone = false;
    [HideInInspector] public bool puzzleSolved = false;

    // Internal cached assembled positions.
    private Vector3[] assembledPositions;
    // Used to detect changes in the active character's tag.
    private string lastActiveTag = "";

    private void Start()
    {
        if (puzzlePieces == null || puzzlePieces.Length == 0)
        {
            enabled = false;
            return;
        }
        if (disassembledOffsets == null || disassembledOffsets.Length != puzzlePieces.Length)
        {
            enabled = false;
            return;
        }
        if (parallaxMultipliers == null || parallaxMultipliers.Length != puzzlePieces.Length)
        {
            parallaxMultipliers = new float[puzzlePieces.Length];
            for (int i = 0; i < puzzlePieces.Length; i++)
                parallaxMultipliers[i] = 1f;
        }

        assembledPositions = new Vector3[puzzlePieces.Length];
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            assembledPositions[i] = puzzlePieces[i].localPosition;
        }
    }

    private void Update()
    {
        if (puzzleSolved)
            return;

        GameObject activeObj = GetActiveGround();
        if (activeObj == null)
            return;

        if (inCorrectZone)
        {
            AssemblePuzzlePieces();
            puzzleSolved = true;
            enabled = false;
            return;
        }

        float distanceToZone = Vector3.Distance(activeObj.transform.position, correctZoneCenter.position);

        if (distanceToZone > influenceRadius)
        {
            // Gradually reassemble puzzle pieces.
            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                puzzlePieces[i].localPosition = Vector3.Lerp(puzzlePieces[i].localPosition, assembledPositions[i], Time.deltaTime * reassembleSpeed);
            }
        }
        else
        {
            // Compute progress (0 when far, 1 when at center).
            float progress = 1f - Mathf.Clamp01(distanceToZone / influenceRadius);
            Vector3 controllerOffset = activeObj.transform.position - correctZoneCenter.position;
            for (int i = 0; i < puzzlePieces.Length; i++)
            {
                Vector3 targetPos = CalculateTargetPosition(i, controllerOffset, progress);
                if (activeObj.tag != lastActiveTag)
                    puzzlePieces[i].localPosition = targetPos;
                else
                    puzzlePieces[i].localPosition = Vector3.Lerp(puzzlePieces[i].localPosition, targetPos, Time.deltaTime * reassembleSpeed);
            }
        }
        lastActiveTag = activeObj.tag;
    }

    /// <summary>
    /// Computes the target local position for a puzzle piece based on its disassembled offset,
    /// parallax multiplier, and the active character's offset.
    /// </summary>
    private Vector3 CalculateTargetPosition(int index, Vector3 controllerOffset, float progress)
    {
        Vector3 baseOffset = Vector3.Lerp(disassembledOffsets[index], Vector3.zero, progress);
        Vector3 parallaxOffset = controllerOffset * parallaxMultipliers[index] * (1f - progress);
        return assembledPositions[index] + baseOffset + parallaxOffset;
    }

    /// <summary>
    /// Instantly assembles all puzzle pieces to their final positions.
    /// </summary>
    private void AssemblePuzzlePieces()
    {
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            puzzlePieces[i].localPosition = assembledPositions[i];
        }
    }

    /// <summary>
    /// Retrieves the active ground character based on the active player ID from CameraSwitchManager.
    /// </summary>
    private GameObject GetActiveGround()
    {
        if (CameraSwitchManager.Instance == null)
            return null;

        int activePlayer = CameraSwitchManager.Instance.ActivePlayer;
        switch (activePlayer)
        {
            case 1:
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && player.GetComponentInParent<ThirdPersonController>()?.enabled == true)
                    return player;
                break;
            case 2:
                GameObject fox = GameObject.FindGameObjectWithTag("Fox");
                if (fox != null && fox.GetComponentInParent<FoxController>()?.enabled == true)
                    return fox;
                break;
            case 3:
                GameObject eagle = GameObject.FindGameObjectWithTag("Eagle");
                if (eagle != null && eagle.GetComponentInParent<EagleAlwaysAirController>()?.enabled == true)
                    return eagle;
                break;
            default:
                break;
        }
        return null;
    }

    /// <summary>
    /// Forces the puzzle to assemble and disables the parallax effect.
    /// Typically called when the active character fully enters the correct zone.
    /// </summary>
    public void EnterCorrectZone()
    {
        inCorrectZone = true;
        AssemblePuzzlePieces();
        puzzleSolved = true;
        enabled = false;
    }

    /// <summary>
    /// Exits the correct zone, allowing puzzle pieces to be influenced again if unsolved.
    /// </summary>
    public void ExitCorrectZone()
    {
        if (!puzzleSolved)
        {
            inCorrectZone = false;
        }
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
