using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    /// <summary>
    /// 배치 시 아군 또는 적군 n명에게 일정 턴 동안 스턴을 부여하는 소스 이펙트 데이터입니다.
    /// 실제 스턴 상태 유지는 대상 유닛에 부착되는 StunStatusEffect가 담당합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DeployTeamStunEffect", menuName = "Game/Unit Effects/Deploy/TeamStun")]
    public class DeployTeamStunEffectData : UnitEffectData
    {
        [Header("스턴 설정")]
        [SerializeField] private int stunTurns = 1;

        [Header("대상 설정")]
        [SerializeField] private int maxTargets = 1;
        [SerializeField] private DeployEffectTeamTargetConfig targetConfig;

        public int StunTurns => stunTurns;
        public int MaxTargets => maxTargets;
        public DeployEffectTeamTargetConfig TargetConfig => targetConfig ?? new DeployEffectTeamTargetConfig();

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.DeployTeamStunSourceEffect(
                owner,
                StunTurns,
                MaxTargets,
                TargetConfig.TargetRelation,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}

