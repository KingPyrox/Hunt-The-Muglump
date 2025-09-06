#pragma warning disable CS0649
/**************************************************
 *  PlatformManager.cs
 *  
 *  copyright (c) 2019 Old School Games
 **************************************************/
namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;
    using Debug = UnityEngine.Debug;

#if UNITY_STEAMWORKS && !UNITY_WEBGL
    using Steamworks;
#endif

    using OldSchoolGames.HuntTheMuglump.Scripts.Components;
    using OldSchoolGames.HuntTheMuglump.Scripts.Exceptions;
    using OldSchoolGames.HuntTheMuglump.Scripts.MonoBehaviours;
    using OldSchoolGames.HuntTheMuglump.Scripts.Utilities;
    using System.Diagnostics;
#if UNITY_EDITOR
    using UnityEditor.Localization.Plugins.XLIFF.V20;
#endif

    public class PlatformManager : MonoBehaviour
    {
#if UNITY_STEAMWORKS && !UNITY_WEBGL
        public static SteamManager SteamManager
        {
            get => SteamManager.Instance;
        }

        private Dictionary<string, Leaderboard> leaderboards;
#endif
        public const SupportedPlatform Platform =

#if UNITY_STANDALONE_OSX
                                        SupportedPlatform.OSX;
#elif UNITY_IOS
                                        SupportedPlatform.iOS;
#elif UNITY_WEBGL
                                        SupportedPlatform.WebGL;
#elif !DISABLESTEAMWORKS
                                        SupportedPlatform.Steam;
#else
                                        SupportedPlatform.Windows;
#endif

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static PlatformManager Instance { get; private set; }

        public static string UserName
        {
            get
            {
                string username = null;
#if UNITY_WEBGL
                username = WebGLLeaderboardManager.Instance.GetPlayerName();
#elif UNITY_STEAMWORKS && !UNITY_WEBGL
                username = SteamManager.GetUsername();
#elif UNITY_IOS && !UNITY_WEBGL
                username = GameCenterManager.Instance.GetUsername();
                Debug.Log($"Username: {username}");
#endif
                return username;
            }
        }

        /// <summary>
        /// Executes when this instance is awakened.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }

#if UNITY_WEBGL
            // WebGL managers initialize themselves as singletons
            var achievementManager = WebGLAchievementManager.Instance;
            var leaderboardManager = WebGLLeaderboardManager.Instance;
#elif UNITY_STEAMWORKS && !UNITY_WEBGL
            this.leaderboards = new Dictionary<string, Leaderboard>()
            {
                {LeaderboardNames.TopScores,  new Leaderboard() { Name = LeaderboardNames.TopScores } },
                {LeaderboardNames.RoomsExplored, new Leaderboard() { Name = LeaderboardNames.RoomsExplored } }
            };

            foreach (var leaderboard in this.leaderboards.Values)
            {
                leaderboard.Init();
            }
#elif UNITY_IOS && !UNITY_WEBGL
            KTGameCenter.SharedCenter().Authenticate();
#endif

        }

        void OnEnable()
        {
#if UNITY_IOS && !UNITY_WEBGL
            StartCoroutine(RegisterForGameCenter());
#endif
        }

#if UNITY_IOS && !UNITY_WEBGL
        IEnumerator RegisterForGameCenter()
        {
            yield return new WaitForSeconds(0.5f);
            KTGameCenter.SharedCenter().GCUserAuthenticated += GCAuthentication;
            // KTGameCenter.SharedCenter().GCScoreSubmitted += ScoreSubmitted;
            // KTGameCenter.SharedCenter().GCAchievementSubmitted += AchievementSubmitted;
            // KTGameCenter.SharedCenter().GCAchievementsReset += AchivementsReset;
            // KTGameCenter.SharedCenter().GCMyScoreFetched += MyScoreFetched;
        }

        void GCAuthentication(string status)
        {
            Debug.Log("delegate call back status= " + status);
            StartCoroutine(CheckAttributes());
        }

        IEnumerator CheckAttributes()
        {
            yield return new WaitForSeconds(1.0f);
            Debug.Log(" alias= " + KTGameCenter.SharedCenter().PlayerAlias + " name= " +
                   KTGameCenter.SharedCenter().PlayerName + " id= " + KTGameCenter.SharedCenter().PlayerId);
        }
#endif
        public void EarnBadge(Badge badge)
        {
#if UNITY_WEBGL
            WebGLAchievementManager.Instance.EarnBadge(badge);
#elif UNITY_STEAMWORKS && !UNITY_WEBGL
            StartCoroutine(nameof(this.EarnBadgeRoutine), badge);
#elif UNITY_IOS && !UNITY_WEBGL
            KTGameCenter.SharedCenter().SubmitAchievement(100, badge.Name, true);
#endif
        }

        public void UpdateLeaderboardScore(string leaderboardName, int score)
        {
#if UNITY_WEBGL
            WebGLLeaderboardManager.Instance.SubmitScore(leaderboardName, score);
#elif UNITY_STEAMWORKS && !UNITY_WEBGL
            this.leaderboards[leaderboardName].UploadLeaderboardScore(score);
#endif
        }

#if UNITY_STEAMWORKS && !UNITY_WEBGL
        private IEnumerator EarnBadgeRoutine(Badge badge)
        {
            try
            {
                var earned = SteamUserStats.SetAchievement(badge.Name);
                Debug.Log($"Badge {badge.Name} earned: {earned}");
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            yield return null;
        }
#endif

#if UNITY_IOS && !UNITY_WEBGL
        private void AchievementSubmitted(string achId, string error)
        {
            
        }
#endif

        public void SaveFile(string text)
        {
#if UNITY_WEBGL
            WebGLManager.Instance.SaveData("HTM_SaveGame", text);
#elif UNITY_STEAMWORKS && !UNITY_WEBGL
            SteamManager.SaveFileOffline(text);
#else
            PlayerPrefs.SetString("HTM_SaveGame", text);
            PlayerPrefs.Save();
#endif
        }

        public string GetSaveFileContent()
        {
#if UNITY_WEBGL
            return WebGLManager.Instance.LoadData("HTM_SaveGame");
#elif UNITY_STEAMWORKS && !UNITY_WEBGL
            return SteamManager.GetSaveFileContent();
#else
            return PlayerPrefs.GetString("HTM_SaveGame", string.Empty);
#endif
        }
    }
}
