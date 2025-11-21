using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using Game.Card.Effects;

namespace Game.Services
{
    /// <summary>
    /// 소환 및 주문 사용 위치의 유효성을 검증하는 서비스
    /// 비용, 위치, 페이즈 등 모든 검증 규칙을 담당
    /// GlobalStateManager 통합 - VFX 재생 중 카드 사용 차단
    /// </summary>
    public class SpawnValidator : MonoBehaviour, ISpawnValidator
    {
        [Header("검증 설정")]
        [SerializeField] private bool enableLogging = true;

        // ServiceLocator를 통해 주입받을 의존성들
        private IGridController gridController;
        private ITurnService turnService;
        private IResourceManager resourceManager;

        // GlobalStateManager 참조 (VFX 재생 중 카드 사용 차단용)
        private IGlobalStateManager _globalStateManager;
        private bool _isGameFlowLocked = false;

        // 초기화 상태
        private bool isInitialized = false;

        /// <summary>검증자가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            // CardServiceManager에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        private void OnDestroy()
        {
            // GlobalStateManager 이벤트 구독 해제 (메모리 누수 방지)
            if (_globalStateManager != null)
            {
                _globalStateManager.OnBusyStateChanged -= HandleBusyStateChanged;
                Log("Unsubscribed from GlobalStateManager events");
            }
        }

        #endregion

        #region CardServiceManager 호출 메서드

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드
        /// Unit의 Init() 패턴을 따라 외부에서 의존성을 주입받음
        /// </summary>
        /// <param name="iGridController">그리드 컨트롤러</param>
        /// <param name="iTurnService">턴 서비스</param>
        /// <param name="iResourceManager">자원 매니저</param>
        public void Init(IGridController iGridController, ITurnService iTurnService, IResourceManager iResourceManager)
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[SpawnValidator] {gameObject.name} already initialized");
                return;
            }

            Log("✔️ Initializing SpawnValidator...");

            // 외부에서 주입받은 의존성 설정
            InjectDependencies(iGridController, iTurnService, iResourceManager);

            // GlobalStateManager 연동
            SetupGlobalStateManager();

