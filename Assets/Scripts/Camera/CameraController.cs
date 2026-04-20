using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Third-person follow camera.
    /// Autonomously finds the player vehicle using the "Player" tag to guarantee 
    /// it never gets stuck in the void during scene transitions.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Target (Auto-Assigned)")]
        [Tooltip("Leave blank. Script finds the object tagged 'Player'.")]
        [SerializeField] private Transform _target;

        [Header("Offset (relative to target)")]
        [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 6f, -12f); 

        [Header("Smoothing")]
        [SerializeField] private float _positionSmoothTime = 0.15f;
        [SerializeField] private float _rotationSlerpSpeed = 5f;

        [Header("Look-Ahead")]
        [SerializeField] private float _lookAheadDistance = 5f;
        #endregion

        #region Private Fields
        private Vector3 _velocity = Vector3.zero;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            FindTarget();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                FindTarget();
                return;
            }

            Vector3 desiredPosition = _target.position + _target.TransformDirection(_positionOffset);

            if (desiredPosition.y < 2.0f) 
            {
                desiredPosition.y = 2.0f;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                _positionSmoothTime);

            Vector3 lookTarget = _target.position + _target.forward * _lookAheadDistance;
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSlerpSpeed * Time.deltaTime);
        }
        #endregion

        #region Private Methods
        private void FindTarget()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _target = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[CameraController] Could not find an object tagged 'Player' in the scene.");
            }
        }
        #endregion
        
        #region Public Methods
        public void SetTarget(Transform target) => _target = target;
        #endregion
    }
}