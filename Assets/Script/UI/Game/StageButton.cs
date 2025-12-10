using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using Game.Data;
using Game.Managers;
using Game.Core;
using Game.SaveSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Game.Services;
using Game.Controllers;
using Game.UI.Panels;
using DG.Tweening;

namespace Game.UI
{
    /// <summary>
    /// 스테이지 버튼 UI 컴포넌트 V3
    /// DOTween 기반 애니메이션 시스템
    /// </summary>
    public class StageButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Stage Configuration")]
        [SerializeField]
        private string stageId;
        
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

        [Header("Info Panel")]
        [SerializeField]
        private Button infoButton;

        [SerializeField]
        private StageInfoEventChannelSO stageInfoEventChannel;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<string> onStageSelected = new UnityEvent<string>();
        
        [SerializeField]
        private UnityEvent<StageInfo> onStageInfoRequested = new UnityEvent<StageInfo>();
        
        [SerializeField]
        private UnityEvent<List<string>> onShowUnlockRequirements = new UnityEvent<List<string>>();

        [SerializeField]
        [Tooltip("스테이지 단일 클릭 시 적 유닛 프리뷰 등을 위해 StageDataSO를 전달하는 이벤트")]
        private UnityEvent<StageDataSO> onStagePreviewRequested = new UnityEvent<StageDataSO>();

        // Runtime State
        private IStageProgressManager progressManager;
        private StageInfo currentStageInfo;
        private bool isInitialized = false;
        private CanvasGroup canvasGroup;
        private Sequence currentAnimation;

        #region Initialization

        private void Awake()
        {
            if (autoFindComponents)
            {
                FindComponents();
            }

            // Get references
            if (ServiceLocator.IsRegistered<IStageProgressManager>())
            {
                progressManager = ServiceLocator.Get<IStageProgressManager>();
            }
            else
            {
                Debug.LogError("[StageButton] IStageProgressManager not found in ServiceLocator");
            }

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

            if (string.IsNullOrEmpty(stageId))
            {
                Debug.LogError($"[StageButton] No stage ID assigned to {gameObject.name}");
                return;
            }

            var stageData = GetStageData();
            if (stageData == null)
            {
                Debug.LogError($"[StageButton] Stage data not found for ID: {stageId}");
                return;
            }

            // Setup button
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                // 클릭 동작은 IPointerClickHandler.OnPointerClick에서 처리합니다.
            }

            // Setup info button (opens StageDetailInfoPanel via event channel)
            if (infoButton != null)
            {
                infoButton.onClick.RemoveAllListeners();
                infoButton.onClick.AddListener(OnInfoButtonClicked);
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

        private void OnDestroy()
        {
            // DOTween 애니메이션 정리 (메모리 누수 방지)
            currentAnimation?.Kill();
            currentAnimation = null;
        }

        /// <summary>
        /// StageId로부터 StageDataSO를 조회합니다.
        /// </summary>
        private StageDataSO GetStageData()
        {
            if (string.IsNullOrEmpty(stageId) || progressManager == null)
                return null;

            return progressManager.GetStageData(stageId);
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
            var stageData = GetStageData();
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
            var stageData = GetStageData();
            if (stageData == null) return;

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
            var stageData = GetStageData();
            if (stageData == null) return;

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
                else
                {
                    progressText.text = "";
                }
            }
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Unity UI 이벤트 시스템을 통한 클릭 처리
        /// clickCount를 사용해 단일/더블 클릭을 구분합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            var stageData = GetStageData();
            if (stageData == null) return;

            // 좌클릭만 처리
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // 잠긴 스테이지는 잠금 해제 조건만 표시
            if (currentStageInfo.state == StageState.Locked)
            {
                ShowUnlockRequirements();
                return;
            }

            if (eventData.clickCount == 1)
            {
                HandleSingleClick(stageData);
            }
            else if (eventData.clickCount == 2)
            {
                HandleDoubleClick();
            }
        }

        /// <summary>
        /// Info 버튼 클릭 시 스테이지 상세 정보 패널을 요청합니다.
        /// 잠긴 스테이지라도 정보는 항상 표시합니다.
        /// </summary>
        private void OnInfoButtonClicked()
        {
            var stageData = GetStageData();
            if (stageData == null)
            {
                Debug.LogWarning("[StageButton] Cannot show stage info - StageData is null");
                return;
            }

            if (stageInfoEventChannel == null)
            {
                Debug.LogError("[StageButton] StageInfoEventChannelSO is not assigned");
                return;
            }

            stageInfoEventChannel.ShowStageInfo(stageData);
            Debug.Log($"[StageButton] Info button clicked for stage: {stageData.StageId}");
        }

