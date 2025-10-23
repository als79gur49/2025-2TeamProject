<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# 하이브리드 UI 아키텍처: Canvas 관리 및 호출 통합 전략

당신의 두 가지 우려가 정확합니다. 이를 해결하는 완전한 솔루션을 제시합니다.

***

## 문제 1: Canvas 관리 전략

### 🎯 핵심 원칙: Canvas Sort Order 계층화

```
렌더링 순서 (Sort Order)
┌────────────────────────────────────┐
│ Sort Order: 1000 (최상위)            │
│ GlobalCanvas (전역 팝업)              │
│ - ConfirmDialog                     │
│ - LoadingScreen                     │
│ - NotificationPanel                 │
└────────────────────────────────────┘
         ↑ 항상 위에 렌더링
┌────────────────────────────────────┐
│ Sort Order: 100                     │
│ OverlayCanvas (씬 오버레이)           │
│ - 일시정지 메뉴                       │
│ - 설정 패널                          │
└────────────────────────────────────┘
         ↑
┌────────────────────────────────────┐
│ Sort Order: 0 (기본)                 │
│ LocalCanvas (씬 기본 UI)              │
│ - GameHUD                           │
│ - VictoryPanel                      │
│ - DefeatPanel                       │
└────────────────────────────────────┘
         ↑
┌────────────────────────────────────┐
│ 3D Scene (게임 월드)                 │
└────────────────────────────────────┘
```


***

## 해결 방안 1: 계층화된 Canvas 아키텍처

### GlobalUIPanelManager.cs (전역 Canvas 관리)

```csharp
namespace Game.UI
{
    /// <summary>
    /// 전역 팝업 UI 관리 - DontDestroyOnLoad
    /// Canvas Sort Order: 1000 (최상위)
    /// </summary>
    public class GlobalUIPanelManager : MonoBehaviour
    {
        public static GlobalUIPanelManager Instance => ServiceLocator.Get<GlobalUIPanelManager>();
        
        [Header("Canvas Configuration")]
        [SerializeField] private int globalCanvasSortOrder = 1000;
        
        [Header("Global Popup Prefabs")]
        [SerializeField] private ConfirmDialog confirmDialogPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;
        [SerializeField] private NotificationPanel notificationPanelPrefab;
        [SerializeField] private ErrorDialog errorDialogPrefab;
        [SerializeField] private ToastMessage toastMessagePrefab;
        
        // Canvas & Components
        private Canvas globalCanvas;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;
        
        // Popup instances
        private ConfirmDialog confirmDialogInstance;
        private LoadingScreen loadingScreenInstance;
        private NotificationPanel notificationPanelInstance;
        private ErrorDialog errorDialogInstance;
        private ToastMessage toastMessageInstance;
        
        public void Initialize()
        {
            CreateGlobalCanvas();
            CreateGlobalPopups();
            
            Debug.Log($"[GlobalUIPanelManager] Initialized (Sort Order: {globalCanvasSortOrder})");
        }
        
        /// <summary>
        /// 전역 Canvas 생성 - Screen Space Overlay 모드
        /// </summary>
        private void CreateGlobalCanvas()
        {
            // Canvas GameObject 생성
            GameObject canvasObj = new GameObject("GlobalUICanvas");
            canvasObj.transform.SetParent(transform);
            
            // Canvas 컴포넌트 설정
            globalCanvas = canvasObj.AddComponent<Canvas>();
            globalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            globalCanvas.sortingOrder = globalCanvasSortOrder; // ✅ 최상위 렌더링
            
            // CanvasScaler 설정 (반응형 UI)
            canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // GraphicRaycaster 추가 (입력 처리)
            graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log($"[GlobalUIPanelManager] Global Canvas created (Sort Order: {globalCanvasSortOrder})");
        }
        
        /// <summary>
        /// 전역 팝업들 미리 생성 (Lazy Loading 대신 초기화 시 생성)
        /// </summary>
        private void CreateGlobalPopups()
        {
            // ConfirmDialog
            if (confirmDialogPrefab != null)
            {
                confirmDialogInstance = Instantiate(confirmDialogPrefab, globalCanvas.transform);
                confirmDialogInstance.gameObject.SetActive(false);
            }
            
            // LoadingScreen
            if (loadingScreenPrefab != null)
            {
                loadingScreenInstance = Instantiate(loadingScreenPrefab, globalCanvas.transform);
                loadingScreenInstance.gameObject.SetActive(false);
            }
            
            // NotificationPanel
            if (notificationPanelPrefab != null)
            {
                notificationPanelInstance = Instantiate(notificationPanelPrefab, globalCanvas.transform);
                notificationPanelInstance.gameObject.SetActive(false);
            }
            
            // ErrorDialog
            if (errorDialogPrefab != null)
            {
                errorDialogInstance = Instantiate(errorDialogPrefab, globalCanvas.transform);
                errorDialogInstance.gameObject.SetActive(false);
            }
            
            // ToastMessage
            if (toastMessagePrefab != null)
            {
                toastMessageInstance = Instantiate(toastMessagePrefab, globalCanvas.transform);
                toastMessageInstance.gameObject.SetActive(false);
            }
            
            Debug.Log("[GlobalUIPanelManager] All global popups created");
        }
        
        #region Public API
        
        /// <summary>
        /// 확인 다이얼로그 표시
        /// </summary>
        public void ShowConfirmDialog(string message, Action onConfirm, Action onCancel = null)
        {
            if (confirmDialogInstance != null)
            {
                confirmDialogInstance.Show(message, onConfirm, onCancel);
            }
            else
            {
                Debug.LogError("[GlobalUIPanelManager] ConfirmDialog instance is null!");
            }
        }
        
        /// <summary>
        /// 로딩 화면 표시 (씬 전환 동안 유지)
        /// </summary>
        public void ShowLoadingScreen(string message = "Loading...")
        {
            if (loadingScreenInstance != null)
            {
                loadingScreenInstance.Show(message);
            }
        }
        
        public void HideLoadingScreen()
        {
            if (loadingScreenInstance != null)
            {
                loadingScreenInstance.Hide();
            }
        }
        
        /// <summary>
        /// 알림 패널 표시
        /// </summary>
        public void ShowNotification(string message, float duration = 3f)
        {
            if (notificationPanelInstance != null)
            {
                notificationPanelInstance.Show(message, duration);
            }
        }
        
        /// <summary>
        /// 에러 다이얼로그 표시
        /// </summary>
        public void ShowErrorDialog(string errorMessage)
        {
            if (errorDialogInstance != null)
            {
                errorDialogInstance.Show(errorMessage);
            }
        }
        
        /// <summary>
        /// 토스트 메시지 표시 (짧은 알림)
        /// </summary>
        public void ShowToast(string message, float duration = 2f)
        {
            if (toastMessageInstance != null)
            {
                toastMessageInstance.Show(message, duration);
            }
        }
        
        #endregion
    }
}
```


