using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 회복 효과 구현
    /// ICardEffect를 구현하여 대상을 회복시키는 효과입니다.
    /// VFX Dynamic Data System: IVFXAwareEffect 구현으로 공격 성공/실패 반응
    /// </summary>
    public class HealEffect : IVFXAwareEffect
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

            // 타일 기반: 사전 계산된 타일이 있는지 확인
            return context.PredeterminedTiles != null && context.PredeterminedTiles.Count > 0;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("HealEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            // 타일 기반: 사전 계산된 타일에서 유닛 찾기
            var healAmount = _effectData.Value;
            int affectedCount = 0;

            foreach (var tile in context.PredeterminedTiles)
            {
                if (tile?.OccupyingUnit != null)
                {
                    tile.OccupyingUnit.Heal(healAmount);
                    affectedCount++;
                }
            }

            Debug.Log($"HealEffect: {affectedCount}개 유닛을 {healAmount}만큼 회복시킵니다.");

            // 시각적 효과 재생
            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// VFX TriggerData를 포함한 효과 실행 (IVFXAwareEffect 구현)
        /// Phase 3.3: 타일 기반 타겟팅으로 리팩토링
        /// 공격 성공/실패 여부에 따라 회복 적용 여부 결정
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            // VFX 트리거 시점 검증
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[HealEffect] Heal failed: {triggerData.ValidationFailureReason}");
                return;
            }

            // ✅ 타일 기반: 타일 좌표로 타일 조회
            var targetTile = context.GridController.GetTileAtPosition(
                new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

            if (targetTile?.OccupyingUnit != null)
            {
                var healAmount = _effectData.Value;
                // 타일에 유닛이 있으면 힐 적용
                targetTile.OccupyingUnit.Heal(healAmount);
                Debug.Log($"[HealEffect] {healAmount} heal to unit at {triggerData.TileGridPosition}");
            }

            // VFX 재생 (타일 위치 기반)
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
        /// </summary>
        private void PlayHealEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[HealEffect] Playing heal effect at world position {worldPos}");

            // VFXData의 이펙트 프리팹 사용
            if (_effectData.EffectPrefab != null)
            {
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }
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