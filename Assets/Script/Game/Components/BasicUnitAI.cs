using UnityEngine;
using System.Linq;
using Game.Interfaces;
using Game.Core;

namespace Game.Components
{
    /// <summary>
    /// 기본 유닛 AI 구현
    /// - 가장 가까운 적 공격 (Unit과 Base 모두 공격 가능)
    /// - 적이 없으면 전진
    /// - IHealthComponent 기반 타겟팅으로 모든 공격 가능 대상 통합
    /// </summary>
    public class BasicUnitAI : MonoBehaviour, IUnitAI
    {
        [Header("AI Configuration")]
        [SerializeField] private AIStrategy strategy = AIStrategy.Basic;

        [Header("Debug Settings")]
        [SerializeField] private bool enableLogging = false;

        // 캐시된 컴포넌트
        private ICombatSystem combatComponent;
        private IMovementSystem movementComponent;
        private ITeamComponent teamComponent;
        private IGridManager gridManager;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            // ServiceLocator에서 GridManager 가져오기
            gridManager = ServiceLocator.Get<IGridManager>();

            if (gridManager == null)
            {
                Debug.LogWarning($"[BasicUnitAI] {gameObject.name} could not get GridManager from ServiceLocator");
            }
        }

        private void InitializeComponents()
        {
            combatComponent = GetComponent<ICombatSystem>();
            movementComponent = GetComponent<IMovementSystem>();
            teamComponent = GetComponent<ITeamComponent>();

            Log($"Initialized for {gameObject.name}");
        }

        #region IUnitAI Implementation

        public ActionDecision DecideAction()
        {
            // 1. 타겟 검색 (Unit과 Base 모두 포함)
            var target = FindBestTarget();

            // 2. 타겟이 있으면 공격
            if (target != null)
            {
                Log($"Decision: Attack target {(target as Component)?.gameObject.name}");
                return ActionDecision.Attack(target);
            }

            // 3. 타겟이 없으면 전진
            var movePosition = GetForwardMovePosition();
            if (movePosition.HasValue)
            {
                Log($"Decision: Move to {movePosition.Value}");
                return ActionDecision.Move(movePosition.Value);
            }

            // 4. 이동할 곳도 없으면 대기
            Log("Decision: Idle (no valid actions)");
            return ActionDecision.Idle();
        }

        public IHealthComponent FindBestTarget()
        {
            if (combatComponent == null || gridManager == null)
            {
                LogWarning("Cannot find target - missing components");
                return null;
            }

            // CombatComponent의 GetTargetsInRange() 활용
            var myPosition = gridManager.GetUnitPosition(gameObject);
            var targetsInRange = combatComponent.GetTargetsInRange(myPosition);

            if (targetsInRange.Count == 0)
            {
                Log("No targets in range");
                return null;
            }

            Log($"Found {targetsInRange.Count} potential targets in range");

            // 각 GameObject에서 IHealthComponent 추출
            foreach (var targetObj in targetsInRange)
            {
                var healthComponent = targetObj.GetComponent<IHealthComponent>();

                // IHealthComponent가 있고 살아있으며 적군인지 확인
                if (healthComponent != null &&
                    healthComponent.IsAlive &&
                    IsEnemyTarget(targetObj))
                {
                    Log($"Selected target: {targetObj.name} (Type: {targetObj.GetType().Name})");
                    return healthComponent; // 첫 번째 유효한 타겟 반환
                }
            }

            Log("No valid targets found (all filtered out)");
            return null;
        }

        public void SetStrategy(AIStrategy newStrategy)
        {
            strategy = newStrategy;
            Debug.Log($"[BasicUnitAI] {gameObject.name} strategy changed to {strategy}");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 타겟이 적군인지 확인
        /// </summary>
        private bool IsEnemyTarget(GameObject target)
        {
            if (teamComponent == null) return true;

            var targetTeam = target.GetComponent<ITeamComponent>();
            if (targetTeam == null) return true;

            var relation = teamComponent.GetRelationTo(targetTeam);
            return relation == TeamRelation.Enemy;
        }

        /// <summary>
        /// 전진할 위치 계산 (기존 MoveForward 로직 활용)
        /// </summary>
        private Vector2Int? GetForwardMovePosition()
        {
            if (movementComponent == null || gridManager == null)
                return null;

            var myPosition = gridManager.GetUnitPosition(gameObject);
            var validPositions = movementComponent.GetValidMovePositions();

            if (validPositions.Count == 0)
            {
                Log("No valid move positions");
                return null;
            }

            // 팀에 따라 전진 방향 결정
            int direction = teamComponent?.Team == TeamType.Player ? 1 : -1;

            // 가장 멀리 전진할 수 있는 위치 선택
            Vector2Int? bestPosition = null;
            int maxDistance = 0;

            foreach (var pos in validPositions)
            {
                int distance = (pos.y - myPosition.y) * direction;
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestPosition = pos;
                }
            }

            if (bestPosition.HasValue)
            {
                Log($"Best forward position: {bestPosition.Value} (distance: {maxDistance})");
            }

            return bestPosition;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[BasicUnitAI] {gameObject.name}: {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[BasicUnitAI] {gameObject.name}: {message}");
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Test DecideAction")]
        private void TestDecideAction()
        {
            Debug.Log($"=== Testing AI Decision for {gameObject.name} ===");
            var decision = DecideAction();
            Debug.Log($"Decision Type: {decision.Type}");

            switch (decision.Type)
            {
                case ActionType.Attack:
                    Debug.Log($"Target: {decision.TargetObject?.name ?? "NULL"}");
                    break;
                case ActionType.Move:
                    Debug.Log($"Move Position: {decision.MovePosition}");
                    break;
                case ActionType.Idle:
                    Debug.Log("Idle - no valid actions");
                    break;
            }
        }

        [ContextMenu("Log AI Status")]
        private void LogAIStatus()
        {
            Debug.Log($"=== BasicUnitAI Status for {gameObject.name} ===");
            Debug.Log($"Strategy: {strategy}");
            Debug.Log($"Combat Component: {combatComponent != null}");
            Debug.Log($"Movement Component: {movementComponent != null}");
            Debug.Log($"Team Component: {teamComponent != null} (Team: {teamComponent?.Team})");
            Debug.Log($"GridManager: {gridManager != null}");

            if (gridManager != null)
            {
                var pos = gridManager.GetUnitPosition(gameObject);
                Debug.Log($"Current Position: {pos}");
            }
        }

        #endregion
    }
}
