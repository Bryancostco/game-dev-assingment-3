using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Handles all audio playback: engine loop (pitch-scaled by speed) and one-shot SFX.
    /// Uses two AudioSources — one for the looping engine, one for SFX.
    /// Reacts to game state, delivery events, and braking via subscriptions. No polling.
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
        [SerializeField] private float _vehicleMaxSpeed = 25f; 
        #endregion

        #region Private Fields
        private float _masterVolume = 1f;
        private float _sfxVolume    = 1f;
        private float _currentSpeed = 0f;
        private bool _isBraking = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
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
            if (_engineSource == null || !_engineSource.isPlaying) return;

            if (_isBraking)
            {
                // Rapidly drop pitch and volume when spacebar is held
                _engineSource.pitch = Mathf.Lerp(_engineSource.pitch, _enginePitchIdle - 0.2f, Time.deltaTime * 5f);
                _engineSource.volume = Mathf.Lerp(_engineSource.volume, _masterVolume * 0.5f, Time.deltaTime * 5f);
            }
            else
            {
                // Scale engine pitch smoothly based on vehicle speed
                float speedRatio = _vehicleMaxSpeed > 0f ? Mathf.Clamp01(_currentSpeed / _vehicleMaxSpeed) : 0f;
                _engineSource.pitch = Mathf.Lerp(_engineSource.pitch, Mathf.Lerp(_enginePitchIdle, _enginePitchMax, speedRatio), Time.deltaTime * 3f);
                _engineSource.volume = Mathf.Lerp(_engineSource.volume, _masterVolume, Time.deltaTime * 3f);
            }
        }
        #endregion

        #region Public Methods
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume * _masterVolume);
        }

        public void PlayUIClick() => PlaySFX(_sfxUIClick);

        public void SetMasterVolume(float vol)
        {
            _masterVolume = Mathf.Clamp01(vol);
            if (_engineSource != null) _engineSource.volume = _masterVolume;
        }

        public void SetSFXVolume(float vol) => _sfxVolume = Mathf.Clamp01(vol);

        public void UpdateVehicleSpeed(float speed) => _currentSpeed = speed;

        public void SetBraking(bool isBraking) => _isBraking = isBraking;
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

        private void HandlePackagePickedUp(int deliveryId)  => PlaySFX(_sfxPickup);
        private void HandleDeliverySuccessful(int deliveryId) => PlaySFX(_sfxDeliver);
        #endregion
    }
}