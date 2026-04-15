using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeliveryGame
{
    /// <summary>
    /// Drives the Main Menu scene UI.
    /// Manages panel visibility for Settings and Controls overlays.
    /// All button callbacks are wired in the Inspector via Unity UI Button.OnClick events.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _controlsPanel;

        [Header("Settings Sliders")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI _masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI _sfxVolumeLabel;
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

            ShowMainPanel();
        }
        #endregion

        #region Button Callbacks — wire these to Button.OnClick in the Inspector
        /// <summary>Loads Level 1 and starts the game.</summary>
        public void OnStartGame()
        {
            AudioManager.Instance?.PlayUIClick();
            GameManager.Instance?.StartGame();
        }

        /// <summary>Opens the Settings panel.</summary>
        public void OnOpenSettings()
        {
            AudioManager.Instance?.PlayUIClick();
            SetPanelActive(_mainPanel,     false);
            SetPanelActive(_settingsPanel, true);
            SetPanelActive(_controlsPanel, false);
        }

        /// <summary>Opens the Controls information panel.</summary>
        public void OnOpenControls()
        {
            AudioManager.Instance?.PlayUIClick();
            SetPanelActive(_mainPanel,     false);
            SetPanelActive(_settingsPanel, false);
            SetPanelActive(_controlsPanel, true);
        }

        /// <summary>Returns to the main panel from any sub-panel.</summary>
        public void OnBack()
        {
            AudioManager.Instance?.PlayUIClick();
            ShowMainPanel();
        }

        /// <summary>Quits the application.</summary>
        public void OnQuit()
        {
            AudioManager.Instance?.PlayUIClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion

        #region Slider Callbacks
        private void OnMasterVolumeChanged(float val)
        {
            SettingsManager.Instance?.SetMasterVolume(val);
            if (_masterVolumeLabel != null)
                _masterVolumeLabel.text = $"Master: {Mathf.RoundToInt(val * 100f)}%";
        }

        private void OnSFXVolumeChanged(float val)
        {
            SettingsManager.Instance?.SetSFXVolume(val);
            if (_sfxVolumeLabel != null)
                _sfxVolumeLabel.text = $"SFX: {Mathf.RoundToInt(val * 100f)}%";
        }
        #endregion

        #region Private Helpers
        private void ShowMainPanel()
        {
            SetPanelActive(_mainPanel,     true);
            SetPanelActive(_settingsPanel, false);
            SetPanelActive(_controlsPanel, false);
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
        #endregion
    }
}
