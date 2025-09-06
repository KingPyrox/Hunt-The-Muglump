using System;
using System.Collections.Generic;
using UnityEngine;

namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int score;
        public DateTime timestamp;
    }

    [Serializable]
    public class LeaderboardData
    {
        public string leaderboardId;
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        public int personalBest;
    }

    [Serializable]
    public class LeaderboardSaveData
    {
        public List<LeaderboardData> leaderboards = new List<LeaderboardData>();
        public string playerName = "Player";
    }

    public class WebGLLeaderboardManager : MonoBehaviour
    {
        private static WebGLLeaderboardManager instance;
        private LeaderboardSaveData saveData;
        private const string SAVE_KEY = "HTM_Leaderboards";
        private const int MAX_ENTRIES = 10;

        public static WebGLLeaderboardManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("WebGLLeaderboardManager");
                    instance = go.AddComponent<WebGLLeaderboardManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public event Action<string, int> OnHighScoreAchieved;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLeaderboards();
        }

        private void LoadLeaderboards()
        {
            var data = WebGLManager.Instance.LoadData(SAVE_KEY);
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    saveData = JsonUtility.FromJson<LeaderboardSaveData>(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load leaderboards: {e.Message}");
                    saveData = new LeaderboardSaveData();
                }
            }
            else
            {
                saveData = new LeaderboardSaveData();
                InitializeDefaultLeaderboards();
            }
        }

        private void InitializeDefaultLeaderboards()
        {
            // Initialize with the game's default leaderboards
            saveData.leaderboards.Add(new LeaderboardData 
            { 
                leaderboardId = "TopScores",
                entries = new List<LeaderboardEntry>()
            });
            
            saveData.leaderboards.Add(new LeaderboardData 
            { 
                leaderboardId = "RoomsExplored",
                entries = new List<LeaderboardEntry>()
            });
        }

        private void SaveLeaderboards()
        {
            try
            {
                var json = JsonUtility.ToJson(saveData);
                WebGLManager.Instance.SaveData(SAVE_KEY, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save leaderboards: {e.Message}");
            }
        }

        public void SubmitScore(string leaderboardId, int score)
        {
            var leaderboard = GetOrCreateLeaderboard(leaderboardId);
            
            // Update personal best
            if (score > leaderboard.personalBest)
            {
                leaderboard.personalBest = score;
                OnHighScoreAchieved?.Invoke(leaderboardId, score);
            }

            // Add to local leaderboard
            var entry = new LeaderboardEntry
            {
                playerName = saveData.playerName,
                score = score,
                timestamp = DateTime.Now
            };

            leaderboard.entries.Add(entry);
            
            // Sort and keep only top entries
            leaderboard.entries.Sort((a, b) => b.score.CompareTo(a.score));
            if (leaderboard.entries.Count > MAX_ENTRIES)
            {
                leaderboard.entries.RemoveRange(MAX_ENTRIES, leaderboard.entries.Count - MAX_ENTRIES);
            }

            SaveLeaderboards();
            Debug.Log($"Score submitted to {leaderboardId}: {score}");
        }

        public List<LeaderboardEntry> GetLeaderboard(string leaderboardId)
        {
            var leaderboard = saveData.leaderboards.Find(l => l.leaderboardId == leaderboardId);
            return leaderboard != null ? new List<LeaderboardEntry>(leaderboard.entries) : new List<LeaderboardEntry>();
        }

        public int GetPersonalBest(string leaderboardId)
        {
            var leaderboard = saveData.leaderboards.Find(l => l.leaderboardId == leaderboardId);
            return leaderboard?.personalBest ?? 0;
        }

        public void SetPlayerName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                saveData.playerName = name;
                SaveLeaderboards();
            }
        }

        public string GetPlayerName()
        {
            return saveData.playerName;
        }

        public void ResetLeaderboards()
        {
            saveData = new LeaderboardSaveData();
            InitializeDefaultLeaderboards();
            SaveLeaderboards();
        }

        private LeaderboardData GetOrCreateLeaderboard(string leaderboardId)
        {
            var leaderboard = saveData.leaderboards.Find(l => l.leaderboardId == leaderboardId);
            if (leaderboard == null)
            {
                leaderboard = new LeaderboardData
                {
                    leaderboardId = leaderboardId,
                    entries = new List<LeaderboardEntry>(),
                    personalBest = 0
                };
                saveData.leaderboards.Add(leaderboard);
            }
            return leaderboard;
        }
    }
}