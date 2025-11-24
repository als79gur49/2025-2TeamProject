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

            // 1. 현재 사용 가능한 마나 확인
            int currentMana = resourceManager.EnemyMana;
            Log($"Starting summon phase with {currentMana} mana");

            // 2. 손패의 모든 카드에 대해 최고 가치와 위치를 계산
            var cardValueInfos = new List<CardValueInfo>();
            foreach (var card in enemyHand)
            {
                var valueInfo = CalculateBestSituationalValue(card);
                if (valueInfo.Value > 0)
                {
                    cardValueInfos.Add(valueInfo);
                    Log($"Card '{card.CardName}': Value={valueInfo.Value}, BestPos={valueInfo.Position}");
                }
            }

            if (cardValueInfos.Count == 0)
            {
                Log("No valid card placements found");
                yield break;
            }

            // 3. Knapsack 알고리즘 실행
            var selectedCardInfos = KnapsackCardSelector.SelectOptimalCards(cardValueInfos, currentMana);

            if (selectedCardInfos.Count == 0)
            {
                Log("No cards selected to play this turn");
                yield break;
            }

            Log($"Knapsack selected {selectedCardInfos.Count} cards (Total Value: {selectedCardInfos.Sum(c => c.Value)})");

            // 4. 선택된 카드들을 순차적으로 실행 (v2.3: 실행 전 재검증)
            int successCount = 0;
            foreach (var info in selectedCardInfos)
            {
                // 🔴 중요: 다음 카드를 실행하기 전에 GameFlowLock이 해제될 때까지 대기
                // 즉, 이전 카드의 VFX나 다른 블로킹 애니메이션이 끝날 때까지 기다립니다.
                yield return new WaitUntil(() => !_stateManager.IsBusy(BusyType.GameFlowLock));

                // 🆕 v2.3: 카드 실행 직전 필드 상태 재검증
                bool isStillValid = spawnValidator.CanUseCard(info.Card, info.Position, isPlayerUnit: false);
                int currentValue = 0;

                if (isStillValid)
                {
                    currentValue = CalculateValueAtPosition(info.Card, info.Position);
                }

                CardValueInfo finalInfo = info; // 기본값: 원래 계획 사용

                // 원래 계획이 더 이상 최적이 아닌 경우 재계산
                if (!isStillValid || currentValue <= MIN_VALUE_THRESHOLD)
                {
                    Log($"⚠️ Original plan for '{info.Card.CardName}' at {info.Position} is no longer optimal " +
                        $"(Valid: {isStillValid}, Value: {currentValue}). Recalculating...");

                    var recalculatedInfo = CalculateBestSituationalValue(info.Card);

                    if (recalculatedInfo.Value > MIN_VALUE_THRESHOLD)
                    {
                        finalInfo = recalculatedInfo;
                        Log($"✅ Found better position: {finalInfo.Position} with value {finalInfo.Value}");
                    }
                    else
                    {
                        Log($"❌ No valid alternative found. Skipping '{info.Card.CardName}'");
                        continue; // 이 카드는 건너뛰기
                    }
                }

                // 최종 결정된 위치에 카드 실행
                // TryExecuteCard는 내부적으로 VFX를 재생하고 GameFlowLock을 설정해야 합니다.
                bool success = cardSpawnService.TryExecuteCard(finalInfo.Card, finalInfo.Position, TeamType.Enemy);
                if (success)
                {
                    enemyHand.Remove(finalInfo.Card);
                    successCount++;
                    Log($"Executed '{finalInfo.Card.CardName}' at {finalInfo.Position}. Waiting for its VFX to complete...");

                    // 🔔 이벤트 발생
                    OnCardUsed?.Invoke();
                }
                else
                {
                    LogError($"Failed to execute '{finalInfo.Card.CardName}' at {finalInfo.Position}");
                }
            }

            Log($"Summon phase complete: {successCount}/{selectedCardInfos.Count} cards played successfully");
        }

        #endregion

        #region 가치 평가 (v2.0 핵심 로직)

        /// <summary>
        /// 카드의 모든 가능한 위치를 탐색하여 최고 가치와 위치를 계산합니다.
        /// </summary>
        private CardValueInfo CalculateBestSituationalValue(CardData card)
        {
            int maxValue = 0;
            Vector2Int bestPosition = -Vector2Int.one; // 유효하지 않은 위치로 초기화

            // 그리드의 모든 타일을 순회하며 유효한 위치 탐색
            if (gridController == null)
            {
                LogError("⚠️ GridController not available for value calculation");
                return new CardValueInfo(card, 0, bestPosition);
            }

            // GridController의 GridSize 사용
            Vector2Int gridSize = gridController.GridSize;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var currentPosition = new Vector2Int(x, y);

                    // 1. 위치 유효성 검증
                    if (!gridController.IsValidPosition(currentPosition))
                        continue;

                    // 2. 배치 가능 위치 검증
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
