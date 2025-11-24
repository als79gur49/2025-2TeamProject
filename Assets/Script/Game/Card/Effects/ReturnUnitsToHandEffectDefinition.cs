using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 타일 기반으로 선택된 유닛들을 손패로 되돌리는 효과 정의
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/ReturnUnitsToHandEffect")]
    public class ReturnUnitsToHandEffectDefinition : EffectDefinition
    {
        [Header("손패로 되돌릴 최대 유닛 수 (0이면 제한 없음)")]
        [SerializeField] private int maxUnitsToReturn = 0;

        public int MaxUnitsToReturn => maxUnitsToReturn;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new ReturnUnitsToHandEffect(this);
        }
    }
}

