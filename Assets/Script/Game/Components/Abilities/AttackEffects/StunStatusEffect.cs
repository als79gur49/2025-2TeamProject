using Game.Core.Effects;
using Game.VFX;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 유닛에 부착되어 일정 턴 동안 기절 상태를 유지하는 Effect입니다.
    /// 실제 행동 스킵은 Unit.ExecuteAITurn에서 IsStunned() 체크로 처리합니다.
    /// </summary>
    public class StunStatusEffect : IEffect, IPersistentVFXEffect
    {
        public string EffectName => "스턴";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        private readonly VFXData vfxOverride;
        private readonly Vector3 extraOffset;
        private readonly VFXAnchorType anchor;

        /// <summary>
        /// 전역 StatusVFXConfig 설정을 사용하는 기본 생성자입니다.
        /// </summary>
        public StunStatusEffect(Unit owner, int stunTurns)
            : this(owner, stunTurns, null, VFXAnchorType.Head, Vector3.zero)
        {
        }

        /// <summary>
        /// 커스텀 VFXData를 사용하는 생성자입니다.
        /// </summary>
        public StunStatusEffect(
            Unit owner,
            int stunTurns,
            VFXData vfxOverride,
            VFXAnchorType anchor,
            Vector3 extraOffset)
        {
            Owner = owner;
            Trigger = EffectTrigger.OnTurnStart;
            Priority = 999;
            RemainingDuration = stunTurns;

            this.vfxOverride = vfxOverride;
            this.anchor = anchor;
            this.extraOffset = extraOffset;
        }

        public void TickDuration()
        {
            if (RemainingDuration > 0)
                RemainingDuration--;
        }

        public bool CanApply(EffectContext context)
        {
            return Owner != null && Owner.IsAlive;
        }

        public void Apply(EffectContext context)
        {
            Debug.Log($"[StunStatusEffect] {Owner?.name} is stunned. Remaining turns: {RemainingDuration}");
        }

        // IPersistentVFXEffect 구현
        public string GetPersistentVFXId() => "Stun";

        public VFXData GetVFXOverrideOrNull() => vfxOverride;

        public VFXAnchorType GetVFXAnchor() => anchor;

        public Vector3 GetVFXOffset() => extraOffset;
    }
}
