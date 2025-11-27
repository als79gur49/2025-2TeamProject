using System;
using System.Collections.Generic;
using Game.Core;
using Game.Data.Modifiers;
using Game.Interfaces;
using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// Grid 기반 이동형 투사체
    /// Transform 이동은 외부에서 처리하고, 이 컴포넌트는
    /// 월드 → 그리드 변환 + DDA 기반 타일 순회로 충돌만 판단한다.
    /// </summary>
    public class GridProjectile : MonoBehaviour
    {
        private Unit owner;
        private TeamType ownerTeam;
        private IGridManager gridManager;

        private Vector2Int originGrid;
        private Vector2Int lastGridPos;
        private Vector3 lastWorldPos;

        private int maxRange;
        private bool piercing;
        private int traveledSteps;

        // 이번 공격에서 실제로 맞아야 할 타일 집합 (ActionResult.ValidTiles 기반)
        private HashSet<Vector2Int> targetTiles;

        [SerializeField]
        private ProjectileExecutionType executionType = ProjectileExecutionType.Moving;

        [SerializeField]
        [Tooltip("InstantLaser 모드에서 VFX 이후 타격까지 지연 시간")]
        private float hitDelay = 0.2f;

        // InstantLaser 모드에서 사용할 주 타겟 타일 (origin → mainTarget 경로를 기준으로 레이저 경로 계산)
        private Vector2Int? mainTargetGrid;

        // 같은 HealthComponent를 여러 번 맞추지 않기 위한 중복 방지
        private readonly HashSet<HealthComponent> alreadyHitTargets = new HashSet<HealthComponent>();

        public Action<GridProjectile, HealthComponent, Vector2Int> OnHitTargetTile;
        public Action<GridProjectile> OnFinished;

        public ProjectileExecutionType ExecutionType => executionType;
        public int MaxRange => maxRange;

        public void Initialize(
            Unit owner,
            Vector2Int originGrid,
            AttackConfig attackConfig,
            IGridManager gridManager,
            HashSet<Vector2Int> targetTiles,
            Vector2Int? mainTargetGrid,
            Action<GridProjectile, HealthComponent, Vector2Int> onHitTargetTile,
            Action<GridProjectile> onFinished)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (gridManager == null) throw new ArgumentNullException(nameof(gridManager));

            this.owner = owner;
            this.gridManager = gridManager;
            this.originGrid = originGrid;
            this.mainTargetGrid = mainTargetGrid;

            var teamComponent = owner.GetComponent<ITeamComponent>();
            ownerTeam = teamComponent != null ? teamComponent.Team : TeamType.Neutral;

            maxRange = attackConfig.Range;
            piercing = attackConfig.Piercing;

            this.targetTiles = targetTiles ?? new HashSet<Vector2Int>();

            OnHitTargetTile = onHitTargetTile;
            OnFinished = onFinished;

            lastWorldPos = transform.position;
            lastGridPos = gridManager.WorldToGridPosition(lastWorldPos);
            traveledSteps = 0;

            alreadyHitTargets.Clear();

            if (executionType == ProjectileExecutionType.InstantLaser)
            {
                StartCoroutine(ExecuteInstantLaserRoutine());
            }

            Debug.Log($"[GridProjectile] owner: {owner}, originGrid:{originGrid} -> lastGrid:{lastGridPos}, range{maxRange}");
        }

        private void Update()
        {
            if (executionType != ProjectileExecutionType.Moving)
                return;

            if (gridManager == null || owner == null)
                return;

            Vector3 currentWorldPos = transform.position;
            Vector2Int currentGridPos = gridManager.WorldToGridPosition(currentWorldPos);

            if (currentGridPos == lastGridPos)
                return;

            var traversedTiles = GridTraversalUtility.GetTraversedTiles(lastGridPos, currentGridPos);

            for (int i = 0; i < traversedTiles.Count; i++)
            {
                var tilePos = traversedTiles[i];

                // 시작 타일은 이미 지난 프레임에 처리했으므로 스킵
                if (i == 0 && tilePos == lastGridPos)
                    continue;

                traveledSteps++;

                if (!gridManager.IsValidPosition(tilePos) || traveledSteps > maxRange)
                {
                    FinishProjectile();
                    return;
                }

                // 이 공격에서 실제로 맞아야 하는 타일인지 확인
                if (!targetTiles.Contains(tilePos))
                    continue;

                // 타일 위 공격 가능한 타겟 찾기 (Grid 기반)
                var targetGO = gridManager.GetAttackableTargetAtPosition(tilePos);
                if (targetGO == null)
                    continue;

                var targetTeam = targetGO.GetComponent<ITeamComponent>();
                var health = targetGO.GetComponent<HealthComponent>();

                if (targetTeam == null || health == null || !health.IsAlive)
                    continue;

                if (TeamRelationMatrix.GetRelation(ownerTeam, targetTeam.Team) != TeamRelation.Enemy)
                    continue;

                if (!alreadyHitTargets.Add(health))
                    continue;

                OnHitTargetTile?.Invoke(this, health, tilePos);

                if (!piercing)
                {
                    FinishProjectile();
                    return;
                }
            }

            lastGridPos = currentGridPos;
            lastWorldPos = currentWorldPos;
        }

        private System.Collections.IEnumerator ExecuteInstantLaserRoutine()
        {
            if (gridManager == null || owner == null)
            {
                FinishProjectile();
                yield break;
            }

            // 메인 타겟이 없으면 레이저 경로를 계산할 수 없으므로 즉시 종료
            if (!mainTargetGrid.HasValue)
            {
                FinishProjectile();
                yield break;
            }

            Vector2Int origin = originGrid;
            Vector2Int target = mainTargetGrid.Value;

            var pathTiles = GridTraversalUtility.GetTraversedTiles(origin, target);

            if (hitDelay > 0f)
            {
                yield return new WaitForSeconds(hitDelay);
            }

            var uniqueTargets = new HashSet<HealthComponent>();

            foreach (var pos in pathTiles)
            {
                // Range 제한
                traveledSteps++;
                if (!gridManager.IsValidPosition(pos) || traveledSteps > maxRange)
                    break;

                var targetGO = gridManager.GetAttackableTargetAtPosition(pos);
                if (targetGO == null)
                    continue;

                var targetTeam = targetGO.GetComponent<ITeamComponent>();
                var health = targetGO.GetComponent<HealthComponent>();

                if (targetTeam == null || health == null || !health.IsAlive)
                    continue;

                if (TeamRelationMatrix.GetRelation(ownerTeam, targetTeam.Team) != TeamRelation.Enemy)
                    continue;

                if (!uniqueTargets.Add(health))
                    continue;

                OnHitTargetTile?.Invoke(this, health, pos);

                if (!piercing)
                {
                    break;
                }
            }

            FinishProjectile();
        }

        private void FinishProjectile()
        {
            OnFinished?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
