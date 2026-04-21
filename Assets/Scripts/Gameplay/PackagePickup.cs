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

            // Auto-find first child as visual if nothing assigned in Inspector
            if (_packageVisual == null && transform.childCount > 0)
                _packageVisual = transform.GetChild(0).gameObject;
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
            if (!IsPlayer(other)) return;

            DeliveryManager.Instance?.RegisterPickupZone(this);
            UIManager.Instance?.ShowNotification("Press E to pick up package");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            DeliveryManager.Instance?.UnregisterPickupZone(this);
        }
        #endregion

        #region Private Methods
        private void HandlePackagePickedUp(int deliveryId)
        {
            if (deliveryId != _deliveryId) return;
            if (_packageVisual != null) _packageVisual.SetActive(false);
            gameObject.SetActive(false);
        }

        // Checks the collider itself AND any parent for Player tag or VehicleController
        private static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            if (other.transform.root.CompareTag("Player")) return true;
            return other.GetComponentInParent<VehicleController>() != null;
        }
        #endregion
    }
}
