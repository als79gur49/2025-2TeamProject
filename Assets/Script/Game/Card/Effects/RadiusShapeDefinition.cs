using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// 기존 AffectedRange와 동일한 맨해튼 거리 기반 다이아몬드 범위를 구현한 Shape
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Shapes/RadiusShape")]
    public class RadiusShapeDefinition : AreaShapeDefinition
    {
        [SerializeField] private int range = 0;

        public int Range => range;

        public override IEnumerable<Tile> GetTiles(Vector2Int center, IGridController gridController)
        {
            var tiles = new List<Tile>();

            if (gridController == null)
            {
                return tiles;
            }

            if (range == 0)
            {
                var tile = gridController.GetTileAtPosition(center);
                if (tile != null) tiles.Add(tile);
                return tiles;
            }

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    int manhattan = Mathf.Abs(x) + Mathf.Abs(y);
                    if (manhattan > range) continue;

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

        public override string GetRangeDescription()
        {
            if (range <= 0)
            {
                return "해당 타일만";
            }

            return $"주변 {range}칸";
        }
    }
}
