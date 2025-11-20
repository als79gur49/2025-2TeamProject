using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.VFX;
using Game.Components;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 회복 효과 구현
    /// ICardEffect를 구현하여 대상을 회복시키는 효과입니다.
    /// VFX Dynamic Data System: IVFXAwareEffect 구현으로 공격 성공/실패 반응
    /// HashSet 중복 제거: 동일한 HealthComponent에 중복 회복 방지
    /// </summary>
    public class HealEffect : IVFXAwareEffect
    {
        private readonly HealEffectDefinition _definition;

        public EffectType EffectType => EffectType.Heal;
        public int Priority => _definition?.Priority ?? 0;

        public HealEffect(HealEffectDefinition definition)
        {
            _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("HealEffect: 유효하지 않은 HealEffectDefinition입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("HealEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 타일 기반: 사전 계산된 타일이 있는지 확인
            return context.PredeterminedTiles != null && context.PredeterminedTiles.Count > 0;
        }

        /// <summary>
        /// ✅ 범위 회복 중복 제거 적용
        /// HashSet으로 동일한 HealthComponent에 중복 회복 방지
        /// </summary>
        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("HealEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            int healAmount = _definition.HealAmount;

            // ✅ 중복 제거용 HashSet
            HashSet<HealthComponent> healedTargets = new HashSet<HealthComponent>();
            int affectedCount = 0;

            foreach (var tile in context.PredeterminedTiles)
            {
                if (tile == null) continue;

                // 타일에서 회복 가능한 타겟 가져오기
                HealthComponent targetHealth = tile.GetDamageableTarget();

                // ✅ 이미 회복받은 타겟인지 확인
                if (targetHealth != null && targetHealth.IsAlive && healedTargets.Add(targetHealth))
                {
                    targetHealth.Heal(healAmount);
                    affectedCount++;
                }
            }

            Debug.Log($"HealEffect: {context.PredeterminedTiles.Count}개 타일 중 {affectedCount}개 고유 타겟을 {healAmount}만큼 회복시켰습니다.");

            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// ✅ VFX 데이터 포함 실행
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[HealEffect] Heal failed: {triggerData.ValidationFailureReason}");
                return;
            }

            var targetTile = context.GridController.GetTileAtPosition(
                new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

            HealthComponent targetHealth = targetTile?.GetDamageableTarget();

            if (targetHealth != null && targetHealth.IsAlive)
            {
                int healAmount = _definition.HealAmount;
                targetHealth.Heal(healAmount);

                Debug.Log($"[HealEffect] {healAmount} heal to target at {triggerData.TileGridPosition}");
            }

            PlayHealEffect(triggerData.TileWorldPosition, context);
        }

        /// <summary>
        /// 회복 실패 시 Miss 효과 재생 (VFX Aware)
        /// </summary>
        private void PlayMissEffect(Vector2Int gridPos, GameContext context)
        {
            Debug.Log($"[HealEffect] Playing miss effect at grid position {gridPos}");
            // TODO: Miss VFX 프리팹 재생 로직 구현
        }

        /// <summary>
        /// 회복 성공 시 Heal 효과 재생 (VFX Aware)
        /// VFX는 SpellEffectExecutor에서 처리되므로 여기서는 로그만 남김
        /// </summary>
        private void PlayHealEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[HealEffect] Playing heal effect at world position {worldPos}");
            // VFX는 VFXData를 통해 SpellEffectExecutor에서 처리됨
        }

        /// <summary>
        /// 시각적 효과를 재생합니다.
        /// VFX는 SpellEffectExecutor에서 처리되므로 여기서는 로그만 남김
        /// </summary>
        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            Debug.Log($"[HealEffect] Visual effect requested at {targetPos}");
            // VFX는 VFXData를 통해 SpellEffectExecutor에서 처리됨
        }

        public override string ToString()
        {
            return $"HealEffect[Value: {_definition?.HealAmount}, Scope: {_definition?.TargetScope}]";
        }
    }
}
