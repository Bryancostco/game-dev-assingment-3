using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Trigger zone for completing a delivery.
    /// Validates that the player is carrying the correct package before
    /// forwarding to DeliveryManager. Hides its visual marker on success.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PackageDropoff : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Delivery Settings")]
        [SerializeField] private int   _deliveryId    = 0;
        [SerializeField] private float _triggerRadius = 4f;

        [Header("References")]
        [SerializeField] private GameObject _markerVisual; // Visible ground decal / zone marker
        #endregion

        #region Private Fields
        private SphereCollider _trigger;
        #endregion

        #region Properties
        /// <summary>The delivery ID this dropoff zone accepts.</summary>
        public int DeliveryId => _deliveryId;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _trigger           = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius    = _triggerRadius;
            gameObject.layer   = LayerMask.NameToLayer("Dropoff");
        }

        private void OnEnable()
        {
            DeliveryManager.OnDeliverySuccessful += HandleDeliverySuccessful;
        }

        private void OnDisable()
        {
            DeliveryManager.OnDeliverySuccessful -= HandleDeliverySuccessful;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Guard: only process when Playing (timer expiry or pause must not award delivery)
            if (GameManager.Instance?.CurrentState != GameState.Playing) return;

            // DeliveryManager validates whether the player holds the right package
            DeliveryManager.Instance?.TryDeliver(_deliveryId);
        }
        #endregion

        #region Private Methods
        private void HandleDeliverySuccessful(int deliveryId)
        {
            if (deliveryId != _deliveryId) return;

            // Hide the ground marker after the matching delivery is completed
            if (_markerVisual != null) _markerVisual.SetActive(false);
            gameObject.SetActive(false);
        }
        #endregion
    }
}
