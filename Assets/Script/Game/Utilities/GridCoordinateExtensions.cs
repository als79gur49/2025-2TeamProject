using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// 좌표계 통합을 위한 확장 메서드
    /// </summary>
    public static class GridCoordinateExtensions
    {
        // 레거시 지원 메서드
        public static Vector2Int ToGridPosition(int x, int y) => new Vector2Int(x, y);
        
        public static void Deconstruct(this Vector2Int pos, out int x, out int y) 
        {
            x = pos.x;
            y = pos.y;
        }
        
        // Tile 통합
        public static Vector2Int GetGridPosition(this Tile tile) => new Vector2Int(tile.X, tile.Y);
        
        public static void SetGridPosition(this Tile tile, Vector2Int position)
        {
            tile.Initialize(position.x, position.y);
        }
    }
}