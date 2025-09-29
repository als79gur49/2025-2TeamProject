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

            // Phase 2.11: 개선된 완전한 검증 로직 구현
            bool isValidPhase = ValidatePhaseForSpawn(isPlayerUnit);
            bool isValidPosition = ValidateSpawnPosition(gridPosition, isPlayerUnit);
            bool hasEnoughResources = ValidateSpawnCost(cardData, isPlayerUnit);

            // Phase 2.11: 통합된 타겟 검증 사용
            Vector2Int basePosition = isPlayerUnit ? GetPlayerBasePosition() : GetEnemyBasePosition();
            bool isValidTarget = ValidateTargetWithCardData(cardData, basePosition, gridPosition, isPlayerUnit);

            bool canSpawn = isValidPhase && isValidPosition && hasEnoughResources && isValidTarget;

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
            if (resourceManager != null && !resourceManager.CanAfford(true, cardData.ManaCost))
            {
                Log($"Insufficient resources for spell {cardData.CardName}: Mana={cardData.ManaCost}");
                return false;
            }

            // 5. Phase 2.11: 통합된 배치 대상 및 거리 검증
            // 플레이어 기준점을 사용하여 검증 (주문은 플레이어가 사용)
            Vector2Int playerBasePosition = GetPlayerBasePosition();
            if (!ValidateTargetWithCardData(cardData, playerBasePosition, targetPosition, true))
            {
                Log($"Target validation failed for spell {cardData.CardName} at {targetPosition}");
                return false;
            }

            Log($"✅ Spell validation passed for {cardData.CardName} at {targetPosition}");
            return true;
        }

        /// <summary>
        /// Phase 2.5: 카드 배치 대상 유효성 검증 (주문 대상 지정에서 배치 대상 검증으로 변경)
        /// </summary>
        private bool ValidatePlacementTarget(CardData cardData, Vector2Int targetPosition)
        {
            // 배치 대상 타입에 따른 검증
            switch (cardData.Target)
            {
                case CardData.TargetType.None:
                    return true; // 타일이 없는 곳에서도 배치 가능

                case CardData.TargetType.Ground:
                    // 타일이 있는 곳 어디든 배치 가능 (빈 타일에만)
                    return gridController?.IsValidPosition(targetPosition) ?? true &&
                           !(gridController?.IsPositionOccupied(targetPosition) ?? false);

                case CardData.TargetType.Enemy:
                    // 적군 유닛이 있는 위치에만 배치 가능 (예: 파이어볼)
                    return gridController?.HasEnemyUnit(targetPosition) ?? true;

                case CardData.TargetType.Ally:
                    // 아군 유닛이 있는 위치에만 배치 가능 (예: 힐링, 버프)
                    return gridController?.HasPlayerUnit(targetPosition) ?? true;

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
        /// Phase 2.11: TargetRange 배치 거리 제한 검증 (개선된 버전)
        /// 플레이어는 가장 왼쪽 유닛 기준, 적군은 가장 오른쪽 유닛 기준으로 거리 계산
        /// CardData의 새로운 거리 계산 메서드를 활용
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

            Vector2Int basePosition;

            if (isPlayerUnit)
            {
                // 플레이어: 가장 왼쪽 유닛 기준
                basePosition = GetPlayerBasePosition();
            }
            else
            {
                // 적군: 가장 오른쪽 유닛 기준
                basePosition = GetEnemyBasePosition();
            }

            // Phase 2.11: CardData의 새로운 맨하탄 거리 계산 메서드 사용
            int distance = CardData.CalculateManhattanDistance(basePosition, targetPosition);

            bool isInRange = distance <= cardData.TargetRange;

            if (isInRange)
            {
                Log($"✅ Target range validation passed for {cardData.CardName} - Distance: {distance}, Max: {cardData.TargetRange}");
            }
            else
            {
                Log($"❌ Target out of range for {cardData.CardName} - Distance: {distance}, Max: {cardData.TargetRange}");
            }

            return isInRange;
        }

        /// <summary>
        /// Phase 2.11: CardData의 기본 유효성 검사를 활용한 통합 검증
        /// CardData.IsValidTargetWithContext를 사용하여 일관성 있는 검증 수행
        /// </summary>
        /// <param name="cardData">카드 데이터</param>
        /// <param name="originPosition">시전자 위치</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>통합 검증 결과</returns>
        private bool ValidateTargetWithCardData(CardData cardData, Vector2Int originPosition, Vector2Int targetPosition, bool isPlayerCard)
        {
            // CardData의 기본 검증 먼저 수행
            if (!cardData.IsValidTargetWithContext(originPosition, targetPosition, isPlayerCard))
            {
                Log($"❌ CardData basic validation failed for {cardData.CardName}");
                return false;
            }

            // 추가적인 SpawnValidator 전용 검증
            if (!ValidatePlacementTarget(cardData, targetPosition))
            {
                Log($"❌ Placement target validation failed for {cardData.CardName}");
                return false;
            }

            // TargetRange 검증 (플레이어/적군 기준점 기반)
            if (!ValidateTargetRange(cardData, targetPosition, isPlayerCard))
            {
                Log($"❌ Target range validation failed for {cardData.CardName}");
                return false;
            }

            Log($"✅ All target validations passed for {cardData.CardName}");
            return true;
        }

        /// <summary>
        /// 플레이어 기준점 계산 (가장 왼쪽 끝 기준)
        /// </summary>
        private Vector2Int GetPlayerBasePosition()
        {
            // 플레이어는 가장 왼쪽 끝 (x=0)에서 가장 가까운 유닛 기준
            // 유닛이 없으면 그리드 왼쪽 가운데를 기준점으로 사용
            var gridSize = gridController.GridSize;

            // 가장 왼쪽 열에서 플레이어 유닛 찾기
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(0, y);
                if (gridController.HasPlayerUnit(pos))
                {
                    Log($"Player base position found at leftmost unit: {pos}");
                    return pos;
                }
            }

            // 유닛이 없으면 왼쪽 가운데를 기준점으로 사용
            Vector2Int fallbackPosition = new Vector2Int(0, gridSize.y / 2);
            Log($"No player unit found, using fallback position: {fallbackPosition}");
            return fallbackPosition;
        }

        /// <summary>
        /// 적군 기준점 계산 (가장 오른쪽 끝 기준)
        /// </summary>
        private Vector2Int GetEnemyBasePosition()
        {
            // 적군은 가장 오른쪽 끝에서 가장 가까운 유닛 기준
            // 유닛이 없으면 그리드 오른쪽 가운데를 기준점으로 사용
            var gridSize = gridController.GridSize;
            int rightmostColumn = gridSize.x - 1;

            // 가장 오른쪽 열에서 적군 유닛 찾기
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(rightmostColumn, y);
                if (gridController.HasEnemyUnit(pos))
                {
                    Log($"Enemy base position found at rightmost unit: {pos}");
                    return pos;
                }
            }

            // 유닛이 없으면 오른쪽 가운데를 기준점으로 사용
            Vector2Int fallbackPosition = new Vector2Int(rightmostColumn, gridSize.y / 2);
            Log($"No enemy unit found, using fallback position: {fallbackPosition}");
            return fallbackPosition;
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

        #region 공개 API

        /// <summary>
        /// Phase 2.11: CardData의 TargetType과 TargetRange를 활용한 배치 유효성 검증
        /// 외부에서 직접 호출할 수 있는 공개 메서드
        /// </summary>
        /// <param name="cardData">검증할 카드 데이터</param>
        /// <param name="originPosition">시전자/소환자 위치</param>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>배치 가능 여부</returns>
        public bool ValidateCardPlacement(CardData cardData, Vector2Int originPosition, Vector2Int targetPosition, bool isPlayerCard)
        {
            if (!isInitialized)
            {
                LogError("SpawnValidator not initialized for card placement validation");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot validate placement for null CardData");
                return false;
            }

            Log($"🎯 Validating card placement: {cardData.CardName} from {originPosition} to {targetPosition} (Player: {isPlayerCard})");

            return ValidateTargetWithCardData(cardData, originPosition, targetPosition, isPlayerCard);
        }

        /// <summary>
        /// Phase 2.11: 새로운 TargetRange 시스템 테스트를 위한 검증 메서드
        /// </summary>
        /// <param name="cardData">테스트할 카드</param>
        /// <param name="testPositions">테스트할 위치들</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>각 위치별 검증 결과</returns>
        public System.Collections.Generic.Dictionary<Vector2Int, bool> TestTargetRangeValidation(
            CardData cardData,
            System.Collections.Generic.List<Vector2Int> testPositions,
            bool isPlayerCard)
        {
            var results = new System.Collections.Generic.Dictionary<Vector2Int, bool>();

            if (!isInitialized || cardData == null)
            {
                return results;
            }

            Vector2Int basePosition = isPlayerCard ? GetPlayerBasePosition() : GetEnemyBasePosition();

            Log($"🧪 Testing TargetRange validation for {cardData.CardName}");
            Log($"Base position: {basePosition}, TargetRange: {cardData.TargetRange}");

            foreach (var testPos in testPositions)
            {
                int distance = CardData.CalculateManhattanDistance(basePosition, testPos);
                bool isValid = ValidateTargetWithCardData(cardData, basePosition, testPos, isPlayerCard);

                results[testPos] = isValid;
                Log($"Position {testPos}: Distance={distance}, Valid={isValid}");
            }

            return results;
        }

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