using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Wraps all PlayerPrefs access with safe defaults and type-checked getters/setters.
    /// No other script reads or writes PlayerPrefs directly — all access goes through here.
    /// Singleton with DontDestroyOnLoad.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region Singleton
        public static SaveManager Instance { get; private set; }
        #endregion

        #region PlayerPrefs Keys
        // Centralised key constants prevent typo bugs across scripts
        private const string KEY_MASTER_VOLUME    = "MasterVolume";
        private const string KEY_SFX_VOLUME       = "SFXVolume";
        private const string KEY_HIGHEST_LEVEL    = "HighestLevelCompleted";
        private const string KEY_BEST_TIME_L1     = "BestTime_Level_01";
        private const string KEY_BEST_TIME_L2     = "BestTime_Level_02";
        private const string KEY_LAST_LEVEL       = "LastPlayedLevel";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Guard: destroy duplicate if another SaveManager already exists (DontDestroyOnLoad)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Volume Settings
        /// <summary>Returns the saved master volume. Defaults to 1.0 if the key is missing.</summary>
        public float GetMasterVolume() => PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1.0f);

        /// <summary>Saves the master volume and immediately flushes to disk.</summary>
        public void SetMasterVolume(float vol)
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, vol);
            PlayerPrefs.Save();
        }

        /// <summary>Returns the saved SFX volume. Defaults to 1.0 if the key is missing.</summary>
        public float GetSFXVolume() => PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1.0f);

        /// <summary>Saves the SFX volume and immediately flushes to disk.</summary>
        public void SetSFXVolume(float vol)
        {
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, vol);
            PlayerPrefs.Save();
        }
        #endregion

        #region Progression
        /// <summary>Returns the index of the highest level the player has completed (0 = none).</summary>
        public int GetHighestLevelCompleted() => PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        /// <summary>Returns the best completion time for Level 1 in seconds. 0 = no record yet.</summary>
        public float GetBestTimeLevel1() => PlayerPrefs.GetFloat(KEY_BEST_TIME_L1, 0f);

        /// <summary>Returns the best completion time for Level 2 in seconds. 0 = no record yet.</summary>
        public float GetBestTimeLevel2() => PlayerPrefs.GetFloat(KEY_BEST_TIME_L2, 0f);

        /// <summary>
        /// Records that a level was completed and stores the completion time if it is a new best.
        /// Also updates the highest level completed.
        /// </summary>
        public void SaveLevelComplete(int levelIndex, float completionTimeSeconds)
        {
            // Update highest level reached
            int currentHighest = GetHighestLevelCompleted();
            if (levelIndex > currentHighest)
                PlayerPrefs.SetInt(KEY_HIGHEST_LEVEL, levelIndex);

            // Update best time (0 = no record, so always overwrite; otherwise only if faster)
            string timeKey = levelIndex == 1 ? KEY_BEST_TIME_L1 : KEY_BEST_TIME_L2;
            float currentBest = PlayerPrefs.GetFloat(timeKey, 0f);
            if (currentBest == 0f || completionTimeSeconds < currentBest)
                PlayerPrefs.SetFloat(timeKey, completionTimeSeconds);

            PlayerPrefs.Save();
        }

        /// <summary>Saves the current level index so progress survives quit during gameplay.</summary>
        public void SaveProgress(int levelIndex)
        {
            PlayerPrefs.SetInt(KEY_LAST_LEVEL, levelIndex);
            PlayerPrefs.Save();
        }

        /// <summary>Returns the last level the player was playing (0 if never started).</summary>
        public int GetLastPlayedLevel() => PlayerPrefs.GetInt(KEY_LAST_LEVEL, 0);

        /// <summary>Clears all saved data. Use only for debug/testing.</summary>
        public void ResetAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] All save data cleared.");
        }
        #endregion
    }
}
