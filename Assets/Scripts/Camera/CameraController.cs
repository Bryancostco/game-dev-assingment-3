using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Third-person follow camera using SmoothDamp for position and Slerp for rotation.
    /// Does NOT depend on Cinemachine, so it compiles immediately.
    /// (Design choice: look orbiting is not implemented to keep the camera simple and stable.
    ///  Look input from the Input System is ignored — the camera always trails behind the vehicle.)
    ///
    /// Attach this to the Main Camera in each gameplay scene and assign the vehicle transform.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Target")]
        [SerializeField] private Transform _target; // Assign the vehicle root transform

        [Header("Offset (relative to target)")]
        [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 4f, -8f);

        [Header("Smoothing")]
        [SerializeField] private float _positionSmoothTime = 0.15f;
        [SerializeField] private float _rotationSlerpSpeed = 5f;

        [Header("Look-Ahead")]
        [SerializeField] private float _lookAheadDistance = 3f;
        #endregion

        #region Private Fields
        private Vector3 _velocity = Vector3.zero; // Used by SmoothDamp
        #endregion

        #region Unity Lifecycle
        private void LateUpdate()
        {
            if (_target == null) return;

            // Calculate desired position: behind-and-above the vehicle in world space
            Vector3 desiredPosition = _target.position
                                    + _target.TransformDirection(_positionOffset);

            // Smooth the camera position
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                _positionSmoothTime);

            // Look at the vehicle with a slight look-ahead in the vehicle's forward direction
            Vector3 lookTarget = _target.position + _target.forward * _lookAheadDistance;
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSlerpSpeed * Time.deltaTime);
        }
        #endregion

        #region Public Methods
        /// <summary>Assigns the follow target at runtime (e.g., after vehicle is spawned).</summary>
        public void SetTarget(Transform target) => _target = target;
        #endregion
    }
}
