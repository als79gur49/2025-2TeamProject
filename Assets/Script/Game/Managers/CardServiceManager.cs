using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Services
{
    /// <summary>
    /// 카드 시스템 총괄 매니저 - 모든 카드 관련 서비스를 관리하고 초기화
    /// GameServiceManager와 동일한 레벨에서 동작하며, 카드 시스템의 중앙 허브 역할
    /// </summary>
    public class CardServiceManager : MonoBehaviour, ICardServiceManager
    {
        [Header("카드 서비스 컴포넌트")]
        [SerializeField] private CardHandManager cardHandManager;
        [SerializeField] private CardSpawnService cardSpawnService;
        [SerializeField] private SpawnValidator spawnValidator;

        [Header("초기화 설정")]
        [SerializeField] private bool enableEventLogging = true;

        // 초기화 상태 추적
        private bool isInitialized = false;
        private bool areServicesHealthy = false;

        /// <summary>Gets whether the card service manager is fully initialized</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>Gets whether all card services are healthy</summary>
        public bool AreServicesHealthy => areServicesHealthy;

        #region Unity Lifecycle

        private void Awake()
        {
            // GameInitializer에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        private void OnDestroy()
        {
            DisconnectServiceEvents();
        }

        #endregion

        #region GameInitializer 호출 메서드

        /// <summary>
        /// GameInitializer에 의해 호출될 초기화 메서드
        /// 모든 카드 서비스를 초기화하고 ServiceLocator에 등록
        /// </summary>
        public void InitializeAndRegisterServices()
        {
            Log("🃏 Starting card services initialization...");

            try
            {
                // 1. 하위 서비스들 생성 및 초기화
                CreateCardServiceComponents();
                Log("✅ Card service components created");

                // 2. 하위 서비스들 초기화 (ServiceLocator 의존성 주입)
                InitializeCardServices();
                Log("✅ Card services initialized");

                // 3. ServiceLocator에 자신과 하위 서비스들 등록
                RegisterServicesWithLocator();
                Log("✅ Card services registered with ServiceLocator");

                // 4. 다른 서비스의 이벤트 구독 (TurnService 등)
                ConnectToGameServiceEvents();
                Log("✅ Connected to game service events");

                // 5. 서비스 상태 검증
                ValidateServiceHealth();
                Log("✅ Card service health validated");

                isInitialized = true;
                Log("🎉 Card services initialization completed successfully!");
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Card services initialization failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 초기화 파이프라인

        /// <summary>
        /// 필요한 카드 서비스 컴포넌트들을 생성
        /// </summary>
        private void CreateCardServiceComponents()
        {
            if (cardHandManager == null)
            {
                cardHandManager = gameObject.AddComponent<CardHandManager>();
                Log("🖐️ Created CardHandManager component");
            }

            if (cardSpawnService == null)
            {
                cardSpawnService = gameObject.AddComponent<CardSpawnService>();
                Log("⭐ Created CardSpawnService component");
            }

            if (spawnValidator == null)
            {
                spawnValidator = gameObject.AddComponent<SpawnValidator>();
                Log("✔️ Created SpawnValidator component");
            }
        }

        /// <summary>
        /// 카드 서비스들 초기화 (ServiceLocator 의존성 해결)
        /// </summary>
        private void InitializeCardServices()
        {
            // CardSpawnService 초기화 - UnitService 의존성 주입
            if (cardSpawnService != null)
            {
                cardSpawnService.Initialize();
                Log("💉 CardSpawnService dependencies injected");
            }

            // SpawnValidator 초기화
            if (spawnValidator != null)
            {
                spawnValidator.Initialize();
                Log("💉 SpawnValidator dependencies injected");
            }

            // CardHandManager 초기화
            if (cardHandManager != null)
            {
                cardHandManager.Initialize();
                Log("💉 CardHandManager dependencies injected");
            }
        }

        /// <summary>
        /// ServiceLocator에 모든 카드 서비스들 등록
        /// </summary>
        private void RegisterServicesWithLocator()
        {
            // CardServiceManager 자신 등록
            ServiceLocator.Register<ICardServiceManager>(this);
            Log("📋 ICardServiceManager registered");

            // 하위 서비스들 등록
            if (cardHandManager != null)
            {
                ServiceLocator.Register<ICardHandManager>(cardHandManager);
                Log("📋 ICardHandManager registered");
            }

            if (cardSpawnService != null)
            {
                ServiceLocator.Register<ICardSpawnService>(cardSpawnService);
                Log("📋 ICardSpawnService registered");
            }

            if (spawnValidator != null)
            {
                ServiceLocator.Register<ISpawnValidator>(spawnValidator);
                Log("📋 ISpawnValidator registered");
            }
        }

        /// <summary>
        /// 다른 게임 서비스들의 이벤트에 연결
        /// </summary>
        private void ConnectToGameServiceEvents()
        {
            // TurnService의 페이즈 변경 이벤트 구독
            var turnService = ServiceLocator.Get<ITurnService>();
            if (turnService != null)
            {
                // Phase 3에서 이벤트 연결 활성화
                turnService.OnPhaseChanged += HandlePhaseChanged;
                Log("🔗 Connected to TurnService events - Phase changes will be handled");
            }
            else
            {
                LogError("❌ TurnService not found - phase management won't work");
            }
        }

        /// <summary>
        /// 모든 카드 서비스의 상태 검증
        /// </summary>
        private void ValidateServiceHealth()
        {
            bool allHealthy = true;

            if (cardHandManager == null)
            {
                LogError("❌ CardHandManager is null");
                allHealthy = false;
            }

            if (cardSpawnService == null)
            {
                LogError("❌ CardSpawnService is null");
                allHealthy = false;
            }

            if (spawnValidator == null)
            {
                LogError("❌ SpawnValidator is null");
                allHealthy = false;
            }

            areServicesHealthy = allHealthy;

            if (allHealthy)
            {
                Log("💚 All card services are healthy");
            }
            else
            {
                LogError("💔 Some card services are unhealthy");
            }
        }

        #endregion

        #region 이벤트 핸들러 (Phase 3 구현 완료)

        /// <summary>
        /// TurnService의 페이즈 변경 이벤트 핸들러
        /// </summary>
        private void HandlePhaseChanged(TurnPhase newPhase)
        {
            Log($"🔄 Phase changed to: {newPhase}");

            // AllySummon 페이즈일 때만 핸드 매니저 활성화
            if (newPhase == TurnPhase.AllySummon)
            {
                if (cardHandManager != null)
                {
                    cardHandManager.EnablePlayerSummonMode();
                    Log("🟢 Player summon mode enabled for AllySummon phase");
                }
            }
            else
            {
                if (cardHandManager != null)
                {
                    cardHandManager.DisablePlayerSummonMode();
                    Log("🔴 Player summon mode disabled for non-summon phase");
                }
            }

            // 추가 페이즈별 처리 (필요시 확장)
            switch (newPhase)
            {
                case TurnPhase.TurnStart:
                    // 턴 시작 시 카드 드로우 등
                    HandleTurnStartPhase();
                    break;

                case TurnPhase.EnemySummon:
                    // 적군 소환 페이즈
                    HandleEnemySummonPhase();
                    break;

                case TurnPhase.AllySummon:
                    // 아군 소환 페이즈
                    HandleAllySummonPhase();
                    break;

                case TurnPhase.EnemyAction:
                    // 적군 행동 페이즈
                    HandleEnemyActionPhase();
                    break;

                case TurnPhase.AllyAction:
                    // 아군 행동 페이즈
                    HandleAllyActionPhase();
                    break;

                case TurnPhase.TurnEnd:
                    // 턴 종료
                    HandleTurnEndPhase();
                    break;
            }
        }

        /// <summary>
        /// 턴 시작 페이즈 처리 - 매 턴 시작 시 마다 변경이 필요한 것들 이행
        /// </summary>
        private void HandleTurnStartPhase()
        {
            Log("🎯 Turn start phase - 매 턴 사용가능 Cost 1증가, 랜덤 카드 드로우 등");
            
            // 1. 매 턴 사용가능 Cost 1증가
            var resourceManager = ServiceLocator.Get<IResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.IncreaseTurnlyMana();
                Log("💰 Turn mana increased");
            }
            
            // 2. 랜덤 카드 드로우 (플레이어에게만)
            if (cardHandManager != null)
            {
                cardHandManager.DrawRandomCard();
                Log("🃏 Random card drawn for player");
            }
            
            // 3. 기타 턴 시작 시 초기화 작업
            Log("✅ Turn start phase completed");
        }

        /// <summary>
        /// 아군 소환 페이즈 처리 - 아군이 가지고 있는 카드를 배치
        /// </summary>
        private void HandleAllySummonPhase()
        {
            Log("⚔️ Ally summon phase - 아군이 가지고 있는 카드를 배치");
            // 이미 HandlePhaseChanged에서 EnablePlayerSummonMode 처리됨
            Log("✅ Ally summon phase ready - player can now summon units");
        }

        /// <summary>
        /// 아군 행동 페이즈 처리 - 아군이 순차적 행동
        /// </summary>
        private void HandleAllyActionPhase()
        {
            Log("🏃 Ally action phase - 아군이 순차적 행동");
            // UnitService에서 아군 유닛의 순차적 행동을 처리
            // 카드 관련 특별 처리가 필요한 경우 여기서 구현
            Log("✅ Ally action phase ready");
        }

        /// <summary>
        /// 적군 소환 페이즈 처리 - 적이 가지고 있는 카드를 배치
        /// </summary>
        private void HandleEnemySummonPhase()
        {
            Log("👹 Enemy summon phase - 적이 가지고 있는 카드를 배치");
            
            // AI 소환 로직 구현
            // TODO: AI 시스템에서 적군의 카드 소환 처리
            
            Log("✅ Enemy summon phase completed");
        }

        /// <summary>
        /// 적군 행동 페이즈 처리 - 적군이 순차적 행동
        /// </summary>
        private void HandleEnemyActionPhase()
        {
            Log("🏃‍♂️ Enemy action phase - 적군이 순차적 행동");
            // UnitService에서 적군 유닛의 순차적 행동을 처리
            Log("✅ Enemy action phase ready");
        }

        /// <summary>
        /// 턴 종료 페이즈 처리 - 턴이 끝날 때마다 변경 혹은 정보 수정
        /// </summary>
        private void HandleTurnEndPhase()
        {
            Log("🏁 Turn end phase - 턴이 끝날 때마다 변경 혹은 정보 수정");
            
            // 1. 턴 종료 시 카드 효과 정리
            CleanupTurnEffects();
            
            // 2. 다음 턴을 위한 준비 작업
            PrepareForNextTurn();
            
            Log("✅ Turn end phase completed");
        }
        
        /// <summary>
        /// 턴 종료 시 효과 정리
        /// </summary>
        private void CleanupTurnEffects()
        {
            // 임시 카드 효과 제거
            // 버프/디버프 박상 시간 감소
            Log("🧽 Cleaning up temporary card effects");
        }
        
        /// <summary>
        /// 다음 턴을 위한 준비
        /// </summary>
        private void PrepareForNextTurn()
        {
            // 새 턴에 대비한 준비 작업
            Log("🔄 Preparing for next turn cycle");
        }

        #endregion

        #region 이벤트 정리

        /// <summary>
        /// 모든 이벤트 연결 해제 (메모리 누수 방지)
        /// </summary>
        private void DisconnectServiceEvents()
        {
            // TurnService 이벤트 구독 해제
            var turnService = ServiceLocator.Get<ITurnService>();
            if (turnService != null)
            {
                turnService.OnPhaseChanged -= HandlePhaseChanged;
                Log("🔗 Disconnected from TurnService events");
            }

            Log("🔗 All card service events disconnected");
        }

        #endregion

        #region 로깅 시스템

        /// <summary>
        /// 이벤트 로깅 (타임스탬프 포함)
        /// </summary>
        private void Log(string message)
        {
            if (enableEventLogging)
            {
                Debug.Log($"[CardServiceManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        /// <summary>
        /// 에러 로깅
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[CardServiceManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 카드 서비스 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetServiceStatus()
        {
            return $"Card Services Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Healthy: {areServicesHealthy}\n" +
                   $"- CardHandManager: {(cardHandManager != null ? "✅" : "❌")}\n" +
                   $"- CardSpawnService: {(cardSpawnService != null ? "✅" : "❌")}\n" +
                   $"- SpawnValidator: {(spawnValidator != null ? "✅" : "❌")}\n";
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("에디터 도구")]
        [SerializeField] private bool showDebugInfo = true;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(320, 10, 300, 200));
            GUILayout.Box("Card Service Manager Debug");

            if (isInitialized)
            {
                GUILayout.Label("✅ CardServiceManager Initialized");
            }
            else
            {
                GUILayout.Label("❌ CardServiceManager Not Initialized");
            }

            GUILayout.Label($"Services Healthy: {(areServicesHealthy ? "✅" : "❌")}");
            GUILayout.Label($"CardHandManager: {(cardHandManager != null ? "✅" : "❌")}");
            GUILayout.Label($"CardSpawnService: {(cardSpawnService != null ? "✅" : "❌")}");
            GUILayout.Label($"SpawnValidator: {(spawnValidator != null ? "✅" : "❌")}");

            if (GUILayout.Button("Re-Initialize Card Services"))
            {
                InitializeAndRegisterServices();
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}