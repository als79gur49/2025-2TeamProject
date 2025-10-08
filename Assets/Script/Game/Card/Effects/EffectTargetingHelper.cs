using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Interfaces;
using Game.Services;

namespace Game.Card.Effects
{
    /// <summary>
    /// 타일 기반 통합 타겟팅 시스템
    /// 핵심 원칙: "모든 타겟팅은 타일을 반환하고, 효과는 타일에 적용된다"
    /// VFX 시스템: 타일 위치(Vector3Int)를 VFX 재생 좌표로 사용
    /// </summary>
    public static class EffectTargetingHelper
    {
        #region Public API - 타일 기반 VFX 통합

        /// <summary>
        /// [타일 기반] EffectData 기반으로 대상 타일들을 반환
        /// SpellEffectExecutor.CalculateAllPotentialTiles()에서 호출됨
        /// </summary>
        /// <param name="center">중심 위치 (카드 드롭 위치)</param>
        /// <param name="effectData">효과 데이터 (AffectedType, AffectedRange)</param>
        /// <param name="context">게임 컨텍스트 (GridController, CasterTeam)</param>
        /// <returns>필터링된 타겟 타일 리스트 (동기 반환)</returns>
        public static List<Tile> GetTargetTiles(
            Vector2Int center,
            EffectData effectData,
            GameContext context)
        {
            if (context?.GridController == null)
            {
                Debug.LogError("[EffectTargeting] GridController null");
                return new List<Tile>();
            }

            // 1. 범위 내 모든 타일 수집 (동기)
            var tilesInRange = GetTilesInRange(
                center,
                effectData.AffectedRange,
                context.GridController);

            // 2. AffectedType으로 타일 필터링 (동기)
            return FilterTilesByAffectedType(
                tilesInRange,
                effectData.AffectedType,
                context.CasterTeam);
        }

        /// <summary>
        /// [타일 기반] 타일 리스트를 VFX 재생용 월드 좌표 리스트로 변환
        /// </summary>
        /// <param name="tiles">타겟 타일 리스트</param>
        /// <returns>VFX 재생에 사용할 Vector3 위치 리스트</returns>
        public static List<Vector3> TilesToWorldPositions(List<Tile> tiles)
        {
            return tiles.Select(t => t.transform.position).ToList();
        }

        /// <summary>
        /// [타일 기반] 타일 리스트를 그리드 좌표 리스트로 변환
        /// </summary>
        /// <param name="tiles">타겟 타일 리스트</param>
        /// <returns>Grid 좌표 리스트 (Vector3Int)</returns>
        public static List<Vector3Int> TilesToGridPositions(List<Tile> tiles)
        {
            return tiles.Select(t =>
            {
                var pos = t.GetGridPosition();
                return new Vector3Int(pos.x, pos.y, 0);
            }).ToList();
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 중심 위치에서 지정된 범위 내의 모든 타일 반환 (동기)
        /// 맨해튼 거리 기반 다이아몬드 형태 범위 계산
        /// </summary>
        private static List<Tile> GetTilesInRange(
            Vector2Int center,
            int range,
            IGridController gridController)
        {
            var tiles = new List<Tile>();

            // Range 0: 단일 타일
            if (range == 0)
            {
                var tile = gridController.GetTileAtPosition(center);
                if (tile != null) tiles.Add(tile);
                return tiles;
            }

            // Range 1+: 맨해튼 거리 기반 다이아몬드 형태
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    // 맨해튼 거리 계산 (문제 2 해결: 대각선 제외)
                    int manhattanDistance = Mathf.Abs(x) + Mathf.Abs(y);
                    if (manhattanDistance > range) continue;

                    var pos = center + new Vector2Int(x, y);
                    var tile = gridController.GetTileAtPosition(pos);

                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// AffectedType에 따라 타일 필터링 (동기)
        /// 타일 기반 설계: 모든 필터링은 타일의 상태를 기준으로 수행
        /// </summary>
        private static List<Tile> FilterTilesByAffectedType(
            List<Tile> tiles,
            AffectedType affectedType,
            TeamType casterTeam)
        {
            return affectedType switch
            {
                // None: 필터링 없음 - Range 내 모든 타일 반환
                AffectedType.None => tiles,

                // Ally: 아군 유닛이 있는 타일만
                AffectedType.Ally => tiles.Where(t =>
                    t.OccupyingUnit != null &&
                    IsSameTeam(t.OccupyingUnit, casterTeam)
                ).ToList(),

                // Enemy: 적군 유닛이 있는 타일만
                AffectedType.Enemy => tiles.Where(t =>
                    t.OccupyingUnit != null &&
                    !IsSameTeam(t.OccupyingUnit, casterTeam)
                ).ToList(),

                // Any: 유닛이 있는 모든 타일 (팀 무관)
                AffectedType.Any => tiles.Where(t =>
                    t.OccupyingUnit != null
                ).ToList(),

                // NotAny: 유닛이 없는 빈 타일만
                AffectedType.NotAny => tiles.Where(t =>
                    t.OccupyingUnit == null
                ).ToList(),

                _ => tiles
            };
        }

        /// <summary>
        /// 유닛이 시전자와 같은 팀인지 확인
        /// </summary>
        private static bool IsSameTeam(Unit unit, TeamType casterTeam)
        {
            if (unit == null) return false;

            TeamType unitTeam = unit.IsPlayerUnit ? TeamType.Player : TeamType.Enemy;
            return unitTeam == casterTeam;
        }

        #endregion
    }
}
