using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    /// <summary>
    /// 배치 시 아군 또는 적군 n명에게 TeamStatBuffEffect를 부여하는 소스 이펙트 데이터입니다.
    /// 실제 스탯 변경은 대상 유닛에 부여된 TeamStatBuffEffect가 처리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DeployTeamMultiStatSourceEffect", menuName = "Game/Unit Effects/Deploy/TeamMultiStatBuff")]
    public class DeployTeamMultiStatSourceEffectData : UnitEffectData
    {
        [Header("버프 증감 수치")]
        [SerializeField] private int healthDelta = 0;
        [SerializeField] private int attackDelta = 0;
        [SerializeField] private int movementDelta = 0;

        [Header("대상 설정")]
        [SerializeField] private int maxTargets = 1;
        [SerializeField] private DeployEffectTeamTargetConfig targetConfig;

        [Header("부여될 버프 지속 턴")]
        [SerializeField] private int buffDurationTurns = 1;

        [Header("버프 VFX 설정")]
        [SerializeField] private string buffVfxIdOverride;

        public int HealthDelta => healthDelta;
        public int AttackDelta => attackDelta;
        public int MovementDelta => movementDelta;
        public int MaxTargets => maxTargets;
        public int BuffDurationTurns => buffDurationTurns;
        public string BuffVfxIdOverride => buffVfxIdOverride;
        public DeployEffectTeamTargetConfig TargetConfig => targetConfig ?? new DeployEffectTeamTargetConfig();

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.DeployTeamMultiStatSourceEffect(
                owner,
                HealthDelta,
                AttackDelta,
                MovementDelta,
                MaxTargets,
                TargetConfig.TargetRelation,
                BuffDurationTurns,
                Trigger,
                Priority,
                DurationTurns,
                BuffVfxIdOverride
            );
        }
    }
}
