using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryGame
{
    /// <summary>
    /// WheelCollider-based vehicle physics controller using Unity's New Input System.
    /// Rear-wheel drive (RWD) with anti-roll bar logic and automatic visual wheel sync.
    /// Blocks all input when the game is not in the Playing state.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Wheel Colliders")]
        [SerializeField] private WheelCollider _wheelFL;
        [SerializeField] private WheelCollider _wheelFR;
        [SerializeField] private WheelCollider _wheelRL;
        [SerializeField] private WheelCollider _wheelRR;

        [Header("Wheel Visual Meshes")]
        [SerializeField] private Transform _meshFL;
        [SerializeField] private Transform _meshFR;
        [SerializeField] private Transform _meshRL;
        [SerializeField] private Transform _meshRR;

        [Header("Driving Parameters")]
        [SerializeField] private float _maxMotorTorque   = 400f;
        [SerializeField] private float _maxSteeringAngle = 35f;
        [SerializeField] private float _maxSpeed         = 25f;   // m/s
        [SerializeField] private float _brakeTorque      = 2000f;

        [Header("Anti-Roll Bar")]
        [SerializeField] private float _antiRollForce = 5000f;

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActionsAsset;
        #endregion

        #region Private Fields — Physics
        private Rigidbody _rb;
        private Vector3   _lastValidPosition;
        private Quaternion _lastValidRotation;
        private float     _flipTimer = 0f;
        private const float FLIP_RESET_THRESHOLD = 3f;
        #endregion

        #region Private Fields — Input
        private InputAction _moveAction;
        private InputAction _brakeAction;
        private InputAction _interactAction;
        private InputAction _pauseAction;
        private Vector2     _moveInput;
        private bool        _brakeHeld;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Lower center of mass prevents flipping at speed
            _rb.centerOfMass          = new Vector3(0f, -0.5f, 0f);
            _rb.mass                  = 1400f;
            _rb.interpolation         = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Store initial pose as the "safe" reset target
            _lastValidPosition = transform.position + Vector3.up;
            _lastValidRotation = transform.rotation;

            BindInputActions();
        }

        private void OnEnable()
        {
            var playerMap = _inputActionsAsset?.FindActionMap("Player");
            // Only enable player input if we are currently playing
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
                playerMap?.Enable();

            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            _inputActionsAsset?.FindActionMap("Player")?.Disable();
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            UnbindInputActions();
        }

        private void FixedUpdate()
        {
            // Block all driving input when not in Playing state.
            // If GameManager is absent (direct scene testing), treat as Playing.
            bool isPlaying = GameManager.Instance == null ||
                             GameManager.Instance.CurrentState == GameState.Playing;

            float motor   = isPlaying ? _moveInput.y : 0f;
            float steer   = isPlaying ? _moveInput.x : 0f;
            bool  braking = isPlaying ? _brakeHeld   : true; // Force brake when paused/won/lost

            ApplyMotor(motor);
            ApplySteering(steer);
            ApplyBrakes(braking);
            ApplyAntiRollBar(_wheelFL, _wheelFR);
            ApplyAntiRollBar(_wheelRL, _wheelRR);
            SyncAllWheelMeshes();

            if (isPlaying)
            {
                AudioManager.Instance?.UpdateVehicleSpeed(_rb.linearVelocity.magnitude);
                TrackFlip();
            }
        }
        #endregion

        #region Input Binding
        private void BindInputActions()
        {
            if (_inputActionsAsset == null)
            {
                Debug.LogError("[VehicleController] InputActionAsset not assigned in the Inspector.");
                return;
            }

            var map = _inputActionsAsset.FindActionMap("Player");
            if (map == null)
            {
                Debug.LogError("[VehicleController] 'Player' action map not found in InputActionAsset.");
                return;
            }

            // Enable the map here directly so it works regardless of GameManager state
            map.Enable();

            _moveAction     = map.FindAction("Move");
            _brakeAction    = map.FindAction("Brake");
            _interactAction = map.FindAction("Interact");
            _pauseAction    = map.FindAction("Pause");


            if (_moveAction != null)
            {
                _moveAction.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
                _moveAction.canceled  += ctx => _moveInput = Vector2.zero;
            }
            if (_brakeAction != null)
            {
                _brakeAction.performed += ctx => _brakeHeld = true;
                _brakeAction.canceled  += ctx => _brakeHeld = false;
            }
            if (_interactAction != null)
                _interactAction.performed += OnInteract;

            if (_pauseAction != null)
                _pauseAction.performed += ctx => GameManager.Instance?.TogglePause();
        }

        private void UnbindInputActions()
        {
            if (_interactAction != null) _interactAction.performed -= OnInteract;
            if (_pauseAction    != null) _pauseAction.performed    -= ctx => GameManager.Instance?.TogglePause();
        }

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            // Input fires in Playing state only (Player map is disabled while paused)
            DeliveryManager.Instance?.PlayerInteracted();
        }
        #endregion

        #region Physics Methods
        private void ApplyMotor(float vertical)
        {
            // Cap torque at max speed to prevent runaway acceleration
            float speed  = _rb.linearVelocity.magnitude;
            float torque = speed < _maxSpeed ? vertical * _maxMotorTorque : 0f;
            _wheelRL.motorTorque = torque;
            _wheelRR.motorTorque = torque;
        }

        private void ApplySteering(float horizontal)
        {
            float angle = horizontal * _maxSteeringAngle;
            _wheelFL.steerAngle = angle;
            _wheelFR.steerAngle = angle;
        }

        private void ApplyBrakes(bool active)
        {
            float torque = active ? _brakeTorque : 0f;
            _wheelFL.brakeTorque = torque;
            _wheelFR.brakeTorque = torque;
            _wheelRL.brakeTorque = torque;
            _wheelRR.brakeTorque = torque;
        }

        /// <summary>
        /// Anti-roll bar: reduces the suspension travel difference between left and right wheels
        /// to counteract body roll at speed.
        /// </summary>
        private void ApplyAntiRollBar(WheelCollider left, WheelCollider right)
        {
            float travelL = 1f;
            float travelR = 1f;

            bool groundedL = left.GetGroundHit(out WheelHit hitL);
            if (groundedL)
                travelL = (-left.transform.InverseTransformPoint(hitL.point).y - left.radius)
                          / left.suspensionDistance;

            bool groundedR = right.GetGroundHit(out WheelHit hitR);
            if (groundedR)
                travelR = (-right.transform.InverseTransformPoint(hitR.point).y - right.radius)
                          / right.suspensionDistance;

            float force = (travelL - travelR) * _antiRollForce;
            if (groundedL) _rb.AddForceAtPosition(left.transform.up  * -force, left.transform.position);
            if (groundedR) _rb.AddForceAtPosition(right.transform.up *  force, right.transform.position);
        }

        private void SyncAllWheelMeshes()
        {
            SyncWheel(_wheelFL, _meshFL);
            SyncWheel(_wheelFR, _meshFR);
            SyncWheel(_wheelRL, _meshRL);
            SyncWheel(_wheelRR, _meshRR);
        }

        private static void SyncWheel(WheelCollider col, Transform mesh)
        {
            if (col == null || mesh == null) return;
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.SetPositionAndRotation(pos, rot);
        }

        /// <summary>
        /// Detects if the vehicle has been upside-down (transform.up.y less than 0) for too long
        /// and auto-resets it to the last safe position/rotation.
        /// </summary>
        private void TrackFlip()
        {
            if (transform.up.y < 0f)
            {
                _flipTimer += Time.fixedDeltaTime;
                if (_flipTimer >= FLIP_RESET_THRESHOLD)
                    ResetVehicle();
            }
            else
            {
                _flipTimer = 0f;
                // Cache valid ground pose for reset fallback
                _lastValidPosition = transform.position + Vector3.up * 0.5f;
                _lastValidRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
        }

        private void ResetVehicle()
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_lastValidPosition, _lastValidRotation);
            _flipTimer = 0f;
        }
        #endregion

        #region Event Handlers
        private void HandleGameStateChanged(GameState state)
        {
            var playerMap = _inputActionsAsset?.FindActionMap("Player");
            var uiMap     = _inputActionsAsset?.FindActionMap("UI");

            switch (state)
            {
                case GameState.Playing:
                    // Input fires during play; disable UI map
                    playerMap?.Enable();
                    uiMap?.Disable();
                    break;
                case GameState.Paused:
                    // Disable player controls; enable UI map for menu navigation
                    playerMap?.Disable();
                    uiMap?.Enable();
                    break;
                case GameState.Won:
                case GameState.Lost:
                case GameState.MainMenu:
                    playerMap?.Disable();
                    uiMap?.Disable();
                    break;
            }
        }
        #endregion
    }
}
