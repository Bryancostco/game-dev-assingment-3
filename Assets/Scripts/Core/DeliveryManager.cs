using System;
using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Manages the entire delivery gameplay loop.
    /// Tracks which package the player is carrying, validates pickups and drop-offs,
    /// prevents duplicate scoring, and fires events for UI and GameManager to react to.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class DeliveryManager : MonoBehaviour
    {
        #region Singleton
        public static DeliveryManager Instance { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired when the player successfully picks up a package (deliveryId).</summary>
        public static event Action<int> OnPackagePickedUp;

        /// <summary>Fired when a delivery is successfully completed (deliveryId).</summary>
        public static event Action<int> OnDeliverySuccessful;

        /// <summary>Fired when the delivery count changes — use for HUD updates (completed, total).</summary>
        public static event Action<int, int> OnDeliveryCountChanged;

        /// <summary>Fired when every delivery in the level has been completed.</summary>
        public static event Action OnAllDeliveriesComplete;
        #endregion

        #region Private Fields
        private int         _currentDeliveryId   = -1; // -1 means not carrying any package
        private int         _deliveriesCompleted  = 0;
        private int         _totalDeliveries      = 5;
        private bool        _isProcessing         = false; // Prevents spam triggers in the same frame
        private PackagePickup _pickupInRange       = null;  // The pickup zone currently in range
        #endregion

        #region Properties
        public bool IsCarryingPackage   => _currentDeliveryId >= 0;
        public int  CurrentDeliveryId   => _currentDeliveryId;
        public int  DeliveriesCompleted => _deliveriesCompleted;
        public int  TotalDeliveries     => _totalDeliveries;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Guard: destroy duplicate Singleton from DontDestroyOnLoad
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Public — Level Lifecycle
        /// <summary>
        /// Resets delivery state for a new level.
        /// Called by SceneUIConnector before GameManager.InitializeLevel.
        /// </summary>
        public void InitializeLevel(int totalDeliveries)
        {
            _totalDeliveries     = totalDeliveries;
            _deliveriesCompleted = 0;
            _currentDeliveryId   = -1;
            _isProcessing        = false;
            _pickupInRange       = null;
        }
        #endregion

        #region Public — Pickup Zone Registration
        /// <summary>
        /// Registers a pickup zone as the one currently in range.
        /// Called by PackagePickup.OnTriggerEnter so VehicleController can
        /// route interact presses here without polling.
        /// </summary>
        public void RegisterPickupZone(PackagePickup pickup) => _pickupInRange = pickup;

        /// <summary>Clears the registered pickup zone when the player leaves it.</summary>
        public void UnregisterPickupZone(PackagePickup pickup)
        {
            // Only clear if this is the pickup currently registered
            // (prevents clearing a newer registration when exiting an old zone)
            if (_pickupInRange == pickup) _pickupInRange = null;
        }

        /// <summary>
        /// Called by VehicleController when the player presses the Interact button.
        /// Routes to TryPickup if a pickup zone is currently in range.
        /// </summary>
        public void PlayerInteracted()
        {
            if (_pickupInRange != null)
                TryPickup(_pickupInRange.DeliveryId);
        }
        #endregion

        #region Public — Pickup / Delivery
        /// <summary>
        /// Attempts to pick up the package with the given delivery ID.
        /// Blocks with a UI message if the player is already carrying a package.
        /// </summary>
        public void TryPickup(int deliveryId)
        {
            // Guard: spam-trigger protection — only one pickup per frame
            if (_isProcessing) return;

            // Guard: player already carrying a package
            if (IsCarryingPackage)
            {
                UIManager.Instance?.ShowNotification("Already carrying a package!");
                return;
            }

            // Guard: pickups are only valid while playing
            if (GameManager.Instance?.CurrentState != GameState.Playing) return;

            _isProcessing      = true;
            _currentDeliveryId = deliveryId;

            OnPackagePickedUp?.Invoke(deliveryId);
            UIManager.Instance?.SetObjectiveText($"Deliver to Zone {deliveryId + 1}");

            StartCoroutine(ResetProcessingFlag());
        }

        /// <summary>
        /// Attempts to deliver the currently carried package to the given delivery zone.
        /// Blocks with a UI message if the player isn't carrying anything, or has the wrong package.
        /// </summary>
        public void TryDeliver(int deliveryId)
        {
            // Guard: spam-trigger protection
            if (_isProcessing) return;

            // Guard: player has no package
            if (!IsCarryingPackage)
            {
                UIManager.Instance?.ShowNotification("No package to deliver!");
                return;
            }

            // Guard: wrong delivery zone
            if (_currentDeliveryId != deliveryId)
            {
                UIManager.Instance?.ShowNotification("Wrong delivery zone!");
                return;
            }

            // Guard: deliveries only valid while playing
            if (GameManager.Instance?.CurrentState != GameState.Playing) return;

            _isProcessing = true;

            int completedId  = _currentDeliveryId;
            _currentDeliveryId = -1;
            _deliveriesCompleted++;

            OnDeliverySuccessful?.Invoke(completedId);
            OnDeliveryCountChanged?.Invoke(_deliveriesCompleted, _totalDeliveries);

            // Check win condition — fires GameManager.Won via event chain
            if (_deliveriesCompleted >= _totalDeliveries)
                OnAllDeliveriesComplete?.Invoke();
            else
                UIManager.Instance?.SetObjectiveText("Pick up the next package");

            StartCoroutine(ResetProcessingFlag());
        }
        #endregion

        #region Private Methods
        /// <summary>Waits one frame before allowing another trigger, preventing same-frame duplicates.</summary>
        private System.Collections.IEnumerator ResetProcessingFlag()
        {
            yield return null;
            _isProcessing = false;
        }
        #endregion
    }
}
