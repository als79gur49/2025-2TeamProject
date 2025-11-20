using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 특정 팀의 Base(넥서스)를 회복시키는 전역(Global) 효과 정의
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/HealBaseEffect")]
    public class HealBaseEffectDefinition : EffectDefinition
    {
        [Header("베이스 회복 설정")]
        [SerializeField] private int healAmount = 5;
        [SerializeField] private TeamRelation targetRelation = TeamRelation.Self;

        public int HealAmount => healAmount;
        public TeamRelation TargetRelation => targetRelation;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new HealBaseEffect(this);
        }
    }
}

