using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 체력/공격력/이동력을 동시에 변경하는 버프/디버프 효과 정의용 ScriptableObject
    /// TileBased + AreaShape + TargetFilter 조합으로 여러 유닛에게 TeamStatBuffEffect를 부여합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/MultiStatBuffEffect")]
    public class MultiStatBuffEffectDefinition : EffectDefinition
    {
        [Header("버프 증감 수치")]
        [SerializeField] private int healthDelta = 0;
        [SerializeField] private int attackDelta = 0;
        [SerializeField] private int movementDelta = 0;

        [Header("대상 개수 및 지속 턴")]
        [SerializeField] private int maxTargets = 1;
        [SerializeField] private int buffDurationTurns = 1;
        [SerializeField] private bool revertOnExpire = true;

        public int HealthDelta => healthDelta;
        public int AttackDelta => attackDelta;
        public int MovementDelta => movementDelta;
        public int MaxTargets => maxTargets;
        public int BuffDurationTurns => buffDurationTurns;
        public bool RevertOnExpire => revertOnExpire;

        private void OnEnable()
        {
            // MultiStatBuffEffect는 항상 Buff 타입으로 간주
            // ScriptableObject 직렬화 특성상 직접 필드를 수정할 수는 없으므로,
            // EffectType 프로퍼티는 런타임 참조용으로만 사용하고,
            // 여기서는 디버그용 로그만 남깁니다.
        }

        public override ICardEffect CreateRuntimeEffect()
        {
            return new MultiStatBuffEffect(this);
        }
    }
}

