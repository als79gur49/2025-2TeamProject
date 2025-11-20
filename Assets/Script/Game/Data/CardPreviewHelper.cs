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

            if (cardData == null || !cardData.IsEffectBasedCard || gridManager == null)
                return positions;

            // IGridManager를 통해 실제 IGridController를 가져와서 AreaShape 계산에 사용
            var gridController = gridManager.GetGridController();
            if (gridController == null)
            {
                Debug.LogWarning("[CardPreviewHelper] GridController is null, cannot compute preview tiles");
                return positions;
            }

            // EffectDefinition 기반: 각 정의의 AreaShape를 사용하여 범위 계산
            foreach (var def in cardData.EffectDefinitions)
            {
                if (def == null || def.AreaShape == null) continue;

                var tiles = def.AreaShape.GetTiles(centerPos, gridController);
                foreach (var tile in tiles)
                {
                    if (tile == null) continue;
                    var pos = tile.GetGridPosition();
                    if (!positions.Contains(pos))
                        positions.Add(pos);
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