***

### LocalUIPanelManager.cs (씬 종속 Canvas 관리)

```csharp
namespace Game.UI
{
    /// <summary>
    /// 씬 종속 UI 패널 관리 - 각 씬마다 독립 존재
    /// Canvas Sort Order: 0~100 (씬 설정 가능)
    /// </summary>
    public class LocalUIPanelManager : MonoBehaviour
    {
        private static LocalUIPanelManager instance;
        
        public static LocalUIPanelManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<LocalUIPanelManager>();
                }
                return instance;
            }
        }
        
        [Header("Canvas Configuration")]
        [SerializeField] private Canvas localCanvas;
        [SerializeField] private int canvasSortOrder = 0; // ✅ Inspector에서 설정 가능
        
        [Header("Local UI Settings")]
        [SerializeField] private bool autoRegisterOnStart = true;
        [SerializeField] private bool validateCanvasOnAwake = true;
        
        // 씬별 패널 레지스트리
        private readonly Dictionary<Type, UIPanel> typedPanels = new Dictionary<Type, UIPanel>();
        private readonly List<UIPanel> activePanels = new List<UIPanel>();
        
        private void Awake()
        {
            instance = this;
            
            // ✅ Canvas 검증 및 설정
            if (validateCanvasOnAwake)
            {
                ValidateAndConfigureCanvas();
            }
            
            Debug.Log($"[LocalUIPanelManager] Initialized for scene: {gameObject.scene.name} (Sort Order: {canvasSortOrder})");
        }
        
        private void Start()
        {
            if (autoRegisterOnStart)
            {
                AutoRegisterLocalPanels();
            }
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                CleanupAllPanels();
                instance = null;
            }
        }
        
        /// <summary>
        /// Canvas 검증 및 자동 설정
        /// </summary>
        private void ValidateAndConfigureCanvas()
        {
            // 1. localCanvas가 할당되지 않았으면 현재 씬에서 찾기
            if (localCanvas == null)
            {
                localCanvas = GetComponentInChildren<Canvas>();
                
                if (localCanvas == null)
                {
                    // Canvas가 없으면 자동 생성
                    GameObject canvasObj = new GameObject("LocalUICanvas");
                    canvasObj.transform.SetParent(transform);
                    
                    localCanvas = canvasObj.AddComponent<Canvas>();
                    localCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    Debug.Log($"[LocalUIPanelManager] Auto-created Canvas for scene '{gameObject.scene.name}'");
                }
            }
            
            // 2. Sort Order 적용
            if (localCanvas != null)
            {
                localCanvas.sortingOrder = canvasSortOrder;
                Debug.Log($"[LocalUIPanelManager] Canvas Sort Order set to: {canvasSortOrder}");
            }
            else
            {
                Debug.LogError("[LocalUIPanelManager] Failed to validate Canvas!");
            }
        }
        
        /// <summary>
        /// 현재 씬의 패널만 자동 등록
        /// </summary>
        private void AutoRegisterLocalPanels()
        {
            if (localCanvas == null)
            {
                Debug.LogError("[LocalUIPanelManager] Cannot auto-register panels - Canvas is null!");
                return;
            }
            
            // localCanvas 하위의 모든 UIPanel 찾기
            UIPanel[] panels = localCanvas.GetComponentsInChildren<UIPanel>(true);
            int registeredCount = 0;
            
            foreach (var panel in panels)
            {
                RegisterPanel(panel);
                registeredCount++;
            }
            
            Debug.Log($"[LocalUIPanelManager] Registered {registeredCount} local panels in '{gameObject.scene.name}'");
        }
        
        public void RegisterPanel(UIPanel panel)
        {
            if (panel == null) return;
            
            Type panelType = panel.GetType();
            
            if (!typedPanels.ContainsKey(panelType))
            {
                typedPanels[panelType] = panel;
                Debug.Log($"[LocalUIPanelManager] Panel registered: {panelType.Name}");
            }
        }
        
        public T GetPanel<T>() where T : UIPanel
        {
            Type panelType = typeof(T);
            
            if (typedPanels.TryGetValue(panelType, out UIPanel panel))
            {
                return panel as T;
            }
            
            Debug.LogWarning($"[LocalUIPanelManager] Panel not found: {panelType.Name}");
            return null;
        }
        
        private void CleanupAllPanels()
        {
            typedPanels.Clear();
            activePanels.Clear();
        }
    }
}
```


