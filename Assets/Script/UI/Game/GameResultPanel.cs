using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 결과 패널 (Victory/Defeated 통합)
/// 기존 VictoryPanel과 DefeatedPanel의 기능을 통합한 새로운 구현
/// </summary>
public class GameResultPanel : UIPanel, IDataBindable<GameResultData>
{
    [Header("게임 결과 UI 요소")]
    [SerializeField] private GameObject victoryContainer;
    [SerializeField] private GameObject defeatContainer;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultMessageText;
    
    [Header("별 표시 시스템")]
    [SerializeField] private GameObject starsContainer;
    [SerializeField] private Image[] starImages;
    [SerializeField] private Sprite starFilledSprite;
    [SerializeField] private Sprite starEmptySprite;
    
    [Header("버튼 설정")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button menuButton;
    
    [Header("애니메이션 설정")]
    [SerializeField] private bool useResultAnimation = true;
    [SerializeField] private float animationDelay = 0.5f;
    
    // 데이터 바인딩
    private GameResultData boundData;
    public GameResultData BoundData => boundData;
    
    // 결과 표시 상태
    private bool isVictory = false;
    
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
        
        // 결과 애니메이션 재생
        if (useResultAnimation && boundData.IsValid)
        {
            PlayResultAnimation();
        }
    }
    
    protected override void OnHidePanel()
    {
        base.OnHidePanel();
        
        // 데이터 정리
        UnbindData();
    }
    
    #endregion
    
    #region IDataBindable 구현
    
    public void BindData(GameResultData data)
    {
        boundData = data;
        isVictory = data.IsVictory;
        
        // UI 업데이트
        UpdateResultDisplay();
        UpdateStarsDisplay();
        UpdateButtonStates();
        
        Debug.Log($"GameResultPanel: 데이터 바인딩 완료 - {(isVictory ? "승리" : "패배")}, 스테이지: {data.StageLevel}");
    }
    
