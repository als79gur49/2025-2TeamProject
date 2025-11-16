using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// 이동 시스템 인터페이스 - 의존성 역전 원칙 적용
    /// </summary>
    public interface IMovementSystem
    {
        // ✅ 기본 이동 정보
        int MovementRange { get; }
        MovementType MovementType { get; }
        
        // ✅ 이동 상태
        bool CanMove { get; }
        bool IsMoving { get; }
        bool HasMovedThisTurn { get; }
        
        // ✅ 이동 가능성 확인
        bool CanMoveTo(Vector2Int targetPosition);
        List<Vector2Int> GetValidMovePositions(int direction = 0);
        
        // ✅ 이동 실행
        MovementResult MoveTo(Vector2Int targetPosition);
        MovementResult MoveToPosition(Vector2Int targetPosition);
        MovementResult MoveInDirection(Vector2Int direction, int distance = 1);
        
        // ✅ 이동 범위 관리
        void SetMovementRange(int newRange);
        void ModifyMovementRange(int modifier);
        
        // ✅ 턴 관리
        void StartTurn();
        void EndTurn();
        void ResetMovement();
        
        // ✅ 이벤트
        event Action<Vector2Int, Vector2Int> OnMovementStarted;  // 시작위치, 목표위치
        event Action<Vector2Int, Vector2Int> OnMovementCompleted; // 시작위치, 최종위치
        event Action<Vector2Int> OnMovementCancelled;
    }

    /// <summary>
    /// 고급 이동 시스템 인터페이스 - 추가 기능
    /// </summary>
    public interface IAdvancedMovementSystem : IMovementSystem
    {
        // ✅ 고급 이동 능력
        bool CanFly { get; }
        bool CanSwim { get; }
        bool CanPhaseThrough { get; }
        bool CanJump { get; }
        int JumpRange { get; }
        
        // ✅ 이동 속도 및 효율
        float MovementSpeed { get; }
        float MovementEfficiency { get; }
        TerrainMovementCosts MovementCosts { get; }
        
        // ✅ 특수 이동
        bool CanTeleport { get; }
        int TeleportRange { get; }
        int TeleportCooldown { get; }
        bool CanUseTeleport { get; }
        
        // ✅ 이동 제한
        List<TerrainType> ImpassableTerrains { get; }
        List<TerrainType> SlowTerrains { get; }
        bool IsTerrainPassable(TerrainType terrain);
        
        // ✅ 고급 이동 메서드
        MovementResult Teleport(Vector2Int targetPosition);
        MovementResult Jump(Vector2Int targetPosition);
        MovementResult MoveWithPathfinding(Vector2Int targetPosition);
        
        // ✅ 이동 비용 계산
        int CalculateMovementCost(Vector2Int fromPosition, Vector2Int toPosition);
        int CalculatePathCost(List<Vector2Int> path);
        List<Vector2Int> GetOptimalPath(Vector2Int targetPosition);
        
        // ✅ 설정 메서드
        void SetMovementSpeed(float speed);
        void SetMovementAbility(MovementAbility ability, bool enabled);
        void SetTerrainMovementCost(TerrainType terrain, int cost);
        
        // ✅ 고급 이벤트
        event Action<Vector2Int> OnTeleportUsed;
        event Action<Vector2Int> OnJumpPerformed;
        event Action<MovementAbility, bool> OnMovementAbilityChanged;
        event Action<TerrainType, int> OnTerrainCostChanged;
    }

    /// <summary>
    /// 이동 결과 구조체
    /// </summary>
    public readonly struct MovementResult
    {
        public readonly bool Success;
        public readonly Vector2Int StartPosition;
        public readonly Vector2Int EndPosition;
        public readonly List<Vector2Int> Path;
        public readonly float TimeTaken;
        public readonly string Message;

        public MovementResult(bool success, Vector2Int startPosition, Vector2Int endPosition,
                             List<Vector2Int> path, float timeTaken, string message = "")
        {
            Success = success;
            StartPosition = startPosition;
            EndPosition = endPosition;
            Path = path ?? new List<Vector2Int>();
            TimeTaken = timeTaken;
            Message = message ?? "";
        }

        public static MovementResult Failed(Vector2Int position, string message) =>
            new MovementResult(false, position, position, null, 0f, message);

        public static MovementResult Succeeded(Vector2Int start, Vector2Int end, List<Vector2Int> path, 
                                              float time, string message = "") =>
            new MovementResult(true, start, end, path, time, message);
    }

    /// <summary>
    /// 이동 타입 열거형
    /// </summary>
    public enum MovementType
    {
        Ground,         // 지상 이동
        Flying,         // 비행
        Swimming,       // 수영
        Teleportation,  // 순간이동
        Phasing,        // 위상 이동
        Jumping         // 점프
    }

    /// <summary>
    /// 이동 능력 열거형
    /// </summary>
    [Flags]
    public enum MovementAbility
    {
        None = 0,
        Flying = 1 << 0,
        Swimming = 1 << 1,
        PhaseThrough = 1 << 2,
        Jumping = 1 << 3,
        Teleporting = 1 << 4,
        IgnoreTerrain = 1 << 5,
        FastMovement = 1 << 6
    }

    /// <summary>
    /// 지형 타입 열거형
    /// </summary>
    public enum TerrainType
    {
        Normal,         // 일반 지형
        Difficult,      // 어려운 지형 (2배 비용)
        Water,          // 물 (수영 필요)
        Mountain,       // 산 (비행 또는 점프 필요)
        Void,           // 공허 (이동 불가)
        Trap,           // 함정
        Healing,        // 회복 지형
        Damaging        // 피해 지형
    }

    /// <summary>
    /// 지형별 이동 비용 설정
    /// </summary>
    [System.Serializable]
    public class TerrainMovementCosts
    {
        [SerializeField] private Dictionary<TerrainType, int> costs = new Dictionary<TerrainType, int>
        {
            { TerrainType.Normal, 1 },
            { TerrainType.Difficult, 2 },
            { TerrainType.Water, 2 },
            { TerrainType.Mountain, 3 },
            { TerrainType.Void, int.MaxValue },
            { TerrainType.Trap, 1 },
            { TerrainType.Healing, 1 },
            { TerrainType.Damaging, 1 }
        };

        public int GetCost(TerrainType terrain)
        {
            return costs.TryGetValue(terrain, out int cost) ? cost : 1;
        }

        public void SetCost(TerrainType terrain, int cost)
        {
            costs[terrain] = Mathf.Max(1, cost);
        }

        public bool IsPassable(TerrainType terrain)
        {
            return GetCost(terrain) < int.MaxValue;
        }
    }

    /// <summary>
    /// 이동 제약 조건
    /// </summary>
    [System.Serializable]
    public class MovementConstraints
    {
        [SerializeField] private bool canMoveWhileDamaged = true;
        [SerializeField] private bool canMoveWhileInCombat = true;
        [SerializeField] private int minimumMovementRange = 0;
        [SerializeField] private int maximumMovementRange = int.MaxValue;

        public bool CanMoveWhileDamaged => canMoveWhileDamaged;
        public bool CanMoveWhileInCombat => canMoveWhileInCombat;
        public int MinimumMovementRange => minimumMovementRange;
        public int MaximumMovementRange => maximumMovementRange;

        public bool IsMovementAllowed(IHealthComponent health, ICombatSystem combat, int range)
        {
            // 체력 조건 확인
            if (!canMoveWhileDamaged && health != null && !health.IsFullHealth)
                return false;

            // 전투 조건 확인
            if (!canMoveWhileInCombat && combat != null && combat.IsInCombat)
                return false;

            // 거리 조건 확인
            if (range < minimumMovementRange || range > maximumMovementRange)
                return false;

            return true;
        }
    }

    /// <summary>
    /// 이동 관련 유틸리티 확장 메서드
    /// </summary>
    public static class MovementSystemExtensions
    {
        /// <summary>
        /// 맨해튼 거리 계산
        /// </summary>
        public static int GetManhattanDistance(this Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
        }

        /// <summary>
        /// 유클리드 거리 계산
        /// </summary>
        public static float GetEuclideanDistance(this Vector2Int from, Vector2Int to)
        {
            return Vector2.Distance(from, to);
        }

        /// <summary>
        /// 체비셰프 거리 계산 (대각선 이동 포함)
        /// </summary>
        public static int GetChebyshevDistance(this Vector2Int from, Vector2Int to)
        {
            return Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
        }

        /// <summary>
        /// 이동 가능 여부 확인 (팀과 체력 고려)
        /// </summary>
        public static bool CanMoveConsidering(this IMovementSystem movement, Vector2Int targetPosition,
                                            IHealthComponent health, ICombatSystem combat)
        {
            if (!movement.CanMoveTo(targetPosition)) return false;
            
            if (movement is Component movementComponent)
            {
                var constraints = movementComponent.GetComponent<MovementConstraints>();
                if (constraints != null)
                {
                    int distance = movementComponent.transform.position.ToGridPosition().GetManhattanDistance(targetPosition);
                    return constraints.IsMovementAllowed(health, combat, distance);
                }
            }

            return true;
        }

        /// <summary>
        /// 이동 효율성 계산
        /// </summary>
        public static float CalculateMovementEfficiency(this IMovementSystem movement, List<Vector2Int> path)
        {
            if (path == null || path.Count < 2) return 1f;

            int directDistance = path[0].GetManhattanDistance(path[path.Count - 1]);
            int actualDistance = path.Count - 1;

            return directDistance / (float)actualDistance;
        }

        /// <summary>
        /// 방향 벡터를 각도로 변환
        /// </summary>
        public static float GetDirectionAngle(this Vector2Int direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 인접한 위치들 가져오기
        /// </summary>
        public static List<Vector2Int> GetAdjacentPositions(this Vector2Int position, bool includeDiagonals = false)
        {
            var positions = new List<Vector2Int>
            {
                position + Vector2Int.up,
                position + Vector2Int.down,
                position + Vector2Int.left,
                position + Vector2Int.right
            };

            if (includeDiagonals)
            {
                positions.AddRange(new[]
                {
                    position + new Vector2Int(-1, -1),
                    position + new Vector2Int(-1, 1),
                    position + new Vector2Int(1, -1),
                    position + new Vector2Int(1, 1)
                });
            }

            return positions;
        }
    }

    /// <summary>
    /// Vector3를 Vector2Int로 변환하는 확장 메서드
    /// </summary>
    public static class Vector3Extensions
    {
        public static Vector2Int ToGridPosition(this Vector3 worldPosition)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
        }
    }
}
