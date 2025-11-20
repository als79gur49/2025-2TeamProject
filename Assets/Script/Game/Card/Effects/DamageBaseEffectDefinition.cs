using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 특정 팀의 Base(넥서스)에 피해를 주는 전역(Global) 효과 정의
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/DamageBaseEffect")]
    public class DamageBaseEffectDefinition : EffectDefinition
    {
        [Header("베이스 공격 설정")]
        [SerializeField] private int damageAmount = 5;
        [SerializeField] private TeamRelation targetRelation = TeamRelation.Enemy;

        public int DamageAmount => damageAmount;
        public TeamRelation TargetRelation => targetRelation;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new DamageBaseEffect(this);
        }
    }
}