    public void UnbindData()
    {
        boundData = default;
        isVictory = false;
        
        // UI 초기화
        SetInitialState();
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// 결과 표시 업데이트
    /// </summary>
    private void UpdateResultDisplay()
    {
        if (!boundData.IsValid) return;
        
        // 컨테이너 활성화/비활성화
        if (victoryContainer != null)
            victoryContainer.SetActive(isVictory);
        
        if (defeatContainer != null)
            defeatContainer.SetActive(!isVictory);
        
        // 텍스트 업데이트
        if (resultTitleText != null)
        {
            resultTitleText.text = isVictory ? "승리!" : "패배";
            resultTitleText.color = isVictory ? Color.yellow : Color.red;
        }
        
        if (resultMessageText != null)
        {
            resultMessageText.text = GetResultMessage();
        }
    }
    
    /// <summary>
    /// 별 표시 업데이트
    /// </summary>
    private void UpdateStarsDisplay()
    {
        if (!boundData.IsValid || !isVictory) 
        {
            // 패배 시 별 숨김
            if (starsContainer != null)
                starsContainer.SetActive(false);
            return;
        }
        
        if (starsContainer != null)
            starsContainer.SetActive(true);
        
        if (starImages != null && starImages.Length > 0)
        {
            int achievedStars = boundData.AchievedStars;
            
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    bool isFilled = i < achievedStars;
                    starImages[i].sprite = isFilled ? starFilledSprite : starEmptySprite;
                    starImages[i].color = isFilled ? Color.white : Color.gray;
                }
            }
        }
    }
    
    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        if (!boundData.IsValid) return;
        
        // 재시도 버튼 (항상 활성)
        if (retryButton != null)
        {
            retryButton.interactable = true;
        }
        
        // 다음 스테이지 버튼 (승리 시에만 활성)
        if (nextStageButton != null)
        {
            nextStageButton.interactable = isVictory && boundData.HasNextStage;
            nextStageButton.gameObject.SetActive(isVictory);
        }
        
        // 메뉴 버튼 (항상 활성)
        if (menuButton != null)
        {
            menuButton.interactable = true;
        }
    }
    
    #endregion
    
    #region 버튼 이벤트
    
    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    private void SetupButtonEvents()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }
        
        if (nextStageButton != null)
        {
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }
    }
    
    /// <summary>
    /// 재시도 버튼 클릭
    /// </summary>
    private void OnRetryClicked()
    {
        if (!boundData.IsValid) return;
        
        Debug.Log($"재시도 요청: {boundData.SceneName}");
        
        // SceneLoader 방식으로 씬 로드
        LoadScene(boundData.SceneName);
        
        // 패널 숨김
        OnHide();
    }
    
    /// <summary>
    /// 다음 스테이지 버튼 클릭
    /// </summary>
    private void OnNextStageClicked()
    {
        if (!boundData.IsValid || !boundData.HasNextStage) return;
        
        Debug.Log($"다음 스테이지 요청: {boundData.NextSceneName}");
        
        // 다음 스테이지 로드
        LoadScene(boundData.NextSceneName);
        
        // 패널 숨김
        OnHide();
    }
    
    /// <summary>
    /// 메뉴 버튼 클릭
    /// </summary>
    private void OnMenuClicked()
    {
        Debug.Log("메인 메뉴로 이동");
        
        // 메인 메뉴 씬 로드
        LoadScene("MainMenu"); // 또는 boundData.MenuSceneName
        
        // 패널 숨김
        OnHide();
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    private void SetInitialState()
    {
        if (victoryContainer != null)
            victoryContainer.SetActive(false);
        
        if (defeatContainer != null)
            defeatContainer.SetActive(false);
        
        if (starsContainer != null)
            starsContainer.SetActive(false);
        
        if (resultTitleText != null)
            resultTitleText.text = "";
        
        if (resultMessageText != null)
            resultMessageText.text = "";
    }
    
    /// <summary>
    /// 결과 메시지 생성
    /// </summary>
    private string GetResultMessage()
    {
        if (!boundData.IsValid) return "";
        
        if (isVictory)
        {
            return $"스테이지 {boundData.StageLevel} 클리어!\n별 {boundData.AchievedStars}개 획득";
        }
        else
        {
            return $"스테이지 {boundData.StageLevel} 실패\n다시 도전해보세요";
        }
    }
    
    /// <summary>
    /// 씬 로드 (기존 SceneLoader 기능 통합)
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("GameResultPanel: 로드할 씬 이름이 없습니다.");
            return;
        }
        
        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GameResultPanel: 씬 로드 실패 - {sceneName}, 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 결과 애니메이션 재생
    /// </summary>
    private void PlayResultAnimation()
    {
        // 간단한 애니메이션 예제 (실제로는 DOTween 등 사용)
        if (isVictory && starsContainer != null)
        {
            StartCoroutine(AnimateStars());
        }
    }
    
    /// <summary>
    /// 별 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator AnimateStars()
    {
        yield return new WaitForSeconds(animationDelay);
        
        if (starImages != null && boundData.IsValid)
        {
            for (int i = 0; i < Mathf.Min(starImages.Length, boundData.AchievedStars); i++)
            {
                if (starImages[i] != null)
                {
                    // 간단한 스케일 애니메이션
                    starImages[i].transform.localScale = Vector3.zero;
                    
                    float elapsedTime = 0f;
                    float duration = 0.3f;
                    
                    while (elapsedTime < duration)
                    {
                        elapsedTime += Time.deltaTime;
                        float scale = Mathf.Lerp(0f, 1f, elapsedTime / duration);
                        starImages[i].transform.localScale = Vector3.one * scale;
                        yield return null;
                    }
                    
                    starImages[i].transform.localScale = Vector3.one;
                }
                
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
    
    #endregion
    
    #region 정리
    
    protected override void OnCleanup()
    {
        // 버튼 이벤트 정리
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
        
        if (nextStageButton != null)
            nextStageButton.onClick.RemoveListener(OnNextStageClicked);
        
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
        
        // 데이터 정리
        UnbindData();
        
        base.OnCleanup();
    }
    
    #endregion
}

/// <summary>
/// 게임 결과 데이터 구조체
/// </summary>
[System.Serializable]
public struct GameResultData
{
    public bool IsVictory;
    public int StageLevel;
    public int AchievedStars;
    public string SceneName;
    public string NextSceneName;
    public string MenuSceneName;
    public bool HasNextStage;
    
    public bool IsValid => StageLevel >= 0 && !string.IsNullOrEmpty(SceneName);
    
    public GameResultData(bool isVictory, int stageLevel, int achievedStars = 0, 
                         string sceneName = "", string nextSceneName = "", 
                         string menuSceneName = "MainMenu")
    {
        IsVictory = isVictory;
        StageLevel = stageLevel;
        AchievedStars = Mathf.Clamp(achievedStars, 0, 3);
        SceneName = sceneName;
        NextSceneName = nextSceneName;
        MenuSceneName = menuSceneName;
        HasNextStage = !string.IsNullOrEmpty(nextSceneName);
    }
}