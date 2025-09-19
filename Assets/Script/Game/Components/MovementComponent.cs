using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;
using Game.Data;
using Game.Core;

namespace Game.Components
{
    /// <summary>
    /// 이동 시스템 컴포넌트 구현
    /// </summary>
    [System.Serializable]
    public class MovementComponent : MonoBehaviour, IAdvancedMovementSystem
    {
        [Header("기본 이동 설정")]
        [SerializeField] private int movementRange = 3;
        [SerializeField] private MovementType movementType = MovementType.Ground;
        [SerializeField] private float movementSpeed = 1f;
        [SerializeField] private float movementEfficiency = 1f;

        [Header("특수 이동 능력")]
        [SerializeField] private MovementAbility movementAbilities = MovementAbility.None;
        [SerializeField] private int jumpRange = 2;
        [SerializeField] private int teleportRange = 5;
        [SerializeField] private int teleportCooldown = 3;

        [Header("지형 설정")]
        [SerializeField] private TerrainMovementCosts movementCosts = new TerrainMovementCosts();
        [SerializeField] private List<TerrainType> impassableTerrains = new List<TerrainType> { TerrainType.Void };
        [SerializeField] private List<TerrainType> slowTerrains = new List<TerrainType> { TerrainType.Difficult, TerrainType.Water };

        [Header("이동 제약")]
        [SerializeField] private MovementConstraints constraints = new MovementConstraints();

        // 런타임 상태
        private int currentMovementPoints;
        private int maxMovementPoints;
        private bool hasMovedThisTurn = false;
        private bool isMoving = false;
        private int lastTeleportTurn = -999;
        private List<StatModifier> movementRangeModifiers = new List<StatModifier>();

        // 캐시된 컴포넌트
        private IGridManager gridManager;
        private IHealthComponent healthComponent;
        private ICombatSystem combatSystem;
        private ITeamComponent teamComponent;

        #region Unity Lifecycle

        private void Awake()
        {
            // 컴포넌트 캐싱
            healthComponent = GetComponent<IHealthComponent>();
            combatSystem = GetComponent<ICombatSystem>();
            teamComponent = GetComponent<ITeamComponent>();
            
            // 초기 이동력 설정
            maxMovementPoints = movementRange;
            currentMovementPoints = maxMovementPoints;
        }

        private void Start()
        {
            // GridManager는 ServiceLocator에서 가져오기
            gridManager = ServiceLocator.Get<IGridManager>();
        }

        private void OnValidate()
        {
            // 에디터에서 값 검증
            movementRange = Mathf.Max(0, movementRange);
            jumpRange = Mathf.Max(1, jumpRange);
            teleportRange = Mathf.Max(1, teleportRange);
            teleportCooldown = Mathf.Max(0, teleportCooldown);
            movementSpeed = Mathf.Max(0.1f, movementSpeed);
            movementEfficiency = Mathf.Clamp01(movementEfficiency);
        }

        #endregion

        #region IMovementSystem Implementation

        public int MovementRange => GetModifiedMovementRange();
        public int CurrentMovementPoints => currentMovementPoints;
        public int MaxMovementPoints => maxMovementPoints;
        public MovementType MovementType => movementType;
        public bool CanMove => healthComponent?.IsAlive == true && currentMovementPoints > 0 && !isMoving;
        public bool IsMoving => isMoving;
        public bool HasMovedThisTurn => hasMovedThisTurn;

        public bool CanMoveTo(Vector2Int targetPosition)
        {
            if (!CanMove) return false;
            if (gridManager == null) return false;

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            int distance = currentPosition.GetManhattanDistance(targetPosition);

            // 이동 거리 확인
            if (distance > currentMovementPoints) return false;

            // 제약 조건 확인
            if (!constraints.IsMovementAllowed(healthComponent, combatSystem, distance)) return false;

            // 그리드 유효성 확인
            if (!gridManager.IsValidPosition(targetPosition)) return false;
            if (gridManager.IsPositionOccupied(targetPosition)) return false;

            // 경로 확인
            if (CanFly || CanPhaseThrough)
            {
                return true; // 비행이나 위상 이동 가능하면 직접 이동
            }

            var path = gridManager.FindPath(currentPosition, targetPosition, gameObject);
            return path != null && path.Count > 0;
        }

