using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Interfaces;
using Game.Services;
using Game.Card.Effects;
using Game.AI.CardSelection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    /// <summary>
    /// 적군의 카드 사용 AI를 총괄하는 컨트롤러 (v2.4 - Strategy Pattern)
    /// GlobalStateManager의 GameFlowLock 상태를 감지하여, 이전 VFX가 끝나면 다음 카드를 실행합니다.
    /// v2.3: 카드 실행 직전 필드 상태 재검증 및 대안 위치 탐색으로 지능적인 카드 사용 구현
    /// v2.4: 전략 패턴 적용으로 카드 선택 로직을 교체 가능하게 개선 (EnemyCardPoolSO 기반)
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField]
        [Tooltip("적군이 사용할 카드 풀 ScriptableObject")]
        private EnemyCardPoolSO cardPool;

        [SerializeField] private bool enableLogging = true;

        // 카드 가치 판정 임계값 (0 = 양수 가치면 허용)
        private const int MIN_VALUE_THRESHOLD = 0;
        // 한 소환 페이즈에서 재계획 루프 최대 반복 횟수
        private const int MAX_LOOPS_PER_SUMMON_PHASE = 5;
        // 한 소환 페이즈에서 사용할 수 있는 최대 카드 수 (무한 루프 방지용)
        private const int MAX_CARDS_PER_SUMMON_PHASE = 20;

        // 이벤트
        public event System.Action OnCardUsed;   // 카드 사용 시 발생
        public event System.Action OnCardDrawn;  // 카드 드로우 시 발생

        // 내부 상태
        private List<CardData> enemyHand = new List<CardData>();
        private Game.AI.CardSelection.ICardSelectionStrategy selectionStrategy;  // v2.4: 전략 패턴

        // 서비스 참조
        private IResourceManager resourceManager;
        private ICardSpawnService cardSpawnService;
        private IGridController gridController;
        private ISpawnValidator spawnValidator;
        private IGridState gridState;
        private IUnitService unitService;
        private IGlobalStateManager _stateManager;

        private bool isInitialized = false;

        /// <summary>초기화 완료 여부</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>현재 손패 크기 (디버깅용)</summary>
        public int HandSize => enemyHand.Count;

        /// <summary>적군 핸드 카드 목록 (읽기 전용)</summary>
        public IReadOnlyList<CardData> EnemyHand => enemyHand.AsReadOnly();

        /// <summary>
        /// 외부에서 적군 손패에 카드를 추가합니다. (예: ReturnToHand 효과)
        /// </summary>
        public void AddCardToHand(CardData card)
        {
            if (card == null)
            {
                LogError("AddCardToHand called with null card");
                return;
            }

            enemyHand.Add(card);
            Log($"🃏 Enemy card returned to hand: {card.CardName} (Hand size: {enemyHand.Count})");

            // 적 손패 변경을 알리기 위해 드로우 이벤트 재사용
            OnCardDrawn?.Invoke();
        }

        #region 초기화

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드 (v2.4 - Strategy Pattern)
        /// </summary>
        public void Initialize(
            IResourceManager res,
            ICardSpawnService spawn,
            IGridController grid,
            ISpawnValidator validator,
            IGridState state,
            IUnitService unit)
        {
            resourceManager = res;
            cardSpawnService = spawn;
            gridController = grid;
            spawnValidator = validator;
            gridState = state;
            unitService = unit;

            _stateManager = ServiceLocator.Get<IGlobalStateManager>();
            if (_stateManager == null)
            {
                LogError("❌ IGlobalStateManager not found in ServiceLocator! AI cannot function correctly.");
            }

            // v2.4: 카드 풀 검증 및 전략 초기화
            if (cardPool == null)
            {
                LogError("❌ EnemyCardPoolSO is not assigned! AI cannot draw cards.");
                return;
            }

            if (!cardPool.IsValid())
            {
                LogError("❌ EnemyCardPoolSO is invalid! AI cannot draw cards.");
                return;
            }
            
            // v2.4: 전략 패턴 초기화
            selectionStrategy = cardPool.GetStrategy();
            Log($"🎲 Card selection strategy initialized: {selectionStrategy?.StrategyName ?? "None"}");
            Log($"📊 Card pool info:\n{cardPool.GetPoolInfo()}");

            // 카드 풀에서 초기 핸드 드로우
            DrawInitialHand(cardPool.InitialHandSize);

            isInitialized = true;
            Log("🤖 [EnemyAI v2.4] Initialized with strategy pattern system");
        }

        /// <summary>
        /// 런타임에 카드 풀을 설정합니다 (StageDataSO로부터 주입용)
        /// CardServiceManager.ConfigureStage()에서 호출됩니다
        /// </summary>
        public void SetCardPool(EnemyCardPoolSO pool)
        {
            if (pool == null)
            {
                LogError("❌ Cannot set null card pool!");
                return;
            }

            if (!pool.IsValid())
            {
                LogError($"❌ Card pool '{pool.name}' is invalid!");
                return;
            }

            cardPool = pool;
            selectionStrategy = pool.GetStrategy();

            Log($"🔄 Card pool updated: {pool.name}");
            Log($"🎲 Strategy updated: {selectionStrategy?.StrategyName ?? "None"}");
            Log($"📊 Pool info:\n{pool.GetPoolInfo()}");

            // 기존 핸드 초기화 및 새로운 초기 핸드 드로우
            enemyHand.Clear();
            DrawInitialHand(pool.InitialHandSize);
        }

        /// <summary>
        /// 초기 손패 드로우 (v2.4: 전략 패턴 사용)
        /// </summary>
        private void DrawInitialHand(int handSize)
        {
            if (cardPool == null || cardPool.Cards.Count == 0)
            {
                LogError("❌ Card pool is empty! Cannot draw initial hand");
                return;
            }

            if (selectionStrategy == null)
            {
                LogError("❌ Selection strategy is not initialized! Cannot draw initial hand");
                return;
            }

            for (int i = 0; i < handSize; i++)
            {
                DrawCard();
            }

            Log($"📇 Initial hand drawn: {enemyHand.Count} cards");
        }

        #endregion

        #region 카드 드로우

        /// <summary>
        /// 카드 풀에서 카드를 드로우합니다 (v2.4: 전략 패턴 사용)
        /// </summary>
        public void DrawCard(int amount = 1)
        {
            if (cardPool == null || cardPool.Cards.Count == 0)
            {
                LogError("❌ Card pool is empty! Cannot draw card");
                return;
            }

            if (selectionStrategy == null)
            {
                LogError("❌ Selection strategy is not initialized! Cannot draw card");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                // v2.4: 전략을 사용하여 카드 선택
                CardData drawnCard = selectionStrategy.DrawCard(cardPool.Cards);

                if (drawnCard != null)
                {
                    enemyHand.Add(drawnCard);
                    Log($"🃏 Enemy drew: {drawnCard.CardName} (Rarity: {drawnCard.Rarity}, Hand size: {enemyHand.Count})");

                    // 🔔 이벤트 발생
                    OnCardDrawn?.Invoke();
                }
                else
                {
                    LogError("❌ Failed to draw card from pool (strategy returned null)");
                }
            }
        }

        #endregion

        #region 소환 페이즈 실행 (v2.3 - Hybrid Re-validation)

        /// <summary>
        /// EnemySummonPhase가 시작될 때 CardServiceManager에 의해 호출됩니다.
        /// v2.3: VFX 완료를 기다리며, 각 카드 실행 직전 필드 상태를 재검증하여 지능적 실행
        /// </summary>
        public void ExecuteSummonPhase()
        {
            if (!isInitialized || _stateManager == null)
            {
                LogError("[EnemyAI] Not initialized or StateManager is missing!");
                return;
            }

            StartCoroutine(ExecuteSummonPhaseCoroutine());
        }

        /// <summary>
        /// 선택된 카드를 순차적으로 사용하는 코루틴.
        /// 각 카드를 사용하기 전에 GameFlowLock이 해제될 때까지 대기합니다.
        /// </summary>
        private IEnumerator ExecuteSummonPhaseCoroutine()
        {
            if (enemyHand.Count == 0)
            {
                Log("No cards in hand to play");
                yield break;
            }

            int totalSuccessCount = 0;
            int loopCount = 0;

            while (loopCount < MAX_LOOPS_PER_SUMMON_PHASE)
            {
                loopCount++;

                int currentMana = resourceManager != null ? resourceManager.EnemyMana : 0;
                if (currentMana <= 0 || enemyHand.Count == 0)
                {
                    break;
                }

                var selectedCardInfos = PlanCardsForCurrentState(currentMana);
                if (selectedCardInfos == null || selectedCardInfos.Count == 0)
                {
                    Log("No cards selected to play this loop");
                    break;
                }

                Log($"Knapsack selected {selectedCardInfos.Count} cards (Total Value: {selectedCardInfos.Sum(c => c.Value)})");

                int playedThisLoop = 0;
                yield return ExecuteSelectedCardsCoroutine(selectedCardInfos, count => playedThisLoop = count);

                totalSuccessCount += playedThisLoop;

                if (playedThisLoop == 0)
                {
                    break;
                }

                if (totalSuccessCount >= MAX_CARDS_PER_SUMMON_PHASE)
                {
                    break;
                }
            }

            Log($"Summon phase complete: {totalSuccessCount} cards played successfully");
        }

        #endregion

        #region 가치 평가 (v2.0 핵심 로직)

        private List<CardValueInfo> PlanCardsForCurrentState(int currentMana)
        {
            var cardValueInfos = new List<CardValueInfo>();

            foreach (var card in enemyHand)
            {
                if (card == null)
                    continue;

                if (card.ManaCost > currentMana)
                    continue;

                var valueInfo = CalculateBestSituationalValue(card);
                if (valueInfo.Value > MIN_VALUE_THRESHOLD)
                {
                    cardValueInfos.Add(valueInfo);
                    Log($"Card '{card.CardName}': Value={valueInfo.Value}, BestPos={valueInfo.Position}");
                }
            }

            if (cardValueInfos.Count == 0)
            {
                Log("No valid card placements found");
                return new List<CardValueInfo>();
            }

            return KnapsackCardSelector.SelectOptimalCards(cardValueInfos, currentMana);
        }

        private IEnumerator ExecuteSelectedCardsCoroutine(List<CardValueInfo> selectedCardInfos, System.Action<int> onCompleted)
        {
            if (selectedCardInfos == null || selectedCardInfos.Count == 0)
            {
                onCompleted?.Invoke(0);
                yield break;
            }

            int successCount = 0;

            foreach (var info in selectedCardInfos)
            {
                if (info == null || info.Card == null)
                    continue;

                yield return new WaitUntil(() => !_stateManager.IsBusy(BusyType.GameFlowLock));

                if (resourceManager != null && !resourceManager.CanEnemyAfford(info.Card.ManaCost))
                {
                    continue;
                }

                var finalInfo = RevalidateAndMaybeRecalculate(info);
                if (finalInfo == null)
                    continue;

                bool success = cardSpawnService.TryExecuteCard(finalInfo.Card, finalInfo.Position, TeamType.Enemy);
                if (success)
                {
                    enemyHand.Remove(finalInfo.Card);
                    successCount++;
                    Log($"Executed '{finalInfo.Card.CardName}' at {finalInfo.Position}. Waiting for its VFX to complete...");
                    OnCardUsed?.Invoke();
                }
                else
                {
                    LogError($"Failed to execute '{finalInfo.Card.CardName}' at {finalInfo.Position}");
                }
            }

            onCompleted?.Invoke(successCount);
        }

        private CardValueInfo RevalidateAndMaybeRecalculate(CardValueInfo plannedInfo)
        {
            if (plannedInfo == null || plannedInfo.Card == null)
                return null;

            bool isStillValid = spawnValidator.CanUseCard(plannedInfo.Card, plannedInfo.Position, isPlayerUnit: false);
            int currentValue = 0;

            if (isStillValid)
            {
                currentValue = CalculateValueAtPosition(plannedInfo.Card, plannedInfo.Position);
            }

            CardValueInfo finalInfo = plannedInfo;

            if (!isStillValid || currentValue <= MIN_VALUE_THRESHOLD)
            {
                Log($"⚠️ Original plan for '{plannedInfo.Card.CardName}' at {plannedInfo.Position} is no longer optimal " +
                    $"(Valid: {isStillValid}, Value: {currentValue}). Recalculating...");

                var recalculatedInfo = CalculateBestSituationalValue(plannedInfo.Card);

                if (recalculatedInfo.Value > MIN_VALUE_THRESHOLD)
                {
                    finalInfo = recalculatedInfo;
                    Log($"✅ Found better position: {finalInfo.Position} with value {finalInfo.Value}");
                }
                else
                {
                    Log($"❌ No valid alternative found. Skipping '{plannedInfo.Card.CardName}'");
                    return null;
                }
            }

            return finalInfo;
        }

        /// <summary>
        /// 카드의 모든 가능한 위치를 탐색하여 최고 가치와 위치를 계산합니다.
        /// 팀 인덱스 기반 후보 위치 생성기를 사용하고, 필요 시 전체 그리드 스캔으로 폴백합니다.
        /// </summary>
        private CardValueInfo CalculateBestSituationalValue(CardData card)
        {
            int maxValue = 0;
            Vector2Int bestPosition = -Vector2Int.one; // 유효하지 않은 위치로 초기화

            if (gridController == null)
            {
                LogError("⚠️ GridController not available for value calculation");
                return new CardValueInfo(card, 0, bestPosition);
            }

            // 중심 좌표에 따라 결과가 변하지 않는 카드(GlobalAreaShape + TargetType.None + 무제한 거리)는
            // 대표 좌표 하나만 평가하는 빠른 경로를 사용한다.
            if (CanUseCenterIndependentFastPath(card))
            {
                return EvaluateCenterIndependentCard(card);
            }

            // 1차: 카드 메타 정보와 팀 인덱스를 사용하여 후보 위치 생성
            var candidatePositions = GenerateCandidatePositions(card);
            var candidates = candidatePositions?.Distinct().ToList() ?? new List<Vector2Int>();

            Vector2Int gridSize = gridController.GridSize;
            int totalTiles = gridSize.x * gridSize.y;

            // 후보가 없거나, 거의 전체 그리드와 비슷하면 전체 스캔으로 폴백
            bool shouldFallbackToFullScan = candidates.Count == 0 || candidates.Count > totalTiles * 0.8f;

            if (shouldFallbackToFullScan)
            {
                candidates.Clear();
                for (int x = 0; x < gridSize.x; x++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            foreach (var currentPosition in candidates)
            {
                // 1. 위치 유효성 검증
                if (!gridController.IsValidPosition(currentPosition))
                    continue;

                // 2. 배치 가능 위치 검증 (SpawnValidator 포함)
                if (!CanPlaceCardAtPosition(card, currentPosition))
                    continue;

                // 3. 해당 위치에서의 가치 계산
                int currentValue = CalculateValueAtPosition(card, currentPosition);

                // 4. 최고 가치 갱신
                if (currentValue > maxValue)
                {
                    maxValue = currentValue;
                    bestPosition = currentPosition;
                }
            }

            return new CardValueInfo(card, maxValue, bestPosition);
        }

        /// <summary>
        /// 특정 위치에 카드를 배치할 수 있는지 검증합니다.
        /// </summary>
        private bool CanPlaceCardAtPosition(CardData card, Vector2Int position)
        {
            if (card == null || spawnValidator == null)
                return false;

            return spawnValidator.CanUseCard(card, position, isPlayerUnit: false);
        }

        /// <summary>
        /// 카드의 AreaShape가 모두 중심 독립(IsCenterIndependent)인지, 그리고
        /// TargetType/TargetRange가 중심 좌표에 의존하지 않는 안전한 패턴인지 확인합니다.
        /// </summary>
        private bool CanUseCenterIndependentFastPath(CardData card)
        {
            if (card == null)
                return false;

            if (!IsCenterIndependentAreaCard(card))
                return false;

            // TargetType.None: 배치 좌표에 유닛/빈 타일 조건이 없음
            if (card.Target != CardData.TargetType.None)
                return false;

            // TargetRange가 -1인 경우에만 우선 적용 (거리 제한이 걸리면 추가 설계 필요)
            if (card.TargetRange >= 0)
                return false;

            return true;
        }

        /// <summary>
        /// 카드의 모든 AreaShape가 중심 좌표에 무관한지(IsCenterIndependent) 여부를 검사합니다.
        /// 하나라도 중심 의존 Shape가 있으면 false를 반환합니다.
        /// </summary>
        private bool IsCenterIndependentAreaCard(CardData card)
        {
            if (card.EffectDefinitions == null || card.EffectDefinitions.Count == 0)
                return false;

            bool hasAreaShape = false;

            foreach (var def in card.EffectDefinitions)
            {
                if (def == null)
                    continue;

                var shape = def.AreaShape;
                if (shape == null)
                    continue;

                hasAreaShape = true;

                if (!shape.IsCenterIndependent)
                {
                    return false;
                }
            }

            // AreaShape가 하나도 없는 카드는 여기 최적화 대상이 아니다.
            return hasAreaShape;
        }

        /// <summary>
        /// 중심 좌표에 따라 결과가 변하지 않는 카드에 대해,
        /// 대표 좌표 몇 개만 시험하여 가치와 위치를 계산합니다.
        /// </summary>
        private CardValueInfo EvaluateCenterIndependentCard(CardData card)
        {
            Vector2Int invalid = -Vector2Int.one;

            if (gridController == null)
            {
                return new CardValueInfo(card, 0, invalid);
            }

            Vector2Int gridSize = gridController.GridSize;

            // 대표 좌표 후보들: 중앙, Enemy 기준점, (0,0)
            var trialPositions = new List<Vector2Int>
            {
                new Vector2Int(gridSize.x / 2, gridSize.y / 2),
                gridController.GetEnemyBasePosition(),
                new Vector2Int(0, 0)
            };

            foreach (var pos in trialPositions)
            {
                if (!gridController.IsValidPosition(pos))
                    continue;

                if (!CanPlaceCardAtPosition(card, pos))
                    continue;

                int value = CalculateValueAtPosition(card, pos);
                return new CardValueInfo(card, value, pos);
            }

            // 어떤 좌표에서도 사용 불가능하면 0 가치와 invalid 위치 반환
            return new CardValueInfo(card, 0, invalid);
        }

        /// <summary>
        /// 카드 타입과 팀 인덱스를 기반으로, 가치 평가에 사용할 후보 위치들을 생성합니다.
        /// Ground(소환)와 TargetType.None(AoE)을 최적화하고, 나머지는 전체 그리드와 유사한 후보를 반환합니다.
        /// </summary>
        private IEnumerable<Vector2Int> GenerateCandidatePositions(CardData card)
        {
            if (card == null || gridController == null)
            {
                yield break;
            }

            // Global 전역 효과만 있는 카드라면, 중심 위치는 의미 없으므로 임의의 한 위치만 사용
            if (IsPureGlobalEffectCard(card))
            {
                yield return gridController.GetEnemyBasePosition();
                yield break;
            }

            // Ground 타겟 카드: Enemy 기준 TargetRange 내의 빈 타일만 후보로 사용
            if (card.Target == CardData.TargetType.Ground)
            {
                foreach (var pos in GenerateGroundCandidates(card))
                {
                    yield return pos;
                }

                yield break;
            }

            // TargetType.None + Local AoE 카드: 적/아군 유닛 주변만 후보로 사용
            if (card.Target == CardData.TargetType.None && HasLocalAreaEffect(card))
            {
                foreach (var pos in GenerateAoECandidates(card))
                {
                    yield return pos;
                }

                yield break;
            }

            // 그 외의 경우: 전체 그리드 기반 기본 후보 (필요 시 다른 전략으로 확장 가능)
            Vector2Int gridSize = gridController.GridSize;
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        /// <summary>
        /// 카드에 타일 기반 AreaShape가 존재하지만, 모든 타일 기반 효과가 Global 범위인지 여부를 판단합니다.
        /// </summary>
        private bool IsPureGlobalEffectCard(CardData card)
        {
            if (card.EffectDefinitions == null || card.EffectDefinitions.Count == 0)
            {
                return false;
            }

            bool hasTileBasedEffect = false;
            bool hasNonGlobalTileEffect = false;

            foreach (var def in card.EffectDefinitions)
            {
                if (def == null)
                    continue;

                if (def.AreaShape == null)
                    continue;

                hasTileBasedEffect = true;

                if (def.TargetScope != EffectTargetScope.Global)
                {
                    hasNonGlobalTileEffect = true;
                    break;
                }
            }

            // 타일 기반 효과가 있고, 모든 타일 기반 효과가 Global 범위인 카드만 "순수 Global"로 간주
            return hasTileBasedEffect && !hasNonGlobalTileEffect;
        }

        /// <summary>
        /// 카드에 Local 타일 기반 AreaShape 효과가 하나라도 있는지 여부.
        /// </summary>
        private bool HasLocalAreaEffect(CardData card)
        {
            if (card.EffectDefinitions == null)
            {
                return false;
            }

            foreach (var def in card.EffectDefinitions)
            {
                if (def == null)
                    continue;

                if (def.AreaShape == null)
                    continue;

                if (def.TargetScope != EffectTargetScope.Global)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ground(소환/함정) 카드에 대한 후보 위치 생성.
        /// Enemy 기준 TargetRange 내의 빈 타일만 반환하고, 필요 시 전체 그리드로 폴백합니다.
        /// </summary>
        private IEnumerable<Vector2Int> GenerateGroundCandidates(CardData card)
        {
            if (gridController == null)
            {
                yield break;
            }

            // TargetRange가 없는 경우에는 전체 그리드를 후보로 사용
            if (card.TargetRange < 0)
            {
                Vector2Int gridSize = gridController.GridSize;
                for (int x = 0; x < gridSize.x; x++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        yield return new Vector2Int(x, y);
                    }
                }

                yield break;
            }

            // Enemy 기준 TargetRange 내의 위치들 (점유 여부는 includeOccupied 플래그로 제어)
            var positionsInRange = gridController.GetPositionsWithinRange(card.TargetRange, isPlayerBased: false, includeOccupied: false);

            foreach (var pos in positionsInRange)
            {
                yield return pos;
            }
        }

        /// <summary>
        /// TargetType.None + Local AoE 카드에 대한 후보 위치 생성.
        /// 플레이어/적군 유닛 주변의 작은 반경만 후보로 사용하고, TargetRange를 함께 고려합니다.
        /// </summary>
        private IEnumerable<Vector2Int> GenerateAoECandidates(CardData card)
        {
            if (gridController == null)
            {
                yield break;
            }

            Vector2Int gridSize = gridController.GridSize;

            // Enemy 관점에서의 적군(플레이어) 유닛 위치들
            var enemyUnitPositions = gridController.GetUnitPositionsForTeam(TeamType.Player);
            var allyUnitPositions = gridController.GetUnitPositionsForTeam(TeamType.Enemy);

            // AoE는 보통 적군 중심으로 가치가 발생하므로, 우선 순위를 둠
            var interestingPositions = new HashSet<Vector2Int>(enemyUnitPositions);
            foreach (var pos in allyUnitPositions)
            {
                interestingPositions.Add(pos);
            }

            if (interestingPositions.Count == 0)
            {
                // 필드에 유닛이 없으면 별도 최적화가 의미 없으므로 전체 그리드를 후보로 사용
                for (int x = 0; x < gridSize.x; x++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        yield return new Vector2Int(x, y);
                    }
                }

                yield break;
            }

            int aoeRadius = GetMaxAreaShapeRadius(card);
            int searchRadius = Mathf.Max(1, aoeRadius + 1); // 여유를 위해 +1

            var candidateSet = new HashSet<Vector2Int>();

            foreach (var unitPos in interestingPositions)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    for (int dy = -searchRadius; dy <= searchRadius; dy++)
                    {
                        var candidate = new Vector2Int(unitPos.x + dx, unitPos.y + dy);

                        if (!gridController.IsValidPosition(candidate))
                            continue;

                        // TargetRange가 설정되어 있다면 Enemy 기준으로 검증
                        if (card.TargetRange >= 0 && !gridController.ValidateCardTargetRange(card, candidate, isPlayerCard: false))
                            continue;

                        candidateSet.Add(candidate);
                    }
                }
            }

            foreach (var pos in candidateSet)
            {
                yield return pos;
            }
        }

        /// <summary>
        /// 카드의 EffectDefinitions에서 AreaShape 크기를 기반으로 최대 AoE 반경을 추정합니다.
        /// Shape 타입에 따라 Range/TileCount를 사용하며, 알 수 없는 경우에는 TargetRange를 사용합니다.
        /// </summary>
        private int GetMaxAreaShapeRadius(CardData card)
        {
            if (card.EffectDefinitions == null)
            {
                return 1;
            }

            int maxRadius = 1;

            foreach (var def in card.EffectDefinitions)
            {
                if (def == null || def.AreaShape == null)
                    continue;

                switch (def.AreaShape)
                {
                    case Game.Card.Effects.SquareShapeDefinition square:
                        maxRadius = Mathf.Max(maxRadius, square.Range);
                        break;
                    case Game.Card.Effects.RadiusShapeDefinition radius:
                        maxRadius = Mathf.Max(maxRadius, radius.Range);
                        break;
                    case Game.Card.Effects.LineShapeDefinition line:
                        // RangeAroundCenter 모드일 때 Range 사용, FullLine은 그리드 전체이므로 TargetRange로 제한
                        if (line.RangeMode == Game.Card.Effects.LineRangeMode.RangeAroundCenter)
                        {
                            maxRadius = Mathf.Max(maxRadius, line.Range);
                        }
                        else
                        {
                            // FullLine은 길이가 길 수 있으므로, TargetRange나 그리드 크기를 기반으로 제한
                            maxRadius = Mathf.Max(maxRadius, card.TargetRange >= 0 ? card.TargetRange : maxRadius);
                        }
                        break;
                    case Game.Card.Effects.SequentialLineShapeDefinition seq:
                        // tileCount는 최대 타일 개수이므로, 대략적인 반경으로 사용
                        maxRadius = Mathf.Max(maxRadius, seq.TileCount);
                        break;
                    case Game.Card.Effects.GlobalAreaShapeDefinition _:
                        // Global은 반경 개념보다 전체 맵이므로, TargetRange나 기본값 사용
                        maxRadius = Mathf.Max(maxRadius, card.TargetRange >= 0 ? card.TargetRange : maxRadius);
                        break;
                }
            }

            // TargetRange가 더 작은 상한이면, 그 범위 안에서만 중심을 고려하는 것이 자연스럽다.
            if (card.TargetRange >= 0)
            {
                maxRadius = Mathf.Min(maxRadius, card.TargetRange);
            }

            return Mathf.Max(1, maxRadius);
        }

        /// <summary>
        /// 특정 위치에서 카드의 총 가치를 계산합니다. (EffectDefinition 기반)
        /// </summary>
        private int CalculateValueAtPosition(CardData card, Vector2Int position)
        {
            if (card?.EffectDefinitions == null || card.EffectDefinitions.Count == 0)
                return 0;

            int totalValue = 0;

            foreach (var def in card.EffectDefinitions)
            {
                if (def == null) continue;
                
                switch (def)
                {
                    case SummonEffectDefinition summonDef:
                        totalValue += CalculateSummonValue(summonDef);
                        break;
                    case DamageEffectDefinition dmgDef:
                        totalValue += CalculateDamageValue(card, dmgDef, position);
                        break;
                    case HealEffectDefinition healDef:
                        totalValue += CalculateHealValue(card, healDef, position);
                        break;
                    case Game.Card.Effects.MultiStatBuffEffectDefinition buffDef:
                        totalValue += CalculateBuffValue(card, buffDef, position);
                        break;
                    case Game.Card.Effects.StunEffectDefinition stunDef:
                        totalValue += CalculateStunValue(card, stunDef, position);
                        break;
                    case Game.Card.Effects.DrawCardsEffectDefinition drawDef:
                        totalValue += CalculateDrawCardsValue(card, drawDef);
                        break;
                    case Game.Card.Effects.HealBaseEffectDefinition healBaseDef:
                        totalValue += CalculateHealBaseValue(card, healBaseDef);
                        break;
                    case Game.Card.Effects.DamageBaseEffectDefinition dmgBaseDef:
                        totalValue += CalculateDamageBaseValue(card, dmgBaseDef);
                        break;
                }
            }

            return totalValue;
        }

        private int CalculateSummonValue(SummonEffectDefinition def)
        {
            var unit = def.UnitToSummon;
            if (unit == null)
                return 0;

            return unit.MaxHealth + unit.AttackPower + unit.MovementRange;
        }

        private int CalculateDamageValue(CardData card, DamageEffectDefinition def, Vector2Int position)
        {
            if (gridController == null || def.AreaShape == null)
                return 0;

            var tiles = def.AreaShape.GetTiles(position, gridController);
            int hitCount = 0;

            foreach (var tile in tiles)
            {
                if (tile == null || tile.OccupyingUnit == null) continue;

                var teamComponent = tile.OccupyingUnit.GetComponent<ITeamComponent>();
                if (teamComponent == null) continue;

                bool isEnemy = teamComponent.Team == TeamType.Player;
                if (isEnemy) hitCount++;
            }

            return hitCount * def.DamageAmount;
        }

        private int CalculateHealValue(CardData card, HealEffectDefinition def, Vector2Int position)
        {
            if (gridController == null || def.AreaShape == null)
                return 0;

            var tiles = def.AreaShape.GetTiles(position, gridController);
            int healCount = 0;

            foreach (var tile in tiles)
            {
                if (tile == null || tile.OccupyingUnit == null) continue;

                var unitOnTile = tile.OccupyingUnit.gameObject;
                var teamComponent = unitOnTile.GetComponent<ITeamComponent>();
                var healthComponent = unitOnTile.GetComponent<IHealthComponent>();

                if (teamComponent == null || healthComponent == null) continue;

                bool isAlly = teamComponent.Team == TeamType.Enemy;
                if (isAlly && healthComponent.CurrentHealth < healthComponent.MaxHealth)
                {
                    healCount++;
                }
            }

            return healCount * def.HealAmount;
        }

        private int CalculateBuffValue(CardData card, Game.Card.Effects.MultiStatBuffEffectDefinition def, Vector2Int position)
        {
            if (gridController == null || def.AreaShape == null)
                return 0;

            var tiles = def.AreaShape.GetTiles(position, gridController);
            int targetCount = 0;

            foreach (var tile in tiles)
            {
                if (tile == null || tile.OccupyingUnit == null) continue;

                var teamComponent = tile.OccupyingUnit.GetComponent<ITeamComponent>();
                if (teamComponent == null) continue;

                // 적 유닛 기준으로, 아군(Enemy 팀)에게 버프가 들어가는 경우만 가치 있음
                bool isAlly = teamComponent.Team == TeamType.Enemy;
                if (isAlly) targetCount++;
            }

            int statSum = Mathf.Abs(def.HealthDelta) + Mathf.Abs(def.AttackDelta) * 2 + Mathf.Abs(def.MovementDelta);
            return targetCount * statSum;
        }

        private int CalculateStunValue(CardData card, Game.Card.Effects.StunEffectDefinition def, Vector2Int position)
        {
            if (gridController == null || def.AreaShape == null)
                return 0;

            var tiles = def.AreaShape.GetTiles(position, gridController);
            int stunCount = 0;

            foreach (var tile in tiles)
            {
                if (tile == null || tile.OccupyingUnit == null) continue;

                var teamComponent = tile.OccupyingUnit.GetComponent<ITeamComponent>();
                if (teamComponent == null) continue;

                // 적 유닛(Player 팀)을 스턴시키는 경우 가치 부여
                bool isEnemy = teamComponent.Team == TeamType.Player;
                if (isEnemy) stunCount++;
            }

            return stunCount * Mathf.Max(1, def.StunTurns) * 3;
        }

        private int CalculateDrawCardsValue(CardData card, Game.Card.Effects.DrawCardsEffectDefinition def)
        {
            // 적 AI 입장에서: Enemy 팀 카드 드로우만 가치가 있다고 가정
            // DrawCardsEffectDefinition.TargetRelation 해석은 실제 적용 시점에 이뤄지므로,
            // 여기서는 단순히 드로우 수에 비례한 가치만 부여
            return def.DrawCount * 2;
        }

        private int CalculateHealBaseValue(CardData card, Game.Card.Effects.HealBaseEffectDefinition def)
        {
            // 적 AI는 자신의 베이스를 회복하는 카드에만 관심이 있음
            // AI 체력 상황을 고려한 가중치는 추후 확장 가능
            return def.HealAmount;
        }

        private int CalculateDamageBaseValue(CardData card, Game.Card.Effects.DamageBaseEffectDefinition def)
        {
            // 적 AI는 플레이어 베이스에 피해를 주는 카드를 매우 가치 있게 평가
            return def.DamageAmount * 2;
        }

        #endregion

        #region 로깅

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[EnemyAI v2.4] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnemyAI v2.4] {message}");
        }

        #endregion

        #region 디버깅 API

        /// <summary>
        /// 현재 손패 정보 반환 (디버깅용)
        /// </summary>
        public string GetHandInfo()
        {
            if (enemyHand.Count == 0)
            {
                return "Hand: Empty";
            }

            var cardNames = enemyHand.Select(c => $"{c.CardName}({c.ManaCost}M)");
            return $"Hand ({enemyHand.Count}): {string.Join(", ", cardNames)}";
        }

        /// <summary>
        /// AI 상태 정보 반환 (v2.4: 카드 풀 및 전략 정보 추가)
        /// </summary>
        public string GetStatus()
        {
            return $"Enemy AI v2.4 Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Card Pool: {(cardPool != null ? cardPool.name : "None")}\n" +
                   $"- Pool Size: {cardPool?.Cards.Count ?? 0}\n" +
                   $"- Selection Strategy: {selectionStrategy?.StrategyName ?? "None"}\n" +
                   $"- Hand Size: {enemyHand.Count}\n" +
                   $"- {GetHandInfo()}\n" +
                   $"- Current Mana: {resourceManager?.EnemyMana ?? 0}\n";
        }

        #endregion

        #region Unity Editor 디버깅

