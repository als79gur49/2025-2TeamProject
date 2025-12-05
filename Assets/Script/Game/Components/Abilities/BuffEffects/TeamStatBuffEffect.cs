using Game.Core.Effects;
using Game.Components;
using Game.Interfaces;
using Game.VFX;
using UnityEngine;

namespace Game.Components.Abilities
{
    /// <summary>
    /// 대상 유닛에 부착되어 일정 턴 동안 체력/공격력/이동력을 변경하는 버프/디버프 Effect입니다.
    /// - EffectManager에 추가되는 즉시 IImmediateEffect.ApplyImmediately를 통해 1회 스탯을 적용합니다.
    /// - Trigger는 OnDeploy로 설정되지만, CanApply가 false를 반환하므로 TriggerEffects를 통해서는 실행되지 않습니다.
    /// - 지속 시간이 0이 될 때 공격력/이동력 변경분을 원래대로 되돌릴 수 있습니다.
    /// </summary>
    public class TeamStatBuffEffect : IEffect, IImmediateEffect, IPersistentVFXEffect
    {
        public string EffectName => "팀 스탯 버프";
        public int Priority { get; private set; }
        public Unit Owner { get; private set; }
        public EffectTrigger Trigger { get; private set; }
        public int RemainingDuration { get; private set; }

        public DurationType DurationType => DurationType.ActionBased;

        private readonly int healthDelta;
        private readonly int attackDelta;
        private readonly int movementDelta;
        private readonly bool revertOnExpire;

        // VFX 설정
        private readonly string persistentVfxId;
        private readonly VFXData vfxOverride;
        private readonly VFXAnchorType anchor;
        private readonly Vector3 extraOffset;

        private bool isApplied;

        public TeamStatBuffEffect(
            Unit owner,
            int healthDelta,
            int attackDelta,
            int movementDelta,
            int durationTurns,
            int priority,
            bool revertOnExpire = true,
            string persistentVfxId = null,
            VFXData vfxOverride = null,
            VFXAnchorType anchor = VFXAnchorType.Body,
            Vector3 extraOffset = default)
        {
            Owner = owner;
            this.healthDelta = healthDelta;
            this.attackDelta = attackDelta;
            this.movementDelta = movementDelta;
            RemainingDuration = durationTurns;
            Priority = priority;
            this.revertOnExpire = revertOnExpire;

            // VFX 관련 설정
            this.persistentVfxId = persistentVfxId;
            this.vfxOverride = vfxOverride;
            this.anchor = anchor;
            this.extraOffset = extraOffset;

            // 버프는 OnDeploy 트리거 그룹에 보관하지만, CanApply가 false이므로 TriggerEffects로는 실행되지 않습니다.
            Trigger = EffectTrigger.OnDeploy;
            isApplied = false;
        }

        /// <summary>
        /// EffectManager에 Add되는 즉시 한 번 호출되어 실제 스탯 변경을 수행합니다.
        /// </summary>
        public void ApplyImmediately(EffectContext context)
        {
            if (isApplied || Owner == null || !Owner.IsAlive)
            {
                return;
            }

            var health = Owner.GetComponent<IHealthComponent>();
            var combat = Owner.GetComponent<ICombatSystem>();
            var movement = Owner.GetComponent<IMovementSystem>();

            // 체력 변경
            if (health != null && healthDelta != 0)
            {
                if (healthDelta > 0)
                {
                    health.SetMaxHealth(health.MaxHealth + healthDelta);
                    health.Heal(healthDelta);
                }
                else
                {
                    // 디버프 체력은 피해로 처리
                    health.TakeDamage(-healthDelta);
                }
            }

            // 공격력 변경
            if (combat != null && attackDelta != 0)
            {
                combat.ModifyAttackPower(attackDelta);
            }

            // 이동력 변경
            if (movement != null && movementDelta != 0)
            {
                movement.ModifyMovementRange(movementDelta);
            }

            isApplied = true;

            Debug.Log($"[TeamStatBuffEffect] {Owner.name} stat buff applied: HP {healthDelta}, ATK {attackDelta}, MOVE {movementDelta}, Duration {RemainingDuration}");
        }

        public void TickDuration(DurationTickContext context)
        {
            // 스탯 버프는 유닛의 행동 턴(Action Turn)이 한 번 처리되었을 때를 기준으로 감소합니다.
            if (context.Source != DurationTickSource.ActionEnd)
            {
                return;
            }

            if (RemainingDuration > 0)
            {
                RemainingDuration--;

                // 만료 시 공격력/이동력 변경분을 원래대로 되돌립니다.
                if (RemainingDuration == 0 && revertOnExpire && Owner != null && Owner.IsAlive)
                {
                    var combat = Owner.GetComponent<ICombatSystem>();
                    var movement = Owner.GetComponent<IMovementSystem>();

                    if (combat != null && attackDelta != 0)
                    {
                        combat.ModifyAttackPower(-attackDelta);
                    }

                    if (movement != null && movementDelta != 0)
                    {
                        movement.ModifyMovementRange(-movementDelta);
                    }

                    Debug.Log($"[TeamStatBuffEffect] {Owner.name} stat buff expired: ATK {-attackDelta}, MOVE {-movementDelta}");
                }
            }
        }

        /// <summary>
        /// 버프는 TriggerEffects를 통해 별도로 실행할 필요가 없으므로 항상 false를 반환합니다.
        /// </summary>
        public bool CanApply(EffectContext context)
        {
            return false;
        }

        /// <summary>
        /// 즉시 적용형 버프이므로 Apply는 사용하지 않습니다.
        /// </summary>
        public void Apply(EffectContext context)
        {
            // No-op: 실제 로직은 ApplyImmediately에서 처리됩니다.
        }

        // IPersistentVFXEffect 구현
        public string GetPersistentVFXId()
        {
            // 개별 이펙트에서 ID를 지정하지 않으면 공통 기본 VFX("StatBuff")를 사용합니다.
            return string.IsNullOrEmpty(persistentVfxId) ? "StatBuff" : persistentVfxId;
        }

        public VFXData GetVFXOverrideOrNull() => vfxOverride;

        public VFXAnchorType GetVFXAnchor() => anchor;

        public Vector3 GetVFXOffset() => extraOffset;
    }
}
