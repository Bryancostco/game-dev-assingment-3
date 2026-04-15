using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryGame
{
    /// <summary>
    /// Updates all UI elements by listening to game events.
    /// Contains zero game logic — it only reflects state changes fired by other systems.
    /// Gets its scene-specific UI references via Initialize() called by SceneUIConnector.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager Instance { get; private set; }
        #endregion

        #region Runtime UI References (set by SceneUIConnector.Initialize)
        // HUD
        private TextMeshProUGUI _deliveryCounterText;
        private TextMeshProUGUI _timerText;
        private Image           _healthBarFill;
        private RawImage        _minimapDisplay;
        private TextMeshProUGUI _objectiveText;
        private TextMeshProUGUI _notificationText;
        private CanvasGroup     _notificationGroup;

        // Briefing
        private CanvasGroup     _briefingPanel;
        private TextMeshProUGUI _briefingText;

        // Panels (GameObjects toggled on/off)
        private GameObject _hudPanel;
        private GameObject _pausePanel;
        private GameObject _winPanel;
        private GameObject _losePanel;

        // Win screen
        private TextMeshProUGUI _winTimeText;
        private TextMeshProUGUI _winDeliveriesText;
        private Button          _nextLevelButton;

        // Lose screen
        private TextMeshProUGUI _loseReasonText;
        #endregion

        #region Private Fields
        private Coroutine _notificationRoutine;
        private Coroutine _briefingRoutine;
        private bool      _isInitialized = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Guard: destroy duplicate Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged      += HandleGameStateChanged;
            GameManager.OnTimerUpdated          += HandleTimerUpdated;
            DeliveryManager.OnDeliveryCountChanged += HandleDeliveryCountChanged;
            VehicleHealth.OnHealthChanged       += HandleHealthChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged      -= HandleGameStateChanged;
            GameManager.OnTimerUpdated          -= HandleTimerUpdated;
            DeliveryManager.OnDeliveryCountChanged -= HandleDeliveryCountChanged;
            VehicleHealth.OnHealthChanged       -= HandleHealthChanged;
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Called by SceneUIConnector at scene start.
        /// Passes all scene-specific UI object references to this persistent singleton.
        /// </summary>
        public void Initialize(GameplayUIRefs refs)
        {
            _deliveryCounterText = refs.DeliveryCounter;
            _timerText           = refs.TimerText;
            _healthBarFill       = refs.HealthBarFill;
            _minimapDisplay      = refs.MinimapDisplay;
            _objectiveText       = refs.ObjectiveText;
            _notificationText    = refs.NotificationText;
            _notificationGroup   = refs.NotificationGroup;
            _briefingPanel       = refs.BriefingPanel;
            _briefingText        = refs.BriefingText;
            _hudPanel            = refs.HUDPanel;
            _pausePanel          = refs.PausePanel;
            _winPanel            = refs.WinPanel;
            _losePanel           = refs.LosePanel;
            _winTimeText         = refs.WinTimeText;
            _winDeliveriesText   = refs.WinDeliveriesText;
            _nextLevelButton     = refs.NextLevelButton;
            _loseReasonText      = refs.LoseReasonText;

            _isInitialized = true;

            // Start with only HUD visible
            SetPanelActive(_hudPanel,   true);
            SetPanelActive(_pausePanel, false);
            SetPanelActive(_winPanel,   false);
            SetPanelActive(_losePanel,  false);

            // Show the level-start briefing
            if (_briefingText != null)
                _briefingText.text = refs.BriefingMessage;
            ShowBriefing();

            // Set up initial delivery counter
            if (DeliveryManager.Instance != null)
                UpdateDeliveryCounter(0, DeliveryManager.Instance.TotalDeliveries);
        }
        #endregion

        #region Public Methods
        /// <summary>Displays a short notification message (fades out after 2 seconds).</summary>
        public void ShowNotification(string message)
        {
            if (_notificationText == null) return;
            if (_notificationRoutine != null) StopCoroutine(_notificationRoutine);
            _notificationText.text = message;
            _notificationRoutine   = StartCoroutine(FadeNotification());
        }

        /// <summary>Updates the objective hint text in the HUD.</summary>
        public void SetObjectiveText(string text)
        {
            if (_objectiveText != null)
                _objectiveText.text = text;
        }
        #endregion

        #region Event Handlers
        private void HandleGameStateChanged(GameState state)
        {
            if (!_isInitialized) return;

            switch (state)
            {
                case GameState.Playing:
                    SetPanelActive(_hudPanel,   true);
                    SetPanelActive(_pausePanel, false);
                    SetPanelActive(_winPanel,   false);
                    SetPanelActive(_losePanel,  false);
                    break;

                case GameState.Paused:
                    SetPanelActive(_pausePanel, true);
                    break;

                case GameState.Won:
                    SetPanelActive(_hudPanel,  false);
                    SetPanelActive(_winPanel,  true);
                    PopulateWinScreen();
                    break;

                case GameState.Lost:
                    SetPanelActive(_hudPanel,  false);
                    SetPanelActive(_losePanel, true);
                    PopulateLoseScreen();
                    break;
            }
        }

        private void HandleTimerUpdated(float timeRemaining)
        {
            if (_timerText == null) return;
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            _timerText.text = $"Time: {minutes}:{seconds:D2}";
        }

        private void HandleDeliveryCountChanged(int completed, int total)
            => UpdateDeliveryCounter(completed, total);

        private void HandleHealthChanged(float hp)
        {
            if (_healthBarFill == null || VehicleHealth.MaxHealthStatic <= 0) return;
            _healthBarFill.fillAmount = hp / VehicleHealth.MaxHealthStatic;
        }
        #endregion

        #region Private Helpers
        private void UpdateDeliveryCounter(int completed, int total)
        {
            if (_deliveryCounterText != null)
                _deliveryCounterText.text = $"Deliveries: {completed} / {total}";
        }

        private void PopulateWinScreen()
        {
            if (_winTimeText != null && GameManager.Instance != null)
            {
                float elapsed = GameManager.Instance.ElapsedTime;
                int m = Mathf.FloorToInt(elapsed / 60f);
                int s = Mathf.FloorToInt(elapsed % 60f);
                _winTimeText.text = $"Time: {m}:{s:D2}";
            }
            if (_winDeliveriesText != null && DeliveryManager.Instance != null)
            {
                int comp  = DeliveryManager.Instance.DeliveriesCompleted;
                int total = DeliveryManager.Instance.TotalDeliveries;
                _winDeliveriesText.text = $"Delivered: {comp} / {total}";
            }
            // Only show "Next Level" on Level 1
            if (_nextLevelButton != null)
                _nextLevelButton.gameObject.SetActive(GameManager.Instance?.CurrentLevelIndex == 1);
        }

        private void PopulateLoseScreen()
        {
            if (_loseReasonText == null) return;
            _loseReasonText.text = GameManager.Instance?.CurrentLoseReason == LoseReason.TimeOut
                ? "Time's Up!"
                : "Vehicle Destroyed!";
        }

        private void ShowBriefing()
        {
            if (_briefingPanel == null) return;
            if (_briefingRoutine != null) StopCoroutine(_briefingRoutine);
            _briefingRoutine = StartCoroutine(FadeBriefing());
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private IEnumerator FadeNotification()
        {
            if (_notificationGroup != null) _notificationGroup.alpha = 1f;
            yield return new WaitForSeconds(1.5f);
            // Fade out over 0.5s
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                if (_notificationGroup != null)
                    _notificationGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                yield return null;
            }
            if (_notificationGroup != null) _notificationGroup.alpha = 0f;
        }

        private IEnumerator FadeBriefing()
        {
            if (_briefingPanel != null) _briefingPanel.alpha = 1f;
            yield return new WaitForSeconds(3.5f);
            // Fade out over 0.5s
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                if (_briefingPanel != null)
                    _briefingPanel.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                yield return null;
            }
            if (_briefingPanel != null)
            {
                _briefingPanel.alpha          = 0f;
                _briefingPanel.interactable   = false;
                _briefingPanel.blocksRaycasts = false;
            }
        }
        #endregion
    }

    /// <summary>
    /// Plain-data container for all scene-specific UI references.
    /// Populated in the Inspector on SceneUIConnector and passed to UIManager.Initialize().
    /// Defined here so UIManager and SceneUIConnector share the same type.
    /// </summary>
    [System.Serializable]
    public class GameplayUIRefs
    {
        [Header("HUD")]
        public TextMeshProUGUI DeliveryCounter;
        public TextMeshProUGUI TimerText;
        public Image           HealthBarFill;
        public RawImage        MinimapDisplay;
        public TextMeshProUGUI ObjectiveText;
        public TextMeshProUGUI NotificationText;
        public CanvasGroup     NotificationGroup;

        [Header("Briefing")]
        public CanvasGroup     BriefingPanel;
        public TextMeshProUGUI BriefingText;
        [TextArea] public string BriefingMessage;

        [Header("Panels")]
        public GameObject HUDPanel;
        public GameObject PausePanel;
        public GameObject WinPanel;
        public GameObject LosePanel;

        [Header("Win Screen")]
        public TextMeshProUGUI WinTimeText;
        public TextMeshProUGUI WinDeliveriesText;
        public Button          NextLevelButton;

        [Header("Lose Screen")]
        public TextMeshProUGUI LoseReasonText;
    }
}
