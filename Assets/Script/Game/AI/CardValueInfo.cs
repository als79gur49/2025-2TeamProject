using UnityEngine;
using Game.Data;

namespace Game.AI
{
    /// <summary>
    /// 카드별로 계산된 상황 가치와 최적 배치 위치를 저장하는 데이터 구조
    /// Knapsack 알고리즘에서 동적 가치 평가를 위해 사용됩니다.
    /// </summary>
    public class CardValueInfo
    {
        /// <summary>카드 데이터</summary>
        public CardData Card { get; set; }

        /// <summary>계산된 상황 가치 (위치별 최대값)</summary>
        public int Value { get; set; }

        /// <summary>최적 배치 위치</summary>
        public Vector2Int Position { get; set; }

        /// <summary>카드 비용 (마나 코스트)</summary>
        public int Cost => Card?.ManaCost ?? 0;

        public CardValueInfo(CardData card, int value, Vector2Int position)
        {
            Card = card;
            Value = value;
            Position = position;
        }

        public override string ToString()
        {
            return $"CardValueInfo[Card={Card?.CardName}, Value={Value}, Position={Position}, Cost={Cost}]";
        }
    }
}
