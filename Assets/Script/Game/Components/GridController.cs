using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Interfaces;
using UnityEngine.UIElements;

namespace Game.Components
{
    /// <summary>
    /// 그리드 컨트롤러 - 비즈니스 로직 및 게임 규칙 담당
    /// Phase 3: Clean Architecture Business Logic Layer - 성능 최적화 및 캐싱 적용
    /// </summary>
    public class GridController : IGridController
    {
        private IGridState gridState;
        
        [Header("경로 탐색 설정")]
        private bool allowDiagonalMovement = false;
        private int maxPathfindingIterations = 1000;
        private bool useAdvancedPathfinding = true;

        // Phase 3: 성능 최적화 - 개선된 캐싱 시스템
        private readonly Dictionary<(Vector2Int, Vector2Int), PathfindingResult> pathCache = 
            new Dictionary<(Vector2Int, Vector2Int), PathfindingResult>();
        private readonly Dictionary<Vector2Int, List<Vector2Int>> rangeCache = 
            new Dictionary<Vector2Int, List<Vector2Int>>();
        private float lastCacheClearTime;
        private const float CACHE_CLEAR_INTERVAL = 30f; // 30초마다 캐시 정리
        private const int MAX_CACHE_SIZE = 1000; // 최대 캐시 크기
        
        // 성능 모니터링
        private int pathfindingCalls = 0;
        private int cacheHits = 0;
        private float totalPathfindingTime = 0f;

        // IGridController 구현 - 속성들
        public Vector2Int GridSize => gridState?.GridSize ?? Vector2Int.zero;
        public float TileSize => gridState?.TileSize ?? 1f;

        // IGridController 구현 - 이벤트들
        public event Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved;
        public event Action<Vector2Int, GameObject> OnUnitPlaced;
        public event Action<Vector2Int, GameObject> OnUnitRemoved;

        /// <summary>
        /// GridController 생성자 - 의존성 주입
        /// </summary>
        public GridController(IGridState gridState)
        {
            Initialize(gridState);
        }

        /// <summary>
        /// 의존성 초기화
        /// </summary>
        public void Initialize(IGridState gridState)
        {
            this.gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
            
            // GridState 이벤트 연결
            ConnectToGridStateEvents();
            
            lastCacheClearTime = Time.time;
        }

        /// <summary>
        /// GridState 이벤트 연결
        /// </summary>
        private void ConnectToGridStateEvents()
        {
            if (gridState == null) return;

            gridState.OnUnitMoved += (unit, oldPos, newPos) => OnUnitMoved?.Invoke(unit, oldPos, newPos);
            gridState.OnUnitPlaced += (pos, unit) => OnUnitPlaced?.Invoke(pos, unit);
            gridState.OnUnitRemoved += (pos, unit) => OnUnitRemoved?.Invoke(pos, unit);
        }

        // IGridManager 구현 - 기본 메서드들
        public bool IsValidPosition(Vector2Int gridPosition)
        {
            return gridState?.IsValidPosition(gridPosition) ?? false;
        }

        public bool IsPositionOccupied(Vector2Int gridPosition)
        {
            return gridState?.IsPositionOccupied(gridPosition) ?? false;
        }

        public bool IsPositionBlocked(Vector2Int gridPosition)
        {
            return gridState?.IsPositionBlocked(gridPosition) ?? false;
        }

        public GameObject GetUnitAtPosition(Vector2Int gridPosition)
        {
            return gridState?.GetUnitAtPosition(gridPosition);
        }

        public Vector2Int GetUnitPosition(GameObject unit)
        {
            return gridState?.GetUnitPosition(unit) ?? new Vector2Int(-1, -1);
        }

        public bool TryGetUnitPosition(GameObject unit, out Vector2Int position)
        {
            if (gridState != null)
            {
                return gridState.TryGetUnitPosition(unit, out position);
            }
            
            position = new Vector2Int(-1, -1);
            return false;
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return gridState?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return gridState?.WorldToGridPosition(worldPosition) ?? Vector2Int.zero;
        }

        public void SetTileBlocked(Vector2Int position, bool blocked)
        {
            gridState?.SetTileBlocked(position, blocked);
            ClearPathCache(); // 차단 상태 변경 시 경로 캐시 무효화
        }

