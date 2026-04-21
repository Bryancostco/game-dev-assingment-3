using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryGame
{
    /// <summary>
    /// Drives the 3D Diegetic Main Menu scene UI.
    /// Manages camera gliding between physically separated Canvas nodes in 3D space.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public enum MenuState { Landing, MainMenu, Settings, Controls }

        [Header("Backend State")]
        [SerializeField] private MenuState _currentState = MenuState.Landing;
        private bool _isCameraMoving = false;

        [Header("Camera & Animation")]
        [SerializeField] private Transform _mainCamera;
        private readonly float _cameraGlideDuration = 0.5f; 

        // EXACT CAMERA COORDINATES
        private readonly Vector3 _camPosLanding  = new Vector3(0f, 100f, -15f);
        private readonly Vector3 _camPosMainMenu = new Vector3(0f, 50f, -15f);
        private readonly Vector3 _camPosSettings = new Vector3(-50f, 50f, -15f);
        private readonly Vector3 _camPosControls = new Vector3(50f, 50f, -15f);

        [Header("Canvas Groups (Raycast Blockers)")]
        [SerializeField] private CanvasGroup _groupLanding;
        [SerializeField] private CanvasGroup _groupMainMenu;
        [SerializeField] private CanvasGroup _groupSettings;
        [SerializeField] private CanvasGroup _groupControls;

        [Header("Buttons")]
        [SerializeField] private Button _landingStartButton;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _controlsButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _settingsToMainButton;
        [SerializeField] private Button _controlsToMainButton;

        [Header("Settings Sliders")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        #region Unity Lifecycle
        private void Start()
        {
            _mainCamera.position = _camPosLanding;
            UpdateCanvasInteractivity(MenuState.Landing);

            _landingStartButton.onClick.AddListener(() => TransitionTo(MenuState.MainMenu, _camPosMainMenu));
            _playButton.onClick.AddListener(OnStartGame);
            _controlsButton.onClick.AddListener(OnOpenControls);
            _settingsButton.onClick.AddListener(OnOpenSettings);
            _quitButton.onClick.AddListener(OnQuit);
            _settingsToMainButton.onClick.AddListener(OnBack);
            _controlsToMainButton.onClick.AddListener(OnBack);

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

        #region Button Callbacks
        public void OnStartGame()
        {
            if (_isCameraMoving) return;
            AudioManager.Instance?.PlayUIClick();
            Time.timeScale = 1f;
            GameManager.Instance?.StartGame();
        }

        public void OnOpenSettings()
        {
            TransitionTo(MenuState.Settings, _camPosSettings);
        }

        public void OnOpenControls()
        {
            TransitionTo(MenuState.Controls, _camPosControls);
        }

        public void OnBack()
        {
            TransitionTo(MenuState.MainMenu, _camPosMainMenu);
        }

        public void OnQuit()
        {
            if (_isCameraMoving) return;
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
        }

        private void OnSFXVolumeChanged(float val)
        {
            SettingsManager.Instance?.SetSFXVolume(val);
        }
        #endregion

        #region Private Helpers
        private void TransitionTo(MenuState newState, Vector3 targetCameraPosition)
        {
            if (_isCameraMoving) return; 

            AudioManager.Instance?.PlayUIClick();
            _currentState = newState;
            UpdateCanvasInteractivity(newState);
            StartCoroutine(GlideCamera(targetCameraPosition));
        }

        private void UpdateCanvasInteractivity(MenuState activeState)
        {
            _groupLanding.interactable = false;  _groupLanding.blocksRaycasts = false;
            _groupMainMenu.interactable = false; _groupMainMenu.blocksRaycasts = false;
            _groupSettings.interactable = false; _groupSettings.blocksRaycasts = false;
            _groupControls.interactable = false; _groupControls.blocksRaycasts = false;

            switch (activeState)
            {
                case MenuState.Landing:  _groupLanding.interactable = true;  _groupLanding.blocksRaycasts = true;  break;
                case MenuState.MainMenu: _groupMainMenu.interactable = true; _groupMainMenu.blocksRaycasts = true; break;
                case MenuState.Settings: _groupSettings.interactable = true; _groupSettings.blocksRaycasts = true; break;
                case MenuState.Controls: _groupControls.interactable = true; _groupControls.blocksRaycasts = true; break;
            }
        }

        private IEnumerator GlideCamera(Vector3 targetPosition)
        {
            _isCameraMoving = true;
            float t = 0f;
            Vector3 startPosition = _mainCamera.position;

            while (t < _cameraGlideDuration)
            {
                t += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(t / _cameraGlideDuration);
                float smoothStep = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

                _mainCamera.position = Vector3.Lerp(startPosition, targetPosition, smoothStep);
                yield return null; 
            }

            _mainCamera.position = targetPosition; 
            _isCameraMoving = false;
        }
        #endregion
    }
}