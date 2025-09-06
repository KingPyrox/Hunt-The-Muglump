using System;
using System.Collections.Generic;
using UnityEngine;
using OldSchoolGames.HuntTheMuglump.Scripts.Components;

namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    [Serializable]
    public class AchievementData
    {
        public string id;
        public bool isUnlocked;
        public DateTime unlockedDate;
        public float progress;
    }

    [Serializable]
    public class AchievementSaveData
    {
        public List<AchievementData> achievements = new List<AchievementData>();
    }

    public class WebGLAchievementManager : MonoBehaviour
    {
        private static WebGLAchievementManager instance;
        private AchievementSaveData saveData;
        private const string SAVE_KEY = "HTM_Achievements";

        public static WebGLAchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("WebGLAchievementManager");
                    instance = go.AddComponent<WebGLAchievementManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public event Action<string> OnAchievementUnlocked;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAchievements();
        }

        private void LoadAchievements()
        {
            var data = WebGLManager.Instance.LoadData(SAVE_KEY);
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    saveData = JsonUtility.FromJson<AchievementSaveData>(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load achievements: {e.Message}");
                    saveData = new AchievementSaveData();
                }
            }
            else
            {
                saveData = new AchievementSaveData();
            }
        }

        private void SaveAchievements()
        {
            try
            {
                var json = JsonUtility.ToJson(saveData);
                WebGLManager.Instance.SaveData(SAVE_KEY, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save achievements: {e.Message}");
            }
        }

        public void UnlockAchievement(string achievementId)
        {
            var achievement = GetOrCreateAchievement(achievementId);
            
            if (!achievement.isUnlocked)
            {
                achievement.isUnlocked = true;
                achievement.unlockedDate = DateTime.Now;
                achievement.progress = 1.0f;
                
                SaveAchievements();
                OnAchievementUnlocked?.Invoke(achievementId);
                
                Debug.Log($"Achievement unlocked: {achievementId}");
                ShowAchievementNotification(achievementId);
            }
        }

        public void UpdateAchievementProgress(string achievementId, float progress)
        {
            var achievement = GetOrCreateAchievement(achievementId);
            achievement.progress = Mathf.Clamp01(progress);
            
            if (achievement.progress >= 1.0f && !achievement.isUnlocked)
            {
                UnlockAchievement(achievementId);
            }
            else
            {
                SaveAchievements();
            }
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            var achievement = saveData.achievements.Find(a => a.id == achievementId);
            return achievement != null && achievement.isUnlocked;
        }

        public float GetAchievementProgress(string achievementId)
        {
            var achievement = saveData.achievements.Find(a => a.id == achievementId);
            return achievement?.progress ?? 0f;
        }

        public List<AchievementData> GetAllAchievements()
        {
            return new List<AchievementData>(saveData.achievements);
        }

        public void ResetAchievements()
        {
            saveData = new AchievementSaveData();
            SaveAchievements();
        }

        private AchievementData GetOrCreateAchievement(string achievementId)
        {
            var achievement = saveData.achievements.Find(a => a.id == achievementId);
            if (achievement == null)
            {
                achievement = new AchievementData
                {
                    id = achievementId,
                    isUnlocked = false,
                    progress = 0f
                };
                saveData.achievements.Add(achievement);
            }
            return achievement;
        }

        private void ShowAchievementNotification(string achievementId)
        {
            // This can be expanded to show a visual notification
            // For now, just logging
            Debug.Log($"[Achievement Notification] {achievementId} unlocked!");
        }

        public void EarnBadge(Badge badge)
        {
            UnlockAchievement(badge.Name);
        }
    }
}