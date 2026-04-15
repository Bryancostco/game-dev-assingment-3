using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Top-down orthographic camera that follows the vehicle on the X-Z plane.
    /// Renders to a RenderTexture which is displayed on a RawImage in the HUD.
    ///
    /// Setup: attach to an orthographic Camera GameObject and assign the vehicle transform.
    /// The Camera's RenderTexture should already be assigned in the Inspector and linked to
    /// the HUD RawImage's Texture field.
    /// </summary>
    public class MiniMapCamera : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Follow Target")]
        [SerializeField] private Transform _target; // Vehicle root transform

        [Header("Map Settings")]
        [SerializeField] private float _height          = 50f;  // World units above the vehicle
        [SerializeField] private float _orthographicSize = 40f; // Coverage radius in world units
        #endregion

        #region Private Fields
        private Camera _cam;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null)
            {
                _cam.orthographic     = true;
                _cam.orthographicSize = _orthographicSize;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Follow vehicle on XZ; keep fixed height (Y stays constant)
            transform.position = new Vector3(
                _target.position.x,
                _target.position.y + _height,
                _target.position.z);

            // Always look straight down
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        #endregion

        #region Public Methods
        /// <summary>Assigns the follow target at runtime.</summary>
        public void SetTarget(Transform target) => _target = target;
        #endregion
    }
}
