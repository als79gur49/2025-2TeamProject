using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 모달 스택 관리자 (기존 UIBlocker 개선)
/// 모달 UI의 계층 구조와 백그라운드 블로킹 관리
/// </summary>
public class UIModalStack : MonoBehaviour
{
    [Header("블로커 설정")]
    [SerializeField] private GameObject blockerPrefab;
    [SerializeField] private Color blockerColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private bool closeOnBlockerClick = true;
    
    [Header("애니메이션 설정")]
    [SerializeField] private bool useStackAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    
    [Header("디버그")]
    [SerializeField] private bool debugMode = false;
    
    // 모달 스택
    private readonly Stack<IUIModal> modalStack = new Stack<IUIModal>();
    private readonly Dictionary<IUIModal, GameObject> modalBlockers = new Dictionary<IUIModal, GameObject>();
    
    // 현재 블로커
    private GameObject currentBlocker;
    
    // Singleton 패턴
    private static UIModalStack instance;
    public static UIModalStack Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIModalStack>();
                
                if (instance == null)
                {
                    var stackGO = new GameObject("UIModalStack");
                    instance = stackGO.AddComponent<UIModalStack>();
                    DontDestroyOnLoad(stackGO);
                    Debug.Log("UIModalStack 자동 생성됨");
                }
            }
            return instance;
        }
    }
    
    // 이벤트
    public static event Action<IUIModal> OnModalPushed;
    public static event Action<IUIModal> OnModalPopped;
    public static event Action OnStackEmpty;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Singleton 패턴 구현
        if (instance != null && instance != this)
        {
            Debug.LogWarning("UIModalStack 중복 인스턴스 제거");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 기본 블로커 생성
        CreateDefaultBlocker();
    }
    
    private void Update()
    {
        // ESC 키로 모달 닫기 (옵션)
        if (Input.GetKeyDown(KeyCode.Escape) && modalStack.Count > 0)
        {
            PopModal();
        }
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            CleanupAllModals();
            instance = null;
        }
    }
    
    #endregion
    
    #region Modal Stack Management
    
    /// <summary>
    /// 모달 푸시 (스택에 추가)
    /// </summary>
    public void PushModal(IUIModal modal)
    {
        if (modal == null)
        {
            Debug.LogError("UIModalStack: null 모달을 푸시할 수 없습니다.");
            return;
        }
        
        // 이미 스택에 있는지 확인
        if (modalStack.Contains(modal))
        {
            Debug.LogWarning($"UIModalStack: 모달이 이미 스택에 있습니다: {modal.ModalID}");
            return;
        }
        
        // 모달 스택에 추가
        modalStack.Push(modal);
        
        // 블로커 생성 및 설정
        CreateBlockerForModal(modal);
        
        // 모달 표시
        modal.OnShow();
        
        // 블로커 위치 업데이트
        UpdateBlockerHierarchy();
        
        // 이벤트 알림
        OnModalPushed?.Invoke(modal);
        
        if (debugMode)
            Debug.Log($"UIModalStack: 모달 푸시됨 - {modal.ModalID}, 스택 크기: {modalStack.Count}");
    }
    
    /// <summary>
    /// 모달 팝 (스택에서 제거)
    /// </summary>
    public void PopModal()
    {
        if (modalStack.Count == 0)
        {
            Debug.LogWarning("UIModalStack: 팝할 모달이 없습니다.");
            return;
        }
        
        var modal = modalStack.Pop();
        
        // 모달 숨김
        modal.OnHide();
        
        // 블로커 제거
        RemoveBlockerForModal(modal);
        
        // 블로커 위치 업데이트
        UpdateBlockerHierarchy();
        
        // 이벤트 알림
        OnModalPopped?.Invoke(modal);
        
        if (modalStack.Count == 0)
        {
            OnStackEmpty?.Invoke();
        }
        
        if (debugMode)
            Debug.Log($"UIModalStack: 모달 팝됨 - {modal.ModalID}, 스택 크기: {modalStack.Count}");
    }
    
    /// <summary>
    /// 특정 모달 제거
    /// </summary>
    public void RemoveModal(IUIModal modal)
    {
        if (modal == null || !modalStack.Contains(modal)) return;
        
        // 스택 최상위가 아닌 경우 복잡한 처리 필요
        if (modalStack.Peek() == modal)
        {
            PopModal();
        }
        else
        {
            // 중간 모달 제거 (스택 재구성)
            RemoveModalFromMiddle(modal);
        }
    }
    
    /// <summary>
    /// 모든 모달 제거
    /// </summary>
    public void PopAllModals()
    {
        while (modalStack.Count > 0)
        {
            PopModal();
        }
    }
    
    /// <summary>
    /// 특정 모달까지 팝 (해당 모달 포함)
    /// </summary>
    public void PopToModal(IUIModal targetModal)
    {
        while (modalStack.Count > 0)
        {
            var current = modalStack.Peek();
            PopModal();
            
            if (current == targetModal)
                break;
        }
    }
    
    #endregion
    
    #region Blocker Management
    
    /// <summary>
    /// 기본 블로커 생성
    /// </summary>
    private void CreateDefaultBlocker()
    {
        if (blockerPrefab == null)
        {
            // 프리팹이 없으면 동적 생성
            CreateDynamicBlocker();
        }
    }
    
    /// <summary>
    /// 동적 블로커 생성
    /// </summary>
    private void CreateDynamicBlocker()
    {
        var blockerGO = new GameObject("ModalBlocker");
        var canvasGroup = blockerGO.AddComponent<CanvasGroup>();
        var image = blockerGO.AddComponent<UnityEngine.UI.Image>();
        var button = blockerGO.AddComponent<UnityEngine.UI.Button>();
        
        // 이미지 설정
        image.color = blockerColor;
        image.raycastTarget = true;
        
        // 버튼 설정 (블로커 클릭 시 모달 닫기)
        if (closeOnBlockerClick)
        {
            button.onClick.AddListener(() => OnBlockerClicked());
        }
        
        // RectTransform 설정 (전체 화면 덮도록)
        var rectTransform = blockerGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        blockerPrefab = blockerGO;
        blockerGO.SetActive(false);
    }
    
    /// <summary>
    /// 모달용 블로커 생성
    /// </summary>
    private void CreateBlockerForModal(IUIModal modal)
    {
        if (blockerPrefab == null) return;
        
        var blocker = Instantiate(blockerPrefab);
        blocker.name = $"Blocker_{modal.ModalID}";
        
        // 모달의 부모 Canvas 찾기
        var modalTransform = (modal as MonoBehaviour)?.transform;
        if (modalTransform != null)
        {
            var canvas = modalTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                blocker.transform.SetParent(canvas.transform, false);
            }
        }
        
        // 블로커 등록
        modalBlockers[modal] = blocker;
        currentBlocker = blocker;
        
        // 블로커 활성화
        blocker.SetActive(true);
        
        // 애니메이션 (옵션)
        if (useStackAnimation)
        {
            AnimateBlockerIn(blocker);
        }
    }
    
    /// <summary>
    /// 모달용 블로커 제거
    /// </summary>
    private void RemoveBlockerForModal(IUIModal modal)
    {
        if (!modalBlockers.TryGetValue(modal, out GameObject blocker)) return;
        
        modalBlockers.Remove(modal);
        
        // 애니메이션 후 제거
        if (useStackAnimation && blocker != null)
        {
            StartCoroutine(AnimateBlockerOutAndDestroy(blocker));
        }
        else
        {
            if (blocker != null)
                Destroy(blocker);
        }
        
        // 현재 블로커 업데이트
        UpdateCurrentBlocker();
    }
    
    /// <summary>
    /// 블로커 계층 구조 업데이트
    /// </summary>
    private void UpdateBlockerHierarchy()
    {
        if (modalStack.Count == 0) return;
        
        var topModal = modalStack.Peek();
        var modalTransform = (topModal as MonoBehaviour)?.transform;
        
        if (modalTransform != null && modalBlockers.TryGetValue(topModal, out GameObject blocker))
        {
            // 블로커를 모달 바로 아래에 배치
            blocker.transform.SetSiblingIndex(modalTransform.GetSiblingIndex());
        }
    }
    
    /// <summary>
    /// 현재 블로커 업데이트
    /// </summary>
    private void UpdateCurrentBlocker()
    {
        if (modalStack.Count > 0)
        {
            var topModal = modalStack.Peek();
            if (modalBlockers.TryGetValue(topModal, out GameObject blocker))
            {
                currentBlocker = blocker;
            }
        }
        else
        {
            currentBlocker = null;
        }
    }
    
    #endregion
    
    #region Animation
    
    /// <summary>
    /// 블로커 페이드 인 애니메이션
    /// </summary>
    private void AnimateBlockerIn(GameObject blocker)
    {
        if (blocker == null) return;
        
        var canvasGroup = blocker.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = blocker.AddComponent<CanvasGroup>();
        }
        
        StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, animationDuration));
    }
    
    /// <summary>
    /// 블로커 페이드 아웃 후 제거
    /// </summary>
    private System.Collections.IEnumerator AnimateBlockerOutAndDestroy(GameObject blocker)
    {
        if (blocker == null) yield break;
        
        var canvasGroup = blocker.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, animationDuration));
        }
        
        Destroy(blocker);
    }
    
    /// <summary>
    /// CanvasGroup 페이드 애니메이션
    /// </summary>
    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(from, to, progress);
            yield return null;
        }
        
        canvasGroup.alpha = to;
    }
    
    #endregion
    
    #region Event Handlers
    
    /// <summary>
    /// 블로커 클릭 이벤트
    /// </summary>
    private void OnBlockerClicked()
    {
        if (closeOnBlockerClick && modalStack.Count > 0)
        {
            var topModal = modalStack.Peek();
            
            // 모달이 블로커 클릭으로 닫힐 수 있는지 확인
            if (topModal.CanCloseOnBlockerClick)
            {
                PopModal();
            }
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// 중간 모달 제거 (스택 재구성)
    /// </summary>
    private void RemoveModalFromMiddle(IUIModal targetModal)
    {
        // 스택을 배열로 변환
        var modalArray = modalStack.ToArray();
        modalStack.Clear();
        
        // 블로커들 임시 저장
        var tempBlockers = new Dictionary<IUIModal, GameObject>(modalBlockers);
        modalBlockers.Clear();
        
        // 대상 모달 제외하고 다시 스택에 추가
        for (int i = modalArray.Length - 1; i >= 0; i--)
        {
            var modal = modalArray[i];
            
            if (modal != targetModal)
            {
                modalStack.Push(modal);
                
                // 블로커 복원
                if (tempBlockers.TryGetValue(modal, out GameObject blocker))
                {
                    modalBlockers[modal] = blocker;
                }
            }
            else
            {
                // 대상 모달 정리
                modal.OnHide();
                
                if (tempBlockers.TryGetValue(modal, out GameObject blocker))
                {
                    Destroy(blocker);
                }
            }
        }
        
        // 블로커 위치 업데이트
        UpdateBlockerHierarchy();
        UpdateCurrentBlocker();
    }
    
    /// <summary>
    /// 모든 모달 정리
    /// </summary>
    private void CleanupAllModals()
    {
        // 모든 모달 숨김
        foreach (var modal in modalStack)
        {
            modal.OnHide();
        }
        
        // 모든 블로커 제거
        foreach (var blocker in modalBlockers.Values)
        {
            if (blocker != null)
                Destroy(blocker);
        }
        
        modalStack.Clear();
        modalBlockers.Clear();
        currentBlocker = null;
    }
    
    #endregion
    
    #region Public Properties & Methods
    
    /// <summary>
    /// 현재 스택 크기
    /// </summary>
    public int StackCount => modalStack.Count;
    
    /// <summary>
    /// 스택이 비어있는지 확인
    /// </summary>
    public bool IsEmpty => modalStack.Count == 0;
    
    /// <summary>
    /// 최상위 모달 가져오기
    /// </summary>
    public IUIModal GetTopModal()
    {
        return modalStack.Count > 0 ? modalStack.Peek() : null;
    }
    
    /// <summary>
    /// 특정 모달이 스택에 있는지 확인
    /// </summary>
    public bool ContainsModal(IUIModal modal)
    {
        return modalStack.Contains(modal);
    }
    
    /// <summary>
    /// 모달 스택 상태 로깅
    /// </summary>
    [ContextMenu("Log Modal Stack")]
    public void LogModalStack()
    {
        Debug.Log("=== UI Modal Stack Status ===");
        Debug.Log($"스택 크기: {modalStack.Count}");
        
        int index = 0;
        foreach (var modal in modalStack)
        {
            Debug.Log($"[{index}] {modal.ModalID}");
            index++;
        }
        
        Debug.Log("=============================");
    }
    
    #endregion
}

/// <summary>
/// UI 모달 인터페이스
/// 모달 UI가 구현해야 하는 기본 기능
/// </summary>
public interface IUIModal
{
    /// <summary>
    /// 모달의 고유 식별자
    /// </summary>
    string ModalID { get; }
    
    /// <summary>
    /// 블로커 클릭으로 닫힐 수 있는지 여부
    /// </summary>
    bool CanCloseOnBlockerClick { get; }
    
    /// <summary>
    /// 모달 표시
    /// </summary>
    void OnShow();
    
    /// <summary>
    /// 모달 숨김
    /// </summary>
    void OnHide();
}