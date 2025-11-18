using Game.Components;

namespace Game.Core.Effects
{
    /// <summary>
    /// EffectManager에 추가되는 순간 한 번 즉시 적용되어야 하는 Effect를 나타내는 인터페이스입니다.
    /// Trigger 기반 실행(TriggerEffects)과는 별도로, AddEffect 시점에서만 ApplyImmediately가 호출됩니다.
    /// </summary>
    public interface IImmediateEffect
    {
        /// <summary>
        /// Effect가 EffectManager에 등록된 직후 한 번 호출됩니다.
        /// 대부분의 즉시 버프 Effect는 별도의 컨텍스트가 필요 없으므로
        /// 기본 EffectContext를 사용합니다.
        /// </summary>
        void ApplyImmediately(EffectContext context);
    }
}

