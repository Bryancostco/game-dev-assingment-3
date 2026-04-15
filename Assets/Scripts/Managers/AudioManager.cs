using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Handles all audio playback: engine loop (pitch-scaled by speed) and one-shot SFX.
    /// Uses two AudioSources — one for the looping engine, one for SFX.
    /// Reacts to game state and delivery events via subscriptions. No polling.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        public static AudioManager Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _engineSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _engineLoop;
        [SerializeField] private AudioClip _sfxPickup;
        [SerializeField] private AudioClip _sfxDeliver;
        [SerializeField] private AudioClip _sfxCrash;
        [SerializeField] private AudioClip _sfxUIClick;
        [SerializeField] private AudioClip _sfxWin;
        [SerializeField] private AudioClip _sfxLose;

        [Header("Engine Pitch Range")]
        [SerializeField] private float _enginePitchIdle = 0.7f;
        [SerializeField] private float _enginePitchMax  = 2.0f;
        [SerializeField] private float _vehicleMaxSpeed = 25f; // must match VehicleController
        #endregion

        #region Private Fields
        private float _masterVolume = 1f;
        private float _sfxVolume    = 1f;
        private float _currentSpeed = 0f;
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
            DeliveryManager.OnPackagePickedUp   += HandlePackagePickedUp;
            DeliveryManager.OnDeliverySuccessful += HandleDeliverySuccessful;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged      -= HandleGameStateChanged;
            DeliveryManager.OnPackagePickedUp   -= HandlePackagePickedUp;
            DeliveryManager.OnDeliverySuccessful -= HandleDeliverySuccessful;
        }

        private void Start()
        {
            // Prepare the engine loop without starting playback yet
            if (_engineSource != null && _engineLoop != null)
            {
                _engineSource.clip   = _engineLoop;
                _engineSource.loop   = true;
                _engineSource.volume = _masterVolume;
                _engineSource.pitch  = _enginePitchIdle;
            }
        }

        private void Update()
        {
            // Scale engine pitch smoothly based on vehicle speed
            if (_engineSource == null || !_engineSource.isPlaying) return;
            float speedRatio   = _vehicleMaxSpeed > 0f ? Mathf.Clamp01(_currentSpeed / _vehicleMaxSpeed) : 0f;
            _engineSource.pitch = Mathf.Lerp(_enginePitchIdle, _enginePitchMax, speedRatio);
        }
        #endregion

        #region Public Methods
        /// <summary>Plays a one-shot audio clip through the SFX AudioSource.</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume * _masterVolume);
        }

        /// <summary>Convenience method — plays the UI click sound.</summary>
        public void PlayUIClick() => PlaySFX(_sfxUIClick);

        /// <summary>Sets master volume and applies to the engine source immediately.</summary>
        public void SetMasterVolume(float vol)
        {
            _masterVolume = Mathf.Clamp01(vol);
            if (_engineSource != null) _engineSource.volume = _masterVolume;
        }

        /// <summary>Sets the SFX volume used for all one-shot clips.</summary>
        public void SetSFXVolume(float vol) => _sfxVolume = Mathf.Clamp01(vol);

        /// <summary>
        /// Called by VehicleController every FixedUpdate to keep engine pitch in sync.
        /// </summary>
        public void UpdateVehicleSpeed(float speed) => _currentSpeed = speed;
        #endregion

        #region Event Handlers
        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    if (_engineSource != null && !_engineSource.isPlaying)
                        _engineSource.Play();
                    break;
                case GameState.Paused:
                    _engineSource?.Pause();
                    break;
                case GameState.Won:
                    _engineSource?.Stop();
                    PlaySFX(_sfxWin);
                    break;
                case GameState.Lost:
                    _engineSource?.Stop();
                    PlaySFX(_sfxLose);
                    break;
                case GameState.MainMenu:
                    _engineSource?.Stop();
                    break;
            }
        }

        // Named handlers so they can be properly unsubscribed (lambdas cannot be unsubscribed)
        private void HandlePackagePickedUp(int deliveryId)  => PlaySFX(_sfxPickup);
        private void HandleDeliverySuccessful(int deliveryId) => PlaySFX(_sfxDeliver);
        #endregion
    }
}
