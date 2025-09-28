using UnityEngine;
using System.Collections.Generic;

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
        /// 영향받을 유닛들을 찾습니다.
        /// </summary>
        private List<object> GetAffectedUnits(Vector2Int targetPos, GameContext context)
        {
            var affectedUnits = new List<object>();

            // AffectedRange가 0이면 단일 대상, 1+이면 범위 효과
            if (_effectData.AffectedRange == 0)
            {
                // 단일 대상
                var unit = GetUnitAtPosition(targetPos, context);
                if (unit != null && IsValidTarget(unit, context) && CanBeHealed(unit))
                {
                    affectedUnits.Add(unit);
                }
            }
            else
            {
                // 범위 효과
                for (int x = -_effectData.AffectedRange; x <= _effectData.AffectedRange; x++)
                {
                    for (int y = -_effectData.AffectedRange; y <= _effectData.AffectedRange; y++)
                    {
                        var checkPos = targetPos + new Vector2Int(x, y);
                        var unit = GetUnitAtPosition(checkPos, context);

                        if (unit != null && IsValidTarget(unit, context) && CanBeHealed(unit))
                        {
                            affectedUnits.Add(unit);
                        }
                    }
                }
            }

            return affectedUnits;
        }

        /// <summary>
        /// 특정 위치의 유닛을 가져옵니다.
        /// </summary>
        private object GetUnitAtPosition(Vector2Int position, GameContext context)
        {
            // TODO: UnitService를 통해 실제 유닛 정보를 가져오는 로직 구현
            // 현재는 인터페이스만 정의된 상태이므로 placeholder 반환
            return null;
        }

        /// <summary>
        /// 유닛이 효과의 유효한 대상인지 확인합니다.
        /// </summary>
        private bool IsValidTarget(object unit, GameContext context)
        {
            if (unit == null) return false;

            // TODO: 실제 유닛의 소속을 확인하는 로직 구현
            // AffectedType에 따라 아군/적군/모두 필터링
            return _effectData.AffectedType switch
            {
                AffectedType.Ally => IsAllyUnit(unit, context.PlayerId),
                AffectedType.Enemy => IsEnemyUnit(unit, context.PlayerId),
                AffectedType.Any => true,
                AffectedType.None => false,
                _ => false
            };
        }

        /// <summary>
        /// 아군 유닛인지 확인합니다.
        /// </summary>
        private bool IsAllyUnit(object unit, int playerId)
        {
            // TODO: 실제 유닛의 소속 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 적군 유닛인지 확인합니다.
        /// </summary>
        private bool IsEnemyUnit(object unit, int playerId)
        {
            // TODO: 실제 유닛의 소속 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 유닛이 회복 가능한 상태인지 확인합니다.
        /// </summary>
        private bool CanBeHealed(object unit)
        {
            // TODO: 실제 유닛의 체력 상태 확인 로직 구현
            // - 최대 체력보다 낮은 체력인가?
            // - 언데드나 기계 유닛이 아닌가?
            // - 치명상 상태가 아닌가?
            return true; // placeholder
        }

        /// <summary>
        /// 유닛에게 실제 회복을 적용합니다.
        /// </summary>
        private void ApplyHealToUnit(object unit, int healAmount)
        {
            // TODO: 실제 유닛에게 회복을 적용하는 로직 구현
            // - 현재 체력과 최대 체력 확인
            // - 회복량 계산 (오버힐 방지)
            // - 실제 체력 증가
            // - 유닛 상태 업데이트

            var actualHealAmount = CalculateActualHealAmount(unit, healAmount);
            Debug.Log($"HealEffect: 유닛을 {actualHealAmount}만큼 회복 (요청: {healAmount})");
        }

        /// <summary>
        /// 실제 회복량을 계산합니다 (오버힐 방지).
        /// </summary>
        private int CalculateActualHealAmount(object unit, int requestedHeal)
        {
            // TODO: 실제 체력 상태를 확인하여 오버힐을 방지하는 로직 구현
            // var currentHealth = GetCurrentHealth(unit);
            // var maxHealth = GetMaxHealth(unit);
            // return Mathf.Min(requestedHeal, maxHealth - currentHealth);

            return requestedHeal; // placeholder
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