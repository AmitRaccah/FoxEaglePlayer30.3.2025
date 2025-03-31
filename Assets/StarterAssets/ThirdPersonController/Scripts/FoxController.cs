using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets.Fox
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FoxController : MonoBehaviour
    {
        #region Public Fields

        [Header("Player Identification")]
        [Tooltip("Unique ID for this character. CameraSwitchManager uses this to determine which character is active.")]
        public int playerID = 2;

        [Header("Player Movement Settings")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        [Range(0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;
        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Grounded Check")]
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;
        public bool Grounded = true;

        [Header("Cinemachine & Camera")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70f;
        public float BottomClamp = -30f;
        public float CameraAngleOverride = 0f;
        public bool LockCameraPosition = false;
        public float CameraFollowTurnSpeed = 5f;

        [Header("Input Settings")]
        public float deadZone = 0.2f;
        public float mouseLateralMultiplier = 0.5f;
        public float DiagonalLateralMultiplier = 0.5f;

        [Header("Lateral Movement Settings")]
        public float PureLateralSpeed = 2.0f;

        [Header("Turning Settings")]
        public float TurnInPlaceThreshold = 90f;
        public float TurnSpeedReductionStartAngle = 45f;
        public float TurnSpeedReductionEndAngle = 180f;
        public float LargeTurnThreshold = 45f;
        public float LargeTurnSmoothTime = 0.5f;

        [Header("Reverse Movement Settings")]
        public float ReverseMultiplier = 1.0f;
        public float ReverseSteeringAngle = 30f;
        public float ReverseRotationSpeed = 90f;

        [Header("Biting Settings")]
        public float biteMovementThreshold = 0.5f;
        public float biteDuration = 1.0f;

        [Header("Digging Settings")]
        public float digDuration = 1.5f;

        [Header("Gizmo Settings")]
        public Vector3 gizmoOffset = Vector3.zero;

        #endregion

        #region Private Fields

        private StarterAssetsInputs _input;
        private CharacterController _controller;
        private Animator _animator;
        private GameObject _mainCamera;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private float _speed;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private float _terminalVelocity = 53f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private const float _threshold = 0.01f;

        // Precomputed animator parameter hashes
        private int SpeedXHash = Animator.StringToHash("SpeedX");
        private int SpeedZHash = Animator.StringToHash("SpeedZ");
        private int IsRunningHash = Animator.StringToHash("IsRunning");
        private int isGroundedHash = Animator.StringToHash("isGrounded");
        private int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
        private int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private int IsDiggingHash = Animator.StringToHash("IsDigging");

        private bool isBiting = false;
        private bool isDigging = false;

        private float _smoothedHorizontalInput = 0f;
        private float _horizontalInputVelocity = 0f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _animator = GetComponent<Animator>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            if (CinemachineCameraTarget == null)
            {
                Debug.LogError("CinemachineCameraTarget is not assigned on FoxController.");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            // If this fox is not active, zero out look input.
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer != playerID)
            {
                _input.look = Vector2.zero;
            }

            GroundedCheck();
            HandleAttack();
            HandleDig();
            if (isBiting)
                return;

            JumpAndGravity();
            Move();

            _animator.SetBool(IsRunningHash, _input.sprint && _input.move.y > deadZone);
            _animator.SetBool(isGroundedHash, Grounded);

            float clampedX = Mathf.Abs(_input.move.x) < deadZone ? 0f : _input.move.x;
            float clampedZ = Mathf.Abs(_input.move.y) < deadZone ? 0f : _input.move.y;
            float animForward = clampedZ;
            float animLateral = clampedX;

            if (_input.move.y > 0)
            {
                Vector3 camForward = transform.forward;
                Vector3 camRight = transform.right;
                if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer == playerID && _mainCamera != null)
                {
                    camForward = _mainCamera.transform.forward;
                    camForward.y = 0f;
                    camForward.Normalize();
                    camRight = _mainCamera.transform.right;
                    camRight.y = 0f;
                    camRight.Normalize();
                    camRight *= (mouseLateralMultiplier * DiagonalLateralMultiplier);
                }
                Vector3 desiredDir = (camForward * _input.move.y) + (camRight * _input.move.x);
                desiredDir.Normalize();
                float angleDiff = Vector3.Angle(transform.forward, desiredDir);
                if (!_input.sprint && angleDiff > TurnInPlaceThreshold)
                {
                    animForward = 0f;
                    animLateral = (Mathf.Abs(_input.move.x) < 0.1f) ? (Vector3.Cross(transform.forward, desiredDir).y > 0 ? 1f : -1f) : Mathf.Sign(_input.move.x);
                }
            }
            if (_input.move.y > 0 && CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer == playerID)
            {
                if (Mathf.Abs(_input.look.x) > 0.1f)
                {
                    animLateral = Mathf.Sign(_input.look.x);
                }
            }
            _animator.SetFloat(SpeedXHash, animLateral);
            _animator.SetFloat(SpeedZHash, animForward);
        }

        private void LateUpdate()
        {
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer == playerID)
            {
                CameraRotation();
            }
        }

        #endregion

        #region Private Methods

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer != playerID)
                return;

            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1f : Time.deltaTime;
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }
            if (_input.move.y > 0 && Mathf.Abs(_input.move.x) > deadZone)
            {
                _cinemachineTargetYaw = Mathf.LerpAngle(_cinemachineTargetYaw, transform.eulerAngles.y, CameraFollowTurnSpeed * Time.deltaTime);
            }
            else if (_input.move.y < 0)
            {
                _cinemachineTargetYaw = Mathf.LerpAngle(_cinemachineTargetYaw, transform.eulerAngles.y, CameraFollowTurnSpeed * Time.deltaTime);
            }
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0f);
        }

        private void Move()
        {
            if (isDigging)
                return;

            float effectiveSpeed = 0f;
            if (!isBiting)
            {
                float targetSpeed = (_input.sprint && _input.move.y > deadZone) ? SprintSpeed : MoveSpeed;
                if (_input.move == Vector2.zero)
                    targetSpeed = 0f;

                float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
                float speedOffset = 0.1f;
                float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

                if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
                {
                    _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = targetSpeed;
                }
                effectiveSpeed = _speed;
            }

            Vector3 moveDir = Vector3.zero;
            bool updateRotation = true;

            if (_input.move.y < 0)
            {
                moveDir = (Mathf.Abs(_input.move.x) < deadZone) ? -transform.forward : (-transform.forward * Mathf.Abs(_input.move.y)) + (transform.right * _input.move.x);
                moveDir.Normalize();
                effectiveSpeed *= ReverseMultiplier;

                float steering = _input.move.x;
                float desiredRotation = transform.eulerAngles.y - (steering * ReverseSteeringAngle);
                float newYRotation = Mathf.MoveTowardsAngle(transform.eulerAngles.y, desiredRotation, ReverseRotationSpeed * Time.deltaTime);
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, newYRotation, transform.eulerAngles.z);
                updateRotation = false;
            }
            else if (Mathf.Approximately(_input.move.y, 0f) && Mathf.Abs(_input.move.x) > deadZone)
            {
                moveDir = transform.right * _input.move.x;
                effectiveSpeed = PureLateralSpeed;
                updateRotation = false;
            }
            else
            {
                Vector3 fwd = transform.forward;
                Vector3 rgt = transform.right;
                if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer == playerID && _mainCamera != null)
                {
                    fwd = _mainCamera.transform.forward;
                    fwd.y = 0f;
                    fwd.Normalize();
                    rgt = _mainCamera.transform.right;
                    rgt.y = 0f;
                    rgt.Normalize();
                    rgt *= (mouseLateralMultiplier * DiagonalLateralMultiplier);
                }
                moveDir = (fwd * _input.move.y) + (rgt * _input.move.x);
                moveDir.Normalize();
                float angleDiff = Vector3.Angle(transform.forward, moveDir);
                if (_input.move.y > 0 && angleDiff > TurnInPlaceThreshold && !_input.sprint)
                    effectiveSpeed = 0f;
                else if (angleDiff > TurnSpeedReductionStartAngle)
                {
                    float t = Mathf.Clamp01((TurnSpeedReductionEndAngle - angleDiff) / (TurnSpeedReductionEndAngle - TurnSpeedReductionStartAngle));
                    effectiveSpeed *= t;
                }
            }

            if (updateRotation && _input.move != Vector2.zero)
            {
                float targetRotation = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothTime = RotationSmoothTime;
                if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetRotation)) > LargeTurnThreshold)
                    smoothTime = LargeTurnSmoothTime;
                float newRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref _rotationVelocity, smoothTime);
                transform.rotation = Quaternion.Euler(0f, newRotation, 0f);
            }

            Vector3 velocity = moveDir * (effectiveSpeed * Time.deltaTime);
            Vector3 verticalMovement = new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime;
            _controller.Move(velocity + verticalMovement);
        }

        private void JumpAndGravity()
        {
            if ((_animator && _animator.GetBool(IsDiggingHash)) || isBiting)
                return;

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _animator?.SetTrigger(JumpTriggerHash);
                    _input.jump = false;
                }
                if (_jumpTimeoutDelta >= 0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                _input.jump = false;
            }
            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private void HandleDig()
        {
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer != playerID)
                return;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                isDigging = true;
                if (_animator)
                    _animator.SetBool(IsDiggingHash, true);
                _input.move = Vector2.zero;
                _input.jump = false;
            }
            else
            {
                isDigging = false;
                if (_animator)
                    _animator.SetBool(IsDiggingHash, false);
            }
        }

        private void HandleAttack()
        {
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer != playerID)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                if (Grounded && !_input.sprint && _input.move.magnitude <= biteMovementThreshold)
                {
                    if (!isBiting)
                    {
                        isBiting = true;
                        _animator?.SetTrigger(AttackTriggerHash);
                        StartCoroutine(BiteRoutine());
                    }
                }
            }
        }

        private IEnumerator BiteRoutine()
        {
            yield return new WaitForSeconds(biteDuration);
            isBiting = false;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f)
                angle += 360f;
            if (angle > 360f)
                angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Color color = Grounded ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
            Gizmos.color = color;
            Vector3 pos = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Gizmos.DrawSphere(pos, GroundedRadius);
        }

        #endregion
    }
}
