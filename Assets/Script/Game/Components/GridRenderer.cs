using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 그리드 렌더러 - 시각적 표현 담당
    /// Phase 1: 시각적 요소와 GameObject 타일 관리
    /// </summary>
    public class GridRenderer : MonoBehaviour, IGridRenderer
    {
        private IReadOnlyGridState gridState;
        private GameObject tilePrefab;
        
        // 타일 GameObjects 관리
        private Dictionary<Vector2Int, GameObject> tileObjects;
        private Dictionary<Vector2Int, Renderer> tileRenderers;
        private Dictionary<Vector2Int, Color> originalColors;
        private Dictionary<Vector2Int, Color> currentHighlights;
        
        // 설정
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private Color defaultHighlightColor = Color.yellow;
        
        // 성능 최적화
        private Transform tileParent;
        private bool isInitialized = false;

        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(IReadOnlyGridState gridState, GameObject tilePrefab)
        {
            this.gridState = gridState ?? throw new ArgumentNullException(nameof(gridState));
            this.tilePrefab = tilePrefab ?? throw new ArgumentNullException(nameof(tilePrefab));
            
            // 컬렉션 초기화
            tileObjects = new Dictionary<Vector2Int, GameObject>();
            tileRenderers = new Dictionary<Vector2Int, Renderer>();
            originalColors = new Dictionary<Vector2Int, Color>();
            currentHighlights = new Dictionary<Vector2Int, Color>();
            
            // 타일 부모 객체 생성
            CreateTileParent();
            
            // 그리드 생성
            GenerateVisualGrid();
            
            // 이벤트 구독
            SubscribeToGridStateEvents();
            
            isInitialized = true;
            Debug.Log($"[GridRenderer] Initialized with grid size {gridState.GridSize}");
        }

        /// <summary>
        /// 타일 부모 객체 생성
        /// </summary>
        private void CreateTileParent()
        {
            var parentObject = new GameObject("Grid Tiles");
            parentObject.transform.SetParent(transform);
            parentObject.transform.localPosition = Vector3.zero;
            tileParent = parentObject.transform;
        }

        /// <summary>
        /// 시각적 그리드 생성
        /// </summary>
        private void GenerateVisualGrid()
        {
            if (gridState == null || tilePrefab == null)
            {
                Debug.LogError("[GridRenderer] Cannot generate grid - missing dependencies");
                return;
            }

            var gridSize = gridState.GridSize;
            // tileSize는 사용되지 않으므로 제거 (GridToWorldPosition이 내부적으로 처리)

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var gridPos = new Vector2Int(x, y);
                    var worldPos = gridState.GridToWorldPosition(gridPos);

                    CreateTileAt(gridPos, worldPos);
                }
            }
        }

        /// <summary>
        /// 특정 위치에 타일 생성
        /// </summary>
        private void CreateTileAt(Vector2Int gridPosition, Vector3 worldPosition)
        {
            var tileObject = Instantiate(tilePrefab, worldPosition, Quaternion.identity, tileParent);
            tileObject.name = $"Tile_{gridPosition.x}_{gridPosition.y}";
            
            // 타일 컴포넌트 설정
            var tile = tileObject.GetComponent<Tile>();
            if (tile == null)
            {
                tile = tileObject.AddComponent<Tile>();
            }
            tile.Initialize(gridPosition.x, gridPosition.y);

            // 렌더러 정보 저장 - 자식 오브젝트에서 검색
            var renderer = tileObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                tileRenderers[gridPosition] = renderer;
                originalColors[gridPosition] = renderer.material.color;
            }

            tileObjects[gridPosition] = tileObject;
        }

        /// <summary>
        /// GridState 이벤트 구독
        /// </summary>
        private void SubscribeToGridStateEvents()
        {
            if (gridState == null) return;
            
            gridState.OnUnitMoved += HandleUnitMoved;
            gridState.OnUnitPlaced += HandleUnitPlaced;
            gridState.OnUnitRemoved += HandleUnitRemoved;
            gridState.OnTileBlockedChanged += HandleTileBlockedChanged;
        }

        /// <summary>
        /// 유닛 이동 처리 - Clean Architecture: 시각적 표현만 담당
        /// </summary>
        private void HandleUnitMoved(GameObject unit, Vector2Int oldPos, Vector2Int newPos)
        {
            // ✅ Keep: Visual tile updates only
            UpdateTileVisual(oldPos);
            UpdateTileVisual(newPos);
            
            // ❌ Removed: Unit positioning logic
            // Business Logic Layer (GridController) now handles unit.transform.position
            
            // ✅ Optional: Enhanced visual feedback
            PlayMovementEffect(oldPos, newPos);
            TriggerTileChangeAnimation(oldPos);
            TriggerTileChangeAnimation(newPos);
        }

        /// <summary>
        /// 유닛 배치 처리 - Clean Architecture: 시각적 표현만 담당
        /// </summary>
        private void HandleUnitPlaced(Vector2Int position, GameObject unit)
        {
            UpdateTileVisual(position);
            
            // ❌ Removed: Unit positioning logic
            // GridController now handles unit.transform.position during placement
            
            // ✅ Optional: Placement visual effects
            PlayPlacementEffect(position);
        }

        /// <summary>
        /// 유닛 제거 처리
        /// </summary>
        private void HandleUnitRemoved(Vector2Int position, GameObject unit)
        {
            UpdateTileVisual(position);
        }

        /// <summary>
        /// 타일 차단 상태 변경 처리
        /// </summary>
        private void HandleTileBlockedChanged(Vector2Int position, bool blocked)
        {
            UpdateTileVisual(position);
            
            // 차단된 타일에 특별한 시각적 표시
            if (tileRenderers.TryGetValue(position, out var renderer))
            {
                if (blocked)
                {
                    renderer.material.color = Color.red;
                }
                else if (!currentHighlights.ContainsKey(position))
                {
                    // 하이라이트가 없으면 원래 색상으로 복원
                    renderer.material.color = originalColors.GetValueOrDefault(position, Color.white);
                }
            }
        }

        /// <summary>
        /// 타일 시각적 업데이트
        /// </summary>
        private void UpdateTileVisual(Vector2Int position)
        {
            if (!tileRenderers.TryGetValue(position, out var renderer))
                return;

            // 하이라이트 우선, 그 다음 차단 상태, 마지막으로 점유 상태
            if (currentHighlights.TryGetValue(position, out var highlightColor))
            {
                renderer.material.color = highlightColor;
            }
            else if (gridState.IsPositionBlocked(position))
            {
                renderer.material.color = Color.red;
            }
            else if (gridState.IsPositionOccupied(position))
            {
                renderer.material.color = Color.yellow;
            }
            else
            {
                renderer.material.color = originalColors.GetValueOrDefault(position, Color.white);
            }
        }

        // IGridRenderer 인터페이스 구현

        /// <summary>
        /// 타일 하이라이트 설정
        /// </summary>
        public void SetTileHighlight(Vector2Int position, Color highlightColor)
        {
            if (!IsValidPosition(position))
                return;

            currentHighlights[position] = highlightColor;
            UpdateTileVisual(position);
        }

        /// <summary>
        /// 여러 타일 하이라이트 설정
        /// </summary>
        public void SetMultipleTileHighlights(IEnumerable<Vector2Int> positions, Color color)
        {
            foreach (var position in positions)
            {
                SetTileHighlight(position, color);
            }
        }

        /// <summary>
        /// 모든 하이라이트 정리
        /// </summary>
        public void ClearAllHighlights()
        {
            var positionsToUpdate = new List<Vector2Int>(currentHighlights.Keys);
            currentHighlights.Clear();
            
            foreach (var position in positionsToUpdate)
            {
                UpdateTileVisual(position);
            }
        }

        /// <summary>
        /// 특정 타일 하이라이트 정리
        /// </summary>
        public void ClearHighlight(Vector2Int position)
        {
            if (currentHighlights.Remove(position))
            {
                UpdateTileVisual(position);
            }
        }

        /// <summary>
        /// 타일 GameObject 반환
        /// </summary>
        public GameObject GetTileGameObject(Vector2Int position)
        {
            return tileObjects.GetValueOrDefault(position);
        }

        /// <summary>
        /// 안전한 타일 GameObject 반환
        /// </summary>
        public bool TryGetTileGameObject(Vector2Int position, out GameObject tile)
        {
            return tileObjects.TryGetValue(position, out tile);
        }

        /// <summary>
        /// 타일 이펙트 재생
        /// </summary>
        public void PlayTileEffect(Vector2Int position, string effectName)
        {
            if (!TryGetTileGameObject(position, out var tileObject))
                return;

            // 이펙트 시스템이 있다면 여기서 재생
            Debug.Log($"[GridRenderer] Playing effect '{effectName}' at {position}");
            
            // TODO: 실제 이펙트 시스템과 연동
        }

        /// <summary>
        /// 타일 투명도 설정
        /// </summary>
        public void SetTileTransparency(Vector2Int position, float alpha)
        {
            if (!tileRenderers.TryGetValue(position, out var renderer))
                return;

            var color = renderer.material.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.material.color = color;
        }

        /// <summary>
        /// 타일 크기 업데이트 - Vector2 지원 (X/Y 개별 설정)
        /// </summary>
        public void UpdateTileSize(Vector2 newTileSize)
        {
            if (!isInitialized) return;

            // 기존 타일들의 위치를 새로운 크기에 맞게 조정
            var gridSize = gridState.GridSize;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var gridPos = new Vector2Int(x, y);
                    if (tileObjects.TryGetValue(gridPos, out var tileObject))
                    {
                        var newWorldPos = gridState.GridToWorldPosition(gridPos);
                        tileObject.transform.position = newWorldPos;
                    }
                }
            }
        }

        /// <summary>
        /// 하위 호환성 오버로드 - float 타일 크기 (deprecated)
        /// </summary>
        [System.Obsolete("Use UpdateTileSize(Vector2) instead")]
        public void UpdateTileSize(float newTileSize)
        {
            UpdateTileSize(new Vector2(newTileSize, newTileSize));
        }

        /// <summary>
        /// 위치 유효성 검사
        /// </summary>
        private bool IsValidPosition(Vector2Int position)
        {
            return gridState?.IsValidPosition(position) ?? false;
        }

        /// <summary>
        /// 정리 작업
        /// </summary>
        private void OnDestroy()
        {
            if (gridState != null)
            {
                gridState.OnUnitMoved -= HandleUnitMoved;
                gridState.OnUnitPlaced -= HandleUnitPlaced;
                gridState.OnUnitRemoved -= HandleUnitRemoved;
                gridState.OnTileBlockedChanged -= HandleTileBlockedChanged;
            }
        }

        /// <summary>
        /// 디버깅 정보
        /// </summary>
        public override string ToString()
        {
            return $"GridRenderer[Tiles:{tileObjects.Count}, Highlights:{currentHighlights.Count}]";
        }

        // ✅ Clean Architecture: Enhanced visual methods (Presentation Layer only)
        
        /// <summary>
        /// 이동 이펙트 재생
        /// </summary>
        private void PlayMovementEffect(Vector2Int from, Vector2Int to)
        {
            // Visual effect implementation for movement
            Debug.Log($"[GridRenderer] Playing movement effect from {from} to {to}");
            
            // TODO: Implement actual visual effects (particles, animations, etc.)
        }
        
        /// <summary>
        /// 타일 변경 애니메이션 트리거
        /// </summary>
        private void TriggerTileChangeAnimation(Vector2Int position)
        {
            // Tile animation implementation  
            if (TryGetTileGameObject(position, out var tile))
            {
                // TODO: Trigger animation on tile (scale pulse, color flash, etc.)
                Debug.Log($"[GridRenderer] Triggering tile change animation at {position}");
            }
        }
        
        /// <summary>
        /// 배치 이펙트 재생
        /// </summary>
        private void PlayPlacementEffect(Vector2Int position)
        {
            // Visual effect implementation for unit placement
            Debug.Log($"[GridRenderer] Playing placement effect at {position}");
            
            // TODO: Implement actual placement effects
        }

        /// <summary>
        /// 에디터에서 그리드 시각화
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (gridState == null) return;

            // 그리드 라인 그리기
            Gizmos.color = Color.white;
            var gridSize = gridState.GridSize;
            // tileSize는 사용되지 않으므로 제거 (GridToWorldPosition이 내부적으로 처리)

            for (int x = 0; x <= gridSize.x; x++)
            {
                var start = gridState.GridToWorldPosition(new Vector2Int(x, 0));
                var end = gridState.GridToWorldPosition(new Vector2Int(x, gridSize.y));
                Gizmos.DrawLine(start, end);
            }
            
            for (int y = 0; y <= gridSize.y; y++)
            {
                var start = gridState.GridToWorldPosition(new Vector2Int(0, y));
                var end = gridState.GridToWorldPosition(new Vector2Int(gridSize.x, y));
                Gizmos.DrawLine(start, end);
            }
        }
    }
}