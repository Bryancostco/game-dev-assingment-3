using System;
using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Tracks vehicle hit points, applies collision damage scaled by impact force,
    /// and fires events used by GameManager (destroy) and UIManager (health bar).
    ///
    /// Collision damage guard: uses OnCollisionEnter only (not Stay) and enforces
    /// a 0.5-second invincibility window to prevent multi-hit per single crash.
    /// </summary>
    public class VehicleHealth : MonoBehaviour
    {
        #region Events
        /// <summary>Fired whenever HP changes. Provides the new HP value (0 – maxHealth).</summary>
        public static event Action<float> OnHealthChanged;

        /// <summary>Fired once when HP reaches zero.</summary>
        public static event Action OnVehicleDestroyed;
        #endregion

        #region Inspector Fields
        [Header("Health Settings")]
        [SerializeField] private float _maxHealth            = 100f;
        [SerializeField] private float _minDamage            = 5f;
        [SerializeField] private float _maxDamage            = 20f;
        [SerializeField] private float _damageSpeedThreshold = 2f;   // min relative velocity (m/s) to take damage
        [SerializeField] private float _invincibilityTime    = 0.5f; // seconds of post-hit invincibility

        [Header("Audio")]
        [SerializeField] private AudioClip _crashClip;
        #endregion

        #region Private Fields
        private float _currentHealth;
        private float _invincibilityTimer;
        private bool  _isDestroyed;
        #endregion

        #region Properties
        public float CurrentHealth    => _currentHealth;
        public float HealthNormalized => _currentHealth / _maxHealth;

        /// <summary>Static accessor so UIManager can normalise without holding a direct reference.</summary>
        public static float MaxHealthStatic { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _currentHealth   = _maxHealth;
            MaxHealthStatic  = _maxHealth;
        }

        private void Update()
        {
            if (_invincibilityTimer > 0f)
                _invincibilityTimer -= Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Guard: already destroyed
            if (_isDestroyed) return;

            // Guard: invincibility window active — prevents multiple damage ticks from one crash
            if (_invincibilityTimer > 0f) return;

            // Guard: only damage from Obstacle or Environment layers
            int layer = collision.gameObject.layer;
            if (layer != LayerMask.NameToLayer("Obstacle") &&
                layer != LayerMask.NameToLayer("Environment"))
                return;

            float impactSpeed = collision.relativeVelocity.magnitude;

            // Guard: small bumps below threshold cause no damage
            if (impactSpeed < _damageSpeedThreshold) return;

            // Scale damage linearly: minimum at threshold, maximum at 20 m/s
            float t      = Mathf.Clamp01((impactSpeed - _damageSpeedThreshold) / (20f - _damageSpeedThreshold));
            float damage = Mathf.Lerp(_minDamage, _maxDamage, t);

            ApplyDamage(damage);

            // Start invincibility window to prevent the same crash dealing damage multiple times
            _invincibilityTimer = _invincibilityTime;

            AudioManager.Instance?.PlaySFX(_crashClip);
        }
        #endregion

        #region Private Methods
        private void ApplyDamage(float amount)
        {
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth);

            if (_currentHealth <= 0f && !_isDestroyed)
            {
                _isDestroyed = true;
                OnVehicleDestroyed?.Invoke();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>Resets health to full. Call when restarting the level.</summary>
        public void ResetHealth()
        {
            _currentHealth      = _maxHealth;
            _isDestroyed        = false;
            _invincibilityTimer = 0f;
            OnHealthChanged?.Invoke(_currentHealth);
        }
        #endregion
    }
}
