using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 유닛에게 스턴을 부여하는 TileBased 효과 정의
    /// AreaShape + TargetFilter를 통해 대상 유닛을 선택합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/StunEffect")]
    public class StunEffectDefinition : EffectDefinition
    {
        [Header("스턴 설정")]
        [SerializeField] private int stunTurns = 1;
        [SerializeField] private int maxTargets = 1;

        public int StunTurns => stunTurns;
        public int MaxTargets => maxTargets;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new StunEffect(this);
        }
    }
}

