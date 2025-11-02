using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Interfaces;
using Game.Core;
using Game.Card.Effects;
using Game.Data;
using UnityEngine.UIElements;

namespace Game.Components
{
    /// <summary>
    /// 그리드 컨트롤러 - 비즈니스 로직 및 게임 규칙 담당
    /// Phase 3: Clean Architecture Business Logic Layer - 성능 최적화 및 캐싱 적용
    /// Phase 4: Base 높이 차이 반영 시스템 - IGridHeightCalculator 구현
    /// </summary>
    public class GridController : IGridController, IGridHeightCalculator
    {
        private IGridState gridState;

        [Header("경로 탐색 설정")]
        private bool allowDiagonalMovement = false;
        private int maxPathfindingIterations = 1000;
        private bool useAdvancedPathfinding = true;

        [Header("유닛 배치 설정")]
        [SerializeField] private Vector3 unitOffset = Vector3.up * 0.0f;

        [Header("높이 설정 (Base/Ground 높이 차이)")]
        [SerializeField] private float baseHeight = 1.85f;    // Base 위치 높이
        [SerializeField] private float groundHeight = 0.65f;  // Ground 기본 높이

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

        // GridController 속성들 (비즈니스 로직에서 필요한 정보)
        public Vector2Int GridSize => gridState?.GridSize ?? Vector2Int.zero;

        // ✅ 새로운 Vector2 타일 크기 (X/Y 개별 설정 지원)
        public Vector2 TileSizeVector => gridState?.TileSizeVector ?? Vector2.one;

        // ✅ 기존 프로퍼티 유지 (deprecated, 하위 호환성)
        [System.Obsolete("Use TileSizeVector instead. Returns X component for backward compatibility.")]
        public float TileSize => gridState?.TileSize ?? 1f;

        // 비즈니스 로직 이벤트들 (다른 비즈니스 컴포넌트들이 구독)
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

        // ✅ 비즈니스 로직에서 필요한 GridState 접근 메서드들 (public 유지)
        public bool IsValidPosition(Vector2Int gridPosition)
        {
            return gridState?.IsValidPosition(gridPosition) ?? false;
        }

        public bool IsPositionOccupied(Vector2Int gridPosition)
        {
            return gridState?.IsPositionOccupied(gridPosition) ?? false;
        }

        public bool IsPositionWalkable(Vector2Int gridPosition)
        {
            return gridState?.IsPositionWalkable(gridPosition) ?? false;
        }

        public bool IsPositionBlocked(Vector2Int gridPosition)
        {
            return gridState?.IsPositionBlocked(gridPosition) ?? false;
        }

        public GameObject GetUnitAtPosition(Vector2Int gridPosition)
        {
            return gridState?.GetUnitAtPosition(gridPosition);
        }

