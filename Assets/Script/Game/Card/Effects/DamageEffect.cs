using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.VFX;
using Game.Components;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 데미지 효과 구현
    /// ICardEffect를 구현하여 대상에게 피해를 주는 효과입니다.
    /// VFX Dynamic Data System: IVFXAwareEffect 구현으로 공격 성공/실패 반응
    /// HashSet 중복 제거: 동일한 HealthComponent에 중복 피해 방지
    /// </summary>
    public class DamageEffect : IVFXAwareEffect
    {
        private readonly DamageEffectDefinition _definition;

        public EffectType EffectType => EffectType.Damage;
        public int Priority => _definition?.Priority ?? 0;

        public DamageEffect(DamageEffectDefinition definition)
        {
            _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("DamageEffect: 유효하지 않은 DamageEffectDefinition입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("DamageEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 타일 기반: 사전 계산된 타일이 있는지 확인
            return context.PredeterminedTiles != null && context.PredeterminedTiles.Count > 0;
        }

        /// <summary>
        /// ✅ 범위 공격 중복 제거 적용
        /// HashSet으로 동일한 HealthComponent에 중복 피해 방지
        /// </summary>
        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("DamageEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            int damageAmount = _definition.DamageAmount;

            // ✅ 중복 제거용 HashSet
            HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();
            int affectedCount = 0;

            foreach (var tile in context.PredeterminedTiles)
            {
                if (tile == null) continue;

                // 타일에서 공격 가능한 타겟 가져오기 (유닛 우선, 없으면 기지)
                HealthComponent targetHealth = tile.GetDamageableTarget();

                // ✅ 이미 피해받은 타겟인지 확인 (Add는 새로 추가되면 true 반환)
                if (targetHealth != null && targetHealth.IsAlive && damagedTargets.Add(targetHealth))
                {
                    targetHealth.TakeDamage(damageAmount);
                    affectedCount++;
                }
            }

            Debug.Log($"DamageEffect: {context.PredeterminedTiles.Count}개 타일 중 {affectedCount}개 고유 타겟에게 {damageAmount} 피해를 적용했습니다.");

            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// ✅ VFX 데이터 포함 실행 - 범위 공격 중복 제거 적용
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[DamageEffect] Attack failed: {triggerData.ValidationFailureReason}");
                return;
            }

            var targetTile = context.GridController.GetTileAtPosition(
                new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

            // ✅ 단일 타겟 공격 (VFX 트리거는 보통 단일 타일)
            HealthComponent targetHealth = targetTile?.GetDamageableTarget();

            if (targetHealth != null && targetHealth.IsAlive)
            {
                int damageAmount = _definition.DamageAmount;
                targetHealth.TakeDamage(damageAmount);

                Debug.Log($"[DamageEffect] {damageAmount} damage to target at {triggerData.TileGridPosition}");
            }

            PlayDamageEffect(triggerData.TileWorldPosition, context);
        }

        /// <summary>
        /// 공격 실패 시 Miss 효과 재생 (VFX Aware)
        /// </summary>
        private void PlayMissEffect(Vector2Int gridPos, GameContext context)
        {
            Debug.Log($"[DamageEffect] Playing miss effect at grid position {gridPos}");
            // TODO: Miss VFX 프리팹 재생 로직 구현
            // 예: Instantiate(_missPrefab, context.GridManager.GridToWorldPosition(gridPos), Quaternion.identity);
        }

        /// <summary>
        /// 공격 성공 시 Damage 효과 재생 (VFX Aware)
        /// Phase 3.2: 타일 기반으로 변경
        /// VFX는 SpellEffectExecutor에서 처리되므로 여기서는 로그만 남김
        /// </summary>
        private void PlayDamageEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[DamageEffect] Playing damage effect at world position {worldPos}");
            // VFX는 VFXData를 통해 SpellEffectExecutor에서 처리됨
        }

        /// <summary>
        /// 시각적 효과를 재생합니다.
        /// VFX는 SpellEffectExecutor에서 처리되므로 여기서는 로그만 남김
        /// </summary>
        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            Debug.Log($"[DamageEffect] Visual effect requested at {targetPos}");
            // VFX는 VFXData를 통해 SpellEffectExecutor에서 처리됨
        }

        public override string ToString()
        {
            return $"DamageEffect[Value: {_definition?.DamageAmount}, Scope: {_definition?.TargetScope}]";
        }
    }
}