***

## 문제 2: 호출자 입장에서 Global/Local 구분 문제

### 🎯 해결책: 통합 Facade 패턴 (UIPanelFacade)

```csharp
namespace Game.UI
{
    /// <summary>
    /// UI 패널 호출의 통합 진입점
    /// 호출자는 Global/Local 구분 없이 이 클래스만 사용
    /// </summary>
    public static class UIPanelFacade
    {
        #region Global Popup API (모든 씬에서 사용)
        
        /// <summary>
        /// 확인 다이얼로그 표시 (전역)
        /// </summary>
        public static void ShowConfirmDialog(string message, Action onConfirm, Action onCancel = null)
        {
            var globalUI = ServiceLocator.Get<GlobalUIPanelManager>();
            if (globalUI != null)
            {
                globalUI.ShowConfirmDialog(message, onConfirm, onCancel);
            }
            else
            {
                Debug.LogError("[UIPanelFacade] GlobalUIPanelManager not found!");
            }
        }
        
        /// <summary>
        /// 로딩 화면 표시 (전역)
        /// </summary>
        public static void ShowLoadingScreen(string message = "Loading...")
        {
            var globalUI = ServiceLocator.Get<GlobalUIPanelManager>();
            globalUI?.ShowLoadingScreen(message);
        }
        
        /// <summary>
        /// 로딩 화면 숨기기 (전역)
        /// </summary>
        public static void HideLoadingScreen()
        {
            var globalUI = ServiceLocator.Get<GlobalUIPanelManager>();
            globalUI?.HideLoadingScreen();
        }
        
        /// <summary>
        /// 알림 표시 (전역)
        /// </summary>
        public static void ShowNotification(string message, float duration = 3f)
        {
            var globalUI = ServiceLocator.Get<GlobalUIPanelManager>();
            globalUI?.ShowNotification(message, duration);
        }
        
        /// <summary>
        /// 에러 다이얼로그 표시 (전역)
        /// </summary>
        public static void ShowErrorDialog(string errorMessage)
        {
            var globalUI = ServiceLocator.Get<GlobalUIPanelManager>();
            globalUI?.ShowErrorDialog(errorMessage);
        }
        
        /// <summary>
        /// 토스트 메시지 표시 (전역)
        /// </summary>
        public static void ShowToast(string message, float duration = 2f)
        {
            var globalUI = ServiceLocator.Get<GlobalUIPanelManager>();
            globalUI?.ShowToast(message, duration);
        }
        
        #endregion
        
        #region Local Panel API (씬 종속)
        
        /// <summary>
        /// 로컬 패널 조회 (현재 씬)
        /// </summary>
        public static T GetLocalPanel<T>() where T : UIPanel
        {
            var localUI = LocalUIPanelManager.Instance;
            
            if (localUI != null)
            {
                return localUI.GetPanel<T>();
            }
            else
            {
                Debug.LogError("[UIPanelFacade] LocalUIPanelManager not found in current scene!");
                return null;
            }
        }
        
        /// <summary>
        /// 로컬 패널 표시 (현재 씬)
        /// </summary>
        public static void ShowLocalPanel<T>() where T : UIPanel
        {
            T panel = GetLocalPanel<T>();
            if (panel != null)
            {
                panel.Show();
            }
            else
            {
                Debug.LogWarning($"[UIPanelFacade] Local panel not found: {typeof(T).Name}");
            }
        }
        
        /// <summary>
        /// 로컬 패널 숨기기 (현재 씬)
        /// </summary>
        public static void HideLocalPanel<T>() where T : UIPanel
        {
            T panel = GetLocalPanel<T>();
            if (panel != null)
            {
                panel.Hide();
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 전역 UI 시스템이 초기화되었는지 확인
        /// </summary>
        public static bool IsGlobalUIAvailable()
        {
            return ServiceLocator.IsRegistered<GlobalUIPanelManager>();
        }
        
        /// <summary>
        /// 로컬 UI 시스템이 초기화되었는지 확인
        /// </summary>
        public static bool IsLocalUIAvailable()
        {
            return LocalUIPanelManager.Instance != null;
        }
        
        #endregion
    }
}
```


