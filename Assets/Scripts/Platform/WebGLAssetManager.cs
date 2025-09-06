using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;

namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    public class WebGLAssetManager : MonoBehaviour
    {
        private static WebGLAssetManager instance;
        
        [Header("Asset Loading Settings")]
        [SerializeField] private bool useAssetBundles = false;
        [SerializeField] private string assetBundleURL = "";
        
        [Header("Memory Management")]
        [SerializeField] private int maxCachedTextures = 50;
        [SerializeField] private int maxCachedAudioClips = 20;
        [SerializeField] private float memoryClearInterval = 60f; // Clear unused assets every minute
        
        [Header("Quality Settings")]
        [SerializeField] private bool autoAdjustQuality = true;
        [SerializeField] private int targetFrameRate = 60;
        
        // Asset caches
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
        private Queue<string> textureCacheOrder = new Queue<string>();
        private Queue<string> audioCacheOrder = new Queue<string>();
        
        // Performance monitoring
        private float frameTimeAccumulator = 0f;
        private int frameCount = 0;
        private float currentFPS = 60f;
        
        public static WebGLAssetManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("WebGLAssetManager");
                    instance = go.AddComponent<WebGLAssetManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        public event Action<float> OnLoadProgress;
        public event Action OnLoadComplete;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
#if UNITY_WEBGL
            // Start memory management coroutine
            StartCoroutine(MemoryManagementRoutine());
            
            // Start performance monitoring
            if (autoAdjustQuality)
            {
                StartCoroutine(PerformanceMonitorRoutine());
            }
#endif
        }
        
        private void Start()
        {
#if UNITY_WEBGL
            ApplyInitialQualitySettings();
#endif
        }
        
        private void ApplyInitialQualitySettings()
        {
            // Check if mobile
            bool isMobile = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            isMobile = WebGLManager.Instance.CheckIfMobile();
#endif
            
            if (isMobile)
            {
                // Lower quality for mobile
                QualitySettings.SetQualityLevel(0); // Assuming 0 is lowest quality
                Application.targetFrameRate = 30;
                Screen.SetResolution(Screen.width / 2, Screen.height / 2, true);
            }
            else
            {
                // Medium/High quality for desktop
                QualitySettings.SetQualityLevel(2); // Assuming 2 is medium quality
                Application.targetFrameRate = targetFrameRate;
            }
            
            // WebGL-specific optimizations
            QualitySettings.shadows = ShadowQuality.Disable; // Shadows are expensive in WebGL
            QualitySettings.antiAliasing = 2; // Limit anti-aliasing
            QualitySettings.vSyncCount = 0; // Disable VSync for better performance
        }
        
        public IEnumerator LoadTextureAsync(string path, Action<Texture2D> onComplete)
        {
            // Check cache first
            if (textureCache.ContainsKey(path))
            {
                onComplete?.Invoke(textureCache[path]);
                yield break;
            }
            
            // Load from Resources or URL
            if (path.StartsWith("http"))
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(path))
                {
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var texture = DownloadHandlerTexture.GetContent(request);
                        CacheTexture(path, texture);
                        onComplete?.Invoke(texture);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load texture from {path}: {request.error}");
                        onComplete?.Invoke(null);
                    }
                }
            }
            else
            {
                // Load from Resources
                var resourceRequest = Resources.LoadAsync<Texture2D>(path);
                yield return resourceRequest;
                
                if (resourceRequest.asset != null)
                {
                    var texture = resourceRequest.asset as Texture2D;
                    CacheTexture(path, texture);
                    onComplete?.Invoke(texture);
                }
                else
                {
                    Debug.LogError($"Failed to load texture from Resources: {path}");
                    onComplete?.Invoke(null);
                }
            }
        }
        
        public IEnumerator LoadAudioAsync(string path, Action<AudioClip> onComplete)
        {
            // Check cache first
            if (audioCache.ContainsKey(path))
            {
                onComplete?.Invoke(audioCache[path]);
                yield break;
            }
            
            // Determine audio type based on file extension or path
            AudioType audioType = AudioType.UNKNOWN;
            if (path.EndsWith(".mp3"))
                audioType = AudioType.MPEG;
            else if (path.EndsWith(".ogg"))
                audioType = AudioType.OGGVORBIS;
            else if (path.EndsWith(".wav"))
                audioType = AudioType.WAV;
            
            // Load from URL or Resources
            if (path.StartsWith("http"))
            {
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
                {
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var audioClip = DownloadHandlerAudioClip.GetContent(request);
                        CacheAudioClip(path, audioClip);
                        onComplete?.Invoke(audioClip);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load audio from {path}: {request.error}");
                        onComplete?.Invoke(null);
                    }
                }
            }
            else
            {
                // Load from Resources
                var resourceRequest = Resources.LoadAsync<AudioClip>(path);
                yield return resourceRequest;
                
                if (resourceRequest.asset != null)
                {
                    var audioClip = resourceRequest.asset as AudioClip;
                    CacheAudioClip(path, audioClip);
                    onComplete?.Invoke(audioClip);
                }
                else
                {
                    Debug.LogError($"Failed to load audio from Resources: {path}");
                    onComplete?.Invoke(null);
                }
            }
        }
        
        private void CacheTexture(string path, Texture2D texture)
        {
            if (texture == null) return;
            
            // Remove oldest if cache is full
            if (textureCacheOrder.Count >= maxCachedTextures)
            {
                var oldestPath = textureCacheOrder.Dequeue();
                if (textureCache.ContainsKey(oldestPath))
                {
                    var oldTexture = textureCache[oldestPath];
                    textureCache.Remove(oldestPath);
                    
                    // Don't destroy if it's a Resource
                    if (!oldestPath.StartsWith("http"))
                    {
                        Resources.UnloadAsset(oldTexture);
                    }
                }
            }
            
            textureCache[path] = texture;
            textureCacheOrder.Enqueue(path);
        }
        
        private void CacheAudioClip(string path, AudioClip clip)
        {
            if (clip == null) return;
            
            // Remove oldest if cache is full
            if (audioCacheOrder.Count >= maxCachedAudioClips)
            {
                var oldestPath = audioCacheOrder.Dequeue();
                if (audioCache.ContainsKey(oldestPath))
                {
                    var oldClip = audioCache[oldestPath];
                    audioCache.Remove(oldestPath);
                    
                    // Don't destroy if it's a Resource
                    if (!oldestPath.StartsWith("http"))
                    {
                        Resources.UnloadAsset(oldClip);
                    }
                }
            }
            
            audioCache[path] = clip;
            audioCacheOrder.Enqueue(path);
        }
        
        private IEnumerator MemoryManagementRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(memoryClearInterval);
                