            isInitialized = true;
            Log("✅ SpawnValidator initialization completed");
        }

        /// <summary>
        /// GlobalStateManager 연동 및 이벤트 구독
        /// </summary>
        private void SetupGlobalStateManager()
        {
            _globalStateManager = ServiceLocator.Get<IGlobalStateManager>();

            if (_globalStateManager != null)
            {
                // GameFlowLock 상태 변경 이벤트 구독
                _globalStateManager.OnBusyStateChanged += HandleBusyStateChanged;
                Log("✅ GlobalStateManager event subscription completed");
            }
            else
            {
                LogError("❌ GlobalStateManager not found - Card blocking during VFX disabled");
            }
        }

        /// <summary>
        /// GlobalStateManager 상태 변경 이벤트 핸들러
        /// </summary>
        private void HandleBusyStateChanged(BusyType type, bool isBusy)
        {
            if (type == BusyType.GameFlowLock)
            {
                _isGameFlowLocked = isBusy;
                Log($"GameFlowLock state changed: {isBusy} - Card usage {(isBusy ? "blocked" : "allowed")}");
            }
        }

        /// <summary>
        /// 외부에서 주입받은 서비스 의존성 설정
        /// </summary>
        private void InjectDependencies(IGridController iGridController, ITurnService iTurnService, IResourceManager iResourceManager)
        {
            gridController = iGridController;
            if (gridController != null)
                Log("✅ GridController dependency injected successfully");
            else
                LogError("❌ GridController is null");

            turnService = iTurnService;
            if (turnService != null)
                Log("✅ TurnService dependency injected successfully");
            else
                LogError("❌ TurnService is null");

            resourceManager = iResourceManager;
            if (resourceManager != null)
                Log("✅ ResourceManager dependency injected successfully");
            else
                LogError("❌ ResourceManager is null");
        }

        #endregion

        #region 소환 검증 (Phase 2에서 구현)

        /// <summary>
        /// 범용 카드 사용 가능 여부 검증 (유닛/주문 통합)
        /// ValidateSpawnCost, ValidatePhaseForSpawn, ValidateTargetRange, ValidatePlacementTarget 활용
        /// </summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="isPlayerUnit">플레이어 카드인지 여부</param>
        /// <returns>사용 가능 여부</returns>
        public bool CanUseCard(CardData cardData, Vector2Int targetPosition, bool isPlayerUnit)
        {
            if (!isInitialized)
            {
                LogError("SpawnValidator not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot validate card use for null CardData");
                return false;
            }

            // 🔴 GlobalStateManager: GameFlowLock 상태 확인 (VFX 재생 중 차단)
            if (_isGameFlowLocked)
            {
                Log($"❌ Cannot use card - GameFlowLock is active (VFX or animation playing)");
                return false;
            }

            Log($"Validating card use: {cardData.CardName} at {targetPosition} (Player: {isPlayerUnit})");

            // 1. 소환 비용 검증
            bool hasEnoughResources = ValidateSpawnCost(cardData, isPlayerUnit);
            if (!hasEnoughResources)
            {
                Log($"Cost validation failed for {cardData.CardName}");
                return false;
            }

            // 2. 페이즈 검증
            bool isValidPhase = ValidatePhaseForSpawn(isPlayerUnit);
            if (!isValidPhase)
            {
                Log($"Phase validation failed for {cardData.CardName}");
                return false;
            }

            // 3. 그리드 위치 유효성 검증
            if (gridController != null && !gridController.IsValidPosition(targetPosition))
            {
                Log($"Invalid target position: {targetPosition}");
                return false;
            }

            // 4. 배치 대상 검증 (카드의 Target 타입에 따른 검증)
            bool isValidPlacement = ValidatePlacementTarget(cardData, targetPosition, isPlayerUnit);
            if (!isValidPlacement)
            {
                Log($"Placement target validation failed for {cardData.CardName}");
                return false;
            }

            // 5. 목표 거리 검증 (TargetRange)
            bool isValidRange = ValidateTargetRange(cardData, targetPosition, isPlayerUnit);
            if (!isValidRange)
            {
                Log($"Target range validation failed for {cardData.CardName}");
                return false;
            }

            // 6. EffectDefinition 기반 타겟 필터 검증
            //    AreaShape + TargetFilter(Team/Stat/And/Or)를 모두 적용했을 때
            //    실제로 영향을 줄 수 있는 타겟이 하나라도 있는지 확인
            if (cardData.IsEffectBasedCard)
            {
                bool hasValidEffectTarget = HasAnyValidEffectTarget(cardData, targetPosition, isPlayerUnit);
                if (!hasValidEffectTarget)
                {
                    Log($"Effect target validation failed for {cardData.CardName} at {targetPosition}");
                    return false;
                }
            }

            Log($"Card validation passed for {cardData.CardName} at {targetPosition}");
            return true;
        }

        /// <summary>
        /// EffectDefinition.TargetFilter (TeamFilter, StatThreshold, And/Or 포함)를
        /// 모두 적용했을 때 실제로 영향을 줄 수 있는 타겟 타일이 하나라도 있는지 검사합니다.
        /// </summary>
        private bool HasAnyValidEffectTarget(CardData cardData, Vector2Int targetPosition, bool isPlayerUnit)
        {
            if (cardData == null || !cardData.IsEffectBasedCard)
                return true;

            if (gridController == null)
            {
                LogError("❌ GridController not available for effect target validation");
                return false;
            }

            // 카드 사용자 팀 설정 (TeamFilterDefinition이 참조하는 값)
            var casterTeam = isPlayerUnit ? TeamType.Player : TeamType.Enemy;

            // EffectTargetingHelper가 필요로 하는 최소 정보만 담은 GameContext 생성
            var context = new GameContext(
                unitService: null,
                gridController: gridController,
                cardSpawnService: null,
                spawnValidator: this,
                casterTeam: casterTeam,
                originPosition: targetPosition
            );

            bool hasTileBasedEffects = false;
            bool hasAnyTile = false;

            foreach (var def in cardData.EffectDefinitions)
            {
                if (def == null)
                    continue;

                // Global 효과는 타일 기반 타겟 검증 대상에서 제외
                if (def.TargetScope == EffectTargetScope.Global)
                    continue;

                hasTileBasedEffects = true;

                var tiles = EffectTargetingHelper.GetTargetTiles(targetPosition, def, context);
                if (tiles.Count > 0)
                {
                    hasAnyTile = true;
                    break;
                }
            }

            // 타일 기반 효과가 없다면 기존 로직만으로도 충분하다고 판단
            if (!hasTileBasedEffects)
                return true;

            return hasAnyTile;
        }

        /// <summary>
        /// Phase 2.5 + Fix: 카드 배치 대상 유효성 검증 (카드 사용자 관점 고려)
        /// isPlayerUnit을 고려하여 "적/아군"을 상대적으로 판단합니다.
        /// </summary>
        /// <param name="cardData">카드 데이터</param>
        /// <param name="targetPosition">타겟 위치</param>
        /// <param name="isPlayerUnit">플레이어가 사용하는 카드인지 여부</param>
        private bool ValidatePlacementTarget(CardData cardData, Vector2Int targetPosition, bool isPlayerUnit)
        {
            // 카드 사용자 기준 팀 계산
            var userTeam = isPlayerUnit ? TeamType.Player : TeamType.Enemy;

            // 배치 대상 타입에 따른 검증
            switch (cardData.Target)
            {
                case CardData.TargetType.None:
                    return true; // 타일이 없는 곳에서도 배치 가능

                case CardData.TargetType.Ground:
                    // 타일이 있는 곳 어디든 배치 가능 (빈 타일에만)
                    // Fix: 괄호 추가로 연산자 우선순위 명확화 (occupancy 체크가 반드시 실행되도록)
                    return (gridController?.IsValidPosition(targetPosition) ?? true) &&
                           !(gridController?.IsPositionOccupied(targetPosition) ?? false);

                case CardData.TargetType.Enemy:
                    // 카드 사용자 관점에서 적군 유닛이 있는 위치에만 배치 가능
                    return gridController?.HasUnitWithRelation(
                        targetPosition,
                        userTeam,
                        TeamRelation.Enemy
                    ) ?? true;

                case CardData.TargetType.Ally:
                    // 카드 사용자 관점에서 아군 유닛이 있는 위치에만 배치 가능
                    return gridController?.HasUnitWithRelation(
                        targetPosition,
                        userTeam,
                        TeamRelation.Ally
                    ) ?? true;

                case CardData.TargetType.Any:
                    // 아군/적군 상관없이 유닛이 있는 위치에 배치 가능
                    return gridController?.HasUnit(targetPosition) ?? true;

                default:
                    return true; // 기타 경우 기본적으로 허용
            }
        }

        #endregion

        #region 내부 검증 메서드들 (Phase 2에서 구현)

        /// <summary>
        /// Phase 3.15: TargetRange 배치 거리 제한 검증 (GridController 연동 버전)
        /// GridController의 새로운 TargetRange 검증 메서드를 사용하여 일관성 있는 검증 수행
        /// </summary>
        /// <param name="cardData">카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="isPlayerUnit">플레이어 유닛인지 여부</param>
        /// <returns>범위 내인지 여부</returns>
        private bool ValidateTargetRange(CardData cardData, Vector2Int targetPosition, bool isPlayerUnit)
        {
            // TargetRange가 -1이면 거리 제한 없음
            if (cardData.TargetRange < 0)
            {
                Log($"✅ No range restriction for {cardData.CardName}");
                return true;
            }

            if (gridController == null)
            {
                LogError("❌ GridController not available for range validation");
                return false;
            }

            // Phase 3.15: GridController의 통합 검증 메서드 사용
            bool isInRange = gridController.ValidateCardTargetRange(cardData, targetPosition, isPlayerUnit);

            if (isInRange)
            {
                Log($"✅ Target range validation passed for {cardData.CardName} via GridController");
            }
            else
            {
                Log($"❌ Target out of range for {cardData.CardName} via GridController");
            }

            return isInRange;
        }

        /// <summary>
        /// 현재 페이즈에서 소환이 가능한지 검증
        /// </summary>
        private bool ValidatePhaseForSpawn(bool isPlayerUnit)
        {
            if (turnService == null)
            {
                LogError("❌ TurnService not available for phase validation");
                return false;
            }

            // 현재 페이즈 확인
            var currentPhase = turnService.CurrentPhase;
            
            // 플레이어 유닛은 AllySummon 페이즈에서만 소환 가능
            if (isPlayerUnit && currentPhase != TurnPhase.AllySummon)
            {
                Log($"❌ Player unit spawn denied - Current phase: {currentPhase}, Required: {TurnPhase.AllySummon}");
                return false;
            }

            // 적군 유닛은 EnemySummon 페이즈에서만 소환 가능
            if (!isPlayerUnit && currentPhase != TurnPhase.EnemySummon)
            {
                Log($"❌ Enemy unit spawn denied - Current phase: {currentPhase}, Required: {TurnPhase.EnemySummon}");
                return false;
            }

            Log($"✅ Phase validation passed - {(isPlayerUnit ? "Player" : "Enemy")} can spawn in {currentPhase}");
            return true;
        }

        /// <summary>
        /// 소환 비용이 충분한지 검증
        /// </summary>
        private bool ValidateSpawnCost(CardData cardData, bool isPlayerUnit)
        {
            if (resourceManager == null)
            {
                LogError("❌ ResourceManager not available for cost validation");
                return false;
            }

            int manaCost = cardData.ManaCost;

            // 팀에 따라 자원 검증
            bool canAfford = resourceManager.CanAfford(isPlayerUnit, manaCost);

            if (!canAfford)
            {
                string teamName = isPlayerUnit ? "Player" : "Enemy";
                if (isPlayerUnit)
                {
                    Log($"❌ {teamName} insufficient resources for {cardData.CardName} - Need: {manaCost}M, Have: {resourceManager.PlayerMana}M");
                }
                else
                {
                    Log($"❌ {teamName} insufficient resources for {cardData.CardName} - Need: {manaCost}M, Have: {resourceManager.EnemyMana}M");
                }
                return false;
            }

            Log($"✅ Cost validation passed for {cardData.CardName} - Required: {manaCost}M");
            return true;
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[SpawnValidator] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpawnValidator] {message}");
        }

        #endregion
    }
}
