using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;

namespace Game.Data
{
    /// <summary>
    /// Phase 2.8: CardData AffectedRange 관련 확장 메서드들
    /// GridController나 UnitService에서 사용할 수 있는 유틸리티 메서드 모음
    /// </summary>
    public static class CardDataAffectedRangeExtensions
    {
        /// <summary>
        /// 카드의 모든 효과에 대한 최대 AffectedRange를 계산합니다
        /// </summary>
        public static int CalculateMaxAffectedRange(this CardData card)
        {
            if (!card.IsEffectBasedCard)
            {
                return 0; // 효과가 없으면 0
            }

            return card.EffectDataList.Max(e => e.AffectedRange);
        }

        /// <summary>
        /// 특정 위치에서 영향받을 수 있는 모든 위치를 계산합니다
        /// </summary>
        public static HashSet<Vector2Int> CalculateAllAffectedPositions(this CardData card, Vector2Int targetPosition)
        {
            var allPositions = new HashSet<Vector2Int>();

            if (!card.IsEffectBasedCard)
            {
                // 효과가 없으면 빈 집합 반환
                return allPositions;
            }

            // 새로운 EffectData 시스템: 각 효과별로 처리
            foreach (var effectData in card.EffectDataList)
            {
                AddPositionsInRange(allPositions, targetPosition, effectData.AffectedRange);
            }

            return allPositions;
        }

        /// <summary>
        /// 특정 효과 타입에 대한 영향받을 위치들을 계산합니다
        /// </summary>
        public static List<Vector2Int> CalculateAffectedPositionsForEffect(this CardData card, Vector2Int targetPosition, EffectType effectType)
        {
            var positions = new List<Vector2Int>();

            if (!card.IsEffectBasedCard)
            {
                // 효과가 없으면 빈 리스트 반환
                return positions;
            }

            var effectsOfType = card.GetEffectsByType(effectType);
            foreach (var effect in effectsOfType)
            {
                var effectPositions = new List<Vector2Int>();
                AddPositionsInRange(effectPositions, targetPosition, effect.AffectedRange);

                // 중복 제거하며 추가
                foreach (var pos in effectPositions)
                {
                    if (!positions.Contains(pos))
                    {
                        positions.Add(pos);
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// 두 위치 간의 거리가 특정 효과의 AffectedRange 내에 있는지 확인합니다
        /// </summary>
        public static bool IsPositionInAffectedRangeForEffect(this CardData card, Vector2Int targetPosition, Vector2Int checkPosition, EffectType effectType)
        {
            if (!card.IsEffectBasedCard)
            {
                // 효과가 없으면 false
                return false;
            }

            var effectsOfType = card.GetEffectsByType(effectType);
            return effectsOfType.Any(effect => IsWithinRange(targetPosition, checkPosition, effect.AffectedRange));
        }

        /// <summary>
        /// 카드의 모든 효과가 지정된 위치에 영향을 주는지 확인합니다
        /// </summary>
        public static bool DoesAnyEffectAffectPosition(this CardData card, Vector2Int targetPosition, Vector2Int checkPosition)
        {
            if (!card.IsEffectBasedCard)
            {
                return false; // 효과가 없으면 false
            }

            return card.EffectDataList.Any(effect => IsWithinRange(targetPosition, checkPosition, effect.AffectedRange));
        }

        /// <summary>
        /// AffectedType에 따라 특정 플레이어에게 영향을 주는지 확인합니다
        /// </summary>
        public static bool CanAffectPlayer(this CardData card, int casterPlayerId, int targetPlayerId, EffectType? effectType = null)
        {
            if (!card.IsEffectBasedCard)
            {
                // 효과가 없으면 false
                return false;
            }

            var relevantEffects = effectType.HasValue
                ? card.GetEffectsByType(effectType.Value)
                : card.EffectDataList.ToList();

            return relevantEffects.Any(effect => CanEffectAffectPlayer(effect.AffectedType, casterPlayerId, targetPlayerId));
        }

        /// <summary>
        /// 효과의 영향 범위가 유효한지 검증합니다
        /// </summary>
        public static bool ValidateAffectedRangeConfiguration(this CardData card)
        {
            if (!card.IsEffectBasedCard)
            {
                return false; // 효과가 없으면 유효하지 않음
            }

            // 모든 EffectData의 AffectedRange가 유효한지 확인
            return card.EffectDataList.All(effect => effect.AffectedRange >= 0);
        }

        // Private helper methods

        private static void AddPositionsInRange(ICollection<Vector2Int> positions, Vector2Int center, int range)
        {
            if (range == 0)
            {
                positions.Add(center);
                return;
            }

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    var pos = new Vector2Int(center.x + x, center.y + y);
                    positions.Add(pos);
                }
            }
        }

        private static void AddPositionsInRange(List<Vector2Int> positions, Vector2Int center, int range)
        {
            if (range == 0)
            {
                positions.Add(center);
                return;
            }

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    var pos = new Vector2Int(center.x + x, center.y + y);
                    positions.Add(pos);
                }
            }
        }

        private static bool IsWithinRange(Vector2Int center, Vector2Int target, int range)
        {
            if (range == 0)
            {
                return center == target;
            }

            // 맨하탄 거리 기반 계산
            int distance = Mathf.Abs(center.x - target.x) + Mathf.Abs(center.y - target.y);
            return distance <= range;
        }

        private static bool CanEffectAffectPlayer(AffectedType affectedType, int casterPlayerId, int targetPlayerId)
        {
            return affectedType switch
            {
                AffectedType.None => false,
                AffectedType.Ally => casterPlayerId == targetPlayerId,
                AffectedType.Enemy => casterPlayerId != targetPlayerId,
                AffectedType.Any => true,
                _ => false
            };
        }
    }
}