using UnityEngine;
using System.Collections.Generic;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 데미지 효과 구현
    /// ICardEffect를 구현하여 대상에게 피해를 주는 효과입니다.
    /// </summary>
    public class DamageEffect : ICardEffect
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
                ApplyDamageToUnit(unit, damageAmount);
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
        /// 특정 위치의 유닛을 가져옵니다.
        /// </summary>
        [System.Obsolete("Phase 2.12: GridController.GetUnitAtPosition()을 직접 사용하세요.")]
        private GameObject GetUnitAtPosition(Vector2Int position, GameContext context)
        {
            // TODO: UnitService를 통해 실제 유닛 정보를 가져오는 로직 구현
            // 현재는 인터페이스만 정의된 상태이므로 placeholder 반환
            return context?.GridController?.GetUnitAtPosition(position);
        }

        /// <summary>
        /// 유닛이 효과의 유효한 대상인지 확인합니다.
        /// </summary>
        [System.Obsolete("Phase 2.12: GridController.GetAffectedUnits()에서 팀 필터링이 자동으로 수행됩니다.")]
        private bool IsValidTarget(GameObject unit, GameContext context)
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
        [System.Obsolete("Phase 2.12: GridController.GetAffectedUnits()에서 팀 확인이 자동으로 수행됩니다.")]
        private bool IsAllyUnit(GameObject unit, int playerId)
        {
            // TODO: 실제 유닛의 소속 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 적군 유닛인지 확인합니다.
        /// </summary>
        [System.Obsolete("Phase 2.12: GridController.GetAffectedUnits()에서 팀 확인이 자동으로 수행됩니다.")]
        private bool IsEnemyUnit(GameObject unit, int playerId)
        {
            // TODO: 실제 유닛의 소속 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 유닛에게 실제 피해를 적용합니다.
        /// </summary>
        private void ApplyDamageToUnit(GameObject unit, int damage)
        {
            // TODO: 실제 유닛에게 피해를 적용하는 로직 구현
            // - 방어력 계산 (IgnoreArmor 옵션 고려)
            // - 실제 체력 감소
            // - 유닛 상태 업데이트

            var finalDamage = _effectData.IgnoreArmor ? damage : CalculateDamageWithArmor(unit, damage);
            Debug.Log($"DamageEffect: 유닛에게 {finalDamage} 피해 적용 (원본: {damage}, 방어력 무시: {_effectData.IgnoreArmor})");
        }

        /// <summary>
        /// 방어력을 고려한 최종 피해량을 계산합니다.
        /// </summary>
        private int CalculateDamageWithArmor(GameObject unit, int baseDamage)
        {
            // TODO: 실제 방어력 계산 로직 구현
            return baseDamage; // placeholder
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