        /// <summary>
        /// 공격 가능한 타겟 반환 (Unit 우선, 없으면 Base)
        /// AI 및 전투 시스템에서 공격 대상을 찾을 때 사용
        /// </summary>
        public GameObject GetAttackableTargetAtPosition(Vector2Int gridPosition)
        {
            return gridState?.GetAttackableTargetAtPosition(gridPosition);
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

        public Vector2Int GetPositionToAttackTarget(GameObject target)
        {
            return gridState?.GetPositionToAttackTarget(target) ?? new Vector2Int(-1, -1);
        }

        public bool TryGetPositionToAttackTarget(GameObject target, out Vector2Int position)
        {
            if (gridState != null)
            {
                return gridState.TryGetPositionToAttackTarget(target, out position);
            }
            position = new Vector2Int(-1, -1);
            return false;
        }

        public bool RemoveUnit(GameObject unit)
        {
            if (gridState != null)
            {
                return gridState.RemoveUnit(unit);
            }
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

        public List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true)
        {
            return gridState?.GetPositionsInRange(center, range, includeOccupied) ?? new List<Vector2Int>();
        }

        public List<GameObject> GetUnitsInRange(Vector2Int center, int range)
        {
            return gridState?.GetUnitsInRange(center, range) ?? new List<GameObject>();
        }

        #region 팀 기반 유닛 조회 메서드들 (IGridTeamQuery 구현)

        /// <summary>
        /// 해당 위치에 유닛이 있는지 확인
        /// </summary>
        public bool HasUnit(Vector2Int position)
        {
            return GetUnitAtPosition(position) != null;
        }

        /// <summary>
        /// 해당 위치에 플레이어 유닛이 있는지 확인
        /// </summary>
        public bool HasPlayerUnit(Vector2Int position)
        {
            return HasUnitWithTeam(position, TeamType.Player);
        }

        /// <summary>
        /// 해당 위치에 적군 유닛이 있는지 확인
        /// </summary>
        public bool HasEnemyUnit(Vector2Int position)
        {
            return HasUnitWithTeam(position, TeamType.Enemy);
        }

        /// <summary>
        /// 해당 위치에 특정 팀의 유닛이 있는지 확인 (확장성)
        /// </summary>
        public bool HasUnitWithTeam(Vector2Int position, TeamType team)
        {
            var unit = GetUnitAtPosition(position);
            if (unit == null) return false;

            var teamComponent = unit.GetComponent<ITeamComponent>();
            return teamComponent != null && teamComponent.Team == team;
        }

        /// <summary>
        /// 해당 위치에 특정 관계의 유닛이 있는지 확인 (확장성)
        /// </summary>
        public bool HasUnitWithRelation(Vector2Int position, TeamType relativeTo, TeamRelation relation)
        {
            var unit = GetUnitAtPosition(position);
            if (unit == null) return false;

            var teamComponent = unit.GetComponent<ITeamComponent>();
            if (teamComponent == null) return false;

            // 임시 TeamComponent 생성하여 관계 확인
            var relativeTeamComponent = new TempTeamComponent(relativeTo);
            return teamComponent.GetRelationTo(relativeTeamComponent) == relation;
        }

        /// <summary>
        /// 해당 위치 유닛의 팀 타입 반환 (유닛이 없으면 None)
        /// </summary>
        public TeamType GetUnitTeam(Vector2Int position)
        {
            var unit = GetUnitAtPosition(position);
            if (unit == null) return TeamType.None;

            var teamComponent = unit.GetComponent<ITeamComponent>();
            return teamComponent?.Team ?? TeamType.None;
        }

        /// <summary>
        /// 임시 팀 컴포넌트 (관계 확인용)
        /// </summary>
        private class TempTeamComponent : ITeamComponent
        {
            public TeamType Team { get; set; }
            public string TeamName => Team.ToString();
            public Color TeamColor => Color.white;

            public event Action<TeamType, TeamType> OnTeamChanged;

            public TempTeamComponent(TeamType team)
            {
                Team = team;
            }

            public TeamRelation GetRelationTo(ITeamComponent other)
            {
                if (other == null) return TeamRelation.Neutral;
                if (other == this) return TeamRelation.Self;

                return TeamRelationMatrix.GetRelation(Team, other.Team);
            }

            public bool IsSameTeam(ITeamComponent other)
            {
                return other != null && Team == other.Team && Team != TeamType.None;
            }

            public bool IsEnemy(ITeamComponent other)
            {
                return GetRelationTo(other) == TeamRelation.Enemy;
            }

            public bool IsAlly(ITeamComponent other)
            {
                var relation = GetRelationTo(other);
                return relation == TeamRelation.Ally || relation == TeamRelation.Self;
            }
        }

        #endregion

        #region Phase 2.12: GetAffectedUnits() 메서드 구현

        /// <summary>
        /// Phase 2.12: AffectedType과 AffectedRange를 기반으로 영향받을 유닛 리스트를 반환합니다.
        /// 카드 효과 시스템에서 사용되는 핵심 메서드입니다.
        /// </summary>
        /// <param name="targetPosition">효과의 중심 위치</param>
        /// <param name="affectedType">영향받을 대상 타입 (Ally/Enemy/Any/None)</param>
        /// <param name="affectedRange">효과 범위 (0: 단일 대상, 1+: 범위 효과)</param>
        /// <param name="casterTeam">효과를 발동시킨 팀 (팀 구분용)</param>
        /// <returns>영향받을 유닛들의 GameObject 리스트</returns>
        public List<GameObject> GetAffectedUnits(Vector2Int targetPosition, AffectedType affectedType, int affectedRange, TeamType casterTeam)
        {
            var affectedUnits = new List<GameObject>();

            if (affectedType == AffectedType.None)
            {
                return affectedUnits; // 아무도 영향받지 않음
            }

            List<Vector2Int> positionsToCheck;

            // Phase 2.12: AffectedRange 기반 위치 계산
            if (affectedRange == 0)
            {
                // 단일 대상: 타겟 위치만
                positionsToCheck = new List<Vector2Int> { targetPosition };
                Debug.Log($"LLLLL {positionsToCheck.Count}");
            }
            else
            {
                // 범위 효과: 맨하탄 거리 기반 범위 내 모든 위치
                positionsToCheck = GetPositionsInRange(targetPosition, affectedRange, true);
            }

            // 각 위치의 유닛들을 확인하여 조건에 맞는 유닛 수집
            foreach (var position in positionsToCheck)
            {
                var unit = GetUnitAtPosition(position);
                if (unit != null && IsUnitValidTarget(unit, affectedType, casterTeam))
                {
                    affectedUnits.Add(unit);
                }
            }
            Debug.Log($"LLLLLLL {affectedUnits.Count}");
            return affectedUnits;
        }

        /// <summary>
        /// Phase 2.12: 유닛이 AffectedType 조건에 맞는 유효한 대상인지 확인합니다.
        /// </summary>
        /// <param name="unit">확인할 유닛</param>
        /// <param name="affectedType">대상 타입 조건</param>
        /// <param name="casterTeam">효과 발동자의 팀</param>
        /// <returns>유효한 대상이면 true</returns>
        private bool IsUnitValidTarget(GameObject unit, AffectedType affectedType, TeamType casterTeam)
        {
            if (unit == null) return false;

            // ITeamComponent를 통해 유닛의 팀 정보 획득
            var teamComponent = unit.GetComponent<ITeamComponent>();
            if (teamComponent == null)
            {
                Debug.LogWarning($"GetAffectedUnits: 유닛 {unit.name}에 ITeamComponent가 없습니다. 중립으로 처리합니다.");
                // 팀 정보가 없는 경우 중립 유닛으로 간주하고 Any일 때만 대상으로 포함
                return affectedType == AffectedType.Any;
            }

            // AffectedType에 따른 대상 필터링
            return affectedType switch
            {
                AffectedType.Ally => IsAllyUnit(teamComponent, casterTeam),
                AffectedType.Enemy => IsEnemyUnit(teamComponent, casterTeam),
                AffectedType.Any => true, // 모든 유닛이 대상
                AffectedType.None => false, // 아무도 대상 아님 (위에서 이미 처리됨)
                _ => false
            };
        }

        /// <summary>
        /// Phase 2.12: 유닛이 아군인지 확인합니다.
        /// </summary>
        /// <param name="teamComponent">유닛의 팀 컴포넌트</param>
        /// <param name="casterTeam">기준이 되는 시전자 팀</param>
        /// <returns>아군이면 true</returns>
        private bool IsAllyUnit(ITeamComponent teamComponent, TeamType casterTeam)
        {
            if (teamComponent == null) return false;

            // 시전자와 같은 팀이면 아군
            return teamComponent.Team == casterTeam;
        }

        /// <summary>
        /// Phase 2.12: 유닛이 적군인지 확인합니다.
        /// </summary>
        /// <param name="teamComponent">유닛의 팀 컴포넌트</param>
        /// <param name="casterTeam">기준이 되는 시전자 팀</param>
        /// <returns>적군이면 true</returns>
        private bool IsEnemyUnit(ITeamComponent teamComponent, TeamType casterTeam)
        {
            if (teamComponent == null) return false;

            // 시전자와 다른 팀이면서 None이나 Neutral이 아니면 적군
            return teamComponent.Team != casterTeam &&
                   teamComponent.Team != TeamType.None &&
                   teamComponent.Team != TeamType.Neutral;
        }

        /// <summary>
        /// Phase 2.12: 효과 범위 내 위치들의 맨하탄 거리 기반 필터링
        /// AffectedRange 검증을 위한 헬퍼 메서드
        /// </summary>
        /// <param name="centerPosition">중심 위치</param>
        /// <param name="checkPosition">확인할 위치</param>
        /// <param name="maxRange">최대 범위</param>
        /// <returns>범위 내에 있으면 true</returns>
        public bool IsPositionInAffectedRange(Vector2Int centerPosition, Vector2Int checkPosition, int maxRange)
        {
            if (maxRange < 0) return false; // 잘못된 범위
            if (maxRange == 0) return centerPosition == checkPosition; // 단일 대상

            // 맨하탄 거리로 범위 확인
            int distance = Mathf.Abs(centerPosition.x - checkPosition.x) + Mathf.Abs(centerPosition.y - checkPosition.y);
            return distance <= maxRange;
        }

        /// <summary>
        /// Phase 2.12: 디버깅을 위한 GetAffectedUnits 정보 출력
        /// </summary>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="affectedType">영향 타입</param>
        /// <param name="affectedRange">영향 범위</param>
        /// <param name="casterTeam">시전자 팀</param>
        public void DebugLogAffectedUnits(Vector2Int targetPosition, AffectedType affectedType, int affectedRange, TeamType casterTeam)
        {
            var units = GetAffectedUnits(targetPosition, affectedType, affectedRange, casterTeam);
            Debug.Log($"GetAffectedUnits Debug: 위치({targetPosition.x}, {targetPosition.y}), 타입:{affectedType}, 범위:{affectedRange}, 시전자팀:{casterTeam}, 대상:{units.Count}개");

            foreach (var unit in units)
            {
                var teamComp = unit.GetComponent<ITeamComponent>();
                var teamName = teamComp?.Team.ToString() ?? "Unknown";
                Debug.Log($"  - {unit.name} (팀: {teamName})");
            }
        }

        #endregion

        #region Phase 3.15: TargetRange 거리 계산 메서드들

        /// <summary>
        /// Phase 3.15: 플레이어 기준점에서 대상 위치까지의 X축 거리 계산
        /// 플레이어는 가장 왼쪽 열(X=0)을 기준으로 X축 거리만 계산
        /// </summary>
        /// <param name="targetPosition">목표 위치</param>
        /// <returns>플레이어 기준점에서의 X축 거리</returns>
        public int GetDistanceFromPlayerBase(Vector2Int targetPosition)
        {
            Vector2Int playerBasePosition = GetPlayerBasePosition();
            return CalculateManhattanDistance(playerBasePosition, targetPosition);
        }

        /// <summary>
        /// Phase 3.15: 적군 기준점에서 대상 위치까지의 X축 거리 계산
        /// 적군은 가장 오른쪽 열을 기준으로 X축 거리만 계산
        /// </summary>
        /// <param name="targetPosition">목표 위치</param>
        /// <returns>적군 기준점에서의 X축 거리</returns>
        public int GetDistanceFromEnemyBase(Vector2Int targetPosition)
        {
            Vector2Int enemyBasePosition = GetEnemyBasePosition();
            return CalculateManhattanDistance(enemyBasePosition, targetPosition);
        }

        /// <summary>
        /// Phase 3.15: 플레이어 기준점 위치 반환
        /// 가장 왼쪽 열(X=0)을 고정 기준점으로 사용
        /// X축 거리만 계산하므로 Y 좌표는 의미 없음
        /// </summary>
        /// <returns>플레이어 기준점 위치</returns>
        public Vector2Int GetPlayerBasePosition()
        {
            var gridSize = GridSize;

            // 가장 왼쪽 열을 고정 기준점으로 사용 (Y 좌표는 X축 거리 계산에 영향 없음)
            //Vector2Int basePosition = new Vector2Int(gridSize.x / 2, 0);
            Vector2Int basePosition = new Vector2Int(gridSize.x/2, 0);
            Debug.Log($"[GridController] Player base position (fixed leftmost column): {basePosition}");
            return basePosition;
        }

        /// <summary>
        /// Phase 3.15: 적군 기준점 위치 반환
        /// 가장 오른쪽 열을 고정 기준점으로 사용
        /// X축 거리만 계산하므로 Y 좌표는 의미 없음
        /// </summary>
        /// <returns>적군 기준점 위치</returns>
        public Vector2Int GetEnemyBasePosition()
        {
            var gridSize = GridSize;
            int rightmostColumn = gridSize.y - 1;  // y축(좌우)의 최대값 = 오른쪽

            // 가장 오른쪽 열을 고정 기준점으로 사용 (x축은 중간값, y축은 최대값)
            Vector2Int basePosition = new Vector2Int(gridSize.x / 2, rightmostColumn);
            Debug.Log($"[GridController] Enemy base position (fixed rightmost column): {basePosition}");
            return basePosition;
        }

        /// <summary>
        /// Phase 3.15: X축 기반 거리 계산 헬퍼 메서드
        /// Y축(위아래) 거리는 무시하고 X축(좌우) 거리만 계산
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>X축 거리</returns>
        public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// Phase 3.15: CardData의 TargetRange 검증을 위한 통합 메서드
        /// SpawnValidator에서 사용할 수 있는 공통 검증 로직
        /// </summary>
        /// <param name="cardData">검증할 카드 데이터</param>
        /// <param name="targetPosition">목표 위치</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        /// <returns>TargetRange 검증 결과</returns>
        public bool ValidateCardTargetRange(CardData cardData, Vector2Int targetPosition, bool isPlayerCard)
        {
            if (cardData == null)
            {
                Debug.LogError("[GridController] CardData가 null입니다.");
                return false;
            }

            // TargetRange가 -1이면 거리 제한 없음
            if (cardData.TargetRange < 0)
            {
                Debug.Log($"[GridController] {cardData.CardName}: TargetRange 제한 없음");
                return true;
            }

            int distance;
            string teamName;

            if (isPlayerCard)
            {
                // 플레이어 카드: 플레이어 기준점에서의 거리
                distance = GetDistanceFromPlayerBase(targetPosition);
                teamName = "Player";
            }
            else
            {
                // 적군 카드: 적군 기준점에서의 거리
                distance = GetDistanceFromEnemyBase(targetPosition);
                teamName = "Enemy";
            }

            bool isInRange = distance <= cardData.TargetRange;

            if (isInRange)
            {
                Debug.Log($"[GridController] {teamName} {cardData.CardName}: TargetRange 검증 통과 - Distance: {distance}, Max: {cardData.TargetRange}");
            }
            else
            {
                Debug.Log($"[GridController] {teamName} {cardData.CardName}: TargetRange 범위 초과 - Distance: {distance}, Max: {cardData.TargetRange}");
            }

            return isInRange;
        }

        /// <summary>
        /// Phase 3.15: 플레이어/적군 기준점에서 지정된 거리 내의 모든 유효한 위치 반환
        /// </summary>
        /// <param name="maxRange">최대 거리</param>
        /// <param name="isPlayerBased">플레이어 기준점 사용 여부</param>
        /// <param name="includeOccupied">점유된 위치 포함 여부</param>
        /// <returns>거리 내 유효한 위치들</returns>
        public List<Vector2Int> GetPositionsWithinRange(int maxRange, bool isPlayerBased, bool includeOccupied = true)
        {
            var positions = new List<Vector2Int>();

            if (maxRange < 0) return positions; // 잘못된 범위

            Vector2Int basePosition = isPlayerBased ? GetPlayerBasePosition() : GetEnemyBasePosition();

            // 그리드 내 모든 위치 검사
            var gridSize = GridSize;
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    // 유효한 위치인지 확인
                    if (!IsValidPosition(pos)) continue;

                    // 점유 상태 확인
                    if (!includeOccupied && IsPositionOccupied(pos)) continue;

                    // 거리 확인
                    int distance = CalculateManhattanDistance(basePosition, pos);
                    if (distance <= maxRange)
                    {
                        positions.Add(pos);
                    }
                }
            }

