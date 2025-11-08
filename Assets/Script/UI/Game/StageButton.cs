using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Game.Data;
using Game.Managers;
using Game.Core;
using Game.SaveSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Game.Services;

namespace Game.UI
{
    /// <summary>
    /// 스테이지 버튼 UI 컴포넌트 V2
    /// Dictionary 기반 효율적인 데이터 접근
    /// </summary>
    public class StageButton : MonoBehaviour
    {
        [Header("Stage Configuration")]
        [SerializeField]
        private StageDataSO stageData;
        
        [SerializeField]
        private bool autoFindComponents = true;

        [Header("UI Components")]
        [SerializeField]
        private Button button;
        
        [SerializeField]
        private Image backgroundImage;
        
        [SerializeField]
        private Image stageIcon;
        
        [SerializeField]
        private Image thumbnailImage;
        
        [SerializeField]
        private GameObject lockOverlay;
        
        [SerializeField]
        private GameObject completionBadge;
        
        [SerializeField]
        private GameObject perfectBadge;
        
        [SerializeField]
        private GameObject newBadge;

        [Header("Text Components")]
        [SerializeField]
        private TextMeshProUGUI stageNameText;
        
        [SerializeField]
        private TextMeshProUGUI stageNumberText;
        
        [SerializeField]
        private TextMeshProUGUI difficultyText;
        
        [SerializeField]
        private TextMeshProUGUI scoreText;
        
        [SerializeField]
        private TextMeshProUGUI rankText;

        [Header("Progress Indicators")]
        [SerializeField]
        private Image[] starImages;
        
        [SerializeField]
        private Sprite emptyStar;
        
        [SerializeField]
        private Sprite filledStar;
        
        [SerializeField]
        private Slider progressBar;
        
        [SerializeField]
        private TextMeshProUGUI progressText;

        [Header("Visual Settings")]
        [SerializeField]
        private ColorScheme colorScheme = new ColorScheme();
        
        [SerializeField]
        private AnimationSettings animationSettings = new AnimationSettings();

        [Header("Events")]
        [SerializeField]
        private UnityEvent<string> onStageSelected = new UnityEvent<string>();
        
        [SerializeField]
        private UnityEvent<StageInfo> onStageInfoRequested = new UnityEvent<StageInfo>();
        
        [SerializeField]
        private UnityEvent<List<string>> onShowUnlockRequirements = new UnityEvent<List<string>>();

        // Runtime State
        private StageProgressManager progressManager;
        private StageInfo currentStageInfo;
        private bool isInitialized = false;
        private Animator animator;
        private CanvasGroup canvasGroup;

        #region Initialization

        private void Awake()
        {
            if (autoFindComponents)
            {
                FindComponents();
            }

            // Get references
            progressManager = StageProgressManager.Instance;
            animator = GetComponent<Animator>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void FindComponents()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
            
            // Find text components
            var texts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Name") && stageNameText == null)
                    stageNameText = text;
                else if (text.name.Contains("Number") && stageNumberText == null)
                    stageNumberText = text;
                else if (text.name.Contains("Score") && scoreText == null)
                    scoreText = text;
            }
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized) return;
            
            if (stageData == null)
            {
                Debug.LogError($"[StageButton] No stage data assigned to {gameObject.name}");
                return;
            }

