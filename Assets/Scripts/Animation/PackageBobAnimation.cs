using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Animates a pickup package: continuous Y-axis rotation and a hovering sine-wave bob.
    /// Attach to the visual child object of a PackagePickup zone.
    /// </summary>
    public class PackageBobAnimation : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Rotation")]
        [SerializeField] private float _rotationSpeed = 90f; // degrees per second

        [Header("Bob")]
        [SerializeField] private float _bobAmplitude  = 0.2f; // world units up/down
        [SerializeField] private float _bobFrequency  = 1.5f; // cycles per second
        #endregion

        #region Private Fields
        private Vector3 _originPosition;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Record the original local position so the bob is relative to it
            _originPosition = transform.localPosition;
        }

        private void Update()
        {
            // Y-axis rotation (uses local up so it works regardless of parent orientation)
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);

            // Sine-wave vertical bob relative to origin
            float yOffset = Mathf.Sin(Time.time * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
            transform.localPosition = _originPosition + new Vector3(0f, yOffset, 0f);
        }
        #endregion
    }
}
