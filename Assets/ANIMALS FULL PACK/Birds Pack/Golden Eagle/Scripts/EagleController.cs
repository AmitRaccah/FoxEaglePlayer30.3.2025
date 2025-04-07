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
/// Controls the eagle's flight behavior (movement, altitude, mouse look, banking, animations).
/// Uses continuous altitude sync so the eagle won't revert to old Y positions
/// when switching control away and back again.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class EagleAlwaysAirController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement Settings")]
    public float airMoveSpeed = 5f;

    [Header("Vertical Movement")]
    [Tooltip("Force used to ascend when Space is pressed.")]
    public float ascendForce = 10f;
    [Tooltip("Speed at which altitude decreases when Left Control is held.")]
    public float descendSpeed = 3f;

    [Header("Altitude Constraints")]
    public float minAltitude = 2f;
    public float maxAltitude = 50f;
    [Tooltip("Ground detection layer mask.")]
    public LayerMask groundLayer;
    [Tooltip("Fallback ground Y if raycast fails.")]
    public float fallbackGroundY = 0f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 1f;
    public float pitchMin = -45f;
    public float pitchMax = 45f;

    [Header("Banking Settings")]
    public float maxBankAngle = 90f;
    public float bankSmoothSpeed = 5f;

    [Header("Animation Settings")]
    public float animationTransitionTime = 0.2f;

    #endregion

    #region Private Fields

    private Animator _animator;
    private Rigidbody _rigidbody;

    private bool _isDirectControl = false;
    private bool _isMouseHeldPrev = false;

    private float _targetAltitude;
    private Vector3 _pendingMovement = Vector3.zero;
    private float _yaw, _pitch, _roll;

    private bool _initialized = false;
    private EagleAirState _currentState = EagleAirState.Fly;

    private readonly Dictionary<EagleAirState, string> _stateAnimations = new Dictionary<EagleAirState, string>
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
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false;

        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;    // We'll manage altitude ourselves
        _rigidbody.isKinematic = false;   // So collisions are detected, but we manually set position

        if (!_initialized)
        {
            _initialized = true;

            // Ensure the eagle spawns above minAltitude
            AdjustToMinAltitude();

            _targetAltitude = transform.position.y;

            Vector3 euler = transform.rotation.eulerAngles;
            _pitch = euler.x;
            _yaw = euler.y;
            _roll = euler.z;

            PlayAnimation(EagleAirState.Fly);
            ResetAnimatorParameters();
        }
    }

    private void OnEnable()
    {
        _isDirectControl = true;
    }

    private void OnDisable()
    {
        _isDirectControl = false;
    }

    private void Update()
    {
        if (!_isDirectControl) return;

        HandleInput();
        HandleAnimationTransitions();
        HandleMouseLook();
        HandleBanking();
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if (!_isDirectControl) return;

        // Raycast to find ground level
        float groundY = GetGroundY(transform.position);
        // Clamp the altitude within minAltitude..maxAltitude from ground
        float clampedAltitude = Mathf.Clamp(
            _targetAltitude,
            groundY + minAltitude,
            groundY + maxAltitude
        );

        // Smoothly move the eagle
        float newY = Mathf.Lerp(_rigidbody.position.y, clampedAltitude, 5f * Time.fixedDeltaTime);
        Vector3 newPos = _rigidbody.position + _pendingMovement;
        newPos.y = newY;

        _rigidbody.MovePosition(newPos);

        // **Important fix**: Reset velocity so the physics engine
        // won't keep pushing the eagle after a collision.
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        // Clear the pending movement for the next frame
        _pendingMovement = Vector3.zero;
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

        _pendingMovement += forwardMovement + strafeMovement;

        bool ascendHeld = Input.GetKey(KeyCode.Space);
        bool descendHeld = Input.GetKey(KeyCode.LeftControl);

        // If player is pressing ascend, increase altitude
        if (ascendHeld)
        {
            _targetAltitude += ascendForce * Time.deltaTime;
        }
        // If pressing descend, lower altitude
        else if (descendHeld)
        {
            _targetAltitude -= descendSpeed * Time.deltaTime;
        }
        else
        {
            // IMPORTANT: When not pressing ascend/descend, set _targetAltitude
            // to the eagle's current Y, so it doesn't revert to an old stored altitude.
            _targetAltitude = transform.position.y;
        }
    }

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

    #region Animation

    private void HandleAnimationTransitions()
    {
        bool isMouseHeld = Input.GetMouseButton(0);

        if (isMouseHeld != _isMouseHeldPrev)
        {
            if (isMouseHeld)
            {
                _animator.SetBool("MouseHold", true);
            }
            else
            {
                _animator.SetBool("MouseHold", false);
                _animator.SetBool("MouseRelease", true);
            }
            _isMouseHeldPrev = isMouseHeld;
        }
        else
        {
            _animator.SetBool("MouseRelease", false);
        }

        _animator.SetBool("isGliding", Input.GetKey(KeyCode.LeftControl));
    }

    private void ResetAnimatorParameters()
    {
        _animator.SetBool("MouseHold", false);
        _animator.SetBool("MouseRelease", false);
        _animator.SetBool("isGliding", false);
        _animator.Play("Fly", 0, 0f);
    }

    private void PlayAnimation(EagleAirState newState)
    {
        if (newState == _currentState) return;

        if (_stateAnimations.TryGetValue(newState, out string animName))
        {
            _animator.CrossFadeInFixedTime(animName, animationTransitionTime);
            _currentState = newState;
        }
    }

    #endregion

    #region Helpers

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
            _targetAltitude = minAllowed;
            transform.position = pos;
        }
    }

    #endregion
}
