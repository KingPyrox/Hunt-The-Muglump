using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    /// <summary>
    /// WebGL-specific input handling
    /// Note: Unity's Input System already handles keyboard, mouse, touch, and gamepad input
    /// This adapter provides additional WebGL-specific functionality
    /// </summary>
    public class WebGLInputAdapter : MonoBehaviour
    {
        private static WebGLInputAdapter instance;
        
        [Header("Touch Control Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float doubleTapTime = 0.3f;
        
        private Vector2 touchStartPosition;
        private float lastTapTime;
        private bool useVirtualControls = false;
        
        public static WebGLInputAdapter Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("WebGLInputAdapter");
                    instance = go.AddComponent<WebGLInputAdapter>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Check if we're on a mobile device
            CheckDeviceType();
        }

        private void CheckDeviceType()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            useVirtualControls = WebGLManager.Instance.CheckIfMobile();
            if (useVirtualControls)
            {
                Debug.Log("Mobile device detected - touch controls may be needed");
            }
#else
            useVirtualControls = false;
#endif
        }

        private void Start()
        {
            // Log available input devices
            Debug.Log("WebGL Input Adapter started");
            Debug.Log($"Keyboard available: {Keyboard.current != null}");
            Debug.Log($"Mouse available: {Mouse.current != null}");
            Debug.Log($"Touchscreen available: {Touchscreen.current != null}");
            Debug.Log($"Gamepad available: {Gamepad.current != null}");
        }

        public bool IsUsingVirtualControls()
        {
            return useVirtualControls;
        }

        public void SetVirtualControlsEnabled(bool enabled)
        {
            useVirtualControls = enabled;
        }
        
        /// <summary>
        /// Check if any gamepad is connected
        /// </summary>
        public bool IsGamepadConnected()
        {
            return Gamepad.current != null;
        }
        
        /// <summary>
        /// Get the primary gamepad if available
        /// </summary>
        public Gamepad GetGamepad()
        {
            return Gamepad.current;
        }
        
        /// <summary>
        /// Check if touch input is available
        /// </summary>
        public bool IsTouchAvailable()
        {
            return Touchscreen.current != null;
        }
        
        /// <summary>
        /// Helper method to detect swipe gestures from touch input
        /// </summary>
        public bool DetectSwipe(out Vector2 swipeDirection)
        {
            swipeDirection = Vector2.zero;
            
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return false;
            
            var primaryTouch = touchscreen.primaryTouch;
            
            if (primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchStartPosition = primaryTouch.position.ReadValue();
            }
            else if (primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                var endPosition = primaryTouch.position.ReadValue();
                var delta = endPosition - touchStartPosition;
                
                if (delta.magnitude > swipeThreshold)
                {
                    swipeDirection = delta.normalized;
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Helper method to detect double tap
        /// </summary>
        public bool DetectDoubleTap()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return false;
            
            var primaryTouch = touchscreen.primaryTouch;
            
            if (primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                float currentTime = Time.time;
                if (currentTime - lastTapTime < doubleTapTime)
                {
                    lastTapTime = 0; // Reset to prevent triple tap
                    return true;
                }
                lastTapTime = currentTime;
            }
            
            return false;
        }
    }
}