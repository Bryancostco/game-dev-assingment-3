using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryGame
{
    /// <summary>Represents the reason the player lost, used by UIManager for the lose screen.</summary>
    public enum LoseReason { None, TimeOut, VehicleDestroyed }

    /// <summary>All possible game states.</summary>
    public enum GameState { MainMenu, Playing, Paused, Won, Lost }

    /// <summary>
    /// Central game state machine. Owns the timer, triggers level loading,
    /// and evaluates win/lose conditions by listening to events.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired whenever the game transitions to a new state.</summary>
        public static event Action<GameState> OnGameStateChanged;

        /// <summary>Fired every frame during Playing state with the current time remaining (seconds).</summary>
        public static event Action<float> OnTimerUpdated;
        #endregion

        #region Inspector Fields
        [Header("Scene Names")]
        [SerializeField] private string _mainMenuScene = "MainMenu";
        [SerializeField] private string _level01Scene  = "Level_01";
        [SerializeField] private string _level02Scene  = "Level_02";

        [Header("Default Level Parameters (overridden by SceneUIConnector)")]
        [SerializeField] private int   _defaultDeliveries = 5;
        [SerializeField] private float _defaultLevelTime  = 180f;
        #endregion

        #region Private Fields
        private GameState  _currentState     = GameState.MainMenu;
        private float      _timeRemaining    = 180f;
        private float      _levelStartTime   = 180f; // stores initial time for elapsed-time calc
        private int        _currentLevelIndex = 0;
        private LoseReason _loseReason       = LoseReason.None;
        #endregion

        #region Properties
        public GameState  CurrentState       => _currentState;
        public float      TimeRemaining      => _timeRemaining;
        public int        CurrentLevelIndex  => _currentLevelIndex;
        public LoseReason CurrentLoseReason  => _loseReason;

        /// <summary>Seconds elapsed since the level began (used for best-time saving).</summary>
        public float ElapsedTime => _levelStartTime - _timeRemaining;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Guard: destroy duplicate Singleton created on scene reload
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
            DeliveryManager.OnAllDeliveriesComplete += HandleAllDeliveriesComplete;
            VehicleHealth.OnVehicleDestroyed        += HandleVehicleDestroyed;
            SceneManager.sceneLoaded                += OnSceneLoaded;
        }

        private void OnDisable()
        {
            DeliveryManager.OnAllDeliveriesComplete -= HandleAllDeliveriesComplete;
            VehicleHealth.OnVehicleDestroyed        -= HandleVehicleDestroyed;
            SceneManager.sceneLoaded                -= OnSceneLoaded;
        }

        private void Update()
        {
            // Timer only ticks in Playing state
            if (_currentState != GameState.Playing) return;

            _timeRemaining -= Time.deltaTime;
            OnTimerUpdated?.Invoke(_timeRemaining);

            // Guard: timer hit zero during an active delivery — lose immediately, no partial credit
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _loseReason    = LoseReason.TimeOut;
                SetState(GameState.Lost);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>Starts the game from the main menu by loading Level 1.</summary>
        public void StartGame()
        {
            _currentLevelIndex = 1;
            SceneManager.LoadScene(_level01Scene);
        }

        /// <summary>Loads the next level; returns to main menu if no further levels exist.</summary>
        public void LoadNextLevel()
        {
            _currentLevelIndex++;
            if (_currentLevelIndex > 2)
            {
                LoadMainMenu();
                return;
            }
            SceneManager.LoadScene(_currentLevelIndex == 2 ? _level02Scene : _level01Scene);
        }

        /// <summary>Reloads the current gameplay scene.</summary>
        public void RestartLevel()
        {
            SceneManager.LoadScene(_currentLevelIndex == 1 ? _level01Scene : _level02Scene);
        }

        /// <summary>Unloads the gameplay scene and returns to the main menu.</summary>
        public void LoadMainMenu()
        {
            _currentLevelIndex = 0;
            SetState(GameState.MainMenu);
            SceneManager.LoadScene(_mainMenuScene);
        }

        /// <summary>
        /// Toggles between Playing and Paused. Auto-saves progress on pause so that
        /// a hard quit during play does not lose all progress.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                // Auto-save before pausing in case the player quits from the pause menu
                SaveManager.Instance?.SaveProgress(_currentLevelIndex);
                SetState(GameState.Paused);
            }
            else if (_currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Called by SceneUIConnector when a gameplay scene finishes loading.
        /// Sets up level parameters and transitions to Playing state.
        /// </summary>
        public void InitializeLevel(int totalDeliveries, float levelTime)
        {
            _timeRemaining    = levelTime;
            _levelStartTime   = levelTime;
            _loseReason       = LoseReason.None;
            SetState(GameState.Playing);
        }
        #endregion

        #region Private Methods
        private void SetState(GameState newState)
        {
            _currentState = newState;

            // Freeze time while paused; restore otherwise
            Time.timeScale = (newState == GameState.Paused) ? 0f : 1f;

            if (newState == GameState.Won)
            {
                // Auto-save on level complete
                SaveManager.Instance?.SaveLevelComplete(_currentLevelIndex, ElapsedTime);
            }

            OnGameStateChanged?.Invoke(newState);
        }

        private void HandleAllDeliveriesComplete() => SetState(GameState.Won);

        private void HandleVehicleDestroyed()
        {
            _loseReason = LoseReason.VehicleDestroyed;
            SetState(GameState.Lost);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Ensure time scale is 1 after any scene load
            // (guards against a crash/reload while paused)
            if (_currentState != GameState.Paused)
                Time.timeScale = 1f;
        }
        #endregion
    }
}
