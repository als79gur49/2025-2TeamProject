using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// TileBased 스턴 카드 효과
    /// - PredeterminedTiles에서 유닛을 골라 Unit.AddStun()을 호출합니다.
    /// - 실제 스턴 상태 유지와 행동 제한은 StunStatusEffect가 담당합니다.
    /// </summary>
    public class StunEffect : IVFXAwareEffect
    {
        private readonly StunEffectDefinition _definition;
        private bool _hasExecutedWithVFX;

        public EffectType EffectType => EffectType.Stun;
        public int Priority => _definition?.Priority ?? 0;

        public StunEffect(StunEffectDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _hasExecutedWithVFX = false;
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("[StunEffect] Invalid definition");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("[StunEffect] Invalid GameContext");
                return false;
            }

            if (_definition.StunTurns <= 0 || _definition.MaxTargets <= 0)
            {
                Debug.LogWarning("[StunEffect] Invalid stun configuration");
                return false;
            }

            return context.PredeterminedTiles != null &&
                   context.PredeterminedTiles.Count > 0;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            ApplyStun(context.OriginPosition, context);
        }

        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[StunEffect] Attack failed: {triggerData.ValidationFailureReason}");
                return;
            }

            if (_hasExecutedWithVFX)
            {
                // 여러 TriggerData에 대해 중복으로 스턴을 적용하지 않도록 한 번만 실행
                return;
            }

            _hasExecutedWithVFX = true;
            ApplyStun(context.OriginPosition, context);
        }

        private void ApplyStun(Vector2Int center, GameContext context)
        {
            if (!CanExecute(center, context))
            {
                Debug.LogWarning("[StunEffect] Cannot execute");
                return;
            }

            var tiles = context.PredeterminedTiles;
            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogWarning("[StunEffect] No predetermined tiles");
                return;
            }

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
                Debug.Log("[StunEffect] No valid unit targets");
                return;
            }

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            int appliedCount = 0;
            int maxTargets = Mathf.Max(0, _definition.MaxTargets);
            int stunTurns = Mathf.Max(1, _definition.StunTurns);

            foreach (var (unit, _, _) in candidates)
            {
                if (appliedCount >= maxTargets)
                    break;

                unit.AddStun(stunTurns);
                appliedCount++;
            }

            Debug.Log($"[StunEffect] Applied stun to {appliedCount} unit(s) for {stunTurns} turn(s)");
        }
    }
}
