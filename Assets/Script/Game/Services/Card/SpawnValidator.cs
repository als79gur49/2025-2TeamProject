using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// 소환 및 주문 사용 위치의 유효성을 검증하는 서비스
    /// 비용, 위치, 페이즈 등 모든 검증 규칙을 담당
    /// </summary>
    public class SpawnValidator : MonoBehaviour, ISpawnValidator
    {
        [Header("검증 설정")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool strictValidation = true;

        // ServiceLocator를 통해 주입받을 의존성들
        private IGridController gridController;
        private ITurnService turnService;
        private IResourceManager resourceManager;

        // 초기화 상태
        private bool isInitialized = false;

        /// <summary>검증자가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            // CardServiceManager에 의해 초기화되므로 여기서는 초기화하지 않음
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

            isInitialized = true;
            Log("✅ SpawnValidator initialization completed");
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
        /// 유닛 소환이 가능한지 검증
        /// </summary>
        /// <param name="cardData">소환할 카드 데이터</param>
        /// <param name="gridPosition">소환 위치</param>
        /// <param name="isPlayerUnit">플레이어 유닛인지 여부</param>
        /// <returns>소환 가능 여부</returns>
        public bool CanSpawnUnit(CardData cardData, Vector2Int gridPosition, bool isPlayerUnit = true)
        {
            if (!isInitialized)
            {
                LogError("SpawnValidator not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot validate spawn for null CardData");
                return false;
            }

            Log($"🔍 Validating unit spawn: {cardData.CardName} at {gridPosition} (Player: {isPlayerUnit})");

            // TODO: Phase 2에서 구현
            // 1. 페이즈 검증 (AllySummon 또는 EnemySummon)
            // 2. 위치 검증 (빈 타일인지, 소환 영역 내인지)
            // 3. 비용 검증 (Mana, ActionPoint 충분한지)
            // 4. 플레이어/적군별 소환 영역 검증 (플레이어는 좌측 1열, 적군은 우측 1열)

            // Phase 2: 완전한 검증 로직 구현
            bool isValidPhase = ValidatePhaseForSpawn(isPlayerUnit);
            bool isValidPosition = ValidateSpawnPosition(gridPosition, isPlayerUnit);
            bool hasEnoughResources = ValidateSpawnCost(cardData, isPlayerUnit);

            bool canSpawn = isValidPhase && isValidPosition && hasEnoughResources;

            Log($"{(canSpawn ? "✅" : "❌")} Spawn validation result: {canSpawn}");
            return canSpawn;
        }

        /// <summary>
        /// 주문 사용이 가능한지 검증
        /// </summary>
        /// <param name="cardData">사용할 주문 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>사용 가능 여부</returns>
        public bool CanUseSpell(CardData cardData, Vector2Int targetPosition)
        {
            if (!isInitialized)
            {
                LogError("SpawnValidator not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot validate spell use for null CardData");
                return false;
            }

            Log($"🔮 Validating spell use: {cardData.CardName} at {targetPosition}");

            // 1. 주문 카드인지 확인
            if (!cardData.IsSpellCard)
            {
                LogError($"Card {cardData.CardName} is not a spell card");
                return false;
            }

            // 2. 현재 플레이어의 턴인지 검증
            if (turnService != null && !turnService.IsPlayerTurn)
            {
                Log("Cannot use spell - not player's turn");
                return false;
            }

            // 3. 그리드 위치 유효성 검증
            if (gridController != null && !gridController.IsValidPosition(targetPosition))
            {
                Log($"Invalid target position: {targetPosition}");
                return false;
            }

            // 4. 자원 비용 검증
            if (resourceManager != null && !resourceManager.CanAfford(true, cardData.ManaCost, 0))
            {
                Log($"Insufficient resources for spell {cardData.CardName}: Mana={cardData.ManaCost}");
                return false;
            }

            // 5. 대상 지정 대상 유효성 검증 (주문별 특별 조건)
            if (!ValidateSpellTarget(cardData, targetPosition))
            {
                Log($"Invalid spell target for {cardData.CardName} at {targetPosition}");
                return false;
            }

            Log($"✅ Spell validation passed for {cardData.CardName} at {targetPosition}");
            return true;
        }

        /// <summary>
        /// 주문별 대상 지정 유효성 검증
        /// </summary>
        private bool ValidateSpellTarget(CardData cardData, Vector2Int targetPosition)
        {
            // 대상 타입에 따른 검증
            switch (cardData.Target)
            {
                case CardData.TargetType.None:
                    return true; // 대상 지정 불필요

                case CardData.TargetType.Ground:
                    // 빈 땅에만 사용 가능
                    return ! gridController?.IsPositionOccupied(targetPosition) ?? true;

                case CardData.TargetType.Enemy:
                    // 적 유닛이 있는 위치에만 사용 가능
                    return gridController?.HasEnemyUnit(targetPosition) ?? true;

                case CardData.TargetType.Ally:
                    // 아군 유닛이 있는 위치에만 사용 가능
                    return gridController?.HasPlayerUnit(targetPosition) ?? true;

                case CardData.TargetType.Any:
                    // 어떤 유닛이든 있으면 가능
                    return gridController?.HasUnit(targetPosition) ?? true;

                default:
                    return true; // 기타 경우 기본적으로 허용
            }
        }

        #endregion

        #region 내부 검증 메서드들 (Phase 2에서 구현)

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
        /// 소환 위치가 유효한지 검증
        /// </summary>
        private bool ValidateSpawnPosition(Vector2Int gridPosition, bool isPlayerUnit)
        {
            if (gridController == null)
            {
                LogError("❌ GridController not available for position validation");
                return false;
            }

            // 1. 위치가 그리드 범위 내인지 확인
            if (!gridController.IsValidPosition(gridPosition))
            {
                Log($"❌ Position {gridPosition} is outside grid bounds");
                return false;
            }

            // 2. 타일이 비어있는지 확인
            if (gridController.IsPositionOccupied(gridPosition))
            {
                Log($"❌ Position {gridPosition} is already occupied");
                return false;
            }

            // 3. 타일이 블록되지 않았는지 확인
            if (gridController.IsPositionBlocked(gridPosition))
            {
                Log($"❌ Position {gridPosition} is blocked");
                return false;
            }

            // 4. 소환 영역 검증 (플레이어: 좌측 1열, 적군: 우측 1열)
            var gridSize = gridController.GridSize;
            
            if (isPlayerUnit)
            {
                // 플레이어는 좌측 첫 번째 열(x=0)에만 소환 가능
                if (gridPosition.x != 0)
                {
                    Log($"❌ Player unit can only spawn in leftmost column (x=0), attempted x={gridPosition.x}");
                    return false;
                }
            }
            else
            {
                // 적군은 우측 마지막 열에만 소환 가능
                int rightmostColumn = gridSize.x - 1;
                if (gridPosition.x != rightmostColumn)
                {
                    Log($"❌ Enemy unit can only spawn in rightmost column (x={rightmostColumn}), attempted x={gridPosition.x}");
                    return false;
                }
            }

            Log($"✅ Position validation passed for {(isPlayerUnit ? "Player" : "Enemy")} unit at {gridPosition}");
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
            bool canAfford = resourceManager.CanAfford(isPlayerUnit, manaCost, 0);

            if (!canAfford)
            {
                string teamName = isPlayerUnit ? "Player" : "Enemy";
                if (isPlayerUnit)
                {
                    Log($"❌ {teamName} insufficient resources for {cardData.CardName} - Need: {manaCost}M, Have: {resourceManager.PlayerMana}M/{resourceManager.PlayerActionPoints}A");
                }
                else
                {
                    Log($"❌ {teamName} insufficient resources for {cardData.CardName} - Need: {manaCost}M, Have: {resourceManager.EnemyMana}M/{resourceManager.EnemyActionPoints}A");
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

        #region 공개 API

        /// <summary>
        /// 검증자 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"SpawnValidator Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Strict Validation: {strictValidation}\n" +
                   $"- GridController Available: {(gridController != null ? "✅" : "❌")}\n" +
                   $"- TurnService Available: {(turnService != null ? "✅" : "❌")}\n" +
                   $"- ResourceManager Available: {(resourceManager != null ? "✅" : "❌")}\n";
        }

        #endregion
    }
}