        /// <summary>
        /// 단일 클릭: 스테이지 선택 및 적 유닛 프리뷰를 위한 이벤트만 발생시킵니다.
        /// 씬 전환은 수행하지 않습니다.
        /// </summary>
        private void HandleSingleClick(StageDataSO stageData)
        {
            // 선택/정보 이벤트
            onStageSelected?.Invoke(stageData.StageId);
            onStageInfoRequested?.Invoke(currentStageInfo);

            // 프리뷰 이벤트 (적 유닛 카드 아이콘 표시 등)
            onStagePreviewRequested?.Invoke(stageData);

            // Play animation
            PlaySelectAnimation();
        }

        /// <summary>
        /// 더블 클릭: 기존 OnButtonClick 흐름(덱 검증 및 씬 전환)을 실행합니다.
        /// </summary>
        private void HandleDoubleClick()
        {
            OnButtonClick();
        }

        /// <summary>
        /// 스테이지 입장 전 덱/카드 검증 후 씬 전환을 수행합니다.
        /// (기존 버튼 onClick에서 호출되던 로직)
        /// </summary>
        private void OnButtonClick()
        {
            if (GetStageData() == null) return;

            if (currentStageInfo.state == StageState.Locked)
            {
                ShowUnlockRequirements();
                return;
            }

            // 덱/카드 검증
            if (!ValidateDeckAndCards(out string errorMessage))
            {
                ShowDeckWarning(errorMessage);
                return;
            }

            // 검증 통과 - 스테이지 선택 및 씬 전환
            SelectStage();
        }

        private void SelectStage()
        {
            var stageData = GetStageData();
            if (stageData == null) return;

            // Fire events
            onStageSelected?.Invoke(stageData.StageId);
            onStageInfoRequested?.Invoke(currentStageInfo);

            // Play animation
            PlaySelectAnimation();

            // Load stage scene
            if (!string.IsNullOrEmpty(stageData.SceneToLoad))
            {
                LoadStageScene();
            }
        }

        private void LoadStageScene()
        {
            var stageData = GetStageData();
            if (stageData == null) return;

            // Prepare stage for play (씬 로드 전 준비)
            progressManager.PrepareStageForPlay(stageData.StageId);

            // Validate SceneData
            if (stageData.SceneData == null)
            {
                Debug.LogError($"[StageButton] SceneData not assigned for stage: {stageData.StageId}");
                return;
            }

            // Use SceneTransitionController for consistent async loading with loading screen
            if (ServiceLocator.IsRegistered<ISceneTransitionController>())
            {
                var sceneTransition = ServiceLocator.Get<ISceneTransitionController>();
                sceneTransition.LoadSceneWithLoading(stageData.SceneData);
            }
            else
            {
                Debug.LogError($"[StageButton] ISceneTransitionController not found in ServiceLocator! " +
                              "Ensure SceneTransitionController is registered in Bootstrap scene.");
            }
        }

        private void ShowUnlockRequirements()
        {
            var stageData = GetStageData();
            if (stageData == null) return;

            var requirements = stageData.GetUnlockRequirements();
            onShowUnlockRequirements?.Invoke(requirements);

            // Log for debugging
            Debug.Log($"[StageButton] Unlock requirements for {stageData.StageId}:");
            foreach (var req in requirements)
            {
                Debug.Log($"  - {req}");
            }
        }

