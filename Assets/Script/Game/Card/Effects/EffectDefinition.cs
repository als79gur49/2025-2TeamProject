using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 효과 정의용 ScriptableObject 베이스
    /// 개별 카드 데이터에는 이 정의에 대한 참조만을 들고, 실제 실행 시점에 ICardEffect로 변환됩니다.
    /// </summary>
    public abstract class EffectDefinition : ScriptableObject
    {
        [Header("공통 메타 정보")]
        [SerializeField] private EffectType effectType = EffectType.Damage;
        [SerializeField] private int priority = 0;
        [SerializeField] private EffectTargetScope targetScope = EffectTargetScope.TileBased;

        [Header("타일 기반 타겟팅 (TileBased 전용)")]
        [SerializeField] private AreaShapeDefinition areaShape;
        [SerializeField] private TargetFilterDefinition targetFilter;

        [Header("VFX 설정")]
        [SerializeField] private VFX.VFXData vfx;

        public EffectType EffectType => effectType;
        public int Priority => priority;
        public EffectTargetScope TargetScope => targetScope;

        /// <summary>
        /// TileBased 효과에서만 사용되는 영역(shape) 정의
        /// Global 효과인 경우 null이어도 됩니다.
        /// </summary>
        public AreaShapeDefinition AreaShape => areaShape;

        /// <summary>
        /// TileBased 효과에서만 사용되는 타겟 필터
        /// Global 효과인 경우 null이어도 됩니다.
        /// </summary>
        public TargetFilterDefinition TargetFilter => targetFilter;

        public VFX.VFXData VFX => vfx;

        /// <summary>
        /// 런타임에 사용할 ICardEffect 인스턴스를 생성합니다.
        /// 실제 구현체(DamageEffectDefinition 등)에서 구체 타입을 반환합니다.
        /// </summary>
        public abstract ICardEffect CreateRuntimeEffect();
    }
}

