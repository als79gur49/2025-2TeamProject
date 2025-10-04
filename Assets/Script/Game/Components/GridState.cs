using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 그리드 상태 관리 클래스 - 단일 책임 원칙 적용 (상태 관리만 담당)
    /// </summary>
    public class GridState : MonoBehaviour, IGridState
    {
        [Header("그리드 설정")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(10, 10);
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        // ✅ 그리드 상태 저장소
        private GridTileData[,] tileGrid;
        private readonly Dictionary<GameObject, Vector2Int> unitPositions = new Dictionary<GameObject, Vector2Int>();
        private readonly Dictionary<Vector2Int, GameObject> positionUnits = new Dictionary<Vector2Int, GameObject>();
        private readonly HashSet<Vector2Int> blockedPositions = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, Color> highlightedTiles = new Dictionary<Vector2Int, Color>();

        // ✅ 이벤트
        public event Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved;
        public event Action<Vector2Int, GameObject> OnUnitPlaced;
        public event Action<Vector2Int, GameObject> OnUnitRemoved;
        public event Action<Vector2Int, bool> OnTileBlockedChanged;
        public event Action<Vector2Int, Color> OnTileHighlighted;
        public event Action OnAllHighlightsCleared;

        // ✅ 속성
        public Vector2Int GridSize => gridSize;
        public float TileSize => tileSize;
        public Vector3 GridOrigin => gridOrigin;
        public int TotalTiles => gridSize.x * gridSize.y;
        public int OccupiedTiles => unitPositions.Count;
        public int BlockedTiles => blockedPositions.Count;

        private void Awake()
        {
            InitializeGrid();
        }

        /// <summary>
        /// 그리드 초기화
        /// Phase 3.5: 그리드 초기화 주석 추가
        /// 좌표계: Vector2Int(x,y) = (가로,세로), gridSize.x = 가로 크기, gridSize.y = 세로 크기
        /// </summary>
        private void InitializeGrid()
        {
            tileGrid = new GridTileData[gridSize.x, gridSize.y];

            // Phase 3.5: 변수명 명시화 (xIndex = 가로 인덱스, yIndex = 세로 인덱스)
            // 좌표계: Vector2Int(x,y) = (가로,세로)
            for (int xIndex = 0; xIndex < gridSize.x; xIndex++)
            {
                for (int yIndex = 0; yIndex < gridSize.y; yIndex++)
                {
                    var position = new Vector2Int(xIndex, yIndex);
                    tileGrid[xIndex, yIndex] = new GridTileData(position);
                }
            }

            Debug.Log($"[GridState] Initialized grid {gridSize.x}x{gridSize.y} with {TotalTiles} tiles");
        }

        /// <summary>
        /// 위치 유효성 검증
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridSize.x && 
                   position.y >= 0 && position.y < gridSize.y;
        }

        /// <summary>
        /// 위치 점유 상태 확인
        /// </summary>
        public bool IsPositionOccupied(Vector2Int position)
        {
            return IsValidPosition(position) && positionUnits.ContainsKey(position);
        }

        /// <summary>
        /// 위치 차단 상태 확인
        /// </summary>
        public bool IsPositionBlocked(Vector2Int position)
        {
            return IsValidPosition(position) && blockedPositions.Contains(position);
        }

        /// <summary>
        /// 해당 위치의 유닛 반환
        /// </summary>
        public GameObject GetUnitAtPosition(Vector2Int position)
        {
            return positionUnits.GetValueOrDefault(position);
        }

        /// <summary>
        /// 유닛의 위치 반환
        /// </summary>
        public Vector2Int GetUnitPosition(GameObject unit)
        {
            return unitPositions.GetValueOrDefault(unit, new Vector2Int(-1, -1));
        }

        /// <summary>
        /// 안전한 유닛 위치 조회
        /// </summary>
        public bool TryGetUnitPosition(GameObject unit, out Vector2Int position)
        {
            return unitPositions.TryGetValue(unit, out position);
        }

        /// <summary>
        /// 유닛 위치 설정 (내부용) - 물리적 Tile 컴포넌트와 동기화
        /// </summary>
        /// <summary>
        /// 그리드 데이터 레이어만 업데이트 (Transform 변경 없음)
        /// - unitPositions, positionUnits 딕셔너리 업데이트
        /// - tileGrid 데이터 업데이트
        /// - 물리적 Tile 컴포넌트의 논리적 상태만 업데이트 (Transform 제외)
        /// </summary>
        public bool UpdateGridDataLayer(GameObject unit, Vector2Int newPosition)
        {
            if (unit == null || !IsValidPosition(newPosition))
                return false;

            Vector2Int oldPosition = new Vector2Int(-1, -1);
            bool hadOldPosition = false;

            // 이전 위치 정리 (데이터만)
            if (unitPositions.TryGetValue(unit, out oldPosition))
            {
                hadOldPosition = true;
                positionUnits.Remove(oldPosition);
                if (IsValidPosition(oldPosition))
                {
                    tileGrid[oldPosition.x, oldPosition.y].SetOccupied(null);
                    // 이전 위치의 물리적 Tile 논리 상태만 업데이트 (Transform 제외)
                    UpdatePhysicalTileLogic(oldPosition, null);
                }
            }

            // 새 위치가 이미 점유되어 있는지 확인
            if (IsPositionOccupied(newPosition))
            {
                Debug.LogWarning($"[GridState] Position {newPosition} is already occupied");
                return false;
            }

            // 새 위치 설정 (데이터만)
            unitPositions[unit] = newPosition;
            positionUnits[newPosition] = unit;
            tileGrid[newPosition.x, newPosition.y].SetOccupied(unit);

            // 새 위치의 물리적 Tile 논리 상태만 업데이트 (Transform 제외)
            UpdatePhysicalTileLogic(newPosition, unit);

            // 이벤트 발생
            if (hadOldPosition && IsValidPosition(oldPosition))
            {
                OnUnitMoved?.Invoke(unit, oldPosition, newPosition);
            }
            else
            {
                OnUnitPlaced?.Invoke(newPosition, unit);
            }

            return true;
        }

        /// <summary>
        /// [Deprecated] 하위 호환성을 위한 래퍼 메서드
        /// 새 코드에서는 UpdateGridDataLayer() 사용 권장
        /// </summary>
        [System.Obsolete("Use UpdateGridDataLayer() instead for clearer intent", false)]
        public bool SetUnitPosition(GameObject unit, Vector2Int newPosition)
        {
            return UpdateGridDataLayer(unit, newPosition);
        }

        /// <summary>
        /// 유닛 제거 - 물리적 Tile 컴포넌트와 동기화
        /// </summary>
        public bool RemoveUnit(GameObject unit)
        {
            if (unit == null || !unitPositions.TryGetValue(unit, out var position))
                return false;

            unitPositions.Remove(unit);
            positionUnits.Remove(position);
            tileGrid[position.x, position.y].SetOccupied(null);

            // 물리적 Tile 업데이트
            UpdatePhysicalTile(position, null);

            OnUnitRemoved?.Invoke(position, unit);
            return true;
        }

        /// <summary>
        /// 타일 차단 상태 설정
        /// </summary>
        public void SetTileBlocked(Vector2Int position, bool blocked)
        {
            if (!IsValidPosition(position))
                return;

            bool wasBlocked = blockedPositions.Contains(position);
            
            if (blocked)
            {
                blockedPositions.Add(position);
            }
            else
            {
                blockedPositions.Remove(position);
            }

            if (wasBlocked != blocked)
            {
                tileGrid[position.x, position.y].SetBlocked(blocked);
                OnTileBlockedChanged?.Invoke(position, blocked);
            }
        }

        /// <summary>
        /// 타일 하이라이트 설정
        /// </summary>
        public void SetTileHighlight(Vector2Int position, Color highlightColor)
        {
            if (!IsValidPosition(position))
                return;

            highlightedTiles[position] = highlightColor;
            tileGrid[position.x, position.y].SetHighlight(highlightColor);
            OnTileHighlighted?.Invoke(position, highlightColor);
        }

        /// <summary>
        /// 모든 하이라이트 정리
        /// </summary>
        public void ClearAllHighlights()
        {
            foreach (var position in highlightedTiles.Keys)
            {
                if (IsValidPosition(position))
                {
                    tileGrid[position.x, position.y].ClearHighlight();
                }
            }

            highlightedTiles.Clear();
            OnAllHighlightsCleared?.Invoke();
        }

        /// <summary>
        /// 좌표 변환 - 그리드 → 월드
        /// 좌표계: Y축 하→상 증가 (카르테시안)
        /// gridPosition.y = 0: 최하단, gridPosition.y = max: 최상단
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            // Y축 반전: gridPosition.y가 증가하면 Z축 감소 (화면상 위로)
            return gridOrigin + new Vector3(
                gridPosition.x * tileSize,                           // X축: 좌→우
                0f,                                                   // 높이 고정
                (gridSize.y - 1 - gridPosition.y) * tileSize        // Z축: Y 반전 (하→상)
            );
        }

        /// <summary>
        /// 좌표 변환 - 월드 → 그리드
        /// 좌표계: Y축 하→상 증가 (카르테시안)
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            var localPosition = worldPosition - gridOrigin;
            int x = Mathf.RoundToInt(localPosition.x / tileSize);
            int z = Mathf.RoundToInt(localPosition.z / tileSize);

            // Y축 반전: Z가 작을수록 Y가 큼 (하→상)
            int y = gridSize.y - 1 - z;

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// 범위 내 위치들 반환
        /// </summary>
        public List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true)
        {
            var positions = new List<Vector2Int>();

            for (int x = center.x - range; x <= center.x + range; x++)
            {
                for (int y = center.y - range; y <= center.y + range; y++)
                {
                    var pos = new Vector2Int(x, y);
                    
                    if (!IsValidPosition(pos))
                        continue;

                    var distance = Vector2Int.Distance(center, pos);
                    if (distance > range)
                        continue;

                    if (!includeOccupied && IsPositionOccupied(pos))
                        continue;

                    positions.Add(pos);
                }
            }

            return positions;
        }

        /// <summary>
        /// 범위 내 유닛들 반환
        /// </summary>
        public List<GameObject> GetUnitsInRange(Vector2Int center, int range)
        {
            var units = new List<GameObject>();
            var positions = GetPositionsInRange(center, range, true);

            foreach (var position in positions)
            {
                var unit = GetUnitAtPosition(position);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// 타일 데이터 반환
        /// </summary>
        public GridTileData GetTileData(Vector2Int position)
        {
            if (!IsValidPosition(position))
                return null;

            return tileGrid[position.x, position.y];
        }

        /// <summary>
        /// 모든 점유된 위치 반환
        /// </summary>
        public IReadOnlyDictionary<Vector2Int, GameObject> GetOccupiedPositions()
        {
            return positionUnits;
        }

        /// <summary>
        /// 모든 유닛 위치 반환
        /// </summary>
        public IReadOnlyDictionary<GameObject, Vector2Int> GetUnitPositions()
        {
            return unitPositions;
        }

        /// <summary>
        /// 차단된 위치들 반환
        /// </summary>
        public IReadOnlyCollection<Vector2Int> GetBlockedPositions()
        {
            return blockedPositions;
        }

        /// <summary>
        /// 하이라이트된 위치들 반환
        /// </summary>
        public IReadOnlyDictionary<Vector2Int, Color> GetHighlightedPositions()
        {
            return highlightedTiles;
        }

        /// <summary>
        /// 그리드 크기 변경
        /// </summary>
        public void ResizeGrid(Vector2Int newSize)
        {
            if (newSize.x <= 0 || newSize.y <= 0)
            {
                Debug.LogWarning("[GridState] Invalid grid size");
                return;
            }

            // 기존 상태 백업
            var backupUnits = new Dictionary<GameObject, Vector2Int>(unitPositions);
            var backupBlocked = new HashSet<Vector2Int>(blockedPositions);

            // 그리드 크기 변경
            gridSize = newSize;
            
            // 상태 초기화
            unitPositions.Clear();
            positionUnits.Clear();
            blockedPositions.Clear();
            highlightedTiles.Clear();

            // 그리드 재초기화
            InitializeGrid();

            // 유효한 위치의 유닛들만 복원
            foreach (var kvp in backupUnits)
            {
                if (IsValidPosition(kvp.Value))
                {
                    UpdateGridDataLayer(kvp.Key, kvp.Value);
                }
                else
                {
                    Debug.LogWarning($"[GridState] Unit {kvp.Key.name} lost due to grid resize");
                }
            }

            // 유효한 차단 위치들만 복원
            foreach (var pos in backupBlocked)
            {
                if (IsValidPosition(pos))
                {
                    SetTileBlocked(pos, true);
                }
            }

            Debug.Log($"[GridState] Grid resized to {gridSize}");
        }

        /// <summary>
        /// 모든 상태 초기화
        /// </summary>
        public void ClearAllState()
        {
            unitPositions.Clear();
            positionUnits.Clear();
            blockedPositions.Clear();
            ClearAllHighlights();

            // 타일 데이터 재초기화
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    tileGrid[x, y].Reset();
                }
            }

            Debug.Log("[GridState] All state cleared");
        }

        /// <summary>
        /// 물리적 Tile 컴포넌트의 논리 상태만 업데이트 (Transform 변경 없음)
        /// - Tile의 occupyingUnit, isOccupied 상태만 업데이트
        /// - Unit의 Transform은 MovementComponent가 애니메이션으로 처리
        /// </summary>
        private void UpdatePhysicalTileLogic(Vector2Int position, GameObject unit)
        {
            if (!IsValidPosition(position))
                return;

            // Method 1: 이름으로 Tile 오브젝트 찾기 (GridRenderer와 동일한 패턴)
            GameObject tileObject = GameObject.Find($"Tile_{position.x}_{position.y}");
            if (tileObject != null)
            {
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile != null)
                {
                    if (unit != null)
                    {
                        Unit unitComponent = unit.GetComponent<Unit>();
                        if (unitComponent != null)
                        {
                            // 🔧 FIX: Transform 이동 없이 논리 상태만 업데이트
                            tile.SetOccupyingUnitLogic(unitComponent);
                            Debug.Log($"[GridState] Updated physical tile logic at ({position.x}, {position.y}) - placed unit {unit.name}");
                        }
                    }
                    else
                    {
                        tile.RemoveUnit();
                        Debug.Log($"[GridState] Updated physical tile logic at ({position.x}, {position.y}) - removed unit");
                    }
                    return;
                }
            }

            // Method 2: 월드 위치 기반으로 Tile 찾기 (fallback)
            Vector3 worldPos = GridToWorldPosition(position);
            Collider[] colliders = Physics.OverlapSphere(worldPos, tileSize * 0.6f);
            foreach (var collider in colliders)
            {
                Tile tile = collider.GetComponent<Tile>();
                if (tile != null && tile.X == position.x && tile.Y == position.y)
                {
                    if (unit != null)
                    {
                        Unit unitComponent = unit.GetComponent<Unit>();
                        if (unitComponent != null)
                        {
                            tile.PlaceUnit(unitComponent);
                            Debug.Log($"[GridState] Updated physical tile via collider at ({position.x}, {position.y}) - placed unit {unit.name}");
                        }
                    }
                    else
                    {
                        tile.RemoveUnit();
                        Debug.Log($"[GridState] Updated physical tile via collider at ({position.x}, {position.y}) - removed unit");
                    }
                    return;
                }
            }

            // 물리적 Tile을 찾지 못한 경우 (정상적인 상황일 수 있음)
            Debug.Log($"[GridState] No physical tile found at ({position.x}, {position.y}) - data-only update");
        }

        // ✅ 디버깅용 메서드
        public override string ToString()
        {
            return $"GridState[{gridSize.x}x{gridSize.y}, Units:{OccupiedTiles}, Blocked:{BlockedTiles}]";
        }

        private void OnValidate()
        {
            gridSize.x = Mathf.Max(1, gridSize.x);
            gridSize.y = Mathf.Max(1, gridSize.y);
            tileSize = Mathf.Max(0.1f, tileSize);
        }

        private void OnDrawGizmosSelected()
        {
            // 그리드 시각화
            Gizmos.color = Color.white;
            for (int x = 0; x <= gridSize.x; x++)
            {
                Vector3 start = GridToWorldPosition(new Vector2Int(x, 0));
                Vector3 end = GridToWorldPosition(new Vector2Int(x, gridSize.y));
                Gizmos.DrawLine(start, end);
            }
            
            for (int y = 0; y <= gridSize.y; y++)
            {
                Vector3 start = GridToWorldPosition(new Vector2Int(0, y));
                Vector3 end = GridToWorldPosition(new Vector2Int(gridSize.x, y));
                Gizmos.DrawLine(start, end);
            }

            // 차단된 타일 표시
            Gizmos.color = Color.red;
            foreach (var blockedPos in blockedPositions)
            {
                Vector3 center = GridToWorldPosition(blockedPos) + Vector3.up * 0.1f;
                Gizmos.DrawCube(center, Vector3.one * tileSize * 0.8f);
            }
        }
    }

    /// <summary>
    /// 그리드 타일 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class GridTileData
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private bool isOccupied;
        [SerializeField] private bool isBlocked;
        [SerializeField] private bool isHighlighted;
        [SerializeField] private Color highlightColor = Color.white;
        
        private GameObject occupyingUnit;

        public Vector2Int GridPosition => gridPosition;
        public bool IsOccupied => isOccupied;
        public bool IsBlocked => isBlocked;
        public bool IsHighlighted => isHighlighted;
        public Color HighlightColor => highlightColor;
        public GameObject OccupyingUnit => occupyingUnit;

        public GridTileData(Vector2Int position)
        {
            gridPosition = position;
            Reset();
        }

        public void SetOccupied(GameObject unit)
        {
            occupyingUnit = unit;
            isOccupied = unit != null;
        }

        public void ClearOccupied()
        {
            occupyingUnit = null;
            isOccupied = false;
        }

        public void SetBlocked(bool blocked)
        {
            isBlocked = blocked;
        }

        public void SetHighlight(Color color)
        {
            isHighlighted = true;
            highlightColor = color;
        }

        public void ClearHighlight()
        {
            isHighlighted = false;
            highlightColor = Color.white;
        }

        public void Reset()
        {
            isOccupied = false;
            isBlocked = false;
            isHighlighted = false;
            highlightColor = Color.white;
            occupyingUnit = null;
        }

        public override string ToString()
        {
            return $"Tile[{gridPosition}, O:{isOccupied}, B:{isBlocked}, H:{isHighlighted}]";
        }
    }
}