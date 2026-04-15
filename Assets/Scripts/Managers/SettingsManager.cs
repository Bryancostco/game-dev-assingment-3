using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Exposes UI-bindable methods for reading and writing audio and quality settings.
    /// Reads from SaveManager on startup; writes through SaveManager on change.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        #region Singleton
        public static SettingsManager Instance { get; private set; }
        #endregion

        #region Private Fields
        private float _masterVolume = 1f;
        private float _sfxVolume    = 1f;
        #endregion

        #region Properties
        public float MasterVolume => _masterVolume;
        public float SFXVolume    => _sfxVolume;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Guard: destroy duplicate if one already persists across scene loads
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Load saved settings on first startup
            LoadSettings();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads persisted settings from SaveManager and pushes them to AudioManager.
        /// Safe to call before AudioManager.Start() — AudioManager null-checks are used.
        /// </summary>
        public void LoadSettings()
        {
            if (SaveManager.Instance == null) return;

            _masterVolume = SaveManager.Instance.GetMasterVolume();
            _sfxVolume    = SaveManager.Instance.GetSFXVolume();
            ApplyVolumes();
        }

        /// <summary>Sets master volume (0-1), persists it, and applies immediately.</summary>
        public void SetMasterVolume(float vol)
        {
            _masterVolume = Mathf.Clamp01(vol);
            SaveManager.Instance?.SetMasterVolume(_masterVolume);
            ApplyVolumes();
        }

        /// <summary>Sets SFX volume (0-1), persists it, and applies immediately.</summary>
        public void SetSFXVolume(float vol)
        {
            _sfxVolume = Mathf.Clamp01(vol);
            SaveManager.Instance?.SetSFXVolume(_sfxVolume);
            ApplyVolumes();
        }

        /// <summary>Sets Unity's built-in quality level by index.</summary>
        public void SetQualityLevel(int index)
        {
            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
        }
        #endregion

        #region Private Methods
        private void ApplyVolumes()
        {
            AudioManager.Instance?.SetMasterVolume(_masterVolume);
            AudioManager.Instance?.SetSFXVolume(_sfxVolume);
        }
        #endregion
    }
}
