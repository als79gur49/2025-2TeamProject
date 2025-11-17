using UnityEngine;
using Game.Components;
using Game.Core.Effects;

namespace Game.Data.Effects
{
    /// <summary>
    /// 배치 시 특정 팀이 카드를 드로우하게 만드는 유닛 이펙트 데이터입니다.
    /// 팀 선택은 항상 유닛의 팀을 기준으로 한 상대 관계(Ally/Enemy 등)로 결정됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DeployDrawCardsEffect", menuName = "Game/Unit Effects/Deploy/DrawCards")]
    public class DeployDrawCardsEffectData : UnitEffectData
    {
        [Header("드로우 설정")]
        [SerializeField] private int drawCount = 1;

        [Header("타겟 팀 설정")]
        [SerializeField] private DeployEffectTeamTargetConfig targetConfig;

        public int DrawCount => drawCount;
        public DeployEffectTeamTargetConfig TargetConfig => targetConfig ?? new DeployEffectTeamTargetConfig();

        public override IEffect CreateEffect(Unit owner)
        {
            return new Game.Components.Abilities.DeployDrawCardsEffect(
                owner,
                DrawCount,
                TargetConfig.TargetRelation,
                Trigger,
                Priority,
                DurationTurns
            );
        }
    }
}
