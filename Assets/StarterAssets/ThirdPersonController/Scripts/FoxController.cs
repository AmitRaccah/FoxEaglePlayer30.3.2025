using System.Collections;
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
        #region Inspector – identity & camera

        [Header("Player Identification")]
        [Tooltip("Unique ID read by CameraSwitchManager")] public int playerID = 2;

        [Header("Cinemachine / Camera")]
        public GameObject cinemachineCameraTarget;
        public float topClamp = 70f;
        public float bottomClamp = -30f;
        public float cameraAngleOffset = 0f;
        public bool lockCamera = false;
        public float cameraFollowTurnSpeed = 5f;

        #endregion

        #region Inspector – movement

        [Header("Movement Speeds")]
        public float moveSpeed = 2f;
        public float sprintSpeed = 5.335f;
        [Range(0f, 0.3f)] public float rotationSmoothTime = 0.12f;
        public float speedChangeRate = 10f;
        public float pureLateralSpeed = 2f;

        [Header("Jump & Gravity")]
        public float jumpHeight = 1.2f;
        public float gravity = -15f;
        public float jumpTimeout = 0.5f;
        public float fallTimeout = 0.15f;

        #endregion

        #region Inspector – grounded check

        [Header("Grounded Check")]
        public float groundedOffset = -0.14f;
        public float groundedRadius = 0.28f;
        public LayerMask groundLayers;
        [HideInInspector] public bool grounded = true;

        #endregion

        #region Inspector – fine‑tune input

        [Header("Input Settings")]
        public float deadZone = 0.2f;
        public float mouseLateralMultiplier = 0.5f;
        public float diagonalLateralMultiplier = 0.5f;

        [Header("Turning Settings")]
        public float turnInPlaceThreshold = 90f;
        public float turnSpeedReductionStart = 45f;
        public float turnSpeedReductionEnd = 180f;
        public float largeTurnThreshold = 45f;
        public float largeTurnSmoothTime = 0.5f;

        [Header("Reverse Movement Settings")]
        public float reverseMultiplier = 1f;
        public float reverseSteeringAngle = 30f;
        public float reverseRotationSpeed = 90f;

        [Header("Attack / Dig Settings")]
        public float biteMovementThreshold = 0.5f;
        public float biteDuration = 1f;
        public float digDuration = 1.5f;

        [Header("Gizmos")] public Vector3 gizmoOffset = Vector3.zero;

        #endregion

        #region Private references

        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private Animator _animator;
        private GameObject _mainCamera;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        #endregion

        #region Private state vars

        private float _speed;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private const float _terminalVelocity = 53f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private float _cinYaw;
        private float _cinPitch;
        private const float _inputThreshold = 0.01f;

        // animator hashes
        private static readonly int SpeedXHash = Animator.StringToHash("SpeedX");
        private static readonly int SpeedZHash = Animator.StringToHash("SpeedZ");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
        private static readonly int AttackTrigHash = Animator.StringToHash("AttackTrigger");
        private static readonly int IsDiggingHash = Animator.StringToHash("IsDigging");

        private bool _isBiting;
        private bool _isDigging;

        private float _smoothedHorizontal;
        private float _horizontalVelocity;

        private bool IsMouseScheme
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

        #region Unity lifecycle

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _animator = GetComponent<Animator>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            _cinYaw = cinemachineCameraTarget.transform.eulerAngles.y;
            _jumpTimeoutDelta = jumpTimeout;
            _fallTimeoutDelta = fallTimeout;
        }

        private void Update()
        {
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer != playerID)
            {
                _input.look = Vector2.zero; // ignore look when not active
            }

            GroundedCheck();
            HandleAttack();
            HandleDig();
            if (_isBiting) return; // lock movement during bite

            JumpAndGravity();
            Move();

            // animator sync
            _animator.SetBool(IsRunningHash, _input.sprint && _input.move.y > deadZone);
            _animator.SetBool(IsGroundedHash, grounded);
            UpdateAnimatorBlend();
        }

        private void LateUpdate()
        {
            if (CameraSwitchManager.Instance != null && CameraSwitchManager.Instance.ActivePlayer == playerID)
                CameraRotation();
        }

        #endregion

        #region Core mechanics

        private void GroundedCheck()
        {
            Vector3 spherePos = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
            grounded = Physics.CheckSphere(spherePos, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (lockCamera) return;

            float deltaMultiplier = IsMouseScheme ? 1f : Time.deltaTime;
            if (_input.look.sqrMagnitude >= _inputThreshold)
            {
                _cinYaw += _input.look.x * deltaMultiplier;
                _cinPitch += _input.look.y * deltaMultiplier;
            }

            if (_input.move.y != 0)
                _cinYaw = Mathf.LerpAngle(_cinYaw, transform.eulerAngles.y, cameraFollowTurnSpeed * Time.deltaTime);

            _cinYaw = ClampAngle(_cinYaw, float.MinValue, float.MaxValue);
            _cinPitch = ClampAngle(_cinPitch, bottomClamp, topClamp);

            cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinPitch + cameraAngleOffset, _cinYaw, 0f);
        }

        private void Move()
        {
            if (_isDigging) return;

            float targetSpeed = (_input.sprint && _input.move.y > deadZone) ? sprintSpeed : moveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0f;

            float currentSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMag = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentSpeed < targetSpeed - speedOffset || currentSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentSpeed, targetSpeed * inputMag, Time.deltaTime * speedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f; // round for animator
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 moveDir = CalculateMoveDirection(out bool updateRot);
            if (updateRot && _input.move != Vector2.zero)
            {
                float targetRot = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float smoothT = rotationSmoothTime;
                if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetRot)) > largeTurnThreshold)
                    smoothT = largeTurnSmoothTime;
                float newRot = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRot, ref _rotationVelocity, smoothT);
                transform.rotation = Quaternion.Euler(0f, newRot, 0f);
            }

            Vector3 velocity = moveDir * (_speed * Time.deltaTime);
            Vector3 vertical = Vector3.up * _verticalVelocity * Time.deltaTime;
            _controller.Move(velocity + vertical);
        }

        private Vector3 CalculateMoveDirection(out bool updateRotation)
        {
            updateRotation = true;
            Vector3 moveDir = Vector3.zero;

            if (_input.move.y < 0)
            {
                moveDir = (-transform.forward * Mathf.Abs(_input.move.y)) + (transform.right * _input.move.x);
                moveDir.Normalize();
                _speed *= reverseMultiplier;

                float steer = _input.move.x;
                float desiredRot = transform.eulerAngles.y - (steer * reverseSteeringAngle);
                float newY = Mathf.MoveTowardsAngle(transform.eulerAngles.y, desiredRot, reverseRotationSpeed * Time.deltaTime);
                transform.eulerAngles = new Vector3(0f, newY, 0f);
                updateRotation = false;
            }
            else if (Mathf.Approximately(_input.move.y, 0f) && Mathf.Abs(_input.move.x) > deadZone)
            {
                moveDir = transform.right * _input.move.x;
                _speed = pureLateralSpeed;
                updateRotation = false;
            }
            else
            {
                Vector3 camFwd = transform.forward;
                Vector3 camRight = transform.right;
                if (_mainCamera && CameraSwitchManager.Instance && CameraSwitchManager.Instance.ActivePlayer == playerID)
                {
                    camFwd = _mainCamera.transform.forward; camFwd.y = 0f; camFwd.Normalize();
                    camRight = _mainCamera.transform.right; camRight.y = 0f; camRight.Normalize();
                    camRight *= mouseLateralMultiplier * diagonalLateralMultiplier;
                }
                moveDir = camFwd * _input.move.y + camRight * _input.move.x;
                moveDir.Normalize();

                float angleDiff = Vector3.Angle(transform.forward, moveDir);
                if (_input.move.y > 0 && angleDiff > turnInPlaceThreshold && !_input.sprint) _speed = 0f;
                else if (angleDiff > turnSpeedReductionStart)
                {
                    float t = Mathf.Clamp01((turnSpeedReductionEnd - angleDiff) / (turnSpeedReductionEnd - turnSpeedReductionStart));
                    _speed *= t;
                }
            }
            return moveDir;
        }

        private void JumpAndGravity()
        {
            if (_animator.GetBool(IsDiggingHash) || _isBiting) return;

            if (grounded)
            {
                _fallTimeoutDelta = fallTimeout;
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0f)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    _animator.SetTrigger(JumpTriggerHash);
                    _input.jump = false;
                }
                if (_jumpTimeoutDelta > 0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = jumpTimeout;
                if (_fallTimeoutDelta > 0f) _fallTimeoutDelta -= Time.deltaTime;
            }
            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += gravity * Time.deltaTime;
        }

        private void HandleAttack()
        {
            if (CameraSwitchManager.Instance && CameraSwitchManager.Instance.ActivePlayer != playerID) return;

            if (Input.GetMouseButtonDown(0) && grounded && !_input.sprint && _input.move.magnitude <= biteMovementThreshold)
            {
                if (!_isBiting)
                {
                    _isBiting = true;
                    _animator.SetTrigger(AttackTrigHash);
                    StartCoroutine(BiteRoutine());
                }
            }
        }

        private IEnumerator BiteRoutine()
        {
            yield return new WaitForSeconds(biteDuration);
            _isBiting = false;
        }

        private void HandleDig()
        {
            if (CameraSwitchManager.Instance && CameraSwitchManager.Instance.ActivePlayer != playerID) return;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                _isDigging = true;
                _animator.SetBool(IsDiggingHash, true);
                _input.move = Vector2.zero; _input.jump = false;
            }
            else
            {
                _isDigging = false;
                _animator.SetBool(IsDiggingHash, false);
            }
        }

        private void UpdateAnimatorBlend()
        {
            float fwd = Mathf.Abs(_input.move.y) < deadZone ? 0f : _input.move.y;
            float lat = Mathf.Abs(_input.move.x) < deadZone ? 0f : _input.move.x;

            // turn‑in‑place adjustment
            if (_input.move.y > 0)
            {
                Vector3 desired = CalculateMoveDirection(out _);
                float angleDiff = Vector3.Angle(transform.forward, desired);
                if (!_input.sprint && angleDiff > turnInPlaceThreshold) { fwd = 0f; lat = Mathf.Sign(_input.move.x); }
            }
            if (_input.move.y > 0 && CameraSwitchManager.Instance && CameraSwitchManager.Instance.ActivePlayer == playerID)
            {
                if (Mathf.Abs(_input.look.x) > 0.1f) lat = Mathf.Sign(_input.look.x);
            }
            _animator.SetFloat(SpeedXHash, lat);
            _animator.SetFloat(SpeedZHash, fwd);
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = grounded ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
            Vector3 pos = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z) + gizmoOffset;
            Gizmos.DrawSphere(pos, groundedRadius);
        }

        #endregion
    }
}
