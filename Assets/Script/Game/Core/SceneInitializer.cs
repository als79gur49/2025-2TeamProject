using UnityEngine;
using Game.Core;
using Game.UI.Panels;
using Game.Services;

namespace Game.Initialization
{
    /// <summary>
    /// Template Method 패턴 - 씬 초기화 기본 클래스
    /// 모든 씬 초기화의 공통 구조를 정의
    ///
    /// 초기화 워크플로우:
    /// Phase 1: 전역 서비스 검증 (ServiceLocator)
    /// Phase 2: 로컬 서비스 등록 (LocalUIPanelManager)
    /// Phase 3: UI 패널 초기화 (씬별 구현 필수)
    /// Phase 4: Coordinator 초기화 (씬별 구현 필수)
    /// Phase 5: 씬 특화 초기화 (선택적)
    /// Phase 6: 초기화 완료 후처리
    ///
    /// 상속 패턴:
    /// - GameInitializer (PrototypeTestScene) - 게임 서비스 + 최소 UI
    /// - DeckSceneInitializer (Second.unity) - 덱 빌더 UI
    /// - TitleSceneInitializer (TitleTestScene) - 타이틀 UI
    /// </summary>
    public abstract class SceneInitializer : MonoBehaviour
    {
        [Header("초기화 설정")]
        [SerializeField] protected bool autoInitializeOnStart = true;
        [SerializeField] protected bool logInitializationSteps = true;

        [Header("공통 참조")]
        [SerializeField] protected LocalUIPanelManager panelManager;

        // 초기화 상태
        protected bool isInitialized = false;

        #region Unity Lifecycle

        protected virtual void Start()
        {
            if (autoInitializeOnStart)
            {
                InitializeSceneWorkflow();
            }
        }

        #endregion

        #region Template Method Pattern

        /// <summary>
        /// Template Method - 초기화 워크플로우 정의
        /// 전체 초기화 순서를 제어 (변경 불가)
        /// </summary>
        private void InitializeSceneWorkflow()
        {
            if (isInitialized)
            {
                LogWarning("Scene already initialized - skipping");
                return;
            }

            Log("=== Scene Initialization Start ===");

            try
            {
                // Phase 1: 씬 독립적인 공통 서비스 확인
                ValidateGlobalServices();

                // Phase 2: 씬별 로컬 서비스 등록
                RegisterLocalServices();

                // Phase 3: 씬별 UI 패널 초기화 (추상 메서드)
                InitializeUIPanels();

                // Phase 4: 씬별 Coordinator 초기화 (추상 메서드)
                InitializeCoordinators();

                // Phase 5: 씬별 추가 초기화 (가상 메서드 - 선택적)
                InitializeSceneSpecifics();

                // Phase 6: 초기화 완료 후처리
                FinalizeInitialization();

                isInitialized = true;
                Log("=== Scene Initialization Complete ===");
            }
            catch (System.Exception e)
            {
                LogError($"Scene initialization failed: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }

        #endregion

        #region Common Methods (모든 씬 공통)

        /// <summary>
        /// Phase 1: 전역 서비스 검증 (ServiceLocator 등록 확인)
        /// </summary>
        private void ValidateGlobalServices()
        {
            Log("[Phase 1] Validating global services...");

            // ServiceLocator 초기화 확인
            if (!ServiceLocator.IsInitialized)
            {
                LogWarning("⚠️ ServiceLocator not initialized - may be running scene standalone");
            }

            // Bootstrap에서 등록한 전역 서비스 확인 (선택적)
            if (ServiceLocator.IsRegistered<ISceneTransitionController>())
            {
                Log("✅ Global services detected (Bootstrap initialized)");
            }
            else
            {
                LogWarning("⚠️ ISceneTransitionController not found - global services may not be available");
            }

            Log("✅ Global services validated");
        }

        /// <summary>
        /// Phase 2: 로컬 서비스 등록 (LocalUIPanelManager 등)
        /// </summary>
        private void RegisterLocalServices()
        {
            Log("[Phase 2] Registering local services...");

            // LocalUIPanelManager는 Awake에서 자체 등록하므로 확인만
            if (panelManager == null)
            {
                panelManager = ServiceLocator.Get<LocalUIPanelManager>();
            }

            if (panelManager == null)
            {
                panelManager = FindObjectOfType<LocalUIPanelManager>();
                if (panelManager == null)
                {
                    LogError("❌ LocalUIPanelManager not found in scene!");
                    return;
                }
            }

            Log("✅ Local services registered");
        }

        /// <summary>
        /// Phase 6: 초기화 완료 후처리
        /// </summary>
        private void FinalizeInitialization()
        {
            Log("[Phase 6] Finalizing initialization...");

            // 초기화 완료 이벤트 발생 등
            OnInitializationComplete();

            Log("✅ Finalization complete");
        }

        /// <summary>
        /// 초기화 완료 시 호출 (가상 메서드 - 오버라이드 가능)
        /// </summary>
        protected virtual void OnInitializationComplete()
        {
            // 기본 동작 없음 - 필요 시 씬별로 구현
        }

        #endregion

        #region Abstract Methods (씬별 필수 구현)

        /// <summary>
        /// Phase 3: UI 패널 초기화 (씬별 구현 필수)
        /// 예: InventoryPanel, DeckBuilderPanel 등 초기화
        /// </summary>
        protected abstract void InitializeUIPanels();

        /// <summary>
        /// Phase 4: Coordinator 초기화 (씬별 구현 필수)
        /// 예: DeckInventoryCoordinator, GameUICoordinator 등 초기화
        /// </summary>
        protected abstract void InitializeCoordinators();

        #endregion

        #region Virtual Methods (씬별 선택적 구현)

        /// <summary>
        /// Phase 5: 씬별 특화 초기화 (선택적 구현)
        /// 예: 전투 씬의 적 데이터 로드, 덱 씬의 필터 설정 등
        /// </summary>
        protected virtual void InitializeSceneSpecifics()
        {
            // 기본 구현 없음 - 필요한 씬에서만 오버라이드
        }

        #endregion

        #region Logging Utilities

        protected void Log(string message)
        {
            if (logInitializationSteps)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 수동 초기화 (autoInitializeOnStart = false일 때 사용)
        /// </summary>
        public void ManualInitialize()
        {
            if (!isInitialized)
            {
                InitializeSceneWorkflow();
            }
        }

        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        public bool IsInitialized => isInitialized;

        #endregion
    }
}
