using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Animates a dropoff zone marker: continuous pulsing scale that draws the player's eye.
    /// Attach to the visual marker child of a PackageDropoff zone.
    /// </summary>
    public class DropoffPulseAnimation : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Pulse Settings")]
        [SerializeField] private float _scaleMin       = 0.85f;
        [SerializeField] private float _scaleMax       = 1.15f;
        [SerializeField] private float _pulseFrequency = 1.2f; // cycles per second
        #endregion

        #region Private Fields
        private Vector3 _baseScale;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            // Oscillate between scaleMin and scaleMax uniformly
            float t     = (Mathf.Sin(Time.time * _pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(_scaleMin, _scaleMax, t);
            transform.localScale = _baseScale * scale;
        }
        #endregion
    }
}
