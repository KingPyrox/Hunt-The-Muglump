using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OldSchoolGames.HuntTheMuglump.Scripts.Platform
{
    /// <summary>
    /// Simple touch control overlay for WebGL mobile browsers
    /// This creates UI buttons that can be touched to control the game
    /// </summary>
    public class WebGLTouchControls : MonoBehaviour
    {
        private static WebGLTouchControls instance;
        
        [Header("Control Buttons")]
        [SerializeField] private GameObject touchControlsPanel;
        [SerializeField] private Button upButton;
        [SerializeField] private Button downButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button pauseButton;
        
        private bool isMobile = false;
        
        public static WebGLTouchControls Instance
        {
            get { return instance; }
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
            
#if UNITY_WEBGL && !UNITY_EDITOR
            // Check if mobile and show/hide controls accordingly
            isMobile = WebGLManager.Instance.CheckIfMobile();
            SetTouchControlsVisible(isMobile);
#else
            // Hide touch controls in editor
            SetTouchControlsVisible(false);
#endif
        }
        
        public void SetTouchControlsVisible(bool visible)
        {
            if (touchControlsPanel != null)
            {
                touchControlsPanel.SetActive(visible);
            }
        }
        
        public bool IsMobileDevice()
        {
            return isMobile;
        }
        
        // These methods would be called by the UI buttons
        public void OnUpPressed()
        {
            // Trigger up movement
            Debug.Log("Up pressed");
        }
        
        public void OnUpReleased()
        {
            // Stop up movement
            Debug.Log("Up released");
        }
        
        public void OnDownPressed()
        {
            // Trigger down movement
            Debug.Log("Down pressed");
        }
        
        public void OnDownReleased()
        {
            // Stop down movement
            Debug.Log("Down released");
        }
        
        public void OnLeftPressed()
        {
            // Trigger left movement
            Debug.Log("Left pressed");
        }
        
        public void OnLeftReleased()
        {
            // Stop left movement
            Debug.Log("Left released");
        }
        
        public void OnRightPressed()
        {
            // Trigger right movement
            Debug.Log("Right pressed");
        }
        
        public void OnRightReleased()
        {
            // Stop right movement
            Debug.Log("Right released");
        }
        
        public void OnActionPressed()
        {
            // Trigger action
            Debug.Log("Action pressed");
        }
        
        public void OnPausePressed()
        {
            // Trigger pause
            Debug.Log("Pause pressed");
        }
    }
}