        public void SetTileHighlight(Vector2Int position, Color highlightColor)
        {
            if(gridState is GridState _gridState)
            {
                _gridState?.SetTileHighlight(position, highlightColor);
            }
        }

        public void ClearAllHighlights()
        {
            if (gridState is GridState _gridState)
            {
                _gridState?.ClearAllHighlights();
            }
        }

        public List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true)
        {
            return gridState?.GetPositionsInRange(center, range, includeOccupied) ?? new List<Vector2Int>();
        }

        public List<GameObject> GetUnitsInRange(Vector2Int center, int range)
        {
            return gridState?.GetUnitsInRange(center, range) ?? new List<GameObject>();
        }

        // 유닛 이동 로직
        public bool CanMoveUnit(GameObject unit, Vector2Int targetPosition)
        {
            return true; // Testing

            if (unit == null || gridState == null)
                return false;

            // 대상 위치 유효성 확인
            if (!IsValidPosition(targetPosition))
                return false;

            // 대상 위치가 차단되어 있는지 확인
            if (IsPositionBlocked(targetPosition))
                return false;

            // 대상 위치가 이미 점유되어 있는지 확인 (자신 제외)
            var unitAtTarget = GetUnitAtPosition(targetPosition);
            if (unitAtTarget != null && unitAtTarget != unit)
                return false;

            // 현재 위치에서 대상 위치로 이동 가능한지 확인
            if (!TryGetUnitPosition(unit, out var currentPosition))
                return false;

            // 경로가 존재하는지 확인
            var path = FindPath(currentPosition, targetPosition, unit);
            return path.Count > 0;
        }

        public bool MoveUnit(GameObject unit, Vector2Int newPosition)
        {
            Debug.Log("MoveUnitT1");
            if (!CanMoveUnit(unit, newPosition))
                return false;
            Debug.Log("MoveUnitT2");
            return gridState.SetUnitPosition(unit, newPosition);
        }

        public bool TryMoveUnit(GameObject unit, Vector2Int newPosition, out string errorMessage)
        {
            errorMessage = "";

            if (unit == null)
            {
                errorMessage = "Unit is null";
                return false;
            }

            if (gridState == null)
            {
                errorMessage = "GridState is null";
                return false;
            }

            if (!IsValidPosition(newPosition))
            {
                errorMessage = "Target position is invalid";
                return false;
            }

            if (IsPositionBlocked(newPosition))
            {
                errorMessage = "Target position is blocked";
                return false;
            }

            var unitAtTarget = GetUnitAtPosition(newPosition);
            if (unitAtTarget != null && unitAtTarget != unit)
            {
                errorMessage = "Target position is occupied";
                return false;
            }

            if (!TryGetUnitPosition(unit, out var currentPosition))
            {
                errorMessage = "Unit position not found";
                return false;
            }

            var pathResult = FindPathWithDetails(currentPosition, newPosition, unit);
            if (!pathResult.Success)
            {
                errorMessage = pathResult.ErrorMessage;
                return false;
            }

            return gridState.SetUnitPosition(unit, newPosition);
        }

