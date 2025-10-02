using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 회복 효과 구현
    /// ICardEffect를 구현하여 대상을 회복시키는 효과입니다.
    /// </summary>
    public class HealEffect : ICardEffect
    {
        private readonly EffectData _effectData;

        public EffectType EffectType => EffectType.Heal;
        public int Priority => _effectData?.Priority ?? 0;

        public HealEffect(EffectData effectData)
        {
            _effectData = effectData ?? throw new System.ArgumentNullException(nameof(effectData));

            if (_effectData.Type != EffectType.Heal)
            {
                throw new System.ArgumentException($"EffectData의 타입이 Heal이 아닙니다: {_effectData.Type}");
            }
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_effectData == null || !_effectData.IsValid())
            {
                Debug.LogWarning("HealEffect: 유효하지 않은 EffectData입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("HealEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 회복 가능한 대상이 있는지 확인
            var affectedUnits = GetAffectedUnits(targetPos, context);
            return affectedUnits.Count > 0;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("HealEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            var affectedUnits = GetAffectedUnits(targetPos, context);
            var healAmount = _effectData.Value;

            Debug.Log($"HealEffect: {affectedUnits.Count}개 유닛을 {healAmount}만큼 회복시킵니다.");

            foreach (var unit in affectedUnits)
            {
                ApplyHealToUnit(unit, healAmount);
            }

            // 시각적 효과 재생
            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// Phase 2.12: GridController의 GetAffectedUnits() 메서드를 사용하여 영향받을 유닛들을 찾습니다.
        /// </summary>
        private List<GameObject> GetAffectedUnits(Vector2Int targetPos, GameContext context)
        {
            if (context?.GridController == null)
            {
                Debug.LogError("HealEffect: GridController가 null입니다.");
                return new List<GameObject>();
            }

            // Phase 2.12: 중앙화된 GetAffectedUnits 메서드 사용
            var allAffectedUnits = context.GridController.GetAffectedUnits(
                targetPos,
                _effectData.AffectedType,
                _effectData.AffectedRange,
                context.PlayerId
            );

            // 회복 가능한 유닛들만 필터링
            var healableUnits = new List<GameObject>();
            foreach (var unit in allAffectedUnits)
            {
                if (CanBeHealed(unit))
                {
                    healableUnits.Add(unit);
                }
            }

            return healableUnits;
        }

        /// <summary>
        /// 유닛이 회복 가능한 상태인지 확인합니다.
        /// </summary>
        private bool CanBeHealed(GameObject unit)
        {
            if (unit == null) return false;

            // HealthComponent 확인
            var healthComponent = unit.GetComponent<IHealthComponent>();
            if (healthComponent == null)
            {
                Debug.LogWarning($"HealEffect: {unit.name}에 HealthComponent가 없습니다.");
                return false;
            }

            // 생존 상태 확인
            if (!healthComponent.IsAlive)
            {
                Debug.LogWarning($"HealEffect: {unit.name}은 죽은 상태이다.");
                return false;
            }

            // 최대 체력보다 낮은 체력인지 확인
            if (healthComponent.CurrentHealth >= healthComponent.MaxHealth)
            {
                Debug.LogWarning($"HealEffect: {unit.name}은 이미 최대 체력이다.");
                return false; // 이미 최대 체력
            }

            return true;
        }

        /// <summary>
        /// 유닛에게 실제 회복을 적용합니다.
        /// </summary>
        private void ApplyHealToUnit(GameObject unit, int healAmount)
        {
            if (unit == null)
            {
                Debug.LogWarning("HealEffect: 회복 대상 유닛이 null입니다.");
                return;
            }

            var healthComponent = unit.GetComponent<IHealthComponent>();
            if (healthComponent == null)
            {
                Debug.LogWarning($"HealEffect: {unit.name}에 HealthComponent가 없습니다.");
                return;
            }

            // 실제 회복량 계산 (오버힐 방지)
            var actualHealAmount = CalculateActualHealAmount(unit, healAmount);

            if (actualHealAmount <= 0)
            {
                Debug.Log($"HealEffect: {unit.name}은(는) 이미 최대 체력입니다.");
                return;
            }

            // 실제 체력 증가
            healthComponent.Heal(actualHealAmount);

            Debug.Log($"HealEffect: {unit.name}을(를) {actualHealAmount}만큼 회복 (요청: {healAmount})");
        }

        /// <summary>
        /// 실제 회복량을 계산합니다 (오버힐 방지).
        /// </summary>
        private int CalculateActualHealAmount(GameObject unit, int requestedHeal)
        {
            if (unit == null || requestedHeal <= 0) return 0;

            var healthComponent = unit.GetComponent<IHealthComponent>();
            if (healthComponent == null) return 0;

            // 현재 체력과 최대 체력 확인
            var currentHealth = healthComponent.CurrentHealth;
            var maxHealth = healthComponent.MaxHealth;

            // 오버힐 방지: 최대 체력을 초과하지 않도록 계산
            var actualHeal = Mathf.Min(requestedHeal, maxHealth - currentHealth);

            return Mathf.Max(0, actualHeal);
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
                Debug.Log($"HealEffect: 애니메이션 재생 - {_effectData.EffectAnimation}");
            }
        }

        public override string ToString()
        {
            return $"HealEffect[Value: {_effectData?.Value}, AffectedType: {_effectData?.AffectedType}, Range: {_effectData?.AffectedRange}]";
        }
    }
}