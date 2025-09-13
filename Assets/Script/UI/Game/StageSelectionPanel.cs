using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 선택 패널
/// 기존 StagePanel의 기능을 개선하고 데이터 바인딩 강화
/// DataManager와 연동하여 스테이지 정보를 동적으로 표시
/// </summary>
public class StageSelectionPanel : UIPanel, IDataBindable<StageSelectionData>
{
    [Header("스테이지 정보 UI")]
    [SerializeField] private Image stageImage;
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI stageDescriptionText;
    [SerializeField] private Button stageButton;
    
    [Header("별 표시 시스템")]
    [SerializeField] private GameObject starsContainer;
    [SerializeField] private Image[] starImages;
    [SerializeField] private Sprite starFilledSprite;
    [SerializeField] private Sprite starEmptySprite;
    
    [Header("상태별 표시")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject clearedBadge;
    [SerializeField] private GameObject newBadge;
    
    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color clearedColor = Color.cyan;
    
    [Header("애니메이션 설정")]
    [SerializeField] private bool useUnlockAnimation = true;
    [SerializeField] private float animationDuration = 0.5f;
    
    // 데이터 바인딩
    private StageSelectionData boundData;
    public StageSelectionData BoundData => boundData;
    
    // 현재 스테이지 상태
    private StageClearState currentState;
    
    #region UIPanel 오버라이드
    
    protected override void OnInitialize()
    {
        base.OnInitialize();
        
        // 버튼 이벤트 설정
        SetupButtonEvents();
        
        // 초기 상태 설정
        SetInitialState();
    }
    
    protected override void OnShowPanel()
    {
        base.OnShowPanel();
        
        // 데이터가 바인딩되어 있다면 UI 업데이트
        if (boundData.IsValid)
        {
            UpdateUI();
        }
    }
    
    #endregion
    
    #region IDataBindable 구현
    
    public void BindData(StageSelectionData data)
    {
        if (!data.IsValid)
        {
            Debug.LogError("StageSelectionPanel: 유효하지 않은 데이터입니다.");
            return;
        }
        
        boundData = data;
        currentState = data.StageInfo.stageClearState;
        
        // UI 업데이트
        UpdateUI();
        
        Debug.Log($"StageSelectionPanel: 데이터 바인딩 완료 - 스테이지 {data.StageLevel}, 상태: {currentState}");
    }
    
    public void UnbindData()
    {
        boundData = default;
        currentState = StageClearState.NotCleard;
        
        // UI 초기화
        SetInitialState();
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// UI 전체 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (!boundData.IsValid) return;
        
        var stageInfo = boundData.StageInfo;
        
        // 기본 정보 업데이트
        UpdateStageInfo();
        
        // 상태별 UI 업데이트
        UpdateStateVisuals();
        
        // 별 표시 업데이트
        UpdateStarsDisplay();
        
        // 버튼 상태 업데이트
        UpdateButtonState();
        
        // 색상 업데이트
        UpdateColors();
    }
    
    /// <summary>
    /// 스테이지 기본 정보 업데이트
    /// </summary>
    private void UpdateStageInfo()
    {
        var stageInfo = boundData.StageInfo;
        
        // 스테이지 이미지
        if (stageImage != null && boundData.StageSprite != null)
        {
            stageImage.sprite = boundData.StageSprite;
        }
        
        // 스테이지 이름
        if (stageNameText != null)
        {
            if (currentState == StageClearState.NotCleard)
            {
                stageNameText.text = "???";
            }
            else
            {
                stageNameText.text = stageInfo.stageName;
            }
        }
        
        // 스테이지 설명
        if (stageDescriptionText != null)
        {
            if (currentState == StageClearState.NotCleard)
            {
                stageDescriptionText.text = "잠긴 스테이지";
            }
            else
            {
                stageDescriptionText.text = boundData.Description;
            }
        }
    }
    
    /// <summary>
    /// 상태별 시각적 요소 업데이트
    /// </summary>
    private void UpdateStateVisuals()
    {
        // 잠금 오버레이
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(currentState == StageClearState.NotCleard);
        }
        
        // 클리어 배지
        if (clearedBadge != null)
        {
            clearedBadge.SetActive(currentState == StageClearState.Cleard);
        }
        
        // 새 스테이지 배지
        if (newBadge != null)
        {
            newBadge.SetActive(currentState == StageClearState.InProgress);
        }
    }
    