***

## 사용 예시: 호출자 코드 단순화

### 예시 1: 게임 컨트롤러 (Global + Local 혼용)

```csharp
public class GameController : MonoBehaviour
{
    public void OnPlayerDeath()
    {
        // ✅ 전역 팝업 사용 (Facade를 통해)
        UIPanelFacade.ShowConfirmDialog(
            "다시 시도하시겠습니까?",
            onConfirm: RestartGame,
            onCancel: ReturnToMainMenu
        );
    }
    
    public void OnVictory()
    {
        // ✅ 로컬 패널 사용 (Facade를 통해)
        UIPanelFacade.ShowLocalPanel<VictoryPanel>();
        
        // ✅ 동시에 전역 알림도 표시 가능
        UIPanelFacade.ShowNotification("스테이지 클리어!", 3f);
    }
    
    private void SaveGameData()
    {
        // ✅ 저장 중 로딩 표시
        UIPanelFacade.ShowLoadingScreen("저장 중...");
        
        // 비동기 저장 작업
        SaveDataAsync(() =>
        {
            UIPanelFacade.HideLoadingScreen();
            UIPanelFacade.ShowToast("저장 완료", 2f);
        });
    }
    
    private void RestartGame()
    {
        UIPanelFacade.ShowLoadingScreen("재시작 중...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    private void ReturnToMainMenu()
    {
        var sceneTransition = ServiceLocator.Get<ISceneTransitionController>();
        sceneTransition.LoadSceneWithLoading(mainMenuSceneData);
    }
}
```


### 예시 2: 상점 시스템 (Global만 사용)