        /// <summary>
        /// 덱과 카드 존재 여부를 검증합니다.
        /// </summary>
        /// <param name="errorMessage">검증 실패 시 에러 메시지</param>
        /// <returns>검증 통과 여부</returns>
        private bool ValidateDeckAndCards(out string errorMessage)
        {
            errorMessage = string.Empty;

            // ISaveDataAdapter 가져오기
            if (!ServiceLocator.IsRegistered<ISaveDataAdapter>())
            {
                Debug.LogError("[StageButton] ISaveDataAdapter not found in ServiceLocator!");
                return true; // 서비스가 없으면 검증 스킵 (진행 허용)
            }

            var saveDataAdapter = ServiceLocator.Get<ISaveDataAdapter>();

            // 1. 덱 존재 여부 확인
            var deckNames = saveDataAdapter.GetSavedDeckNames();
            if (deckNames == null || deckNames.Count == 0)
            {
                errorMessage = "저장된 덱이 없습니다.\n타이틀 화면으로 돌아가 덱을 생성해주세요.";
                Debug.LogWarning($"[StageButton] {errorMessage}");
                return false;
            }

            // 2. 마지막 사용 덱 로드
            string lastUsedDeckName = saveDataAdapter.LoadLastUsedDeckName();

            // 마지막 사용 덱이 없으면 첫 번째 덱 사용
            if (string.IsNullOrEmpty(lastUsedDeckName))
            {
                lastUsedDeckName = deckNames[0];
                Debug.Log($"[StageButton] No last used deck found, using first deck: {lastUsedDeckName}");
            }

            // 3. 덱에 카드가 있는지 확인
            var deckCards = saveDataAdapter.LoadDeck(lastUsedDeckName);
            if (deckCards == null || deckCards.Count == 0)
            {
                errorMessage = $"현재 덱({lastUsedDeckName})에 카드가 없습니다.\n타이틀 화면으로 돌아가 카드를 추가해주세요.";
                Debug.LogWarning($"[StageButton] {errorMessage}");
                return false;
            }

            Debug.Log($"[StageButton] Deck validation passed: {lastUsedDeckName} with {deckCards.Count} card(s)");
            return true; // 검증 통과
        }

        /// <summary>
        /// 덱 검증 실패 시 경고 패널을 표시합니다.
        /// </summary>
        /// <param name="message">표시할 경고 메시지</param>
        private void ShowDeckWarning(string message)
        {
            // GlobalUIPanelManager를 통해 ConfirmPanelWithStageData 가져오기
            if (!ServiceLocator.IsRegistered<GlobalUIPanelManager>())
            {
                Debug.LogError("[StageButton] GlobalUIPanelManager not found in ServiceLocator!");
                return;
            }

            var confirmPanel = UIPanelFacade.GetPanel<ConfirmPanelWithSceneData>();

            if (confirmPanel == null)
            {
                Debug.LogError("[StageButton] ConfirmPanelWithSceneData not found in GlobalUIPanelManager! " +
                              "Ensure the panel is registered as a global panel.");
                return;
            }

            // 메시지 설정 및 패널 표시
            confirmPanel.ShowMessage(message);
            Debug.Log($"[StageButton] Showing deck warning panel: {message}");
        }

        #endregion

        #region Event Handlers

        private void HandleStageUnlocked(string stageId)
        {
            if (!string.IsNullOrEmpty(this.stageId) && this.stageId == stageId)
            {
                UpdateButtonState();
                PlayUnlockAnimation();
            }
        }

        private void HandleStageCompleted(string stageId, int score, int stars)
        {
            if (!string.IsNullOrEmpty(this.stageId) && this.stageId == stageId)
            {
                UpdateButtonState();
                PlayClearAnimation();
            }
        }

        private void HandleStageStateChanged(string stageId, StageState newState)
        {
            if (!string.IsNullOrEmpty(this.stageId) && this.stageId == stageId)
            {
                UpdateButtonState();
            }
        }

