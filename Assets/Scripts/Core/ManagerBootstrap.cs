using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Bootstraps all persistent Singleton managers in the MainMenu scene.
    /// Place this on a single "Managers" GameObject in the MainMenu scene only.
    /// All manager scripts (GameManager, UIManager, etc.) are children of this object;
    /// they DontDestroyOnLoad themselves, so they persist across all scenes.
    ///
    /// This script itself is not a Singleton — it exists only to ensure the managers
    /// are present and initialised before any other scene logic runs.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before all other scripts
    public class ManagerBootstrap : MonoBehaviour
    {
        // All Awake() calls complete before any Start() runs, so managers are guaranteed to have
        // set their Instance fields by the time this check runs.
        private void Start()
        {
            if (GameManager.Instance     == null) Debug.LogWarning("[Bootstrap] GameManager not found.");
            if (DeliveryManager.Instance == null) Debug.LogWarning("[Bootstrap] DeliveryManager not found.");
            if (UIManager.Instance       == null) Debug.LogWarning("[Bootstrap] UIManager not found.");
            if (AudioManager.Instance    == null) Debug.LogWarning("[Bootstrap] AudioManager not found.");
            if (SaveManager.Instance     == null) Debug.LogWarning("[Bootstrap] SaveManager not found.");
            if (SettingsManager.Instance == null) Debug.LogWarning("[Bootstrap] SettingsManager not found.");
        }
    }
}
