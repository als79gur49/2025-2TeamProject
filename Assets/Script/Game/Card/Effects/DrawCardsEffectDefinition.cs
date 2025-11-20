using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 특정 팀이 카드를 드로우하는 전역(Global) 효과 정의
    /// 시전자의 팀과 TeamRelation을 사용하여 대상 팀을 결정합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/DrawCardsEffect")]
    public class DrawCardsEffectDefinition : EffectDefinition
    {
        [Header("드로우 설정")]
        [SerializeField] private int drawCount = 1;

        [Header("타겟 팀 설정")]
        [SerializeField] private TeamRelation targetRelation = TeamRelation.Self;

        public int DrawCount => drawCount;
        public TeamRelation TargetRelation => targetRelation;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new DrawCardsEffect(this);
        }
    }
}