#if UNITY_EDITOR
        [Header("에디터 디버깅")]
        [SerializeField] private bool showDebugGUI = true;

        private void OnGUI()
        {
            if (!showDebugGUI || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 300, 350, 250));
            GUILayout.Box("Enemy AI v2.4 Debug (Strategy Pattern)");

            if (isInitialized)
            {
                GUILayout.Label("✅ AI Initialized");
                GUILayout.Label($"Card Pool: {cardPool?.name ?? "None"}");
                GUILayout.Label($"Strategy: {selectionStrategy?.StrategyName ?? "None"}");
                GUILayout.Label($"Pool Size: {cardPool?.Cards.Count ?? 0}");
                GUILayout.Label($"Hand: {enemyHand.Count} cards");
                GUILayout.Label($"Mana: {resourceManager?.EnemyMana ?? 0}");

                if (GUILayout.Button("Draw Card"))
                {
                    DrawCard(1);
                }

                if (GUILayout.Button("Execute Summon Phase"))
                {
                    ExecuteSummonPhase();
                }

                if (GUILayout.Button("Show Hand Info"))
                {
                    Debug.Log(GetHandInfo());
                }

                if (GUILayout.Button("Show Pool Info"))
                {
                    if (cardPool != null)
                    {
                        Debug.Log(cardPool.GetPoolInfo());
                    }
                }
            }
            else
            {
                GUILayout.Label("❌ AI Not Initialized");
                if (cardPool == null)
                {
                    GUILayout.Label("⚠️ Card Pool not assigned!");
                }
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
