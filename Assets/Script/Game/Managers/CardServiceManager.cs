using UnityEngine;
using System.Linq;
using Game.Core;
using Game.Interfaces;
using Game.AI;
using Game.Managers;
using Game.SaveSystem;
using Game.Data;

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
        [SerializeField] private EnemyCardHandView enemyCardHandView;

        [Header("AI 설정")]
        [SerializeField] private EnemyAIController enemyAIController;

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
        /// <param name="stageData">스테이지 데이터 (스테이지 설정 구성에 사용)</param>
        public void InitializeAndRegisterServices(StageDataSO stageData = null)
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

                // 6. 스테이지 설정 구성 (StageData가 제공된 경우)
                if (stageData != null)
                {
                    ConfigureStageInternal(stageData);
                }
                else
                {
                    Log("⚠️ No StageData provided during initialization - stage configuration skipped");
                }

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

            if (enemyCardHandView == null)
            {
                enemyCardHandView = gameObject.AddComponent<EnemyCardHandView>();
                Log("👹 Created EnemyCardHandView component");
            }

            // EnemyAIController 생성 또는 찾기
            if (enemyAIController == null)
            {
                enemyAIController = FindObjectOfType<EnemyAIController>();
                if (enemyAIController == null)
                {
                    var aiObject = new GameObject("EnemyAIController");
                    enemyAIController = aiObject.AddComponent<EnemyAIController>();
                    Log("🤖 Created EnemyAIController component");
                }
                else
                {
                    Log("🤖 Found existing EnemyAIController component");
                }
            }
        }

        /// <summary>
        /// 카드 서비스들 초기화 (외부 의존성 주입 방식)
        /// </summary>
        private void InitializeCardServices()
        {
            // 매니저들에서 하위 서비스 가져오기
            var gridManager = ServiceLocator.Get<IGridManager>();
            var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
            var gridController = gridManager?.GetGridController();
            var gridState = gridManager?.GetGridState();
            var turnService = gameServiceManager?.GetTurnService();
            var unitService = gameServiceManager?.GetUnitService();

            // ResourceManager는 독립적인 서비스로 ServiceLocator에서 직접 가져오기
            var resourceManager = ServiceLocator.Get<IResourceManager>();

            // SpawnValidator 먼저 초기화 (CardSpawnService가 이를 사용하므로)
            if (spawnValidator != null)
            {
                spawnValidator.Init(gridController, turnService, resourceManager);
                Log("💉 SpawnValidator dependencies injected via Init()");
            }

            // CardSpawnService 초기화 - SpawnValidator 포함하여 의존성 주입
            if (cardSpawnService != null)
            {
                cardSpawnService.Init(unitService, gridController, gridState, spawnValidator, resourceManager);
                Log("💉 CardSpawnService dependencies injected via Init()");
            }

            // CardHandManager 초기화
            if (cardHandManager != null)
            {
                cardHandManager.Init(turnService);
                Log("💉 CardHandManager dependencies injected via Init()");

                // Phase 4: PlayerData에서 덱 로드
                LoadPlayerDeck();
            }

            // EnemyAIController 초기화 (v2.0 - 확장된 의존성)
            if (enemyAIController != null)
            {
                enemyAIController.Initialize(
                    resourceManager,
                    cardSpawnService,
                    gridController,
                    spawnValidator,
                    gridState,      // v2.0: 추가 - 그리드 상태 조회
                    unitService     // v2.0: 추가 - 유닛 정보 조회
                );
                Log("💉 EnemyAIController v2.0 dependencies injected via Initialize()");
            }

            // EnemyCardHandView 초기화
            if (enemyCardHandView != null && enemyAIController != null)
            {
                enemyCardHandView.Init(enemyAIController);
                Log("💉 EnemyCardHandView dependencies injected via Init()");

                // 🔔 적군 AI 이벤트 구독
                enemyAIController.OnCardUsed += HandleEnemyCardUsed;
                enemyAIController.OnCardDrawn += HandleEnemyCardDrawn;
                Log("✅ Subscribed to EnemyAIController card events");
            }
        }

        /// <summary>
        /// PlayerData에서 lastUsedDeckName을 가져와 덱 로드
        /// Phase 4: 덱 기반 카드 드로우 시스템
        /// </summary>
        private void LoadPlayerDeck()
        {
            Log("📚 Attempting to load player deck from PlayerData...");

            try
            {
                // PlayerDataManager에서 플레이어 데이터 가져오기
                var playerDataManager = ServiceLocator.Get<IPlayerDataManager>();
                if (playerDataManager == null)
                {
                    LogError("❌ PlayerDataManager not found in ServiceLocator");
                    return;
                }

                var playerData = playerDataManager.GetCurrentPlayerData();
                if (playerData == null)
                {
                    LogError("❌ Current player data is null");
                    return;
                }

                string lastDeckName = playerData.lastUsedDeckName;

                if (string.IsNullOrEmpty(lastDeckName))
                {
                    Log("⚠️ No lastUsedDeckName found - CardHandManager will use fallback (availableCards)");
                    return;
                }

                Log($"📋 Found lastUsedDeckName: '{lastDeckName}'");

                // SaveDataAdapter를 통해 덱 로드
                var saveAdapter = ServiceLocator.Get<ISaveDataAdapter>();
                if (saveAdapter == null)
                {
                    LogError("❌ SaveDataAdapter not found in ServiceLocator");
                    return;
                }

                var deckData = saveAdapter.LoadDeck(lastDeckName);

                if (deckData == null || deckData.Count == 0)
                {
                    LogError($"❌ Deck '{lastDeckName}' not found or empty - using fallback");
                    return;
                }

                // 총 카드 개수 계산
                int totalCards = deckData.Sum(entry => entry.Value);
                Log($"✅ Deck '{lastDeckName}' loaded: {deckData.Count} unique cards, {totalCards} total cards");

                // CardHandManager에 덱 설정
                cardHandManager.LoadDeck(deckData);
                Log($"🎉 Deck successfully loaded into CardHandManager");
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Failed to load player deck: {ex.Message}");
                Log("⚠️ CardHandManager will use fallback (availableCards)");
            }
        }

        /// <summary>
        /// ServiceLocator에 CardServiceManager만 등록 (하위 서비스들은 등록하지 않음)
        /// </summary>
        private void RegisterServicesWithLocator()
        {
            // CardServiceManager 자신만 등록
            ServiceLocator.Register<ICardServiceManager>(this);
            Log("📋 ICardServiceManager registered");

            // 하위 서비스들은 ServiceLocator에 등록하지 않음
            // 대신 Get 메서드를 통해 접근하도록 함
            Log("📋 Child services not registered - access through Get methods");
        }

        /// <summary>
        /// 다른 게임 서비스들의 이벤트에 연결
        /// </summary>
        private void ConnectToGameServiceEvents()
        {
            // TurnService의 페이즈 변경 이벤트 구독
            var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
            var turnService = gameServiceManager?.GetTurnService();
          
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

            if (enemyAIController == null)
            {
                LogError("❌ EnemyAIController is null");
                allHealthy = false;
            }

            if (enemyCardHandView == null)
            {
                LogError("❌ EnemyCardHandView is null");
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

            // 3. 적 AI 카드 드로우
            if (enemyAIController != null)
            {
                enemyAIController.DrawCard(1);
                Log("🃏 Enemy AI drew a card");
                // UI 갱신은 OnCardDrawn 이벤트로 자동 처리됨
            }

            // 4. 기타 턴 시작 시 초기화 작업
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

            // AI 소환 로직 실행
            if (enemyAIController != null)
            {
                enemyAIController.ExecuteSummonPhase();
                // UI 갱신은 OnCardUsed 이벤트로 자동 처리됨
            }
            else
            {
                LogError("❌ EnemyAIController not found!");
            }

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

        #region 적군 카드 이벤트 핸들러

        /// <summary>
        /// 적군이 카드를 사용했을 때 호출되는 핸들러
        /// </summary>
        private void HandleEnemyCardUsed()
        {
            if (enemyCardHandView != null)
            {
                enemyCardHandView.RefreshEnemyHand();
                Log("👹 Enemy card used - UI refreshed via event");
            }
        }

        /// <summary>
        /// 적군이 카드를 드로우했을 때 호출되는 핸들러
        /// </summary>
        private void HandleEnemyCardDrawn()
        {
            if (enemyCardHandView != null)
            {
                enemyCardHandView.RefreshEnemyHand();
                Log("👹 Enemy card drawn - UI refreshed via event");
            }
        }

        #endregion

        #region 이벤트 정리

        /// <summary>
        /// 모든 이벤트 연결 해제 (메모리 누수 방지)
        /// </summary>
        private void DisconnectServiceEvents()
        {
            // TurnService 이벤트 구독 해제
            var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
            var turnService = gameServiceManager?.GetTurnService();

            if (turnService != null)
            {
                turnService.OnPhaseChanged -= HandlePhaseChanged;
                Log("🔗 Disconnected from TurnService events");
            }

            // 적군 AI 이벤트 구독 해제
            if (enemyAIController != null)
            {
                enemyAIController.OnCardUsed -= HandleEnemyCardUsed;
                enemyAIController.OnCardDrawn -= HandleEnemyCardDrawn;
                Log("🔗 Unsubscribed from EnemyAIController card events");
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
                   $"- SpawnValidator: {(spawnValidator != null ? "✅" : "❌")}\n" +
                   $"- EnemyAIController: {(enemyAIController != null ? "✅" : "❌")}\n" +
                   $"- EnemyCardHandView: {(enemyCardHandView != null ? "✅" : "❌")}\n";
        }

        /// <summary>
        /// 특정 팀의 카드 풀에서 카드를 드로우합니다.
        /// Player 팀은 CardHandManager, Enemy 팀은 EnemyAIController를 사용합니다.
        /// </summary>
        public void DrawCardsForTeam(TeamType team, int amount)
        {
            if (amount <= 0)
            {
                LogError($"❌ DrawCardsForTeam called with invalid amount: {amount}");
                return;
            }

            switch (team)
            {
                case TeamType.Player:
                    if (cardHandManager == null)
                    {
                        LogError("❌ CardHandManager is null - cannot draw cards for Player");
                        return;
                    }

                    for (int i = 0; i < amount; i++)
                    {
                        cardHandManager.DrawRandomCard();
                    }
                    break;

                case TeamType.Enemy:
                    if (enemyAIController == null)
                    {
                        LogError("❌ EnemyAIController is null - cannot draw cards for Enemy");
                        return;
                    }

                    enemyAIController.DrawCard(amount);
                    break;

                default:
                    Log($"⚠️ DrawCardsForTeam called for unsupported team: {team}");
                    break;
            }
        }

        /// <summary>
        /// GridManager 패턴을 따라 하위 서비스들에 대한 접근 제공
        /// </summary>
        public ICardHandManager GetCardHandManager() => cardHandManager;
        public ICardSpawnService GetCardSpawnService() => cardSpawnService;
        public ISpawnValidator GetSpawnValidator() => spawnValidator;
        public IEnemyCardHandView GetEnemyCardHandView() => enemyCardHandView;

        /// <summary>
        /// 스테이지 설정 구성 (StageDataSO로부터 필요한 데이터 추출)
        /// InitializeAndRegisterServices() 내부에서 호출됩니다
        /// </summary>
        private void ConfigureStageInternal(StageDataSO stageData)
        {
            if (stageData == null)
            {
                LogError("❌ Cannot configure stage: StageDataSO is null!");
                return;
            }

            Log($"🎮 Configuring stage: {stageData.DisplayName} ({stageData.StageId})");

            // EnemyCardPool 설정
            if (stageData.EnemyCardPool != null && enemyAIController != null)
            {
                enemyAIController.SetCardPool(stageData.EnemyCardPool);
                Log($"✅ Enemy card pool configured: {stageData.EnemyCardPool.name}");
            }
            else
            {
                if (stageData.EnemyCardPool == null)
                {
                    LogError("⚠️ StageDataSO has no EnemyCardPool assigned!");
                }
                if (enemyAIController == null)
                {
                    LogError("⚠️ EnemyAIController is not initialized!");
                }
            }

            // 미래 확장: PlayerCardPool, BossCardPool 등 추가 시 여기에 추가
            // if (stageData.PlayerCardPool != null) { ... }

            Log("✅ Stage configuration completed");
        }

        #endregion
    }
}
