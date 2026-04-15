using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Trigger zone for picking up a package.
    /// Registers with DeliveryManager while the player vehicle is in range so that
    /// the Interact button routes directly here without any polling loop.
    /// Hides itself once the package has been picked up.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PackagePickup : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Delivery Settings")]
        [SerializeField] private int   _deliveryId    = 0;
        [SerializeField] private float _triggerRadius = 3f;

        [Header("References")]
        [SerializeField] private GameObject _packageVisual; // The visible hovering box
        #endregion

        #region Private Fields
        private SphereCollider _trigger;
        #endregion

        #region Properties
        /// <summary>The delivery ID this pickup belongs to.</summary>
        public int DeliveryId => _deliveryId;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _trigger           = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius    = _triggerRadius;
            gameObject.layer   = LayerMask.NameToLayer("Pickup");
        }

        private void OnEnable()
        {
            // Listen for successful pickup to hide this zone
            DeliveryManager.OnPackagePickedUp += HandlePackagePickedUp;
        }

        private void OnDisable()
        {
            DeliveryManager.OnPackagePickedUp -= HandlePackagePickedUp;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Register this zone so DeliveryManager.PlayerInteracted() routes here
            DeliveryManager.Instance?.RegisterPickupZone(this);
            UIManager.Instance?.ShowNotification("Press E to pick up package");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Unregister so interact presses outside this zone are ignored
            DeliveryManager.Instance?.UnregisterPickupZone(this);
        }
        #endregion

        #region Private Methods
        private void HandlePackagePickedUp(int deliveryId)
        {
            if (deliveryId != _deliveryId) return;

            // Hide the visual and deactivate trigger once collected
            if (_packageVisual != null) _packageVisual.SetActive(false);
            gameObject.SetActive(false);
        }
        #endregion
    }
}
