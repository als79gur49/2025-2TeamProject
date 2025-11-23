using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Components;
using Game.Components.Abilities;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// TileBased 카드 효과:
    /// - SpellEffectExecutor가 계산한 PredeterminedTiles를 기준으로
    ///   유닛에게 FrenzyOnKillEffect를 부여하여
    ///   일정 턴 동안 "적 처치 시 추가 행동"을 가능하게 합니다.
    /// </summary>
    public class FrenzyBuffEffect : IVFXAwareEffect
    {
        private readonly FrenzyBuffEffectDefinition _definition;
        private bool _hasExecutedWithVFX;

        public EffectType EffectType => EffectType.Buff;
        public int Priority => _definition?.Priority ?? 0;

        public FrenzyBuffEffect(FrenzyBuffEffectDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _hasExecutedWithVFX = false;
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("[FrenzyBuffEffect] Invalid definition");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("[FrenzyBuffEffect] Invalid GameContext");
                return false;
            }

            if (_definition.MaxTargets <= 0)
            {
                Debug.LogWarning("[FrenzyBuffEffect] MaxTargets must be greater than zero");
                return false;
            }

            // -1: 무한, 1 이상만 유효한 지속 턴으로 간주
            if (_definition.FrenzyDurationTurns == 0)
            {
                Debug.LogWarning("[FrenzyBuffEffect] FrenzyDurationTurns must not be zero");
                return false;
            }

            var tiles = context.PredeterminedTiles;
            if (tiles == null || tiles.Count == 0)
            {
                return false;
            }

            // 유효한 유닛이 하나라도 있는지 확인
            foreach (var tile in tiles)
            {
                if (tile == null) continue;

                var unit = tile.OccupyingUnit;
                if (unit != null && unit.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            ApplyFrenzyBuffs(context.OriginPosition, context);
        }

        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (_hasExecutedWithVFX)
            {
                return;
            }

            _hasExecutedWithVFX = true;
            ApplyFrenzyBuffs(context.OriginPosition, context);
        }

        private void ApplyFrenzyBuffs(Vector2Int center, GameContext context)
        {
            if (!CanExecute(center, context))
            {
                Debug.LogWarning("[FrenzyBuffEffect] Cannot execute");
                return;
            }

            var tiles = context.PredeterminedTiles;
            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogWarning("[FrenzyBuffEffect] No predetermined tiles");
                return;
            }

            var candidates = new List<(Unit unit, Tile tile, int distance)>();

            foreach (var tile in tiles)
            {
                if (tile == null) continue;

                var unit = tile.OccupyingUnit;
                if (unit == null || !unit.IsAlive) continue;

                // 이미 광란 OnKill 효과가 있다면 중복 부여하지 않음
                if (unit.HasEffect<FrenzyOnKillEffect>())
                {
                    continue;
                }

                var pos = tile.GetGridPosition();
                int dist = Mathf.Abs(pos.x - center.x) + Mathf.Abs(pos.y - center.y);
                candidates.Add((unit, tile, dist));
            }

            if (candidates.Count == 0)
            {
                Debug.Log("[FrenzyBuffEffect] No valid unit targets for Frenzy");
                return;
            }

            // 카드 사용 위치 기준으로 가까운 유닛부터 선택
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            int appliedCount = 0;
            int maxTargets = Mathf.Max(0, _definition.MaxTargets);
            int durationTurns = _definition.FrenzyDurationTurns;

            foreach (var (unit, _, _) in candidates)
            {
                if (appliedCount >= maxTargets)
                    break;

                var frenzyEffect = new FrenzyOnKillEffect(
                    owner: unit,
                    trigger: Game.Core.Effects.EffectTrigger.OnAttack,
                    priority: Priority,
                    durationTurns: durationTurns
                );

                unit.AddEffect(frenzyEffect);
                appliedCount++;
            }

            Debug.Log($"[FrenzyBuffEffect] Applied Frenzy to {appliedCount} unit(s) for {durationTurns} turn(s)");
        }
    }
}
