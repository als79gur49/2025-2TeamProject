using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.AI.CardSelection;

namespace Game.Data
{
    /// <summary>
    /// 적군 카드 풀 ScriptableObject
    /// 적군이 사용할 카드 목록과 선택 전략을 정의합니다
    /// 여러 스테이지에서 재사용 가능합니다
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyCardPool_", menuName = "Game/Enemy Card Pool", order = 3)]
    public class EnemyCardPoolSO : ScriptableObject
    {
        [Header("Card Pool Configuration")]
        [Tooltip("적군이 사용할 수 있는 카드 목록")]
        [SerializeField] private List<CardData> cards = new List<CardData>();

        [Header("Draw Configuration")]
        [Tooltip("적군의 초기 핸드 크기")]
        [SerializeField]
        [Range(1, 10)]
        private int initialHandSize = 3;

        [Header("Selection Strategy")]
        [Tooltip("카드 선택 방식")]
        [SerializeField] private SelectionType selectionType = SelectionType.Random;

        [Tooltip("RarityWeighted 전략을 사용할 때의 가중치 설정")]
        [SerializeField] private RarityWeightSettings rarityWeightSettings = new RarityWeightSettings();

        [Header("Pool Info (Read Only)")]
        [Tooltip("카드 풀의 총 카드 수")]
        [SerializeField] private int totalCardCount = 0;

        /// <summary>카드 풀의 모든 카드 (읽기 전용)</summary>
        public IReadOnlyList<CardData> Cards => cards.AsReadOnly();

        /// <summary>초기 핸드 크기</summary>
        public int InitialHandSize => initialHandSize;

        /// <summary>선택된 전략 타입</summary>
        public SelectionType SelectionType => selectionType;

        /// <summary>RarityWeighted 전략용 가중치 설정</summary>
        public RarityWeightSettings RarityWeightSettings => rarityWeightSettings;

        /// <summary>
        /// 현재 설정에 맞는 카드 선택 전략을 생성합니다
        /// </summary>
        /// <returns>생성된 전략 인스턴스</returns>
        public Game.AI.CardSelection.ICardSelectionStrategy GetStrategy()
        {
            return CardSelectionStrategyFactory.CreateStrategy(selectionType, rarityWeightSettings);
        }

        /// <summary>
        /// 카드 풀이 유효한지 검증합니다
        /// </summary>
        public bool IsValid()
        {
            if (cards == null || cards.Count == 0)
            {
                Debug.LogError($"[EnemyCardPoolSO] '{name}' has no cards in the pool!");
                return false;
            }

            // null 카드 체크
            int nullCount = 0;
            foreach (var card in cards)
            {
                if (card == null)
                {
                    nullCount++;
                }
            }

            if (nullCount > 0)
            {
                Debug.LogWarning($"[EnemyCardPoolSO] '{name}' has {nullCount} null cards in the pool");
            }

            if (initialHandSize > cards.Count)
            {
                Debug.LogWarning($"[EnemyCardPoolSO] '{name}' initial hand size ({initialHandSize}) is larger than pool size ({cards.Count})");
            }

            return true;
        }

        /// <summary>
        /// 카드 풀 정보를 문자열로 반환합니다 (디버깅용)
        /// </summary>
        public string GetPoolInfo()
        {
            var rarityCount = new Dictionary<CardData.CardRarity, int>();
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (!rarityCount.ContainsKey(card.Rarity))
                {
                    rarityCount[card.Rarity] = 0;
                }
                rarityCount[card.Rarity]++;
            }

            var info = $"Enemy Card Pool: {name}\n";
            info += $"- Total Cards: {cards.Count}\n";
            info += $"- Initial Hand Size: {initialHandSize}\n";
            info += $"- Selection Type: {selectionType}\n";
            info += $"- Rarity Distribution:\n";

            foreach (var kvp in rarityCount)
            {
                float percentage = (kvp.Value / (float)cards.Count) * 100f;
                info += $"  {kvp.Key}: {kvp.Value} ({percentage:F1}%)\n";
            }

            return info;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Unity Editor에서 값이 변경될 때 호출됩니다
        /// </summary>
        private void OnValidate()
        {
            // 초기 핸드 크기 검증
            initialHandSize = Mathf.Max(1, initialHandSize);

            // null 카드 제거
            if (cards != null)
            {
                cards.RemoveAll(c => c == null);
                totalCardCount = cards.Count;
            }

            // 중복 카드는 허용 (사용자가 의도적으로 중복 추가할 수 있음)
        }
#endif
    }
}