```csharp
public class ShopManager : MonoBehaviour
{
    public void OnPurchaseItem(ShopItem item)
    {
        // ✅ 호출자는 Global/Local 구분 불필요
        UIPanelFacade.ShowConfirmDialog(
            $"{item.itemName}을(를) {item.price} 골드에 구매하시겠습니까?",
            onConfirm: () => CompletePurchase(item),
            onCancel: () => Debug.Log("구매 취소")
        );
    }
    
    private void CompletePurchase(ShopItem item)
    {
        if (PlayerData.Gold >= item.price)
        {
            PlayerData.Gold -= item.price;
            PlayerData.AddItem(item);
            
            // ✅ 구매 성공 토스트
            UIPanelFacade.ShowToast($"{item.itemName} 구매 완료!", 2f);
        }
        else
        {
            // ✅ 에러 다이얼로그
            UIPanelFacade.ShowErrorDialog("골드가 부족합니다!");
        }
    }
}
```


### 예시 3: 씬 전환 시스템 (전역 로딩 활용)

```csharp
public class SceneTransitionController : MonoBehaviour
{
    public void LoadSceneWithLoading(SceneData sceneData)
    {
        StartCoroutine(LoadSceneCoroutine(sceneData));
    }
    
    private IEnumerator LoadSceneCoroutine(SceneData sceneData)
    {
        // ✅ 전역 로딩 화면 표시 (씬 전환 동안 유지됨!)
        UIPanelFacade.ShowLoadingScreen($"{sceneData.SceneName} 로딩 중...");
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneData.SceneName);
        
        while (!operation.isDone)
        {
            // 로딩 진행률 업데이트 (GlobalUIPanelManager에 메서드 추가 필요)
            // UIPanelFacade.UpdateLoadingProgress(operation.progress);
            yield return null;
        }
        
        // ✅ 새 씬 로드 완료 후 로딩 화면 숨기기
        yield return new WaitForSeconds(0.5f);
        UIPanelFacade.HideLoadingScreen();
    }
}
```


***

## Canvas Sort Order 관리 가이드

### 권장 Sort Order 할당표

| Canvas 타입 | Sort Order | 용도 | 예시 |
| :-- | :-- | :-- | :-- |
| **Global Canvas** | 1000 | 최상위 팝업 | ConfirmDialog, ErrorDialog |
| **Overlay Canvas** | 500 | 씬 오버레이 | 일시정지 메뉴, 설정 |
| **HUD Canvas** | 100 | 게임 HUD | 체력바, 미니맵, 스킬 UI |
| **Main Canvas** | 0 | 기본 UI | VictoryPanel, DefeatPanel |
| **Background Canvas** | -100 | 배경 UI | 데코레이션, 정적 배경 |

[^1][^2][^3][^4][^5][^6]

### Inspector 설정 예시

```csharp
// TitleScene의 LocalUIPanelManager
[Header("Canvas Configuration")]
[SerializeField] private int canvasSortOrder = 0; // 기본 UI

// PrototypeScene의 LocalUIPanelManager (HUD 포함)
[Header("Canvas Configuration")]
[SerializeField] private int canvasSortOrder = 100; // HUD 우선순위

// PauseMenuPanel이 있는 별도 Canvas
Canvas pauseCanvas;
pauseCanvas.sortingOrder = 500; // 게임 위에 표시
```


***

## 최종 아키텍처 다이어그램

```
┌─────────────────────────────────────────────────────────┐
│ 호출자 코드 (GameController, ShopManager 등)              │
│                                                          │
│ UIPanelFacade.ShowConfirmDialog()  ← 단일 진입점        │
│ UIPanelFacade.ShowLocalPanel<T>()                       │
└─────────────────────────────────────────────────────────┘
                         ↓
         ┌───────────────┴───────────────┐
         ↓                                ↓
┌──────────────────────┐      ┌──────────────────────┐
│ GlobalUIPanelManager │      │ LocalUIPanelManager  │
│ (전역 - ServiceLocator)│      │ (씬 종속 - Instance) │
│                       │      │                      │
│ Sort Order: 1000      │      │ Sort Order: 0~500   │
│ DontDestroyOnLoad     │      │ 씬마다 독립          │
│                       │      │                      │
│ GlobalCanvas          │      │ LocalCanvas          │
│ ├─ ConfirmDialog      │      │ ├─ VictoryPanel     │
│ ├─ LoadingScreen      │      │ ├─ DefeatPanel      │
│ ├─ NotificationPanel  │      │ ├─ GameHUD          │
│ └─ ErrorDialog        │      │ └─ ...              │
└──────────────────────┘      └──────────────────────┘
```


