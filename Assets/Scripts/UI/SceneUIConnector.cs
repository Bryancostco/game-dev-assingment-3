using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Scene-specific connector that lives in each gameplay scene.
    /// Holds all Inspector-assigned UI references and level parameters,
    /// then passes them to the persistent Singleton managers on scene start.
    ///
    /// This is the ONLY place in the codebase where [SerializeField] UI references are stored —
    /// UIManager itself remains free of scene-specific serialised fields so it can persist.
    /// </summary>
    public class SceneUIConnector : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Level Parameters")]
        [SerializeField] private int   _totalDeliveries = 5;
        [SerializeField] private float _levelTime       = 180f;

        [Header("UI References")]
        [SerializeField] private GameplayUIRefs _uiRefs;

        [Header("Camera References (optional)")]
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private MiniMapCamera    _miniMapCamera;
        [SerializeField] private Transform        _vehicleTransform;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 1. Reset delivery state for this level
            DeliveryManager.Instance?.InitializeLevel(_totalDeliveries);

            // 2. Wire UI to UIManager (must happen before InitializeLevel triggers Playing state)
            UIManager.Instance?.Initialize(_uiRefs);

            // 3. Wire cameras to the vehicle
            _cameraController?.SetTarget(_vehicleTransform);
            _miniMapCamera?.SetTarget(_vehicleTransform);

            // 4. Start the level (sets GameState.Playing, starts the timer)
            GameManager.Instance?.InitializeLevel(_totalDeliveries, _levelTime);
        }
        #endregion
    }
}