    /// <summary>
    /// 별 표시 업데이트
    /// </summary>
    private void UpdateStarsDisplay()
    {
        if (starsContainer == null) return;
        
        // 잠긴 스테이지는 별 숨김
        bool showStars = currentState != StageClearState.NotCleard;
        starsContainer.SetActive(showStars);
        
        if (!showStars || starImages == null) return;
        
        int achievedStars = boundData.StageInfo.achievedStars;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                bool isFilled = i < achievedStars;
                starImages[i].sprite = isFilled ? starFilledSprite : starEmptySprite;
                starImages[i].color = isFilled ? Color.yellow : Color.gray;
            }
        }
    }
    
    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonState()
    {
        if (stageButton == null) return;
        
        // 잠긴 스테이지는 버튼 비활성화
        bool isInteractable = currentState != StageClearState.NotCleard;
        stageButton.interactable = isInteractable;
    }
    
    /// <summary>
    /// 색상 업데이트
    /// </summary>
    private void UpdateColors()
    {
        Color targetColor = currentState switch
        {
            StageClearState.NotCleard => lockedColor,
            StageClearState.Cleard => clearedColor,
            _ => normalColor
        };
        
        // 스테이지 이미지 색상 변경
        if (stageImage != null)
        {
            stageImage.color = targetColor;
        }
    }
    
    #endregion
    
    #region 버튼 이벤트
    
    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    private void SetupButtonEvents()
    {
        if (stageButton != null)
        {
            stageButton.onClick.AddListener(OnStageButtonClicked);
        }
    }
    
    /// <summary>
    /// 스테이지 버튼 클릭
    /// </summary>
    private void OnStageButtonClicked()
    {
        if (!boundData.IsValid || currentState == StageClearState.NotCleard)
        {
            Debug.LogWarning("잠긴 스테이지는 플레이할 수 없습니다.");
            return;
        }
        
        Debug.Log($"스테이지 {boundData.StageLevel} 시작: {boundData.StageInfo.sceneName}");
        
        // 씬 로드
        LoadStageScene();
    }
    
    #endregion
    
    #region 스테이지 로딩
    
    /// <summary>
    /// 스테이지 씬 로드
    /// </summary>
    private void LoadStageScene()
    {
        string sceneName = boundData.StageInfo.sceneName;
        
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("스테이지 씬 이름이 없습니다.");
            return;
        }
        
        try
        {
            // 로딩 화면 표시 (옵션)
            ShowLoadingScreen();
            
            // 씬 로드
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"스테이지 씬 로드 실패: {sceneName}, 오류: {ex.Message}");
            HideLoadingScreen();
        }
    }
    
    /// <summary>
    /// 로딩 화면 표시
    /// </summary>
    private void ShowLoadingScreen()
    {
        // 실제 구현에서는 LoadingPanel 표시
        Debug.Log("로딩 화면 표시");
    }
    
    /// <summary>
    /// 로딩 화면 숨김
    /// </summary>
    private void HideLoadingScreen()
    {
        // 실제 구현에서는 LoadingPanel 숨김
        Debug.Log("로딩 화면 숨김");
    }
    
    #endregion
    
    #region 애니메이션
    
    /// <summary>
    /// 스테이지 잠금 해제 애니메이션
    /// </summary>
    public void PlayUnlockAnimation()
    {
        if (!useUnlockAnimation) return;
        
        StartCoroutine(UnlockAnimationCoroutine());
    }
    
    /// <summary>
    /// 잠금 해제 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator UnlockAnimationCoroutine()
    {
        // 간단한 스케일 애니메이션
        Vector3 originalScale = transform.localScale;
        
        // 축소
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.2f, elapsedTime / (animationDuration / 2));
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 확대
        elapsedTime = 0f;
        while (elapsedTime < animationDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, elapsedTime / (animationDuration / 2));
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
        
        // 잠금 오버레이 제거
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(false);
        }
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    private void SetInitialState()
    {
        if (stageImage != null)
            stageImage.color = lockedColor;
        
        if (stageNameText != null)
            stageNameText.text = "";
        
        if (stageDescriptionText != null)
            stageDescriptionText.text = "";
        
        if (starsContainer != null)
            starsContainer.SetActive(false);
        
        if (lockedOverlay != null)
            lockedOverlay.SetActive(true);
        
        if (clearedBadge != null)
            clearedBadge.SetActive(false);
        
        if (newBadge != null)
            newBadge.SetActive(false);
        
        if (stageButton != null)
            stageButton.interactable = false;
    }
    
    /// <summary>
    /// DataManager에서 데이터 자동 로드
    /// </summary>
    public void LoadFromDataManager(int stageLevel)
    {
        try
        {
            // DataManager에서 스테이지 정보 가져오기
            var playerData = DataManager.Instance.PlayerData;
            
            if (stageLevel < 0 || stageLevel >= playerData.stageInfos.Length)
            {
                Debug.LogError($"유효하지 않은 스테이지 레벨: {stageLevel}");
                return;
            }
            
            var stageInfo = playerData.stageInfos[stageLevel];
            
            // StageSelectionData 생성
            var selectionData = new StageSelectionData
            {
                StageLevel = stageLevel,
                StageInfo = stageInfo,
                Description = $"스테이지 {stageLevel + 1}",
                StageSprite = null // 실제로는 Resources 또는 Addressables에서 로드
            };
            
            // 데이터 바인딩
            BindData(selectionData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DataManager에서 스테이지 데이터 로드 실패: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 정리
    
    protected override void OnCleanup()
    {
        // 버튼 이벤트 정리
        if (stageButton != null)
            stageButton.onClick.RemoveListener(OnStageButtonClicked);
        
        // 데이터 정리
        UnbindData();
        
        base.OnCleanup();
    }
    
    #endregion
}

/// <summary>
/// 스테이지 선택 데이터 구조체
/// </summary>
[System.Serializable]
public struct StageSelectionData
{
    public int StageLevel;
    public StageInfo StageInfo;
    public string Description;
    public Sprite StageSprite;
    
    public bool IsValid => StageLevel >= 0 && !string.IsNullOrEmpty(StageInfo.sceneName);
    
    public StageSelectionData(int stageLevel, StageInfo stageInfo, string description = "", Sprite stageSprite = null)
    {
        StageLevel = stageLevel;
        StageInfo = stageInfo;
        Description = description;
        StageSprite = stageSprite;
    }
}

/// <summary>
/// 기존 프로젝트의 StageInfo 및 관련 enum들 (참조용)
/// 실제로는 기존 프로젝트의 정의를 사용
/// </summary>
/*
[System.Serializable]
public struct StageInfo
{
    public string stageName;
    public string sceneName;
    public StageClearState stageClearState;
    public int achievedStars;
}

public enum StageClearState
{
    NotCleard,
    InProgress,
    Cleard
}
*/