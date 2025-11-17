using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    /// <summary>
    /// 배치 시 특정 팀의 Base를 회복시키는 유닛 이펙트 데이터입니다.
    /// 팀 선택은 항상 유닛의 팀을 기준으로 한 상대 관계(Ally/Enemy 등)로 결정됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DeployHealBaseEffect", menuName = "Game/Unit Effects/Deploy/HealBase")]
    public class DeployHealBaseEffectData : UnitEffectData
    {
        [Header("베이스 회복 설정")]
        [SerializeField] private int healAmount = 5;

        [Header("타겟 팀 설정")]
        [SerializeField] private DeployEffectTeamTargetConfig targetConfig;

        public int HealAmount => healAmount;
        public DeployEffectTeamTargetConfig TargetConfig => targetConfig ?? new DeployEffectTeamTargetConfig();

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.DeployHealBaseEffect(
                owner,
                HealAmount,
                TargetConfig.TargetRelation,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}
