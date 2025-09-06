using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    public class WebGLManager : MonoBehaviour
    {
        private static WebGLManager instance;
        
        public static WebGLManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("WebGLManager");
                    instance = go.AddComponent<WebGLManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SaveToIndexedDB(string key, string data);
        
        [DllImport("__Internal")]
        private static extern string LoadFromIndexedDB(string key);
        
        [DllImport("__Internal")]
        private static extern void DeleteFromIndexedDB(string key);
        
        [DllImport("__Internal")]
        private static extern bool IsMobile();
        
        [DllImport("__Internal")]
        private static extern void RequestFullscreen();
        
        [DllImport("__Internal")]
        private static extern void ExitFullscreen();
        
        [DllImport("__Internal")]
        private static extern bool IsFullscreen();
#endif

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SaveData(string key, string data)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveToIndexedDB(key, data);
#else
            PlayerPrefs.SetString(key, data);
            PlayerPrefs.Save();
#endif
        }

        public string LoadData(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return LoadFromIndexedDB(key);
#else
            return PlayerPrefs.GetString(key, string.Empty);
#endif
        }

        public void DeleteData(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DeleteFromIndexedDB(key);
#else
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
#endif
        }

        public bool CheckIfMobile()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsMobile();
#else
            return false;
#endif
        }

        public void ToggleFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsFullscreen())
            {
                ExitFullscreen();
            }
            else
            {
                RequestFullscreen();
            }
#else
            Screen.fullScreen = !Screen.fullScreen;
#endif
        }

        public bool CheckFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsFullscreen();
#else
            return Screen.fullScreen;
#endif
        }
    }
}