            string teamName = isPlayerBased ? "Player" : "Enemy";
            Debug.Log($"[GridController] {teamName} 기준점 {basePosition}에서 거리 {maxRange} 내 위치 {positions.Count}개 발견");

            return positions;
        }

        /// <summary>
        /// Phase 3.15: TargetRange 검증 디버깅을 위한 상세 정보 출력
        /// </summary>
        /// <param name="cardData">테스트할 카드</param>
        /// <param name="testPositions">테스트할 위치들</param>
        /// <param name="isPlayerCard">플레이어 카드인지 여부</param>
        public void DebugTargetRangeValidation(CardData cardData, List<Vector2Int> testPositions, bool isPlayerCard)
        {
            if (cardData == null || testPositions == null) return;

            string teamName = isPlayerCard ? "Player" : "Enemy";
            Vector2Int basePosition = isPlayerCard ? GetPlayerBasePosition() : GetEnemyBasePosition();

            Debug.Log($"[GridController] === {teamName} {cardData.CardName} TargetRange 검증 디버깅 ===");
            Debug.Log($"기준점: {basePosition}, TargetRange: {cardData.TargetRange}");

            foreach (var testPos in testPositions)
            {
                int distance = CalculateManhattanDistance(basePosition, testPos);
                bool isValid = ValidateCardTargetRange(cardData, testPos, isPlayerCard);

                Debug.Log($"  위치 {testPos}: 거리={distance}, 유효={isValid}");
            }
        }

