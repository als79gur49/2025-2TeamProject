using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;
using Game.Services;
using Game.VFX;
using Game.Core;

namespace Game.Card.Effects
{
    /// <summary>
    /// 타일 기반으로 선택된 유닛들을 해당 팀의 손패로 되돌리는 효과
    /// </summary>
    public class ReturnUnitsToHandEffect : IVFXAwareEffect
    {
        private readonly ReturnUnitsToHandEffectDefinition _definition;
        private bool _hasExecutedWithVFX;

        public EffectType EffectType => EffectType.ReturnToHand;
        public int Priority => _definition?.Priority ?? 0;

        public ReturnUnitsToHandEffect(ReturnUnitsToHandEffectDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _hasExecutedWithVFX = false;
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("[ReturnUnitsToHandEffect] Invalid definition");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("[ReturnUnitsToHandEffect] Invalid GameContext");
                return false;
            }

            if (context.PredeterminedTiles == null || context.PredeterminedTiles.Count == 0)
            {
                Debug.LogWarning("[ReturnUnitsToHandEffect] No predetermined tiles");
                return false;
            }

            var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
            if (cardServiceManager == null)
            {
                Debug.LogError("[ReturnUnitsToHandEffect] ICardServiceManager not found in ServiceLocator");
                return false;
            }

            return true;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            ApplyReturnToHand(context.OriginPosition, context);
        }

        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[ReturnUnitsToHandEffect] Attack failed: {triggerData.ValidationFailureReason}");
                return;
            }

            if (_hasExecutedWithVFX)
            {
                // 여러 TriggerData에 대해 중복 실행 방지
                return;
            }

            _hasExecutedWithVFX = true;
            ApplyReturnToHand(context.OriginPosition, context);
        }

        private void ApplyReturnToHand(Vector2Int center, GameContext context)
        {
            if (!CanExecute(center, context))
            {
                Debug.LogWarning("[ReturnUnitsToHandEffect] Cannot execute");
                return;
            }

            var tiles = context.PredeterminedTiles;
            if (tiles == null || tiles.Count == 0)
            {
                Debug.LogWarning("[ReturnUnitsToHandEffect] No predetermined tiles");
                return;
            }

            var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
            if (cardServiceManager == null)
            {
                Debug.LogError("[ReturnUnitsToHandEffect] ICardServiceManager not found in ServiceLocator");
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
                Debug.Log("[ReturnUnitsToHandEffect] No valid unit targets");
                return;
            }

            // 중심에 가까운 유닛부터 우선적으로 처리
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

            int max = _definition.MaxUnitsToReturn <= 0
                ? int.MaxValue
                : _definition.MaxUnitsToReturn;

            int returnedCount = 0;

            foreach (var (unit, _, _) in candidates)
            {
                if (returnedCount >= max)
                    break;

                if (TryReturnSingleUnitToHand(unit, cardServiceManager))
                {
                    returnedCount++;
                }
            }

            Debug.Log($"[ReturnUnitsToHandEffect] Returned {returnedCount} unit(s) to hand");
        }

        private bool TryReturnSingleUnitToHand(Unit unit, ICardServiceManager cardServiceManager)
        {
            if (unit == null || cardServiceManager == null)
            {
                return false;
            }

            var link = unit.GetComponent<UnitCardLink>();
            if (link == null || link.SourceCard == null)
            {
                Debug.Log($"[ReturnUnitsToHandEffect] Unit {unit.name} has no SourceCard; skipping return");
                return false;
            }

            cardServiceManager.ReturnUnitToHand(unit);
            return true;
        }
    }
}