        // 경로 탐색 구현
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GameObject movingUnit = null)
        {
            var result = FindPathWithDetails(start, end, movingUnit);
            return result.Success ? result.Path : new List<Vector2Int>();
        }

        public PathfindingResult FindPathWithDetails(Vector2Int start, Vector2Int end, GameObject movingUnit = null)
        {
            if (!IsValidPosition(start) || !IsValidPosition(end))
                return PathfindingResult.Failed("Invalid start or end position");

            if (start == end)
                return PathfindingResult.Succeeded(new List<Vector2Int> { start });

            // 캐시 확인
            var cacheKey = (start, end);
            if (pathCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            // 주기적으로 캐시 정리
            if (Time.time - lastCacheClearTime > CACHE_CLEAR_INTERVAL)
            {
                ClearPathCache();
                lastCacheClearTime = Time.time;
            }

            var result = useAdvancedPathfinding ? 
                FindPathAStar(start, end, movingUnit) : 
                FindPathBFS(start, end, movingUnit);

            // 결과 캐싱
            pathCache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// A* 알고리즘 기반 경로 탐색
        /// </summary>
        private PathfindingResult FindPathAStar(Vector2Int start, Vector2Int end, GameObject movingUnit)
        {
            var openSet = new List<AStarNode>();
            var closedSet = new HashSet<Vector2Int>();
            var allNodes = new Dictionary<Vector2Int, AStarNode>();

            var startNode = new AStarNode(start, 0, GetHeuristic(start, end));
            openSet.Add(startNode);
            allNodes[start] = startNode;

            int iterations = 0;
            while (openSet.Count > 0 && iterations < maxPathfindingIterations)
            {
                iterations++;

                // 가장 낮은 F 값을 가진 노드 선택
                var currentNode = openSet.OrderBy(n => n.F).First();
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Position);

                // 목표 도달
                if (currentNode.Position == end)
                {
                    var path = ReconstructPath(currentNode);
                    return PathfindingResult.Succeeded(path);
                }

                // 인접 노드들 탐색
                foreach (var neighbor in GetNeighbors(currentNode.Position))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    if (!IsWalkable(neighbor, movingUnit))
                        continue;

                    var gScore = currentNode.G + GetMovementCost(currentNode.Position, neighbor);
                    var hScore = GetHeuristic(neighbor, end);

                    if (!allNodes.TryGetValue(neighbor, out var neighborNode))
                    {
                        neighborNode = new AStarNode(neighbor, gScore, hScore, currentNode);
                        allNodes[neighbor] = neighborNode;
                        openSet.Add(neighborNode);
                    }
                    else if (gScore < neighborNode.G)
                    {
                        neighborNode.G = gScore;
                        neighborNode.Parent = currentNode;
                        
                        if (!openSet.Contains(neighborNode))
                        {
                            openSet.Add(neighborNode);
                        }
                    }
                }
            }

            return PathfindingResult.Failed("No path found");
        }

        /// <summary>
        /// BFS 기반 단순 경로 탐색
        /// </summary>
        private PathfindingResult FindPathBFS(Vector2Int start, Vector2Int end, GameObject movingUnit)
        {
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var parents = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                {
                    var path = ReconstructPathBFS(start, end, parents);
                    return PathfindingResult.Succeeded(path);
                }

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    if (!IsWalkable(neighbor, movingUnit))
                        continue;

                    visited.Add(neighbor);
                    parents[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return PathfindingResult.Failed("No path found");
        }

        /// <summary>
        /// 이동 가능한 위치인지 확인
        /// </summary>
        private bool IsWalkable(Vector2Int position, GameObject movingUnit)
        {
            if (!IsValidPosition(position))
                return false;

            if (IsPositionBlocked(position))
                return false;

            var unitAtPosition = GetUnitAtPosition(position);
            return unitAtPosition == null || unitAtPosition == movingUnit;
        }

        /// <summary>
        /// 인접 위치들 반환
        /// </summary>
        private List<Vector2Int> GetNeighbors(Vector2Int position)
        {
            var neighbors = new List<Vector2Int>
            {
                position + Vector2Int.up,
                position + Vector2Int.down,
                position + Vector2Int.left,
                position + Vector2Int.right
            };

            if (allowDiagonalMovement)
            {
                neighbors.AddRange(new[]
                {
                    position + new Vector2Int(1, 1),
                    position + new Vector2Int(1, -1),
                    position + new Vector2Int(-1, 1),
                    position + new Vector2Int(-1, -1)
                });
            }

            return neighbors.Where(IsValidPosition).ToList();
        }

        /// <summary>
        /// 이동 비용 계산
        /// </summary>
        private float GetMovementCost(Vector2Int from, Vector2Int to)
        {
            var diff = to - from;
            
            // 대각선 이동은 더 비싸게
            if (allowDiagonalMovement && (Mathf.Abs(diff.x) + Mathf.Abs(diff.y)) > 1)
            {
                return 1.4f; // √2 approximation
            }
            
            return 1f;
        }

        /// <summary>
        /// 휴리스틱 함수 (맨하탄 거리)
        /// </summary>
        private float GetHeuristic(Vector2Int from, Vector2Int to)
        {
            if (allowDiagonalMovement)
            {
                // 유클리드 거리
                return Vector2Int.Distance(from, to);
            }
            else
            {
                // 맨하탄 거리
                return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
            }
        }

        /// <summary>
        /// A* 경로 재구성
        /// </summary>
        private List<Vector2Int> ReconstructPath(AStarNode endNode)
        {
            var path = new List<Vector2Int>();
            var current = endNode;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// BFS 경로 재구성
        /// </summary>
        private List<Vector2Int> ReconstructPathBFS(Vector2Int start, Vector2Int end, Dictionary<Vector2Int, Vector2Int> parents)
        {
            var path = new List<Vector2Int>();
            var current = end;

            while (current != start)
            {
                path.Add(current);
                current = parents[current];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        // 추가 유틸리티 메서드들
        public bool IsPathClear(Vector2Int start, Vector2Int end, GameObject ignoredUnit = null)
        {
            var result = FindPathWithDetails(start, end, ignoredUnit);
            return result.Success;
        }

        public int GetPathDistance(Vector2Int start, Vector2Int end)
        {
            var result = FindPathWithDetails(start, end);
            return result.Success ? result.Distance : -1;
        }

        public List<Vector2Int> GetValidMovePositions(GameObject unit, int moveRange)
        {
            if (!TryGetUnitPosition(unit, out var currentPosition))
                return new List<Vector2Int>();

            var validPositions = new List<Vector2Int>();
            var positions = GetPositionsInRange(currentPosition, moveRange, false);

            foreach (var position in positions)
            {
                if (CanMoveUnit(unit, position))
                {
                    var distance = GetPathDistance(currentPosition, position);
                    if (distance >= 0 && distance <= moveRange)
                    {
                        validPositions.Add(position);
                    }
                }
            }

            return validPositions;
        }

        /// <summary>
        /// 길찾기 설정 변경
        /// </summary>
        public void SetPathfindingOptions(bool allowDiagonal, int maxIterations)
        {
            allowDiagonalMovement = allowDiagonal;
            maxPathfindingIterations = Mathf.Max(100, maxIterations);
            ClearPathCache(); // 설정 변경 시 캐시 무효화
        }

        /// <summary>
        /// Phase 3: 개선된 캐시 정리 - 범위 캐시도 포함
        /// </summary>
        public void ClearPathCache()
        {
            pathCache.Clear();
            rangeCache.Clear();
            
            // 성능 통계 초기화
            pathfindingCalls = 0;
            cacheHits = 0;
            totalPathfindingTime = 0f;
        }

        /// <summary>
        /// Phase 3: 캐시 크기 제한 적용
        /// </summary>
        private void MaintainCacheSize()
        {
            if (pathCache.Count > MAX_CACHE_SIZE)
            {
                // LRU 방식으로 오래된 항목 제거
                var keysToRemove = pathCache.Keys.Take(pathCache.Count - MAX_CACHE_SIZE / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    pathCache.Remove(key);
                }
            }
            
            if (rangeCache.Count > MAX_CACHE_SIZE / 2)
            {
                var keysToRemove = rangeCache.Keys.Take(rangeCache.Count - MAX_CACHE_SIZE / 4).ToList();
                foreach (var key in keysToRemove)
                {
                    rangeCache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Phase 3: 성능 통계 반환
        /// </summary>
        public (int calls, int hits, float avgTime, float hitRate) GetPerformanceStats()
        {
            float hitRate = pathfindingCalls > 0 ? (float)cacheHits / pathfindingCalls * 100f : 0f;
            float avgTime = pathfindingCalls > 0 ? totalPathfindingTime / pathfindingCalls : 0f;
            return (pathfindingCalls, cacheHits, avgTime, hitRate);
        }

        /// <summary>
        /// Phase 3: 향상된 디버깅 정보
        /// </summary>
        public override string ToString()
        {
            var (calls, hits, avgTime, hitRate) = GetPerformanceStats();
            return $"GridController[PathCache:{pathCache.Count}, RangeCache:{rangeCache.Count}, " +
                   $"Calls:{calls}, HitRate:{hitRate:F1}%, AvgTime:{avgTime:F2}ms, Diagonal:{allowDiagonalMovement}]";
        }
    }
}