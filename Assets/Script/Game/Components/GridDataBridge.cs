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
        
        // 브리지 연산 - 기존 시스템과 새 시스템 연결
        
        /// <summary>
        /// 유닛 배치 (레거시 API)
        /// </summary>
        public bool PlaceUnit(int x, int y, Unit unit) 
        {
            if (unit == null)
            {
                Debug.LogWarning($"[GridDataBridge] Attempting to place null unit at ({x}, {y})");
                return false;
            }
            
            var pos = new Vector2Int(x, y);
            return gridState.SetUnitPosition(unit.gameObject, pos);
        }
        
        /// <summary>
        /// 유닛 제거 (레거시 API)
        /// </summary>
        public void RemoveUnit(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            var unit = gridState.GetUnitAtPosition(pos);
            if (unit != null)
                gridState.RemoveUnit(unit);
        }
        
        /// <summary>
        /// 위치 유효성 검사 (레거시 API)
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return gridState.IsValidPosition(new Vector2Int(x, y));
        }
        
        /// <summary>
        /// 유닛 배치 가능성 검사 (레거시 API)
        /// </summary>
        public bool CanPlaceUnitAt(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            return gridState.IsValidPosition(pos) && 
                   !gridState.IsPositionOccupied(pos) && 
                   !gridState.IsPositionBlocked(pos);
        }
        
        /// <summary>
        /// 위치 점유 상태 확인 (레거시 API)
        /// </summary>
        public bool IsOccupied(int x, int y)
        {
            return gridState.IsPositionOccupied(new Vector2Int(x, y));
        }
        
        /// <summary>
        /// 위치의 유닛 반환 (레거시 API)
        /// </summary>
        public Unit GetUnitAt(int x, int y)
        {
            var pos = new Vector2Int(x, y);
            var unitGameObject = gridState.GetUnitAtPosition(pos);
            
            if (unitGameObject != null)
            {
                return unitGameObject.GetComponent<Unit>();
            }
            
            return null;
        }
        
        /// <summary>
        /// 유닛의 위치 반환 (레거시 API)
        /// </summary>
        public Vector2Int GetUnitPosition(Unit unit)
        {
            if (unit == null)
                return new Vector2Int(-1, -1);
                
            return gridState.GetUnitPosition(unit.gameObject);
        }
        
        /// <summary>
        /// 타일 차단 설정 (레거시 API)
        /// </summary>
        public void SetTileBlocked(int x, int y, bool blocked)
        {
            gridState.SetTileBlocked(new Vector2Int(x, y), blocked);
        }
        
        /// <summary>
        /// 타일 하이라이트 설정 (레거시 API)
        /// </summary>
        public void SetTileHighlight(int x, int y, Color color)
        {
            gridState.SetTileHighlight(new Vector2Int(x, y), color);
        }
        
        /// <summary>
        /// 좌표 변환 - 그리드 → 월드 (레거시 API)
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            return gridState.GridToWorldPosition(new Vector2Int(x, y));
        }
        
        /// <summary>
        /// 좌표 변환 - 월드 → 그리드 (레거시 API)
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return gridState.WorldToGridPosition(worldPosition);
        }
        
        /// <summary>
        /// 범위 내 위치들 반환 (레거시 API)
        /// </summary>
        public List<Vector2Int> GetPositionsInRange(int centerX, int centerY, int range, bool includeOccupied = true)
        {
            return gridState.GetPositionsInRange(new Vector2Int(centerX, centerY), range, includeOccupied);
        }
        
        /// <summary>
        /// 범위 내 유닛들 반환 (레거시 API에서 Unit 타입으로 변환)
        /// </summary>
        public List<Unit> GetUnitsInRange(int centerX, int centerY, int range)
        {
            var gameObjects = gridState.GetUnitsInRange(new Vector2Int(centerX, centerY), range);
            var units = new List<Unit>();
            
            foreach (var gameObject in gameObjects)
            {
                var unit = gameObject.GetComponent<Unit>();
                if (unit != null)
                {
                    units.Add(unit);
                }
            }
            
            return units;
        }
        
        /// <summary>
        /// 경로 탐색 (레거시 API - IGridManager 필요)
        /// </summary>
        public List<Vector2Int> FindPath(int startX, int startY, int endX, int endY, Unit movingUnit = null)
        {
            // GridController가 필요하므로 ServiceLocator를 통해 접근
            var gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager != null)
            {
                var start = new Vector2Int(startX, startY);
                var end = new Vector2Int(endX, endY);
                var unitGameObject = movingUnit?.gameObject;
                
                return gridManager.FindPath(start, end, unitGameObject);
            }
            
            return new List<Vector2Int>();
        }
        
        /// <summary>
        /// 유닛 이동 가능성 검사 (레거시 API)
        /// </summary>
        public bool CanMoveUnit(Unit unit, int targetX, int targetY)
        {
            if (unit == null)
                return false;
                
            var gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager != null)
            {
                return gridManager.CanMoveUnit(unit.gameObject, new Vector2Int(targetX, targetY));
            }
            
            return false;
        }
        
        /// <summary>
        /// 유닛 이동 (레거시 API)
        /// </summary>
        public bool MoveUnit(Unit unit, int targetX, int targetY)
        {
            if (unit == null)
                return false;
                
            var gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager != null)
            {
                return gridManager.MoveUnit(unit.gameObject, new Vector2Int(targetX, targetY));
            }
            
            return false;
        }
        
        /// <summary>
        /// 그리드 크기 반환 (레거시 API)
        /// </summary>
        public Vector2Int GetGridSize()
        {
            return gridState.GridSize;
        }
        
        /// <summary>
        /// 타일 크기 반환 (레거시 API)
        /// </summary>
        public float GetTileSize()
        {
            return gridState.TileSize;
        }
        
        /// <summary>
        /// 등록된 모든 타일 반환 (레거시 호환용)
        /// </summary>
        public Dictionary<Vector2Int, Tile> GetAllTiles()
        {
            return new Dictionary<Vector2Int, Tile>(tileObjects);
        }
        
        /// <summary>
        /// Tile[,] 배열 형태로 반환 (최대 레거시 호환성)
        /// </summary>
        public Tile[,] GetTilesArray()
        {
            var gridSize = gridState.GridSize;
            var tiles = new Tile[gridSize.x, gridSize.y];
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    tiles[x, y] = tileObjects.GetValueOrDefault(position);
                }
            }
            
            return tiles;
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