using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// Chebyshev 거리 기반 정사각형 범위를 정의하는 Shape
    /// range = 0: 중심 타일 1개
    /// range = 1: 3x3 = 9타일
    /// range = 2: 5x5 = 25타일 ...
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Shapes/Square")]
    public class SquareShapeDefinition : AreaShapeDefinition
    {
        [SerializeField] [Min(0)] private int range = 0;

        public int Range => range;

        public override IEnumerable<Tile> GetTiles(Vector2Int center, IGridController gridController)
        {
            var tiles = new List<Tile>();

            if (gridController == null)
            {
                return tiles;
            }

            if (range < 0)
            {
                return tiles;
            }

            // GridController의 Chebyshev 기반 정사각형 범위 유틸리티 사용
            return gridController.GetTilesInRange(center, range);
        }

        public override string GetRangeDescription()
        {
            if (range <= 0)
            {
                return "해당 타일만";
            }

            int size = range * 2 + 1;
            return $"주변 {size}x{size} 정사각형";
        }
    }
}

