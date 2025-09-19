using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// [DEPRECATED - Phase 3] 레거시 시스템과 새 시스템 간의 브리지
    /// Phase 1-2: 하위 호환성을 위한 어댑터 패턴 구현
    /// Phase 3에서는 직접 IGridServices 및 인터페이스 사용을 권장합니다.
    /// </summary>
    [System.Obsolete("GridDataBridge is deprecated in Phase 3. Use ServiceLocator.Get<IGridServices>() and direct interface usage instead.", false)]
    public class GridDataBridge
    {
        private readonly IGridState gridState;
        private readonly Dictionary<Vector2Int, Tile> tileObjects;
        
        public GridDataBridge(IGridState state)
        {
            gridState = state ?? throw new System.ArgumentNullException(nameof(state));
            tileObjects = new Dictionary<Vector2Int, Tile>();
        }
        
        /// <summary>
        /// 타일 반환 (int 좌표)
        /// </summary>
        public Tile GetTile(int x, int y) => GetTile(new Vector2Int(x, y));
        
        /// <summary>
        /// 타일 반환 (Vector2Int 좌표)
        /// </summary>
        public Tile GetTile(Vector2Int position)
        {
            return tileObjects.GetValueOrDefault(position);
        }
        
        /// <summary>
        /// 타일 등록 (GridRenderer가 생성한 타일을 브리지에 등록)
        /// </summary>
        public void RegisterTile(Vector2Int position, Tile tile)
        {
            if (tile == null)
            {
                Debug.LogWarning($"[GridDataBridge] Attempting to register null tile at {position}");
                return;
            }
            
            tileObjects[position] = tile;
        }
        
        /// <summary>
        /// 타일 등록 해제
        /// </summary>
        public void UnregisterTile(Vector2Int position)
        {
            tileObjects.Remove(position);
        }
        
        
        /// <summary>
        /// 디버깅 정보
        /// </summary>
        public override string ToString()
        {
            return $"GridDataBridge[RegisteredTiles:{tileObjects.Count}, GridSize:{gridState.GridSize}]";
        }
        
        /// <summary>
        /// 정리 작업
        /// </summary>
        public void Cleanup()
        {
            tileObjects.Clear();
        }
    }
}