            // Setup button
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClick);
            }

            // Initial update
            UpdateButtonState();
            
            isInitialized = true;
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            UpdateButtonState();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Event Management

        private void SubscribeToEvents()
        {
            if (progressManager != null)
            {
                progressManager.OnStageUnlocked += HandleStageUnlocked;
                progressManager.OnStageCompleted += HandleStageCompleted;
                progressManager.OnStageStateChanged += HandleStageStateChanged;
                progressManager.OnProgressUpdated += HandleProgressUpdated;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (progressManager != null)
            {
                progressManager.OnStageUnlocked -= HandleStageUnlocked;
                progressManager.OnStageCompleted -= HandleStageCompleted;
                progressManager.OnStageStateChanged -= HandleStageStateChanged;
                progressManager.OnProgressUpdated -= HandleProgressUpdated;
            }
        }

        #endregion

        #region State Update

        public void UpdateButtonState()
        {
            if (stageData == null || progressManager == null)
                return;

            // Get current stage info
            var progressData = progressManager.GetProgressData();
            currentStageInfo = stageData.GetStageInfo(progressData);

            // Update visuals
            UpdateVisuals();
            UpdateInteractability();
            UpdateProgressIndicators();
        }

        private void UpdateVisuals()
        {
            // Stage identification
            if (stageNameText != null)
            {
                stageNameText.text = currentStageInfo.isUnlocked ? 
                    stageData.DisplayName : "???";
            }

            if (stageNumberText != null)
            {
                stageNumberText.text = $"{stageData.ChapterId.Replace("chapter", "")}-{stageData.StageNumber}";
            }

            if (difficultyText != null)
            {
                difficultyText.text = $"Lv.{stageData.Difficulty}";
            }

            // Icons and images
            if (stageIcon != null && stageData.Icon != null)
            {
                stageIcon.sprite = stageData.Icon;
                stageIcon.color = GetStateColor();
            }

            if (thumbnailImage != null && stageData.Thumbnail != null && currentStageInfo.isUnlocked)
            {
                thumbnailImage.sprite = stageData.Thumbnail;
                thumbnailImage.gameObject.SetActive(true);
            }
            else if (thumbnailImage != null)
            {
                thumbnailImage.gameObject.SetActive(false);
            }

            // State overlays
            if (lockOverlay != null)
                lockOverlay.SetActive(currentStageInfo.state == StageState.Locked);

            if (completionBadge != null)
                completionBadge.SetActive(currentStageInfo.state == StageState.Cleared);

            if (perfectBadge != null)
                perfectBadge.SetActive(currentStageInfo.state == StageState.Perfect);

            if (newBadge != null)
            {
                bool isNew = currentStageInfo.state == StageState.Unlocked && 
                            currentStageInfo.clearCount == 0;
                newBadge.SetActive(isNew);
            }

            // Score and rank
            if (scoreText != null)
            {
                if (currentStageInfo.bestScore > 0)
                {
                    scoreText.text = $"{currentStageInfo.bestScore:N0}";
                    scoreText.gameObject.SetActive(true);
                }
                else
                {
                    scoreText.gameObject.SetActive(false);
                }
            }

            if (rankText != null && currentStageInfo.bestScore > 0)
            {
                string rank = stageData.CalculateRank(currentStageInfo.bestScore);
                if (!string.IsNullOrEmpty(rank))
                {
                    rankText.text = rank;
                    rankText.color = GetRankColor(rank);
                    rankText.gameObject.SetActive(true);
                }
                else
                {
                    rankText.gameObject.SetActive(false);
                }
            }

            // Background color
            if (backgroundImage != null)
            {
                backgroundImage.color = Color.Lerp(
                    GetStateColor(), 
                    stageData.ThemeColor, 
                    0.3f
                );
            }
        }

        private void UpdateInteractability()
        {
            bool canInteract = currentStageInfo.state != StageState.Locked;
            
            if (button != null)
            {
                button.interactable = canInteract;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = canInteract ? 1f : 0.6f;
            }
        }

        private void UpdateProgressIndicators()
        {
            // Stars
            if (starImages != null && starImages.Length > 0)
            {
                for (int i = 0; i < starImages.Length && i < 3; i++)
                {
                    if (starImages[i] == null) continue;

                    bool earned = i < currentStageInfo.bestStars;
                    
                    if (filledStar != null && emptyStar != null)
                    {
                        starImages[i].sprite = earned ? filledStar : emptyStar;
                        starImages[i].color = earned ? colorScheme.starEarnedColor : colorScheme.starEmptyColor;
                    }
                    else
                    {
                        starImages[i].gameObject.SetActive(earned);
                    }
                }
            }

            // Progress bar
            if (progressBar != null)
            {
                float progress = 0f;
                if (currentStageInfo.state == StageState.Perfect)
                {
                    progress = 1f;
                }
                else if (currentStageInfo.bestScore > 0)
                {
                    progress = (float)currentStageInfo.bestScore / stageData.Scoring.maxScore;
                }

                progressBar.value = progress;
            }

            // Progress text
            if (progressText != null)
            {
                if (currentStageInfo.clearCount > 0)
                {
                    progressText.text = $"Cleared: {currentStageInfo.clearCount}x";
                }
                else if (currentStageInfo.state == StageState.InProgress)
                {
                    progressText.text = "In Progress";
                }
                else
                {
                    progressText.text = "";
                }
            }
        }

        #endregion

        #region Interaction

        private void OnButtonClick()
        {
            if (stageData == null) return;

            if (currentStageInfo.state == StageState.Locked)
            {
                ShowUnlockRequirements();
            }
            else
            {
                SelectStage();
            }
        }

        private void SelectStage()
        {
            // Fire events
            onStageSelected?.Invoke(stageData.StageId);
            onStageInfoRequested?.Invoke(currentStageInfo);

            // Play animation
            if (animationSettings.useSelectAnimation && animator != null)
            {
                animator.SetTrigger("Select");
            }

            // Load stage scene
            if (!string.IsNullOrEmpty(stageData.SceneToLoad))
            {
                LoadStageScene();
            }
        }

        private void LoadStageScene()
        {
            // Set current stage in progress manager
            progressManager.StartStage(stageData.StageId);

            // Validate SceneData
            if (stageData.SceneData == null)
            {
                Debug.LogError($"[StageButton] SceneData not assigned for stage: {stageData.StageId}");
                return;
            }

            // Use SceneLoaderService if available
            if (ServiceLocator.IsRegistered<ISceneLoaderService>())
            {
                var sceneLoader = ServiceLocator.Get<ISceneLoaderService>();
                sceneLoader.LoadSceneAsync(stageData.SceneData);
            }
            else
            {
                SceneManager.LoadScene(stageData.SceneData.SceneName);
            }
        }

        private void ShowUnlockRequirements()
        {
            var requirements = stageData.GetUnlockRequirements();
            onShowUnlockRequirements?.Invoke(requirements);

            // Log for debugging
            Debug.Log($"[StageButton] Unlock requirements for {stageData.StageId}:");
            foreach (var req in requirements)
            {
                Debug.Log($"  - {req}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleStageUnlocked(string stageId)
        {
            if (stageData != null && stageData.StageId == stageId)
            {
                UpdateButtonState();
                PlayUnlockAnimation();
            }
        }

        private void HandleStageCompleted(string stageId, int score, int stars)
        {
            if (stageData != null && stageData.StageId == stageId)
            {
                UpdateButtonState();
                
                if (animationSettings.useClearAnimation && animator != null)
                {
                    animator.SetTrigger("Clear");
                }
            }
        }

        private void HandleStageStateChanged(string stageId, StageState newState)
        {
            if (stageData != null && stageData.StageId == stageId)
            {
                UpdateButtonState();
            }
        }

        private void HandleProgressUpdated(StageProgressData progressData)
        {
            UpdateButtonState();
        }

        private void PlayUnlockAnimation()
        {
            if (animationSettings.useUnlockAnimation && animator != null)
            {
                animator.SetTrigger("Unlock");
            }
        }

        #endregion

        #region Helper Methods

        private Color GetStateColor()
        {
            switch (currentStageInfo.state)
            {
                case StageState.Locked:
                    return colorScheme.lockedColor;
                case StageState.Unlocked:
                    return colorScheme.unlockedColor;
                case StageState.InProgress:
                    return colorScheme.inProgressColor;
                case StageState.Cleared:
                    return colorScheme.clearedColor;
                case StageState.Perfect:
                    return colorScheme.perfectColor;
                default:
                    return Color.white;
            }
        }

        private Color GetRankColor(string rank)
        {
            switch (rank)
            {
                case "S": return colorScheme.rankSColor;
                case "A": return colorScheme.rankAColor;
                case "B": return colorScheme.rankBColor;
                case "C": return colorScheme.rankCColor;
                default: return Color.gray;
            }
        }

        #endregion

        #region Public Methods

        public void SetStageData(StageDataSO data)
        {
            stageData = data;
            Initialize();
            UpdateButtonState();
        }

        public StageDataSO GetStageData()
        {
            return stageData;
        }

        public StageInfo GetStageInfo()
        {
            return currentStageInfo;
        }

        public void RefreshDisplay()
        {
            UpdateButtonState();
        }

        #endregion

        #region Nested Classes

        [System.Serializable]
        public class ColorScheme
        {
            public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            public Color unlockedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            public Color inProgressColor = new Color(1f, 0.9f, 0.5f, 1f);
            public Color clearedColor = new Color(0.5f, 1f, 0.5f, 1f);
            public Color perfectColor = new Color(1f, 0.8f, 0.2f, 1f);
            
            public Color starEarnedColor = Color.yellow;
            public Color starEmptyColor = Color.gray;
            
            public Color rankSColor = new Color(1f, 0.8f, 0f, 1f);
            public Color rankAColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            public Color rankBColor = new Color(0.2f, 0.5f, 0.8f, 1f);
            public Color rankCColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        [System.Serializable]
        public class AnimationSettings
        {
            public bool useUnlockAnimation = true;
            public bool useSelectAnimation = true;
            public bool useClearAnimation = true;
            public bool useHoverAnimation = true;
            
            public float animationDuration = 0.3f;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (stageData != null && Application.isPlaying)
            {
                UpdateButtonState();
            }
        }
#endif
    }
}