***

## Canvas 관리 Best Practices

### ✅ Do's

1. **Sort Order 명확히 분리:** Global(1000), Overlay(500), Main(0~100) 계층 준수[^2][^4][^6]
2. **Sub-Canvas 활용:** 동적 UI는 별도 Sub-Canvas로 분리하여 성능 최적화[^7][^8][^9][^1]
3. **Screen Space Overlay 우선:** 대부분의 UI는 Overlay 모드 사용[^3][^10][^11]
4. **Facade 패턴 사용:** 호출자가 Global/Local 구분 불필요하게 통합 인터페이스 제공

### ❌ Don'ts

1. **모든 UI를 하나의 Canvas에 배치 금지:** 성능 저하 심각[^8][^9][^1][^7]
2. **Sort Order 중복 사용 금지:** 렌더링 순서 예측 불가[^4][^6][^2]
3. **Canvas 중첩 남용 금지:** 필요한 경우에만 Sub-Canvas 사용[^1][^7]
4. **World Space Canvas를 UI에 사용 금지:** 특수한 경우(3D 월드 UI)만 사용[^11][^12][^3]

***

## 결론

**이 하이브리드 아키텍처 + Facade 패턴은:**

1. ✅ **Canvas Sort Order 계층화**로 렌더링 순서 보장[^6][^2][^3][^4]
2. ✅ **UIPanelFacade 통합 인터페이스**로 호출자 코드 단순화
3. ✅ **Global/Local 자동 라우팅**으로 개발자가 구분 불필요
4. ✅ **Inspector 설정 가능한 Sort Order**로 씬별 커스터마이징
5. ✅ **성능 최적화 (Split Canvas)**를 통한 불필요한 재렌더링 방지[^9][^7][^1]

호출자는 `UIPanelFacade`만 사용하면 되며, 내부적으로 자동으로 Global/Local을 구분하여 처리합니다.
<span style="display:none">[^13][^14][^15][^16][^17][^18][^19][^20]</span>

<div align="center">⁂</div>

[^1]: https://www.wayline.io/blog/unity-ui-best-practices-for-performance

[^2]: https://www.reddit.com/r/Unity3D/comments/1j02971/controlling_rendering_order_of_multiple_canvases/

[^3]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-Canvas.html

[^4]: https://stackoverflow.com/questions/37691592/unity-how-to-change-the-rendering-order-of-a-canvas

[^5]: https://docs.unity3d.com/ScriptReference/Canvas-sortingOrder.html

[^6]: https://www.reddit.com/r/Unity2D/comments/1eltx0l/how_to_set_sorting_orders_of_images_and_sprites/

[^7]: https://unity.com/how-to/unity-ui-optimization-tips

[^8]: https://www.reddit.com/r/unity/comments/1cq66o1/should_i_have_everything_in_one_canvas_or_have/

[^9]: https://community.gamedev.tv/t/multiple-canvases-is-recommended-by-unity/198288

[^10]: https://www.youtube.com/watch?v=OD-p1eMsyrU

[^11]: https://dev.to/marbleit/unity-ui-system-best-practices-2o24

[^12]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UICanvas.html

[^13]: https://hub.vive.com/storage/docs/en-us/UnityXR/UnityXRMultiLayerCanvas.html

[^14]: https://www.youtube.com/watch?v=5_BwFB-1dAo

[^15]: https://marbleit.rs/blog/unity-ui-system-best-practices/

[^16]: https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Canvas-sortingOrder.html

[^17]: https://stackoverflow.com/questions/53480971/unity2d-layering-multiple-canvas

[^18]: https://stackoverflow.com/questions/46721264/selection-of-canvas-or-panel-or-scene-for-ui-in-unity

[^19]: https://programmingsource.tistory.com/25

[^20]: https://www.reddit.com/r/Unity2D/comments/109xozl/is_there_a_downside_to_having_many_ui_canvases/

