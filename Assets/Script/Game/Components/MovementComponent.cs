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
    ///
    /// ========================================
    /// ✅ 완전한 이벤트 기반 루프 사운드 제어 (권장)
    /// ========================================
    ///
    /// 이동 시스템에서 Owner 기반 루프 사운드를 완전히 이벤트 기반으로 제어하는 방법:
    ///
    /// 1. 필드 선언:
    ///    [SerializeField] private SoundEventChannelSO soundEventChannel;
    ///    [SerializeField] private AudioData movementLoopSound;
    ///
    /// 2. ✅ 이동 시작 시 루프 재생 (이벤트 채널 사용):
    ///    soundEventChannel.RaiseLoopSoundEvent(movementLoopSound, this);
    ///
    /// 3. ✅ 이동 중지 시 특정 사운드만 중지 (이벤트 채널 사용):
    ///    soundEventChannel.RaiseStopLoopEvent(this, movementLoopSound);
    ///    // 또는 명시적 오버로드: soundEventChannel.RaiseStopSpecificLoopEvent(this, movementLoopSound);
    ///
    /// 4. ✅ 유닛 파괴 시 모든 루프 사운드 중지 (이벤트 채널 사용):
    ///    private void OnDestroy() {
    ///        soundEventChannel.RaiseStopLoopEvent(this); // audioData = null
    ///        // 또는 명시적 오버로드: soundEventChannel.RaiseStopAllLoopsEvent(this);
    ///    }
    ///
    /// ========================================
    /// 🎯 핵심 장점 (완전한 이벤트 기반)
    /// ========================================
    ///
    /// ✅ 완전한 분리 (Zero Coupling):
    ///    - AudioManager, IEffectAudioService에 대한 참조 불필요
    ///    - SoundEventChannelSO만 알면 모든 오디오 제어 가능
    ///    - 테스트 시 이벤트 채널만 모킹하면 됨
    ///
    /// ✅ 대칭적 API:
    ///    - 시작: RaiseLoopSoundEvent(audioData, owner)
    ///    - 종료: RaiseStopLoopEvent(owner, audioData)
    ///    - 일관성 있는 패턴으로 코드 가독성 향상
    ///
    /// ✅ Owner 기반 자동 정리:
    ///    - Owner가 파괴되면 해당 Owner의 모든 사운드 한 번에 정리
    ///    - 특정 AudioData만 선택적으로 중지 가능
    ///    - 메모리 누수 방지 및 리소스 관리 자동화
    ///
    /// ✅ Open-Closed Principle:
    ///    - 새로운 리스너(ScreenShake, Analytics 등) 추가 시 기존 코드 변경 불필요
    ///    - 이벤트 채널에 구독만 추가하면 됨
    ///
    /// ========================================
    /// ⚠️ 레거시 패턴 (사용 비권장)
    /// ========================================
    ///
    /// ❌ 직접 서비스 호출 (이벤트 기반 원칙 위반):
    ///    effectAudioService.PlayEffectLoop(movementLoopSound, this);
    ///    effectAudioService.StopLoopsByOwner(this, movementLoopSound);
    ///
    ///    문제점:
    ///    - IEffectAudioService에 직접 의존 (Tight Coupling)
    ///    - 테스트 시 서비스 전체를 모킹해야 함
    ///    - 이벤트 기반 아키텍처의 장점 상실
    ///
    /// ========================================
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

        // Phase 2: Transform 보간 관리
        private Coroutine currentTransformMoveCoroutine;
        private bool isTransformMoving = false; // Transform 보간 진행 상태

        // Phase 5: 순차 이동 시스템
        private Coroutine sequentialMovementCoroutine;
        private bool isSequentialMoving = false; // 순차 이동 진행 상태

        // 새로운 Action System 필드
        private ActionResult currentMoveResult;
        private ActionContext currentMoveContext;

        // 캐시된 컴포넌트
        private IGridManager gridManager;
        private IHealthComponent healthComponent;
        private ICombatSystem combatSystem;
        private ITeamComponent teamComponent;
        private IAnimationController animationController;

        #region Unity Lifecycle

        private void Awake()
        {
            // 컴포넌트 캐싱
            healthComponent = GetComponent<IHealthComponent>();
            combatSystem = GetComponent<ICombatSystem>();
            teamComponent = GetComponent<ITeamComponent>();
            animationController = GetComponent<IAnimationController>();

            // 초기 이동력 설정
            maxMovementPoints = movementRange;
            currentMovementPoints = maxMovementPoints;

            // 애니메이션 이벤트 구독
            if (animationController != null)
            {
                // BlendTree 이동 이벤트 구독
                animationController.OnMoveStart += OnTransformMoveStart;
                animationController.OnMoveEnd += OnTransformMoveEnd;
                animationController.OnAnimationInterrupted += OnAnimationInterrupted;
            }
        }

        private void OnDestroy()
        {
            // 애니메이션 이벤트 구독 해제
            if (animationController != null)
            {
                // BlendTree 이동 이벤트 구독 해제
                animationController.OnMoveStart -= OnTransformMoveStart;
                animationController.OnMoveEnd -= OnTransformMoveEnd;
                animationController.OnAnimationInterrupted -= OnAnimationInterrupted;
            }
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
        // Phase 2: Transform 보간 완료까지 다음 이동 차단
        // Phase 5: 순차 이동 진행 중에도 다음 이동 차단
        public bool CanMove => healthComponent?.IsAlive == true && currentMovementPoints > 0 && !isMoving && !isTransformMoving && !isSequentialMoving;
        public bool IsMoving => isMoving || isSequentialMoving; // Phase 5: 순차 이동 중에도 isMoving true
        public bool HasMovedThisTurn => hasMovedThisTurn;

        public bool CanMoveTo(Vector2Int targetPosition)
        {
            //if (!CanMove) return false;
            //if (gridManager == null) return false;

            if (!CanMove)
            {
                Debug.LogError($"healthComponent is alive {healthComponent?.IsAlive} " +
                    $"| currentMovementPoints {currentMovementPoints}" +
                    $"| isMoveing {isMoving}");

                return false;
            }

            if (gridManager == null)
            {
                Debug.LogError($"gridManager is null");

                return false;
            }

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            int distance = currentPosition.GetManhattanDistance(targetPosition);

            // 이동 거리 확인
            if (distance > currentMovementPoints) return false;

            // 제약 조건 확인
            if (!constraints.IsMovementAllowed(healthComponent, combatSystem, distance)) return false;

            // 그리드 유효성 확인
            if (!gridManager.IsValidPosition(targetPosition))
            {
                Debug.LogError($"IsNotValidPosition at {targetPosition.x},{targetPosition.y}");

                return false;
            }

            if (gridManager.IsPositionOccupied(targetPosition))
            {
                Debug.LogError($"IsPositionOccupied at {targetPosition.x},{targetPosition.y}");

                return false;
            }

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

        /// <summary>
        /// 좌우 방향으로만 이동 가능한 위치 탐색 (Y축만 허용, X축 및 대각선 금지)
        /// </summary>
        /// <param name="direction">방향 필터 (1: 우측만, -1: 좌측만, 0: 양방향)</param>
        public List<Vector2Int> GetValidMovePositions(int direction = 0)
        {
            var validPositions = new List<Vector2Int>();

            if (gridManager == null) return validPositions;

            var currentPosition = gridManager.GetUnitPosition(gameObject);
            int range = currentMovementPoints;

            // 좌우 방향으로만 탐색 (Y축만, X축 고정)
            if (direction > 0)
            {
                // 우측 방향만 (Y+)
                for (int y = 1; y <= range; y++)
                {
                    var pos = new Vector2Int(currentPosition.x, currentPosition.y + y);

                    if (gridManager.IsValidPosition(pos) && gridManager.IsPositionWalkable(pos))
                    {
                        validPositions.Add(pos);

                        // 장애물이 있으면 더 이상 진행 불가
                        if (!CanContinuePath(currentPosition, pos))
                            break;
                    }
                    else
                    {
                        break; // 유효하지 않거나 이동 불가능한 경우 더 이상 진행 불가
                    }
                }
            }
            else if (direction < 0)
            {
                // 좌측 방향만 (Y-)
                for (int y = 1; y <= range; y++)
                {
                    var pos = new Vector2Int(currentPosition.x, currentPosition.y - y);

                    if (gridManager.IsValidPosition(pos) && gridManager.IsPositionWalkable(pos))
                    {
                        validPositions.Add(pos);

                        // 장애물이 있으면 더 이상 진행 불가
                        if (!CanContinuePath(currentPosition, pos))
                            break;
                    }
                    else
                    {
                        break; // 유효하지 않거나 이동 불가능한 경우 더 이상 진행 불가
                    }
                }
            }
            else
            {
                // 양방향 탐색 (direction == 0)
                // 우측 방향
                for (int y = 1; y <= range; y++)
                {
                    var pos = new Vector2Int(currentPosition.x, currentPosition.y + y);

                    if (gridManager.IsValidPosition(pos) && gridManager.IsPositionWalkable(pos))
                    {
                        validPositions.Add(pos);

                        if (!CanContinuePath(currentPosition, pos))
                            break;
                    }
                    else
                    {
                        break;
                    }
                }

                // 좌측 방향
                for (int y = 1; y <= range; y++)
                {
                    var pos = new Vector2Int(currentPosition.x, currentPosition.y - y);

                    if (gridManager.IsValidPosition(pos) && gridManager.IsPositionWalkable(pos))
                    {
                        validPositions.Add(pos);

                        if (!CanContinuePath(currentPosition, pos))
                            break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return validPositions;
        }

        /// <summary>
        /// 경로를 계속 진행할 수 있는지 확인 (지형 체크 등)
        /// </summary>
        private bool CanContinuePath(Vector2Int from, Vector2Int to)
        {
            // 비행이나 위상 이동이 가능하면 항상 통과
            if (CanFly || CanPhaseThrough)
                return true;

            // 추가적인 지형 체크가 필요하면 여기에 구현
            // 현재는 기본적으로 통과 허용
            return true;
        }

        public MovementResult MoveTo(Vector2Int targetPosition)
        {
            return MoveToPosition(targetPosition, true);
        }

        public MovementResult MoveToPosition(Vector2Int targetPosition, bool useMovementPoints = true)
        {
            var startPosition = gridManager?.GetUnitPosition(gameObject) ?? Vector2Int.zero;

            // Phase 5: 순차 이동을 위한 경로 계산 먼저 수행
            List<Vector2Int> path = null;
            int movementCost = 0;

            if (CanFly || CanPhaseThrough)
            {
                // 비행/위상 이동: 직선 경로 생성
                movementCost = startPosition.GetManhattanDistance(targetPosition);
                path = GenerateStraightPath(startPosition, targetPosition);
            }
            else
            {
                // 일반 이동: A* 경로 탐색
                path = gridManager.FindPath(startPosition, targetPosition, gameObject);
                if (path == null || path.Count < 2)
                {
                    return MovementResult.Failed(startPosition, "No valid path found");
                }
                movementCost = path.Count - 1; // 시작 위치 제외한 칸 수
            }

            // 이동력 검증
            if (useMovementPoints && movementCost > currentMovementPoints)
            {
                return MovementResult.Failed(startPosition,
                    $"Insufficient movement points: need {movementCost}, have {currentMovementPoints}");
            }

            // 목표 위치 점유 확인
            if (gridManager.IsPositionOccupied(targetPosition))
            {
                var occupyingUnit = gridManager.GetUnitAtPosition(targetPosition);
                if (occupyingUnit != null && occupyingUnit != gameObject)
                {
                    return MovementResult.Failed(startPosition,
                        $"Target position occupied by {occupyingUnit.name}");
                }
            }

            // Phase 5: 이전 순차 이동 중단 (안전장치)
            InterruptSequentialMovement();

            OnMovementStarted?.Invoke(startPosition, targetPosition);
            isMoving = true;
            hasMovedThisTurn = true;

            // Phase 5: 순차 이동 코루틴 시작 (1칸씩 자동 이동)
            sequentialMovementCoroutine = StartCoroutine(MoveAlongPathSequentially(path));

            Debug.Log($"[MovementComponent] {gameObject.name}: Sequential movement started - " +
                     $"{movementCost} steps from {startPosition} to {targetPosition}");

            // 이동 시작 성공 반환 (실제 완료는 코루틴에서 비동기 처리)
            var result = MovementResult.Succeeded(startPosition, targetPosition, path,
                                                movementCost, 0f, "Sequential movement started");

            // Phase 5: OnMovementCompleted는 코루틴 완료 시 발생 (MoveAlongPathSequentially 내부)
            // isMoving은 코루틴 완료 시 자동으로 false 처리됨
            return result;
        }

        /// <summary>
        /// 비행/위상 이동을 위한 직선 경로 생성
        /// </summary>
        private List<Vector2Int> GenerateStraightPath(Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int> { start };

            Vector2Int current = start;
            while (current != end)
            {
                // 맨하탄 거리 기반 직선 이동 (X 또는 Y 한 방향씩)
                if (current.x != end.x)
                {
                    current.x += (end.x > current.x) ? 1 : -1;
                }
                else if (current.y != end.y)
                {
                    current.y += (end.y > current.y) ? 1 : -1;
                }

                path.Add(current);
            }

            return path;
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
            // 🔧 방어적 상태 초기화 - 적 처치 후 이동 불가 문제 해결
            isMoving = false;                    // 강제로 이동 중 플래그 초기화
            hasMovedThisTurn = false;           // 턴 행동 상태 초기화
            RefreshMovementPoints();            // 이동력 포인트 복구
            
            Debug.Log($"[MovementComponent] {gameObject.name} StartTurn() - CanMove: {CanMove}, " +
                     $"MovementPoints: {currentMovementPoints}, IsMoving: {isMoving}");
        }

        public void EndTurn()
        {
            // 턴 종료시 특별한 처리가 필요하면 여기에 추가
        }

        public void ResetMovement()
        {
            // Phase 2: 완전한 이동 상태 초기화
            hasMovedThisTurn = false;
            isMoving = false;

            // Phase 2: Transform 이동도 중단
            StopTransformMove();

            // Phase 5: 순차 이동도 중단
            InterruptSequentialMovement();

            // 현재 Grid 위치로 동기화 (안전장치)
            if (gridManager != null)
            {
                var currentGridPos = gridManager.GetUnitPosition(gameObject);
                transform.position = gridManager.CalculateWorldPositionWithHeight(currentGridPos);
            }

            RefreshMovementPoints();

            Debug.Log($"[MovementComponent] {gameObject.name} Movement fully reset - " +
                     $"CanMove: {CanMove}, MovementPoints: {currentMovementPoints}");
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
            return;
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

        #region Phase 2: Transform Movement Synchronization

        /// <summary>
        /// Animation Event: 애니메이션 시작 시 Transform 보간 시작
        /// </summary>
        private void OnTransformMoveStart(Vector2Int from, Vector2Int to)
        {
            // 이전 코루틴이 있다면 중단 (안전장치)
            StopTransformMove();

            isTransformMoving = true;
            currentTransformMoveCoroutine = StartCoroutine(SyncTransformWithAnimation(from, to));

            Debug.Log($"[MovementComponent] {gameObject.name}: Transform move started {from} → {to}");
        }

        /// <summary>
        /// Animation Event: 애니메이션 종료 시 Transform 보간 종료
        /// </summary>
        private void OnTransformMoveEnd(Vector2Int targetPos)
        {
            isTransformMoving = false;

            // 최종 위치 보장 (Grid 위치와 동기화)
            if (gridManager != null)
            {
                transform.position = gridManager.CalculateWorldPositionWithHeight(targetPos);
            }

            if (currentTransformMoveCoroutine != null)
            {
                StopCoroutine(currentTransformMoveCoroutine);
                currentTransformMoveCoroutine = null;
            }

            Debug.Log($"[MovementComponent] {gameObject.name}: Transform move ended at {targetPos}");
        }

        /// <summary>
        /// 애니메이션 중단 시 처리 (공격, 스킬 등으로 전환 시)
        /// </summary>
        private void OnAnimationInterrupted()
        {
            Debug.Log($"[MovementComponent] {gameObject.name}: Animation interrupted, snapping to grid position");

            // 현재 Grid 위치로 Transform 스냅 (동기화)
            if (gridManager != null)
            {
                var currentGridPos = gridManager.GetUnitPosition(gameObject);
                transform.position = gridManager.CalculateWorldPositionWithHeight(currentGridPos);
            }

            // Transform 이동 중단
            StopTransformMove();
        }

        /// <summary>
        /// 애니메이션 진행도에 맞춰 Transform을 실시간으로 보간
        /// Phase 4: Base 높이 차이 반영 - CalculateWorldPositionWithHeight() 사용
        /// </summary>
        private System.Collections.IEnumerator SyncTransformWithAnimation(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null || animationController == null)
            {
                Debug.LogError($"[MovementComponent] Cannot sync transform: missing dependencies");
                yield break;
            }

            // Phase 4: 높이를 포함한 월드 좌표 사용
            // IGridManager를 통해 간접 접근 (IGridHeightCalculator 직접 의존 제거)
            Vector3 startPos = gridManager.CalculateWorldPositionWithHeight(from);
            Vector3 endPos = gridManager.CalculateWorldPositionWithHeight(to);

            // 애니메이션 진행도에 맞춰 Transform 보간
            // Vector3.Lerp가 X, Y, Z 모두 자동 보간:
            // - Base→Ground: Y 1.0 → 0.5 (자연스러운 하강)
            // - Ground→Base: Y 0.5 → 1.0 (자연스러운 상승)
            // - Base→Base: Y 1.0 (평행 이동)
            // - Ground→Ground: Y 0.5 (평행 이동)
            while (animationController.IsAnimationPlaying && isTransformMoving)
            {
                float progress = animationController.CurrentAnimationProgress;
                transform.position = Vector3.Lerp(startPos, endPos, progress);

                yield return null;
            }

            // 최종 위치 보장
            transform.position = endPos;
            isTransformMoving = false;
            currentTransformMoveCoroutine = null;

            Debug.Log($"[MovementComponent] {gameObject.name}: Transform sync completed at {endPos}");
        }

        /// <summary>
        /// Transform 이동 강제 중단 (안전장치)
        /// </summary>
        private void StopTransformMove()
        {
            if (currentTransformMoveCoroutine != null)
            {
                StopCoroutine(currentTransformMoveCoroutine);
                currentTransformMoveCoroutine = null;
                Debug.Log($"[MovementComponent] {gameObject.name}: Transform move coroutine stopped");
            }

            isTransformMoving = false;
        }

        #endregion

        #region Phase 5: Sequential Movement System (1칸씩 순차 이동)

        /// <summary>
        /// 경로를 1칸씩 순차적으로 이동하는 코루틴
        /// 한 번의 명령으로 목표 지점까지 자동으로 1칸씩 연속 이동
        /// Base 높이 차이를 자연스럽게 표현하기 위한 시스템
        /// </summary>
        /// <param name="path">이동할 경로 (시작 위치 포함)</param>
        private System.Collections.IEnumerator MoveAlongPathSequentially(List<Vector2Int> path)
        {
            if (path == null || path.Count < 2)
            {
                Debug.LogWarning($"[MovementComponent] {gameObject.name}: Invalid path for sequential movement");
                isSequentialMoving = false;
                yield break;
            }

            isSequentialMoving = true;
            Debug.Log($"[MovementComponent] {gameObject.name}: Starting sequential movement - {path.Count - 1} steps");

            // 시작 위치 제외하고 각 스텝 이동
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int currentStep = path[i - 1];
                Vector2Int nextStep = path[i];
                bool isFirstStep = (i == 1);             // 첫 번째 칸 여부
                bool isLastStep = (i == path.Count - 1); // 마지막 칸 여부

                Debug.Log($"[MovementComponent] {gameObject.name}: Step {i}/{path.Count - 1} - " +
                         $"Moving from {currentStep} to {nextStep} (First: {isFirstStep}, Last: {isLastStep})");

                // 경로 중간에 장애물이 생겼는지 재확인 (안전장치)
                if (gridManager.IsPositionOccupied(nextStep))
                {
                    var occupyingUnit = gridManager.GetUnitAtPosition(nextStep);
                    if (occupyingUnit != null && occupyingUnit != gameObject)
                    {
                        Debug.LogWarning($"[MovementComponent] {gameObject.name}: Path blocked at {nextStep} by {occupyingUnit.name}, stopping movement");
                        break;
                    }
                }

                // 1칸 이동 실행 (첫 칸, 마지막 칸 여부 전달)
                yield return MoveOneStep(currentStep, nextStep, isFirstStep, isLastStep);

                // Transform 보간 완료 대기 (애니메이션 동기화)
                while (isTransformMoving)
                {
                    yield return null;
                }

                // 이동력 1 소비
                ConsumeMovementPoints(1);

                Debug.Log($"[MovementComponent] {gameObject.name}: Step {i}/{path.Count - 1} completed, remaining points: {currentMovementPoints}");

                // 선택적: 각 스텝 사이에 짧은 대기 시간 추가 (시각적 효과)
                // yield return new WaitForSeconds(0.1f);
            }

            // Phase 5: 순차 이동 완료 처리
            isSequentialMoving = false;
            isMoving = false; // 이동 완료, 다음 명령 수락 가능
            sequentialMovementCoroutine = null;

            // Phase 5: 실제 이동 완료 시 이벤트 발생 (시작 위치와 최종 위치)
            Vector2Int startPosition = path[0];
            Vector2Int finalPosition = path[path.Count - 1];
            OnMovementCompleted?.Invoke(startPosition, finalPosition);

            Debug.Log($"[MovementComponent] {gameObject.name}: Sequential movement completed at {finalPosition} - CanMove: {CanMove}");
        }

        /// <summary>
        /// 1칸 단위 이동 처리 (그리드 업데이트 + 애니메이션 트리거)
        /// GridManager에 논리적 위치 업데이트 후 애니메이션 재생
        /// Transform 이동은 AnimationEvent 기반으로 처리됨
        /// </summary>
        /// <param name="from">출발 위치</param>
        /// <param name="to">도착 위치 (1칸 인접)</param>
        /// <param name="isFirstStep">첫 칸 여부 (애니메이션 가속 제어용)</param>
        /// <param name="isLastStep">마지막 칸 여부 (애니메이션 감속 제어용)</param>
        private System.Collections.IEnumerator MoveOneStep(Vector2Int from, Vector2Int to, bool isFirstStep = true, bool isLastStep = true)
        {
            // GridManager에 논리적 위치 업데이트 (데이터 레이어)
            if (gridManager.MoveUnit(gameObject, from, to))
            {
                // 애니메이션 재생 (Transform 보간은 AnimationEvent에서 OnTransformMoveStart 호출 시 시작)
                if (animationController != null)
                {
                    animationController.PlayMoveAnimation(from, to, isFirstStep, isLastStep);
                }
                else
                {
                    // 애니메이션 컨트롤러 없으면 즉시 Transform 이동
                    Debug.LogWarning($"[MovementComponent] {gameObject.name}: No AnimationController, moving transform immediately");
                    transform.position = gridManager.CalculateWorldPositionWithHeight(to);
                }
            }
            else
            {
                Debug.LogError($"[MovementComponent] {gameObject.name}: Failed to move unit on grid from {from} to {to}");
            }

            yield return null;
        }

        /// <summary>
        /// 순차 이동 중단 처리 (공격, 스킬 사용, 사망 등)
        /// </summary>
        public void InterruptSequentialMovement()
        {
            if (sequentialMovementCoroutine != null)
            {
                StopCoroutine(sequentialMovementCoroutine);
                sequentialMovementCoroutine = null;
                Debug.Log($"[MovementComponent] {gameObject.name}: Sequential movement interrupted");
            }

            isSequentialMoving = false;
            isMoving = false;

            // Transform 이동도 중단
            StopTransformMove();

            // 현재 Grid 위치로 Transform 동기화 (안전장치)
            if (gridManager != null)
            {
                var currentGridPos = gridManager.GetUnitPosition(gameObject);
                transform.position = gridManager.CalculateWorldPositionWithHeight(currentGridPos);
                Debug.Log($"[MovementComponent] {gameObject.name}: Snapped to grid position {currentGridPos}");
            }
        }

        #endregion

        #region New Action System Integration

        /// <summary>
        /// ActionResult를 사용한 이동 실행 (새로운 Action System용)
        /// </summary>
        public void ExecuteMoveWithResult(ActionResult result, ActionContext context)
        {
            if (isMoving || !result.MoveDestination.HasValue)
            {
                OnMoveCompleted();
                return;
            }

            isMoving = true;
            currentMoveResult = result;
            currentMoveContext = context;

            var moveModifier = result.SelectedModifier as IMovementModifier;
            Vector2Int finalDestination = result.MoveDestination.Value;

            if (moveModifier != null)
                finalDestination = moveModifier.CalculateFinalDestination(result.MoveDestination.Value, context);

            currentMoveContext.TargetMovePosition = finalDestination;
            MoveTo(finalDestination);
        }

        /// <summary>
        /// 이동 완료 처리 (새 시스템용)
        /// </summary>
        private void OnMoveCompleted()
        {
            isMoving = false;

            if (currentMoveContext?.TargetMovePosition.HasValue == true)
                OnMovementCompleted?.Invoke(currentMoveContext.ActorPosition, currentMoveContext.TargetMovePosition.Value);

            var unit = GetComponent<Unit>();
            if (unit != null)
                unit.OnActionCompleted();

            currentMoveResult = null;
            currentMoveContext = null;
        }

        #endregion
    }
}