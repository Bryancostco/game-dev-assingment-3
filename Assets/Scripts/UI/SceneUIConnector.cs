using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Holds all Inspector-assigned UI references and level parameters,
    /// then passes them to the persistent Singleton managers on scene start.
    /// </summary>
    public class SceneUIConnector : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Level Parameters")]
        [SerializeField] private int   _totalDeliveries = 5;
        [SerializeField] private float _levelTime       = 180f;

        [Header("UI References")]
        [SerializeField] private GameplayUIRefs _uiRefs;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            DeliveryManager.Instance?.InitializeLevel(_totalDeliveries);
            UIManager.Instance?.Initialize(_uiRefs);
            GameManager.Instance?.InitializeLevel(_totalDeliveries, _levelTime);
        }
        #endregion
    }
}