using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// TileBased MultiStat 버프/디버프 카드 효과
    /// - SpellEffectExecutor가 미리 계산한 PredeterminedTiles를 사용합니다.
    /// - 각 유닛에게 TeamStatBuffEffect를 부착하여 스탯 변경을 위임합니다.
    /// - VFX는 SpellEffectExecutor에서 처리하므로 여기서는 TriggerData를 선택 로직에만 활용합니다.
    /// </summary>
    public class MultiStatBuffEffect : IVFXAwareEffect
    {
        private readonly MultiStatBuffEffectDefinition _definition;
        private bool _hasExecutedWithVFX;

        public EffectType EffectType => EffectType.Buff;
        public int Priority => _definition?.Priority ?? 0;

        public MultiStatBuffEffect(MultiStatBuffEffectDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _hasExecutedWithVFX = false;
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("[MultiStatBuffEffect] Invalid definition");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("[MultiStatBuffEffect] Invalid GameContext");
                return false;
            }

            if (_definition.HealthDelta == 0 &&
                _definition.AttackDelta == 0 &&
                _definition.MovementDelta == 0)
            {
                Debug.LogWarning("[MultiStatBuffEffect] All stat deltas are zero");
                return false;
            }

            if (_definition.MaxTargets <= 0)
            {
                Debug.LogWarning("[MultiStatBuffEffect] MaxTargets must be greater than zero");
                return false;
            }

            return context.PredeterminedTiles != null &&
                   context.PredeterminedTiles.Count > 0;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            // VFX 없이도 동작 가능하지만, PredeterminedTiles는 OriginPosition을 기준으로 계산되므로
            // 중심 좌표로 context.OriginPosition을 사용합니다.
            ApplyBuffs(context.OriginPosition, context);
        }

        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (_hasExecutedWithVFX)
            {
                // 다중 타겟 VFX에서 여러 TriggerData가 들어오더라도 한 번만 버프를 적용합니다.
                return;
            }

            _hasExecutedWithVFX = true;
            ApplyBuffs(context.OriginPosition, context);
        }

        private void ApplyBuffs(Vector2Int center, GameContext context)
        {
            if (!CanExecute(center, context))
            {
                Debug.LogWarning("[MultiStatBuffEffect] Cannot execute");
                return;
            }

            var tiles = context.PredeterminedTiles;
            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogWarning("[MultiStatBuffEffect] No predetermined tiles");
                return;
            }

            // 카드 사용 위치(context.OriginPosition)를 기준으로 거리 정렬
            var candidates = new List<(Unit unit, Tile tile, int distance)>();

            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                var unit = tile.OccupyingUnit;
                if (unit == null || !unit.IsAlive) continue;

                var pos = tile.GetGridPosition();
                int dist = Mathf.Abs(pos.x - center.x) + Mathf.Abs(pos.y - center.y);
                candidates.Add((unit, tile, dist));
            }

            if (candidates.Count == 0)
            {
                Debug.Log("[MultiStatBuffEffect] No valid unit targets");
                return;
            }

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            int appliedCount = 0;
            int maxTargets = Mathf.Max(0, _definition.MaxTargets);

            foreach (var (unit, _, _) in candidates)
            {
                if (appliedCount >= maxTargets)
                    break;

                var buff = new Game.Components.Abilities.TeamStatBuffEffect(
                    owner: unit,
                    healthDelta: _definition.HealthDelta,
                    attackDelta: _definition.AttackDelta,
                    movementDelta: _definition.MovementDelta,
                    durationTurns: _definition.BuffDurationTurns,
                    priority: Priority,
                    revertOnExpire: _definition.RevertOnExpire
                );

                unit.AddEffect(buff);
                appliedCount++;
            }

            Debug.Log($"[MultiStatBuffEffect] Applied buff to {appliedCount} unit(s)");
        }
    }
}
