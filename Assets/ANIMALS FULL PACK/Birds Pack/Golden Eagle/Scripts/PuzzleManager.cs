using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a puzzle composed of multiple locks. When all locks are solved, the door opens.
/// Also handles camera transitions and input locking during the door sequence.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle Locks")]
    [SerializeField] private List<PuzzleLock> locks;

    [Header("Optional Door Settings")]
    [SerializeField] private GameObject door;

    [Header("Door Camera")]
    [SerializeField] private Camera doorCamera;

    [Header("Timing Settings")]
    [Tooltip("Delay before swapping door shapes after solving a lock.")]
    public float focusWaitTime = 1.5f;
    [Tooltip("Delay between showing the door and swapping the door shape.")]
    public float shapeSwapDelay = 1.0f;
    [Tooltip("Duration for which the solved shape is displayed before checking the final door state.")]
    public float shapeDisplayDelay = 1.0f;
    [Tooltip("Delay after opening the door before returning to the player camera.")]
    public float doorOpenDelay = 2.0f;

    private int locksSolved = 0;

    private void Start()
    {
        locksSolved = 0;
        foreach (var lk in locks)
        {
            lk.ResetLock();
            lk.SetHighlight(false);
        }

        if (doorCamera != null)
            doorCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by a PuzzleLock when it becomes unlocked.
    /// </summary>
    /// <param name="lockScript">The lock that was unlocked.</param>
    public void OnLockUnlocked(PuzzleLock lockScript)
    {
        locksSolved++;
        StartCoroutine(HandleDoorSequence(lockScript));
    }

    /// <summary>
    /// Handles the door sequence: locks input, switches cameras, updates door shapes, and opens the door if needed.
    /// </summary>
    private IEnumerator HandleDoorSequence(PuzzleLock lockScript)
    {
        Camera activeCamera = CameraSwitchManager.CurrentActiveCamera;

        // Lock input.
        CameraSwitchManager.Instance.LockCameraInput(true);
        yield return new WaitForSeconds(focusWaitTime);

        // Switch to the door camera.
        if (doorCamera != null && activeCamera != null)
        {
            activeCamera.gameObject.SetActive(false);
            doorCamera.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(shapeSwapDelay);

        // Swap door shape.
        SwapDoorShape(lockScript);

        yield return new WaitForSeconds(shapeDisplayDelay);

        // If all locks are solved, open the door.
        if (locksSolved >= locks.Count)
        {
            OpenDoor();
            yield return new WaitForSeconds(doorOpenDelay);
        }

        // Return to the player camera.
        if (doorCamera != null && activeCamera != null)
        {
            doorCamera.gameObject.SetActive(false);
            activeCamera.gameObject.SetActive(true);
        }

        // Unlock input.
        CameraSwitchManager.Instance.LockCameraInput(false);
    }

    /// <summary>
    /// Swaps the door shape from the unsolved to the solved state.
    /// </summary>
    private void SwapDoorShape(PuzzleLock lockScript)
    {
        if (lockScript.DoorShapeUnsolved != null && lockScript.DoorShapeSolved != null)
        {
            lockScript.DoorShapeUnsolved.SetActive(false);
            lockScript.DoorShapeSolved.SetActive(true);
        }
    }

    /// <summary>
    /// Opens the door by triggering its animation, or hides it if no animator is present.
    /// </summary>
    private void OpenDoor()
    {
        if (door != null)
        {
            Animator animator = door.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Open");
            }
            else
            {
                door.SetActive(false);
            }
        }
    }
}

