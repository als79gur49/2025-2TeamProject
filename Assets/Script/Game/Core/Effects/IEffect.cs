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
        /// Effect의 지속시간이 어떤 시간축을 기준으로 감소하는지 나타냅니다.
        /// </summary>
        DurationType DurationType { get; }

        /// <summary>
        /// Duration Tick 컨텍스트에 따라 지속 턴을 감소시킵니다.
        /// </summary>
        void TickDuration(DurationTickContext context);

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

