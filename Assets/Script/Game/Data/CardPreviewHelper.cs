using System.Collections.Generic;
using UnityEngine;
using Game.Card.Effects;
using Game.Interfaces;

namespace Game.Data
{
    /// <summary>
    /// 카드 프리뷰를 위한 영향 범위 계산 헬퍼
    /// SpellEffectExecutor.FilterTriggersForEffect()의 범위 계산 로직 재사용
    /// 맨하탄 거리 기반 정확한 범위 계산을 통해 실제 게임 로직과 100% 일치
    /// </summary>
    public static class CardPreviewHelper
    {
        /// <summary>
        /// 카드의 모든 영향 범위 위치 계산
        /// SpellEffectExecutor Line 311-325의 실제 범위 체크 로직 사용
        /// </summary>
        /// <param name="cardData">카드 데이터</param>
        /// <param name="centerPos">중심 위치 (드롭 위치)</param>
        /// <param name="gridManager">그리드 관리자</param>
        /// <returns>영향받는 모든 타일 위치 리스트</returns>
        public static List<Vector2Int> CalculateAffectedPositions(
            CardData cardData,
            Vector2Int centerPos,
            IGridManager gridManager)
        {
            var positions = new List<Vector2Int>();

            if (!cardData.IsEffectBasedCard || gridManager == null)
                return positions;

            // 각 효과별로 범위 계산
            foreach (var effectData in cardData.EffectDataList)
            {
                var effectPositions = CalculateRangeForEffect(
                    effectData,
                    centerPos,
                    gridManager);

                // 중복 제거하며 추가
                foreach (var pos in effectPositions)
                {
                    if (!positions.Contains(pos))
                        positions.Add(pos);
                }
            }

            return positions;
        }

        /// <summary>
        /// 특정 효과의 영향 범위 계산
        /// ⚠️ SpellEffectExecutor.FilterTriggersForEffect() Line 314-325와 동일한 로직
        /// </summary>
        /// <param name="effectData">효과 데이터</param>
        /// <param name="centerPos">중심 위치</param>
        /// <param name="gridManager">그리드 관리자</param>
        /// <returns>해당 효과의 영향을 받는 타일 위치 리스트</returns>
        private static List<Vector2Int> CalculateRangeForEffect(
            EffectData effectData,
            Vector2Int centerPos,
            IGridManager gridManager)
        {
            var positions = new List<Vector2Int>();
            var gridSize = gridManager.GridSize;

            // Range 0: 단일 타겟 (중심 위치만)
            // SpellEffectExecutor Line 315-318
            if (effectData.AffectedRange == 0)
            {
                if (gridManager.IsValidPosition(centerPos))
                {
                    positions.Add(centerPos);
                }
                return positions;
            }

            // Range 1+: 맨하탄 거리 기반 범위 계산
            // ✅ SpellEffectExecutor Line 319-325의 실제 로직
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int tilePos2D = new Vector2Int(x, y);

                    // 맨하탄 거리 계산 (SpellEffectExecutor Line 322-324와 동일)
                    int distance = Mathf.Abs(tilePos2D.x - centerPos.x) +
                                   Mathf.Abs(tilePos2D.y - centerPos.y);

                    // 범위 내에 있는지 확인 (SpellEffectExecutor Line 325)
                    if (distance <= effectData.AffectedRange)
                    {
                        positions.Add(tilePos2D);
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// 유효성 검사를 통한 위치 분류
        /// EffectTargetingHelper 및 SpawnValidator 활용
        /// </summary>
        /// <param name="cardData">카드 데이터</param>
        /// <param name="centerPos">중심 위치</param>
        /// <param name="gridManager">그리드 관리자</param>
        /// <param name="validator">스폰 검증기</param>
        /// <returns>(유효한 위치 리스트, 무효한 위치 리스트)</returns>
        public static (List<Vector2Int> valid, List<Vector2Int> invalid)
            ValidateAffectedPositions(
                CardData cardData,
                Vector2Int centerPos,
                IGridManager gridManager,
                ISpawnValidator validator,
                bool isPlayerUnit)
        {
            var valid = new List<Vector2Int>();
            var invalid = new List<Vector2Int>();

            if (validator == null)
            {
                // 검증기가 없으면 모두 무효로 처리
                var tempPositions = CalculateAffectedPositions(cardData, centerPos, gridManager);
                invalid.AddRange(tempPositions);
                return (valid, invalid);
            }

            var allPositions = CalculateAffectedPositions(cardData, centerPos, gridManager);

            foreach (var pos in allPositions)
            {
                bool isValid = false;

                isValid = validator.CanUseCard(cardData, pos, true);

                if (isValid)
                    valid.Add(pos);
                else
                    invalid.Add(pos);
            }

            return (valid, invalid);
        }

        /// <summary>
        /// 두 위치 간 맨하탄 거리 계산
        /// SpellEffectExecutor Line 322-324와 동일
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>맨하탄 거리</returns>
        public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// 특정 위치가 효과 범위 내에 있는지 확인
        /// SpellEffectExecutor Line 314-327 로직
        /// </summary>
        /// <param name="centerPos">중심 위치</param>
        /// <param name="checkPos">확인할 위치</param>
        /// <param name="range">범위</param>
        /// <returns>범위 내 포함 여부</returns>
        public static bool IsPositionInRange(
            Vector2Int centerPos,
            Vector2Int checkPos,
            int range)
        {
            if (range == 0)
            {
                return centerPos == checkPos;
            }

            int distance = CalculateManhattanDistance(centerPos, checkPos);
            return distance <= range;
        }
    }
}
