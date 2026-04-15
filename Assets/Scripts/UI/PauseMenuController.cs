using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeliveryGame
{
    /// <summary>
    /// Handles button callbacks for the in-game Pause, Win, and Lose panels.
    /// Attach to the Canvas root (or any persistent object in the scene) and wire
    /// each button's OnClick to these methods in the Inspector.
    /// Also handles the Settings sub-panel inside the pause menu.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Pause > Settings sub-panel")]
        [SerializeField] private GameObject _pauseSettingsPanel;
        [SerializeField] private Slider     _masterVolumeSlider;
        [SerializeField] private Slider     _sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI _masterLabel;
        [SerializeField] private TextMeshProUGUI _sfxLabel;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Initialise sliders to saved values
            if (SettingsManager.Instance != null)
            {
                if (_masterVolumeSlider != null)
                {
                    _masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
                    _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                }
                if (_sfxVolumeSlider != null)
                {
                    _sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
                    _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                }
            }
        }
        #endregion

        #region Pause Menu Callbacks
        /// <summary>Resumes gameplay from the pause menu.</summary>
        public void OnResume()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.TogglePause();
        }

        /// <summary>Restarts the current level.</summary>
        public void OnRestartLevel()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.RestartLevel();
        }

        /// <summary>Opens the settings sub-panel inside the pause menu.</summary>
        public void OnOpenSettings()
        {
            AudioManager.Instance?.PlayUIClick();
            if (_pauseSettingsPanel != null)
                _pauseSettingsPanel.SetActive(true);
        }

        /// <summary>Closes the settings sub-panel and returns to the pause menu.</summary>
        public void OnCloseSettings()
        {
            AudioManager.Instance?.PlayUIClick();
            if (_pauseSettingsPanel != null)
                _pauseSettingsPanel.SetActive(false);
        }

        /// <summary>Returns to the main menu from the pause screen.</summary>
        public void OnQuitToMenu()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.LoadMainMenu();
        }
        #endregion

        #region Win Screen Callbacks
        /// <summary>Loads the next level after winning.</summary>
        public void OnNextLevel()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.LoadNextLevel();
        }

        /// <summary>Returns to the main menu from the win screen.</summary>
        public void OnWinToMenu()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.LoadMainMenu();
        }
        #endregion

        #region Lose Screen Callbacks
        /// <summary>Retries the current level from the lose screen.</summary>
        public void OnRetry()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.RestartLevel();
        }

        /// <summary>Returns to the main menu from the lose screen.</summary>
        public void OnLoseToMenu()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.LoadMainMenu();
        }
        #endregion

        #region Slider Callbacks
        private void OnMasterVolumeChanged(float val)
        {
            SettingsManager.Instance?.SetMasterVolume(val);
            if (_masterLabel != null) _masterLabel.text = $"Master: {Mathf.RoundToInt(val * 100f)}%";
        }

        private void OnSFXVolumeChanged(float val)
        {
            SettingsManager.Instance?.SetSFXVolume(val);
            if (_sfxLabel != null) _sfxLabel.text = $"SFX: {Mathf.RoundToInt(val * 100f)}%";
        }
        #endregion
    }
}