        private void HandleProgressUpdated(StageProgressData progressData)
        {
            UpdateButtonState();
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

        #region DOTween Animation Methods

        /// <summary>
        /// 스테이지 선택 시 펀치 스케일 애니메이션
        /// </summary>
        private void PlaySelectAnimation()
        {
            if (!animationSettings.useSelectAnimation) return;

            currentAnimation?.Kill();

            transform.DOPunchScale(
                animationSettings.selectPunchScale,
                animationSettings.animationDuration,
                animationSettings.selectVibrato,
                animationSettings.selectElasticity
            ).SetEase(animationSettings.selectEase);
        }

        /// <summary>
        /// 스테이지 클리어 시 반짝임 애니메이션 (스케일 + 페이드)
        /// </summary>
        private void PlayClearAnimation()
        {
            if (!animationSettings.useClearAnimation) return;

            currentAnimation?.Kill();
            currentAnimation = DOTween.Sequence();

            Vector3 originalScale = transform.localScale;
            float halfDuration = animationSettings.animationDuration * 0.5f;

            // 커지면서 페이드 아웃 → 작아지면서 페이드 인
            currentAnimation
                .Append(transform.DOScale(animationSettings.clearMaxScale, halfDuration))
                .Append(transform.DOScale(originalScale, halfDuration))
                .SetEase(animationSettings.clearEase);

            // CanvasGroup이 있으면 페이드 효과도 추가
            if (canvasGroup != null)
            {
                currentAnimation.Join(
                    DOTween.Sequence()
                        .Append(canvasGroup.DOFade(animationSettings.clearFadeMin, halfDuration))
                        .Append(canvasGroup.DOFade(1f, halfDuration))
                );
            }
        }

        /// <summary>
        /// 스테이지 잠금 해제 시 회전 + 스케일 애니메이션
        /// </summary>
        private void PlayUnlockAnimation()
        {
            if (!animationSettings.useUnlockAnimation) return;

            currentAnimation?.Kill();
            currentAnimation = DOTween.Sequence();

            Vector3 originalScale = transform.localScale;
            Vector3 originalRotation = transform.localEulerAngles;

            // 시작 스케일로 설정
            transform.localScale = animationSettings.unlockStartScale;

            // 원래 크기로 확대 + 360도 회전
            currentAnimation
                .Append(transform.DOScale(originalScale, animationSettings.animationDuration))
                .Join(transform.DORotate(
                    originalRotation + animationSettings.unlockRotation,
                    animationSettings.animationDuration,
                    RotateMode.FastBeyond360
                ))
                .SetEase(animationSettings.unlockEase)
                .OnComplete(() => {
                    // 회전값 정규화
                    transform.localEulerAngles = originalRotation;
                });
        }

        /// <summary>
        /// 마우스 호버 시 스케일 확대 (IPointerEnterHandler)
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!animationSettings.useHoverAnimation) return;
            if (currentStageInfo.state == StageState.Locked) return; // 잠긴 스테이지는 호버 안함

            currentAnimation?.Kill();
            transform.DOScale(animationSettings.hoverScale, animationSettings.hoverDuration)
                .SetEase(animationSettings.hoverEase);
        }

        /// <summary>
        /// 마우스 호버 해제 시 원래 스케일로 복귀 (IPointerExitHandler)
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!animationSettings.useHoverAnimation) return;

            currentAnimation?.Kill();
            transform.DOScale(Vector3.one, animationSettings.hoverDuration)
                .SetEase(animationSettings.hoverEase);
        }

        #endregion

        #region Public Methods

        public void SetStageId(string id)
        {
            stageId = id;
            Initialize();
            UpdateButtonState();
        }

        public string GetStageId()
        {
            return stageId;
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
            [Header("Animation Toggles")]
            public bool useUnlockAnimation = true;
            public bool useSelectAnimation = true;
            public bool useClearAnimation = true;
            public bool useHoverAnimation = true;

            [Header("General Settings")]
            public float animationDuration = 0.3f;

            [Header("Select Animation (Punch Scale)")]
            [Tooltip("스케일 펀치 강도")]
            public Vector3 selectPunchScale = new Vector3(0.2f, 0.2f, 0.2f);
            [Tooltip("펀치 애니메이션 진동 횟수")]
            public int selectVibrato = 10;
            [Tooltip("펀치 애니메이션 탄성")]
            public float selectElasticity = 1f;
            public Ease selectEase = Ease.OutElastic;

            [Header("Clear Animation (Sparkle - Scale + Fade)")]
            [Tooltip("최대 스케일")]
            public Vector3 clearMaxScale = new Vector3(1.2f, 1.2f, 1.2f);
            [Tooltip("페이드 최소값 (0~1)")]
            [Range(0f, 1f)]
            public float clearFadeMin = 0.5f;
            public Ease clearEase = Ease.OutQuad;

            [Header("Unlock Animation (Rotate + Scale)")]
            [Tooltip("회전 각도 (Z축)")]
            public Vector3 unlockRotation = new Vector3(0, 0, 360f);
            [Tooltip("시작 스케일")]
            public Vector3 unlockStartScale = new Vector3(0.5f, 0.5f, 0.5f);
            public Ease unlockEase = Ease.OutBack;

            [Header("Hover Animation")]
            [Tooltip("호버 시 스케일")]
            public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
            [Tooltip("호버 애니메이션 지속시간")]
            public float hoverDuration = 0.2f;
            public Ease hoverEase = Ease.OutQuad;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(stageId) && Application.isPlaying)
            {
                UpdateButtonState();
            }
        }
#endif
    }
}