#if UNITY_WEBGL
                // Force garbage collection periodically
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                
                // Log memory usage
                if (Debug.isDebugBuild)
                {
                    Debug.Log($"WebGL Memory: Textures cached: {textureCache.Count}, Audio cached: {audioCache.Count}");
                }
#endif
            }
        }
        
        private IEnumerator PerformanceMonitorRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                // Calculate FPS
                currentFPS = frameCount / frameTimeAccumulator;
                frameCount = 0;
                frameTimeAccumulator = 0f;
                
                // Auto-adjust quality based on performance
                if (autoAdjustQuality)
                {
                    AdjustQualityBasedOnPerformance();
                }
            }
        }
        
        private void Update()
        {
            // Track frame time for FPS calculation
            frameTimeAccumulator += Time.unscaledDeltaTime;
            frameCount++;
        }
        
        private void AdjustQualityBasedOnPerformance()
        {
            int currentQualityLevel = QualitySettings.GetQualityLevel();
            
            if (currentFPS < 25 && currentQualityLevel > 0)
            {
                // Decrease quality if FPS is too low
                QualitySettings.DecreaseLevel();
                Debug.Log($"Decreasing quality level to {QualitySettings.GetQualityLevel()} (FPS: {currentFPS:F1})");
            }
            else if (currentFPS > 55 && currentQualityLevel < QualitySettings.names.Length - 1)
            {
                // Increase quality if FPS is stable
                QualitySettings.IncreaseLevel();
                Debug.Log($"Increasing quality level to {QualitySettings.GetQualityLevel()} (FPS: {currentFPS:F1})");
            }
        }
        
        public void PreloadEssentialAssets(string[] texturePaths, string[] audioPaths, Action onComplete)
        {
            StartCoroutine(PreloadAssetsRoutine(texturePaths, audioPaths, onComplete));
        }
        
        private IEnumerator PreloadAssetsRoutine(string[] texturePaths, string[] audioPaths, Action onComplete)
        {
            int totalAssets = (texturePaths?.Length ?? 0) + (audioPaths?.Length ?? 0);
            int loadedAssets = 0;
            
            // Preload textures
            if (texturePaths != null)
            {
                foreach (var path in texturePaths)
                {
                    yield return LoadTextureAsync(path, (texture) =>
                    {
                        loadedAssets++;
                        OnLoadProgress?.Invoke((float)loadedAssets / totalAssets);
                    });
                }
            }
            
            // Preload audio
            if (audioPaths != null)
            {
                foreach (var path in audioPaths)
                {
                    yield return LoadAudioAsync(path, (clip) =>
                    {
                        loadedAssets++;
                        OnLoadProgress?.Invoke((float)loadedAssets / totalAssets);
                    });
                }
            }
            
            OnLoadComplete?.Invoke();
            onComplete?.Invoke();
        }
        
        public void ClearCache()
        {
            textureCache.Clear();
            audioCache.Clear();
            textureCacheOrder.Clear();
            audioCacheOrder.Clear();
            
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
        
        public float GetCurrentFPS()
        {
            return currentFPS;
        }
        
        public int GetCachedTextureCount()
        {
            return textureCache.Count;
        }
        
        public int GetCachedAudioCount()
        {
            return audioCache.Count;
        }
    }
}