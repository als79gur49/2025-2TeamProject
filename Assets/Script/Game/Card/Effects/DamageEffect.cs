using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 데미지 효과 구현
    /// ICardEffect를 구현하여 대상에게 피해를 주는 효과입니다.
    /// VFX Dynamic Data System: IVFXAwareEffect 구현으로 공격 성공/실패 반응
    /// </summary>
    public class DamageEffect : IVFXAwareEffect
    {
        private readonly EffectData _effectData;

        public EffectType EffectType => EffectType.Damage;
        public int Priority => _effectData?.Priority ?? 0;

        public DamageEffect(EffectData effectData)
        {
            _effectData = effectData ?? throw new System.ArgumentNullException(nameof(effectData));

            if (_effectData.Type != EffectType.Damage)
            {
                throw new System.ArgumentException($"EffectData의 타입이 Damage가 아닙니다: {_effectData.Type}");
            }
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_effectData == null || !_effectData.IsValid())
            {
                Debug.LogWarning("DamageEffect: 유효하지 않은 EffectData입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("DamageEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 대상이 있는지 확인
            var affectedUnits = GetAffectedUnits(targetPos, context);
            return affectedUnits.Count > 0;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("DamageEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            var affectedUnits = GetAffectedUnits(targetPos, context);
            var damageAmount = _effectData.Value;

            Debug.Log($"DamageEffect: {affectedUnits.Count}개 유닛에게 {damageAmount} 피해를 적용합니다.");

            foreach (var unit in affectedUnits)
            {
                ApplyDamageToUnit(unit, damageAmount, null);
            }

            // 시각적 효과 재생
            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// VFX TriggerData를 포함한 효과 실행 (IVFXAwareEffect 구현)
        /// 공격 성공/실패 여부에 따라 데미지 적용 여부 결정
        /// NOTE: SpellEffectExecutor가 다중 타겟 처리를 담당하므로, 이 메서드는 단일 타겟만 처리
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            // 공격 실패 시 Miss 효과만 재생하고 종료
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[DamageEffect] Attack failed: {triggerData.ValidationFailureReason}");
                PlayMissEffect(targetPos, context);
                return;
            }

            // 공격 성공: 타겟에게 데미지 적용
            GameObject target = triggerData.PredeterminedTarget;
            if (target == null)
            {
                Debug.LogWarning("[DamageEffect] Attack success but no predetermined target");
                return;
            }

            var damageAmount = _effectData.Value;
            Debug.Log($"[DamageEffect] Attack success! Applying {damageAmount} damage to {target.name} at {targetPos}");

            // 타겟에게 데미지 적용
            ApplyDamageToUnit(target, damageAmount, triggerData);

            // VFX 위치 기반 Hit 효과 재생 (개별 타겟마다)
            PlayHitEffect(triggerData.TriggerWorldPosition, context);
        }

        /// <summary>
        /// Phase 2.12: GridController의 GetAffectedUnits() 메서드를 사용하여 영향받을 유닛들을 찾습니다.
        /// </summary>
        private List<GameObject> GetAffectedUnits(Vector2Int targetPos, GameContext context)
        {
            if (context?.GridController == null)
            {
                Debug.LogError("DamageEffect: GridController가 null입니다.");
                return new List<GameObject>();
            }

            // Phase 2.12: 중앙화된 GetAffectedUnits 메서드 사용
            var affectedUnits = context.GridController.GetAffectedUnits(
                targetPos,
                _effectData.AffectedType,
                _effectData.AffectedRange,
                context.PlayerId
            );

            // 데미지를 줄 수 있는 유닛들만 필터링 (필요시)
            // 현재는 모든 대상 유닛에게 데미지를 줄 수 있다고 가정
            return affectedUnits;
        }


        /// <summary>
        /// 유닛에게 실제 피해를 적용합니다.
        /// </summary>
        private void ApplyDamageToUnit(GameObject unit, int damage, VFXTriggerData triggerData)
        {
            if (unit == null)
            {
                Debug.LogError("DamageEffect: 유닛이 null입니다.");
                return;
            }

            var healthComponent = unit.GetComponent<Game.Components.HealthComponent>();
            if (healthComponent == null)
            {
                Debug.LogError($"DamageEffect: 유닛 {unit.name}에 HealthComponent가 없습니다.");
                return;
            }

            // 방어력 계산 (IgnoreArmor 옵션 고려)
            int finalDamage = _effectData.IgnoreArmor ? damage : CalculateDamageWithArmor(healthComponent, damage);

            Debug.Log($"DamageEffect: 유닛 {unit.name}에게 {finalDamage} 피해 적용 (원본: {damage}, 방어력 무시: {_effectData.IgnoreArmor})");

            // DamageInfo 구조체를 사용하여 피해 적용
            var damageInfo = new Game.Interfaces.DamageInfo(
                damage,
                Game.Interfaces.DamageType.Physical,
                null,
                false,
                _effectData.IgnoreArmor
            );

            // HealthComponent의 ProcessDamage는 private이므로 TakeDamage 사용
            // TakeDamage는 내부적으로 방어력을 계산하므로, IgnoreArmor일 경우 직접 SetHealth 사용
            if (_effectData.IgnoreArmor)
            {
                int newHealth = Mathf.Max(0, healthComponent.CurrentHealth - finalDamage);
                healthComponent.SetHealth(newHealth);
            }
            else
            {
                healthComponent.TakeDamage(damage);
            }
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
        /// 공격 성공 시 Hit 효과 재생 (VFX Aware)
        /// </summary>
        private void PlayHitEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[DamageEffect] Playing hit effect at world position {worldPos}");

            // VFXData의 이펙트 프리팹 사용
            if (_effectData.EffectPrefab != null)
            {
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }
        }

        /// <summary>
        /// 방어력을 고려한 최종 피해량을 계산합니다.
        /// </summary>
        private int CalculateDamageWithArmor(Game.Components.HealthComponent healthComponent, int baseDamage)
        {
            if (healthComponent == null)
            {
                Debug.LogWarning("DamageEffect: HealthComponent가 null입니다. 기본 피해량 반환.");
                return baseDamage;
            }

            // HealthComponent의 CalculateDamageAfterArmor 메서드를 사용
            return healthComponent.CalculateDamageAfterArmor(baseDamage);
        }

        /// <summary>
        /// 시각적 효과를 재생합니다.
        /// </summary>
        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            if (_effectData.EffectPrefab != null)
            {
                var worldPos = new Vector3(targetPos.x, 0, targetPos.y);
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }

            if (!string.IsNullOrEmpty(_effectData.EffectAnimation))
            {
                // TODO: 애니메이션 재생 로직 구현
                Debug.Log($"DamageEffect: 애니메이션 재생 - {_effectData.EffectAnimation}");
            }
        }

        public override string ToString()
        {
            return $"DamageEffect[Value: {_effectData?.Value}, AffectedType: {_effectData?.AffectedType}, Range: {_effectData?.AffectedRange}]";
        }
    }
}