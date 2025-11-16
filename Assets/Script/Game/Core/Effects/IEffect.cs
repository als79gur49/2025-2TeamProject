using Game.Components;

namespace Game.Core.Effects
{
    /// <summary>
    /// 모든 유닛 Effect의 공통 인터페이스입니다.
    /// </summary>
    public interface IEffect
    {
        string EffectName { get; }
        int Priority { get; }
        Unit Owner { get; }
        EffectTrigger Trigger { get; }

        /// <summary>
        /// 남은 지속 턴 수입니다.
        /// -1 이면 무한, 0 이면 제거 대상입니다.
        /// </summary>
        int RemainingDuration { get; }

        /// <summary>
        /// 턴 종료 시 한 번 호출되어 지속 턴을 감소시킵니다.
        /// </summary>
        void TickDuration();

        /// <summary>
        /// 현재 컨텍스트에서 Effect를 적용할 수 있는지 여부입니다.
        /// </summary>
        bool CanApply(EffectContext context);

        /// <summary>
        /// Effect를 실제로 적용합니다.
        /// </summary>
        void Apply(EffectContext context);
    }
}