        public bool CanMoveDistance(int distance)
        {
            return CanMove && distance <= currentMovementPoints && distance >= 0;
        }

        public List<Vector2Int> GetValidMovePositions()
        {
            return GetValidMovePositions(currentMovementPoints);
        }

        public List<Vector2Int> GetValidMovePositions(int range)
        {
            var validPositions = new List<Vector2Int>();
            
            if (gridManager == null) return validPositions;

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            
            if (CanFly || CanPhaseThrough)
            {
                // 비행이나 위상 이동 가능하면 범위 내 모든 위치
                for (int x = -range; x <= range; x++)
                {
                    for (int y = -range; y <= range; y++)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(y) <= range)
                        {
                            var pos = currentPosition + new Vector2Int(x, y);
                            if (gridManager.IsValidPosition(pos) && !gridManager.IsPositionOccupied(pos))
                            {
                                validPositions.Add(pos);
                            }
                        }
                    }
                }
            }
            else
            {
                // 일반 이동은 경로 탐색 사용
                var allPositions = gridManager.GetPositionsInRange(currentPosition, range, false);
                foreach (var pos in allPositions)
                {
                    if (CanMoveTo(pos))
                    {
                        validPositions.Add(pos);
                    }
                }
            }

            return validPositions;
        }

        public MovementResult MoveTo(Vector2Int targetPosition)
        {
            return MoveToPosition(targetPosition, true);
        }

        public MovementResult MoveToPosition(Vector2Int targetPosition, bool useMovementPoints = true)
        {
            var startPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;
            
            if (!CanMoveTo(targetPosition))
            {
                return MovementResult.Failed(startPosition, "Cannot move to target position");
            }

            var startTime = Time.time;
            OnMovementStarted?.Invoke(startPosition, targetPosition);
            isMoving = true;

            try
            {
                // 경로 계산
                List<Vector2Int> path;
                int movementCost;

                if (CanFly || CanPhaseThrough)
                {
                    path = new List<Vector2Int> { startPosition, targetPosition };
                    movementCost = startPosition.GetManhattanDistance(targetPosition);
                }
                else
                {
                    path = gridManager.FindPath(startPosition, targetPosition, gameObject);
                    if (path == null || path.Count == 0)
                    {
                        return MovementResult.Failed(startPosition, "No valid path found");
                    }
                    movementCost = CalculatePathCost(path);
                }

                // 이동 실행 - fromPosition과 toPosition을 명시적으로 전달
                if (gridManager.MoveUnit(gameObject, startPosition, targetPosition))
                {
                    if (useMovementPoints)
                    {
                        ConsumeMovementPoints(movementCost);
                    }

                    hasMovedThisTurn = true;
                    var timeTaken = Time.time - startTime;
                    
                    var result = MovementResult.Succeeded(startPosition, targetPosition, path, 
                                                        movementCost, timeTaken, "Movement successful");
                    
                    OnMovementCompleted?.Invoke(startPosition, targetPosition);
                    return result;
                }
                else
                {
                    return MovementResult.Failed(startPosition, "Failed to move unit on grid");
                }
            }
            finally
            {
                isMoving = false;
            }
        }

        public MovementResult MoveInDirection(Vector2Int direction, int distance = 1)
        {
            if (gridManager == null)
            {
                return MovementResult.Failed(Vector2Int.zero, "No grid manager available");
            }

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            var targetPosition = currentPosition + (direction * distance);

            return MoveToPosition(targetPosition);
        }

        public void SetMovementRange(int newRange)
        {
            var oldRange = movementRange;
            movementRange = Mathf.Max(0, newRange);
            
            if (oldRange != movementRange)
            {
                RefreshMovementPoints();
            }
        }

        public void ModifyMovementRange(int modifier)
        {
            SetMovementRange(movementRange + modifier);
        }

        public void ConsumeMovementPoints(int points)
        {
            int oldPoints = currentMovementPoints;
            currentMovementPoints = Mathf.Max(0, currentMovementPoints - points);
            
            if (oldPoints != currentMovementPoints)
            {
                OnMovementPointsChanged?.Invoke(currentMovementPoints);
            }
        }

        public void RestoreMovementPoints(int points)
        {
            int oldPoints = currentMovementPoints;
            currentMovementPoints = Mathf.Min(maxMovementPoints, currentMovementPoints + points);
            
            if (oldPoints != currentMovementPoints)
            {
                OnMovementPointsChanged?.Invoke(currentMovementPoints);
            }
        }

        public void RefreshMovementPoints()
        {
            int oldMax = maxMovementPoints;
            int oldCurrent = currentMovementPoints;
            
            maxMovementPoints = MovementRange;
            currentMovementPoints = maxMovementPoints;
            
            if (oldMax != maxMovementPoints || oldCurrent != currentMovementPoints)
            {
                OnMovementPointsChanged?.Invoke(currentMovementPoints);
                OnMovementRefreshed?.Invoke();
            }
        }

        public void StartTurn()
        {
            RefreshMovementPoints();
            hasMovedThisTurn = false;
        }

        public void EndTurn()
        {
            // 턴 종료시 특별한 처리가 필요하면 여기에 추가
        }

        public void ResetMovement()
        {
            hasMovedThisTurn = false;
            isMoving = false;
            RefreshMovementPoints();
        }

        #endregion

        #region IAdvancedMovementSystem Implementation

        public bool CanFly => movementAbilities.HasFlag(MovementAbility.Flying);
        public bool CanSwim => movementAbilities.HasFlag(MovementAbility.Swimming);
        public bool CanPhaseThrough => movementAbilities.HasFlag(MovementAbility.PhaseThrough);
        public bool CanJump => movementAbilities.HasFlag(MovementAbility.Jumping);
        public int JumpRange => jumpRange;
        public float MovementSpeed => movementSpeed;
        public float MovementEfficiency => movementEfficiency;
        public TerrainMovementCosts MovementCosts => movementCosts;
        public bool CanTeleport => movementAbilities.HasFlag(MovementAbility.Teleporting);
        public int TeleportRange => teleportRange;
        public int TeleportCooldown => teleportCooldown;
        public bool CanUseTeleport => CanTeleport && (Time.fixedTime - lastTeleportTurn) >= teleportCooldown;
        public List<TerrainType> ImpassableTerrains => new List<TerrainType>(impassableTerrains);
        public List<TerrainType> SlowTerrains => new List<TerrainType>(slowTerrains);

        public bool IsTerrainPassable(TerrainType terrain)
        {
            if (CanFly && terrain != TerrainType.Void) return true;
            if (CanPhaseThrough) return true;
            
            return !impassableTerrains.Contains(terrain);
        }

        public MovementResult Teleport(Vector2Int targetPosition)
        {
            if (!CanUseTeleport)
            {
                return MovementResult.Failed(Vector2Int.zero, "Teleport not available");
            }

            var currentPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;
            int distance = currentPosition.GetManhattanDistance(targetPosition);
            
            if (distance > teleportRange)
            {
                return MovementResult.Failed(currentPosition, "Target too far for teleport");
            }

            if (gridManager == null || !gridManager.IsValidPosition(targetPosition) || 
                gridManager.IsPositionOccupied(targetPosition))
            {
                return MovementResult.Failed(currentPosition, "Invalid teleport destination");
            }

            lastTeleportTurn = (int)Time.fixedTime;
            OnTeleportUsed?.Invoke(targetPosition);

            return MoveToPosition(targetPosition, false); // 텔레포트는 이동력 소모 없음
        }

        public MovementResult Jump(Vector2Int targetPosition)
        {
            if (!CanJump)
            {
                return MovementResult.Failed(Vector2Int.zero, "Cannot jump");
            }

            var currentPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;
            int distance = currentPosition.GetManhattanDistance(targetPosition);
            
            if (distance > jumpRange)
            {
                return MovementResult.Failed(currentPosition, "Jump distance too far");
            }

            OnJumpPerformed?.Invoke(targetPosition);
            return MoveToPosition(targetPosition);
        }

        public MovementResult MoveWithPathfinding(Vector2Int targetPosition)
        {
            var currentPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;
            var path = GetOptimalPath(targetPosition);
            
            if (path == null || path.Count == 0)
            {
                return MovementResult.Failed(currentPosition, "No path found");
            }

            return MoveToPosition(targetPosition);
        }

        public int CalculateMovementCost(Vector2Int fromPosition, Vector2Int toPosition)
        {
            // 기본 거리 비용
            int baseCost = fromPosition.GetManhattanDistance(toPosition);
            
            // 지형 비용 적용 (간단화된 버전)
            return Mathf.RoundToInt(baseCost * movementEfficiency);
        }

        public int CalculatePathCost(List<Vector2Int> path)
        {
            if (path == null || path.Count < 2) return 0;

            int totalCost = 0;
            for (int i = 1; i < path.Count; i++)
            {
                totalCost += CalculateMovementCost(path[i - 1], path[i]);
            }

            return totalCost;
        }

        public List<Vector2Int> GetOptimalPath(Vector2Int targetPosition)
        {
            if (gridManager == null) return new List<Vector2Int>();

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            return gridManager.FindPath(currentPosition, targetPosition, gameObject);
        }

        public void SetMovementSpeed(float speed)
        {
            movementSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetMovementAbility(MovementAbility ability, bool enabled)
        {
            var oldAbilities = movementAbilities;
            
            if (enabled)
            {
                movementAbilities |= ability;
            }
            else
            {
                movementAbilities &= ~ability;
            }

            if (oldAbilities != movementAbilities)
            {
                OnMovementAbilityChanged?.Invoke(ability, enabled);
            }
        }

        public void SetTerrainMovementCost(TerrainType terrain, int cost)
        {
            var oldCost = movementCosts.GetCost(terrain);
            movementCosts.SetCost(terrain, cost);
            
            if (oldCost != cost)
            {
                OnTerrainCostChanged?.Invoke(terrain, cost);
            }
        }

        #endregion

        #region Events

        public event Action<Vector2Int, Vector2Int> OnMovementStarted;
        public event Action<Vector2Int, Vector2Int> OnMovementCompleted;
        public event Action<Vector2Int> OnMovementCancelled;
        public event Action<int> OnMovementPointsChanged;
        public event Action OnMovementRefreshed;
        public event Action<Vector2Int> OnTeleportUsed;
        public event Action<Vector2Int> OnJumpPerformed;
        public event Action<MovementAbility, bool> OnMovementAbilityChanged;
        public event Action<TerrainType, int> OnTerrainCostChanged;

        #endregion

        #region Private Methods

        private int GetModifiedMovementRange()
        {
            float totalRange = movementRange;
            
            foreach (var modifier in movementRangeModifiers)
            {
                totalRange = modifier.ApplyModifier(totalRange);
            }
            
            return Mathf.RoundToInt(totalRange);
        }

        #endregion

        #region Stat Modifier Support

        public void AddMovementRangeModifier(StatModifier modifier)
        {
            if (modifier != null)
            {
                movementRangeModifiers.Add(modifier);
                RefreshMovementPoints();
            }
        }

        public void RemoveMovementRangeModifier(StatModifier modifier)
        {
            if (movementRangeModifiers.Remove(modifier))
            {
                RefreshMovementPoints();
            }
        }

        public void ClearMovementRangeModifiers()
        {
            if (movementRangeModifiers.Count > 0)
            {
                movementRangeModifiers.Clear();
                RefreshMovementPoints();
            }
        }

        #endregion

        #region Debug and Visualization

        private void OnDrawGizmos()
        {
            if (gridManager != null)
            {
                var currentPosition = gridManager.GetUnitPosition(gameObject);
                var validPositions = GetValidMovePositions();

                // 이동 가능한 위치 표시
                Gizmos.color = Color.green;
                foreach (var pos in validPositions)
                {
                    var worldPos = gridManager.GridToWorldPosition(pos);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                }

                // 텔레포트 범위 표시
                if (CanTeleport)
                {
                    Gizmos.color = Color.blue;
                    var centerWorld = gridManager.GridToWorldPosition(currentPosition);
                    Gizmos.DrawWireSphere(centerWorld, teleportRange);
                }

                // 점프 범위 표시
                if (CanJump)
                {
                    Gizmos.color = Color.yellow;
                    var centerWorld = gridManager.GridToWorldPosition(currentPosition);
                    Gizmos.DrawWireSphere(centerWorld, jumpRange);
                }
            }
        }

        #endregion
    }
}