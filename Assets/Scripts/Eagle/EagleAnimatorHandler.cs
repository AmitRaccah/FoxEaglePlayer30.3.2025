using UnityEngine;

/// <summary>
/// Centralised animator controller for the eagle.  
/// Keeps every animation‑related flag and trigger in one place so the other
/// modules (input, movement, banking) stay clean.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EagleAnimationHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;    // will auto‑grab on Awake if null

    [Header("Parameters – names inside the Animator")]
    [SerializeField] private string paramIsGliding = "isGliding";
    [SerializeField] private string paramMouseHold = "MouseHold";
    [SerializeField] private string trigMouseRelease = "MouseRelease";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Call once per frame from EagleController after all logic is calculated.
    /// </summary>
    public void UpdateAnimation(EagleInputData input,
                               bool isGrounded,
                               float verticalSpeed)
    {
        /* 1.  Gliding: true while Ctrl is held OR eagle descending fast */
        bool gliding = input.descend || verticalSpeed < -1f;
        animator.SetBool(paramIsGliding, gliding);

        /* 2.  Mouse claw attack */
        if (Input.GetMouseButtonDown(0))
            animator.SetBool(paramMouseHold, true);
        if (Input.GetMouseButtonUp(0))
        {
            animator.SetBool(paramMouseHold, false);
            animator.SetTrigger(trigMouseRelease);
        }
    }

    /// <summary>
    /// Resets all non‑trigger bools – useful when switching characters.
    /// </summary>
    public void ResetParams()
    {
        animator.SetBool(paramIsGliding, false);
        animator.SetBool(paramMouseHold, false);
    }
}

