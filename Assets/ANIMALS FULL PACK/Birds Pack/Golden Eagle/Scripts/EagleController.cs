using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enum representing the eagle's aerial states.
/// </summary>
public enum EagleAirState
{
    Fly,
    Glide,
    GlideClawAttack,
    GlideCatchPrey
}

/// <summary>
/// Controls the eagle's direct flight, including input handling, altitude control,
/// mouse look for yaw/pitch, banking with Q/E keys, animations, and pickup logic.
/// Uses a separate PickupController for pickup functionality.
/// 
/// Relies on external enabling/disabling (e.g., via a manager) to toggle direct control.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(PickupController))]
public class EagleAlwaysAirController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement Settings")]
    [Tooltip("Base movement speed in air.")]
    public float airMoveSpeed = 5f;

    [Header("Vertical Movement")]
    [Tooltip("Force added to increase altitude when Space is pressed.")]
    public float ascendForce = 10f;
    [Tooltip("Speed at which altitude decreases when Left Control is held.")]
    public float descendSpeed = 3f;

    [Header("Altitude Constraints")]
    [Tooltip("Minimum altitude above ground.")]
    public float minAltitude = 2f;
    [Tooltip("Maximum altitude above ground.")]
    public float maxAltitude = 50f;
    [Tooltip("Layer mask used to detect ground.")]
    public LayerMask groundLayer;
    [Tooltip("Fallback ground Y value if raycast fails.")]
    public float fallbackGroundY = 0f;

    [Header("Rotation Settings")]
    [Tooltip("Mouse look sensitivity.")]
    public float mouseSensitivity = 1f;
    [Tooltip("Minimum pitch angle.")]
    public float pitchMin = -45f;
    [Tooltip("Maximum pitch angle.")]
    public float pitchMax = 45f;

    [Header("Banking Settings")]
    [Tooltip("Maximum roll (bank) angle.")]
    public float maxBankAngle = 90f;
    [Tooltip("Smooth speed for banking (roll).")]
    public float bankSmoothSpeed = 5f;

    [Header("Animation Settings")]
    [Tooltip("Crossfade time between animations.")]
    public float animationTransitionTime = 0.2f;

    [Header("Pickup Settings")]
    [Tooltip("Reference to the PickupController component for item pickup.")]
    public PickupController pickupController;

    #endregion

    #region Private Fields

    private Animator animator;
    private Rigidbody rb;

    private EagleAirState currentState = EagleAirState.Fly;
    private bool isDirectControl = false; // True when this script is enabled as the active controller.

    private bool isMouseHeldPrev = false;
    private float targetAltitude;
    private Vector3 pendingMovement = Vector3.zero;

    private float _yaw, _pitch, _roll; // Rotation components for mouse look and banking.

    private Dictionary<EagleAirState, string> stateAnimations = new Dictionary<EagleAirState, string>
    {
        { EagleAirState.Fly,             "Fly" },
        { EagleAirState.Glide,           "Glide" },
        { EagleAirState.GlideClawAttack, "GlideClawAttack" },
        { EagleAirState.GlideCatchPrey,  "GlideCatchPrey" }
    };

    #endregion

    #region Unity Methods

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        if (pickupController == null)
        {
            pickupController = GetComponent<PickupController>();
        }
    }

    private void Start()
    {
        targetAltitude = transform.position.y;
        AdjustToMinAltitude();

        Vector3 euler = transform.rotation.eulerAngles;
        _pitch = euler.x;
        _yaw = euler.y;
        _roll = euler.z;

        PlayAnimation(EagleAirState.Fly);
        ResetAnimatorParameters();
    }

    private void OnEnable()
    {
        isDirectControl = true;
    }

    private void OnDisable()
    {
        isDirectControl = false;
    }

    private void Update()
    {
        if (!isDirectControl) return;

        HandleInput();
        HandleAnimationTransitions();
        HandleMouseLook();
        HandleBanking();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if (!isDirectControl) return;

        float groundY = GetGroundY(transform.position);
        float clampedAltitude = Mathf.Clamp(targetAltitude, groundY + minAltitude, groundY + maxAltitude);
        float newY = Mathf.Lerp(rb.position.y, clampedAltitude, 5f * Time.fixedDeltaTime);

        Vector3 newPos = rb.position + pendingMovement;
        newPos.y = newY;
        rb.MovePosition(newPos);

        pendingMovement = Vector3.zero;
    }

    #endregion

    #region Input and Movement

    private void HandleInput()
    {
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 forwardMovement = transform.forward * (verticalInput * airMoveSpeed * Time.deltaTime);

        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;
        Vector3 strafeMovement = transform.right * (horizontalInput * airMoveSpeed * Time.deltaTime);

        pendingMovement += forwardMovement + strafeMovement;

        // Ascend
        if (Input.GetKeyDown(KeyCode.Space))
        {
            targetAltitude += ascendForce;
        }
        // Descend
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            targetAltitude -= descendSpeed * Time.deltaTime;
        }
    }

    #endregion

    #region Mouse Look and Banking

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * mouseSensitivity;
        _pitch -= mouseY * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
    }

    private void HandleBanking()
    {
        float bankInput = 0f;
        if (Input.GetKey(KeyCode.Q)) bankInput = 1f;
        else if (Input.GetKey(KeyCode.E)) bankInput = -1f;

        float targetRoll = bankInput * maxBankAngle;
        _roll = Mathf.Lerp(_roll, targetRoll, Time.deltaTime * bankSmoothSpeed);
    }

    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(_pitch, _yaw, _roll);
    }

    #endregion

    #region Animation and Pickup Logic

    private void HandleAnimationTransitions()
    {
        bool isMouseHeld = Input.GetMouseButton(0);

        if (isMouseHeld != isMouseHeldPrev)
        {
            if (isMouseHeld)
            {
                animator.SetBool("MouseHold", true);
            }
            else
            {
                animator.SetBool("MouseHold", false);
                animator.SetBool("MouseRelease", true);

                pickupController?.TogglePickup();
            }
            isMouseHeldPrev = isMouseHeld;
        }
        else
        {
            animator.SetBool("MouseRelease", false);
        }

        animator.SetBool("isGliding", Input.GetKey(KeyCode.LeftControl));
    }

    private void ResetAnimatorParameters()
    {
        animator.SetBool("MouseHold", false);
        animator.SetBool("MouseRelease", false);
        animator.SetBool("isGliding", false);
        animator.Play("Fly", 0, 0f);
    }

    private void PlayAnimation(EagleAirState newState)
    {
        if (newState == currentState)
            return;

        if (!stateAnimations.TryGetValue(newState, out string animName))
            return;

        animator.CrossFadeInFixedTime(animName, animationTransitionTime);
        currentState = newState;
    }

    #endregion

    #region Helper Methods

    private float GetGroundY(Vector3 position)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 1000f, groundLayer))
            return hit.point.y;
        return fallbackGroundY;
    }

    private void AdjustToMinAltitude()
    {
        Vector3 pos = transform.position;
        float groundY = GetGroundY(pos);
        float minAllowed = groundY + minAltitude;
        if (pos.y < minAllowed)
        {
            pos.y = minAllowed;
            targetAltitude = minAllowed;
            transform.position = pos;
        }
    }

    #endregion
}
