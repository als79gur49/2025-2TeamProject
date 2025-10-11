using UnityEngine;
using Game.Core;
using Game.Data;
using Game.Interfaces;
using Game.Services;
using Game.Card.Effects;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    /// <summary>
    /// 적군의 카드 사용 AI를 총괄하는 컨트롤러 (v2.0 - 상황 인식 기반 Knapsack 알고리즘)
    /// 필드 상황을 고려한 동적 가치 평가를 통해 최적의 카드와 배치 위치를 동시에 결정합니다.
    /// </summary>
    public class EnemyAIController : MonoBehaviour
    {
        [Header("AI 설정")]
        [SerializeField] private List<CardData> enemyDeck = new List<CardData>(); // 적이 사용할 수 있는 카드 목록
        [SerializeField] private int initialHandSize = 3; // 초기 손패 크기
        [SerializeField] private bool enableLogging = true;

        // 내부 상태
        private List<CardData> enemyHand = new List<CardData>();

        // 서비스 참조
        private IResourceManager resourceManager;
        private ICardSpawnService cardSpawnService;
        private IGridController gridController;
        private ISpawnValidator spawnValidator;
        private IGridState gridState;
        private IUnitService unitService;

        private bool isInitialized = false;

        /// <summary>초기화 완료 여부</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>현재 손패 크기 (디버깅용)</summary>
        public int HandSize => enemyHand.Count;

        #region 초기화

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드 (v2.0 - 확장된 의존성)
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

            // 덱에서 초기 핸드 드로우
            DrawInitialHand(initialHandSize);

            isInitialized = true;
            Log("🤖 [EnemyAI v2.0] Initialized with context-aware dependencies");
        }

        /// <summary>
        /// 초기 손패 드로우
        /// </summary>
        private void DrawInitialHand(int handSize)
        {
            if (enemyDeck == null || enemyDeck.Count == 0)
            {
                LogError("❌ Enemy deck is empty! Cannot draw initial hand");
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
        /// 덱에서 카드를 드로우합니다 (매 턴 호출)
        /// </summary>
        public void DrawCard(int amount = 1)
        {
            if (enemyDeck == null || enemyDeck.Count == 0)
            {
                LogError("❌ Enemy deck is empty! Cannot draw card");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                // 랜덤하게 덱에서 카드 선택 (실제로는 복사본을 사용)
                CardData drawnCard = enemyDeck[Random.Range(0, enemyDeck.Count)];
                enemyHand.Add(drawnCard);
                Log($"🃏 Enemy drew: {drawnCard.CardName} (Hand size: {enemyHand.Count})");
            }
        }

        #endregion

        #region 소환 페이즈 실행 (v2.0 - 상황 인식 가치 평가)

        /// <summary>
        /// EnemySummonPhase가 시작될 때 CardServiceManager에 의해 호출됩니다.
        /// v2.0: 필드 상황을 고려한 동적 가치 평가 + Knapsack 알고리즘
        /// </summary>
        public void ExecuteSummonPhase()
        {
            if (!isInitialized)
            {
                LogError("❌ [EnemyAI] Not initialized!");
                return;
            }

            if (enemyHand.Count == 0)
            {
                Log("📭 No cards in hand to play");
                return;
            }

            // 1. 현재 사용 가능한 마나 확인
            int currentMana = resourceManager.EnemyMana;
            Log($"💰 Starting summon phase with {currentMana} mana");

            // 2. 손패의 모든 카드에 대해 최고 가치와 위치를 계산
            var cardValueInfos = new List<CardValueInfo>();
            foreach (var card in enemyHand)
            {
                var valueInfo = CalculateBestSituationalValue(card);
                if (valueInfo.Value > 0)
                {
                    cardValueInfos.Add(valueInfo);
                    Log($"💎 Card '{card.CardName}': Value={valueInfo.Value}, BestPos={valueInfo.Position}");
                }
            }

            if (cardValueInfos.Count == 0)
            {
                Log("⚠️ No valid card placements found");
                return;
            }

            // 3. Knapsack 알고리즘 실행
            var selectedCardInfos = KnapsackCardSelector.SelectOptimalCards(cardValueInfos, currentMana);

            if (selectedCardInfos.Count == 0)
            {
                Log("🚫 No cards selected to play this turn");
                return;
            }

            Log($"🎯 Knapsack selected {selectedCardInfos.Count} cards (Total Value: {selectedCardInfos.Sum(c => c.Value)})");

            // 4. 선택된 카드들을 최고 위치에 실행
            int successCount = 0;
            foreach (var info in selectedCardInfos)
            {
                bool success = cardSpawnService.TryExecuteCard(info.Card, info.Position, TeamType.Enemy);
                if (success)
                {
                    enemyHand.Remove(info.Card);
                    successCount++;
                    Log($"✅ Executed '{info.Card.CardName}' at {info.Position}");
                }
                else
                {
                    LogError($"❌ Failed to execute '{info.Card.CardName}' at {info.Position}");
                }
            }

            Log($"🎯 Summon phase complete: {successCount}/{selectedCardInfos.Count} cards played successfully");
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

            // Summon 카드는 SpawnValidator 사용
            if (card.HasEffectType(EffectType.Summon))
            {
                return spawnValidator.CanSpawnUnit(card, position, isPlayerUnit: false);
            }

            // Spell 카드는 TargetType과 TargetRange 확인
            // TODO: 추가 검증 로직 구현 (현재는 임시로 모든 Spell 위치 허용)
            return true;
        }

        /// <summary>
        /// 특정 위치에서 카드의 총 가치를 계산합니다.
        /// </summary>
        private int CalculateValueAtPosition(CardData card, Vector2Int position)
        {
            int totalValue = 0;

            if (card?.EffectDataList == null)
                return 0;

            foreach (var effect in card.EffectDataList)
            {
                totalValue += CalculateEffectValueAtPosition(card, effect, position);
            }

            return totalValue;
        }

        /// <summary>
        /// 개별 효과의 특정 위치에서의 가치를 계산합니다.
        /// </summary>
        private int CalculateEffectValueAtPosition(CardData card, EffectData effect, Vector2Int position)
        {
            switch (effect.Type)
            {
                case EffectType.Summon:
                    return CalculateSummonValue(effect);

                case EffectType.Damage:
                    return CalculateDamageValue(card, effect, position);

                case EffectType.Heal:
                    return CalculateHealValue(card, effect, position);

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Summon 효과의 가치 계산: 유닛 스탯 합산
        /// </summary>
        private int CalculateSummonValue(EffectData effect)
        {
            var unit = effect.UnitToSummon;
            if (unit == null)
                return 0;

            return unit.MaxHealth + unit.AttackPower + unit.MovementRange;
        }

        /// <summary>
        /// Damage 효과의 가치 계산: 타격 가능한 적군 수 × 데미지
        /// </summary>
        private int CalculateDamageValue(CardData card, EffectData effect, Vector2Int position)
        {
            var affectedPositions = GetAffectedPositions(card, effect, position);
            int hitCount = 0;

            foreach (var pos in affectedPositions)
            {
                var unitOnTile = gridController?.GetUnitAtPosition(pos);
                if (unitOnTile != null)
                {
                    // 유닛의 팀 정보 가져오기
                    var teamComponent = unitOnTile.GetComponent<ITeamComponent>();
                    if (teamComponent != null)
                    {
                        // 적군 AI 입장에서 플레이어 유닛은 적
                        bool isEnemy = teamComponent.Team == TeamType.Player;

                        if ((effect.AffectedType == AffectedType.Enemy && isEnemy) ||
                            (effect.AffectedType == AffectedType.Any))
                        {
                            hitCount++;
                        }
                    }
                }
            }

            return hitCount * effect.Value;
        }

        /// <summary>
        /// Heal 효과의 가치 계산: 치유 가능한 아군 수 × 치유량
        /// </summary>
        private int CalculateHealValue(CardData card, EffectData effect, Vector2Int position)
        {
            var affectedPositions = GetAffectedPositions(card, effect, position);
            int healCount = 0;

            foreach (var pos in affectedPositions)
            {
                var unitOnTile = gridController?.GetUnitAtPosition(pos);
                if (unitOnTile != null)
                {
                    // 유닛의 팀 정보와 체력 정보 가져오기
                    var teamComponent = unitOnTile.GetComponent<ITeamComponent>();
                    var healthComponent = unitOnTile.GetComponent<IHealthComponent>();

                    if (teamComponent != null && healthComponent != null)
                    {
                        // 적군 AI 입장에서 적군 유닛은 아군
                        bool isAlly = teamComponent.Team == TeamType.Enemy;

                        if ((effect.AffectedType == AffectedType.Ally && isAlly) ||
                            (effect.AffectedType == AffectedType.Any))
                        {
                            // 현재 체력이 최대 체력보다 낮은 경우에만 가치 부여
                            if (healthComponent.CurrentHealth < healthComponent.MaxHealth)
                            {
                                healCount++;
                            }
                        }
                    }
                }
            }

            return healCount * effect.Value;
        }

        /// <summary>
        /// 효과 범위 내의 영향받는 위치 목록을 가져옵니다 (맨해튼 거리 기반).
        /// </summary>
        private List<Vector2Int> GetAffectedPositions(CardData card, EffectData effect, Vector2Int centerPosition)
        {
            var positions = new List<Vector2Int>();
            int range = effect.AffectedRange;

            if (gridController == null)
                return positions;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    // 맨해튼 거리
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= range)
                    {
                        var targetPos = centerPosition + new Vector2Int(x, y);

                        // 유효한 위치만 추가
                        if (gridController.IsValidPosition(targetPos))
                        {
                            positions.Add(targetPos);
                        }
                    }
                }
            }

            return positions;
        }

        #endregion

        #region 로깅

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[EnemyAI v2.0] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnemyAI v2.0] {message}");
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
        /// AI 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"Enemy AI v2.0 Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Deck Size: {enemyDeck?.Count ?? 0}\n" +
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

            GUILayout.BeginArea(new Rect(10, 300, 300, 200));
            GUILayout.Box("Enemy AI v2.0 Debug");

            if (isInitialized)
            {
                GUILayout.Label("✅ AI Initialized");
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
            }
            else
            {
                GUILayout.Label("❌ AI Not Initialized");
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