        #endregion

        // 유닛 이동 로직
        public bool CanMoveUnit(GameObject unit, Vector2Int targetPosition)
        {
            return true;

            if (unit == null || gridState == null)
                return false;

            Debug.Log($"[GridController] CanMoveUnit: {unit.name} → ({targetPosition.x}, {targetPosition.y})");

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
            if (!CanMoveUnit(unit, newPosition))
                return false;

            // Update state through data layer (논리적 상태만)
            if (!gridState.UpdateGridDataLayer(unit, newPosition))
                return false;

            // Handle positioning (Business Logic responsibility) - 필수 추가
            SetUnitWorldPosition(unit, newPosition);

            return true;
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

            // 초기 배치/이동은 데이터 + Transform 모두 업데이트
            if (!gridState.UpdateGridDataLayer(unit, newPosition))
                return false;

            // Transform 위치 설정 (즉시 이동)
            SetUnitWorldPosition(unit, newPosition);
            return true;
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
        /// Base 및 다른 유닛이 있는 위치는 이동 불가 (자기 자신 제외)
        /// </summary>
        private bool IsWalkable(Vector2Int position, GameObject movingUnit)
        {
            if (!IsValidPosition(position))
                return false;

            if (IsPositionBlocked(position))
                return false;

            // ✅ Check if position has Base or is occupied (movement obstacle)
            if (!gridState.IsPositionWalkable(position))
            {
                // 자기 자신이 있는 위치는 허용 (이동 중 자기 위치 체크)
                var unitAtPosition = GetUnitAtPosition(position);
                return unitAtPosition == movingUnit;
            }

            return true;
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

        // ✅ Clean Architecture: Unit Positioning Methods - Business Logic Layer Responsibility
        
        /// <summary>
        /// Move unit from one position to another with full business logic validation
        /// 데이터 레이어만 업데이트 (Transform은 MovementComponent에서 애니메이션 처리)
        /// </summary>
        public bool MoveUnit(GameObject unit, Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (unit == null || !IsValidPosition(toPosition))
                return false;

            // Business validation
            if (!CanMoveUnit(unit, toPosition))
                return false;

            // Update state through data layer (논리적 상태만, Transform 제외)
            if (!gridState.UpdateGridDataLayer(unit, toPosition))
                return false;

            // Phase 2: Transform 즉시 이동 제거
            // SetUnitWorldPosition(unit, toPosition);
            // → 이제 MovementComponent의 AnimationEvent 기반 보간으로만 처리

            // Notify presentation layer through events (already handled by UpdateGridDataLayer)
            return true;
        }
        
        /// <summary>
        /// Set unit's world position based on grid position (Business Logic responsibility)
        /// </summary>
        public void SetUnitWorldPosition(GameObject unit, Vector2Int gridPosition)
        {
            if (unit == null || !IsValidPosition(gridPosition))
                return;
                
            var worldPos = CalculateUnitWorldPosition(gridPosition);
            unit.transform.position = worldPos;
        }
        
        /// <summary>
        /// Calculate world position for unit placement including height and offset
        /// Phase 4: Base 높이 차이 반영 - CalculateWorldPositionWithHeight() + unitOffset
        /// </summary>
        public Vector3 CalculateUnitWorldPosition(Vector2Int gridPosition)
        {
            // Phase 4: 높이를 포함한 기본 좌표 (Base/Ground 판정)
            Vector3 basePos = CalculateWorldPositionWithHeight(gridPosition);

            // 유닛 배치 오프셋 추가 (시각적 보정)
            return basePos + GetUnitOffset();
        }
        
        /// <summary>
        /// Get the standard unit offset (e.g., Vector3.up * 0.5f)
        /// </summary>
        public Vector3 GetUnitOffset()
        {
            return unitOffset;
        }
        
        /// <summary>
        /// Validate and execute unit movement with comprehensive checks
        /// </summary>
        public bool ValidateAndExecuteUnitMovement(GameObject unit, Vector2Int targetPosition)
        {
            if (!TryGetUnitPosition(unit, out var currentPosition))
                return false;

            return MoveUnit(unit, currentPosition, targetPosition);
        }

        /// <summary>
        /// 지정된 그리드 위치의 Tile 컴포넌트를 반환합니다.
        /// Unit 소환 시 currentTile 설정을 위해 사용됩니다.
        /// </summary>
        /// <param name="gridPosition">찾을 타일의 그리드 위치</param>
        /// <returns>해당 위치의 Tile 컴포넌트, 없으면 null</returns>
        public Tile GetTileAtPosition(Vector2Int gridPosition)
        {
            if (!IsValidPosition(gridPosition))
            {
                Debug.LogWarning($"[GridController] GetTileAtPosition: 유효하지 않은 위치 ({gridPosition.x}, {gridPosition.y})");
                return null;
            }

            // Method 1: 이름으로 검색 (GridRenderer가 생성한 타일들)
            GameObject tileObject = GameObject.Find($"Tile_{gridPosition.x}_{gridPosition.y}");
            if (tileObject != null)
            {
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile != null)
                {
                    Debug.Log($"[GridController] Tile found by name: {tileObject.name}");
                    return tile;
                }
            }

            // Method 2: 모든 Tile에서 위치 매칭 검색
            Tile[] allTiles = UnityEngine.Object.FindObjectsOfType<Tile>();
            foreach (var tile in allTiles)
            {
                if (tile.X == gridPosition.x && tile.Y == gridPosition.y)
                {
                    Debug.Log($"[GridController] Tile found by position matching: {tile.name}");
                    return tile;
                }
            }

            Debug.LogWarning($"[GridController] Tile not found at position ({gridPosition.x}, {gridPosition.y})");
            return null;
        }

        #region IGridHeightCalculator Implementation (Phase 4: Base 높이 차이 반영)

        /// <summary>
        /// Base 높이 프로퍼티 (Inspector 설정 가능)
        /// </summary>
        public float BaseHeight
        {
            get => baseHeight;
            set => baseHeight = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Ground 기본 높이 프로퍼티 (Inspector 설정 가능)
        /// </summary>
        public float GroundHeight
        {
            get => groundHeight;
            set => groundHeight = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 특정 그리드 위치의 지면 높이 반환
        ///
        /// 비즈니스 규칙:
        /// - Base 위치: BaseHeight 반환
        /// - Ground 위치: GroundHeight 반환
        /// - 유효하지 않은 위치: GroundHeight 반환 (기본값)
        ///
        /// 확장 포인트:
        /// - 향후 지형 타입별 높이 지원 가능 (경사로, 계단 등)
        /// </summary>
        public float GetGroundHeightAt(Vector2Int gridPosition)
        {
            // 유효성 검증
            if (!IsValidPosition(gridPosition))
                return groundHeight;

            // Base 위치 확인 (GridState에서 조회)
            GameObject baseObj = gridState?.GetBaseAtPosition(gridPosition);

            // 비즈니스 규칙 적용
            return baseObj != null ? baseHeight : groundHeight;
        }

        /// <summary>
        /// 높이를 포함한 월드 좌표 계산 (Y축 포함)
        ///
        /// 계산 과정 (3단계):
        /// 1. GridToWorldPosition() - Data Layer (순수 좌표, Y=0)
        /// 2. GetGroundHeightAt() - Business Logic (높이 규칙 적용)
        /// 3. Y축 합산 - 최종 월드 좌표 반환
        ///
        /// 주의:
        /// - unitOffset은 포함하지 않음 (이동 시스템과 분리)
        /// - unitOffset은 유닛 배치 시에만 사용 (SetUnitWorldPosition 참조)
        /// </summary>
        public Vector3 CalculateWorldPositionWithHeight(Vector2Int gridPosition)
        {
            // Phase 1: Data Layer (순수 좌표 변환, Y=0)
            Vector3 basePos = GridToWorldPosition(gridPosition);

            // Phase 2: Business Logic (높이 규칙 적용)
            float height = GetGroundHeightAt(gridPosition);

            // Phase 3: 통합 반환 (Y축만 추가)
            return basePos + new Vector3(0f, height, 0f);
        }

        #endregion
    }

}