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
        private struct PendingTileHit
        {
            public Vector2Int GridPos;
            public Vector3 CenterWorldPos;
        }

        private Unit owner;
        private TeamType ownerTeam;
        private IGridManager gridManager;

        private Vector2Int originGrid;
        private Vector3 originWorld;
        private Vector3 targetWorld;
        private bool hasTargetWorld;
        private Vector2Int lastGridPos;
        private Vector3 lastWorldPos;

        private int maxRange;
        private bool piercing;
        private int traveledSteps;

        // InstantLaser 모드에서 실제 타격 대상 기준으로 계산된 유효 사거리
        // (이번 공격에서 실제로 맞게 될 적들 중 가장 먼 타일까지의 타일 수)
        private int instantLaserEffectiveRange;

        // 이번 공격에서 실제로 맞아야 할 타일 집합 (ActionResult.ValidTiles 기반)
        private HashSet<Vector2Int> targetTiles;

        [SerializeField]
        private ProjectileExecutionType executionType = ProjectileExecutionType.Moving;

        [SerializeField]
        [Tooltip("InstantLaser / MovingDelayed 모드에서 VFX 이후 타격까지 지연 시간")]
        private float hitDelay = 0.2f;

        [SerializeField]
        [Tooltip("MovingDelayed 모드에서 ValidTile에 도착했을 때 생성할 히트 VFX")]
        private ParticleSystem hitVfxPrefab;

        // InstantLaser 모드에서 사용할 주 타겟 타일 (origin → mainTarget 경로를 기준으로 레이저 경로 계산)
        private Vector2Int? mainTargetGrid;

        // 같은 HealthComponent를 여러 번 맞추지 않기 위한 중복 방지
        private readonly HashSet<HealthComponent> alreadyHitTargets = new HashSet<HealthComponent>();

        // 이동형 투사체에서 타일 중심 통과 시점을 판정하기 위한 대기 타일 목록
        private readonly List<PendingTileHit> pendingHitTiles = new List<PendingTileHit>();

        // MovingDelayed 모드에서, 지연 중인 히트 개수와 이동 종료 여부를 추적
        private int pendingDelayedHits;
        private bool travelCompleted;

        [Header("Audio Settings")]
        [SerializeField] private SoundEventChannelSO soundEventChannel;
        [SerializeField] private AudioData startSoundData;
        [SerializeField] private AudioData hitSoundData;

        public Action<GridProjectile, HealthComponent, Vector2Int> OnHitTargetTile;
        public Action<GridProjectile> OnFinished;

        public ProjectileExecutionType ExecutionType => executionType;
        public int MaxRange => maxRange;
        // InstantLaser 모드에서 사용할 유효 사거리 (0 이하일 경우 maxRange를 사용)
        public int InstantLaserEffectiveRange => instantLaserEffectiveRange > 0 ? instantLaserEffectiveRange : maxRange;
        public Vector3 OriginWorldPosition => originWorld;
        public Vector3 TargetWorldPosition => targetWorld;
        public bool HasTargetWorldPosition => hasTargetWorld;

        public void PlayStartSound()
        {
            if (soundEventChannel == null || startSoundData == null)
            {
                return;
            }

            var request = AudioPlayRequest.Create(startSoundData, this);
            soundEventChannel.RaiseSoundEvent(request);
        }

        public void PlayHitSound()
        {
            if (soundEventChannel == null || hitSoundData == null)
            {
                return;
            }

            var request = AudioPlayRequest.Create(hitSoundData, this);
            soundEventChannel.RaiseSoundEvent(request);
        }

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
            pendingHitTiles.Clear();
            pendingDelayedHits = 0;
            travelCompleted = false;

            originWorld = transform.position;
            if (this.mainTargetGrid.HasValue && this.gridManager != null)
            {
                targetWorld = this.gridManager.GridToWorldPosition(this.mainTargetGrid.Value);
                hasTargetWorld = true;
            }
            else
            {
                targetWorld = originWorld;
                hasTargetWorld = false;
            }

            // InstantLaser 모드에서는, 실제로 맞게 될 적들 중 가장 먼 타일까지의 타일 수를
            // 미리 계산해 instantLaserEffectiveRange에 저장한다.
            if (executionType == ProjectileExecutionType.InstantLaser &&
                gridManager != null &&
                mainTargetGrid.HasValue)
            {
                var pathTiles = GridTraversalUtility.GetTraversedTiles(originGrid, mainTargetGrid.Value);
                instantLaserEffectiveRange = ComputeInstantLaserEffectiveRange(originGrid, pathTiles);
            }

            if (executionType == ProjectileExecutionType.InstantLaser)
            {
                StartCoroutine(ExecuteInstantLaserRoutine());
            }

            Debug.Log($"[GridProjectile] owner: {owner}, originGrid:{originGrid} -> lastGrid:{lastGridPos}, range{maxRange}");
        }

        private void Update()
        {
            // 이동형 투사체 계열만 처리
            if (executionType != ProjectileExecutionType.Moving &&
                executionType != ProjectileExecutionType.MovingDelayed &&
                executionType != ProjectileExecutionType.BallisticEqualTime)
                return;

            if (gridManager == null || owner == null)
                return;

            // MovingDelayed 모드에서 이동이 이미 종료된 경우, 더 이상 타일을 검사하지 않는다.
            if (executionType == ProjectileExecutionType.MovingDelayed && travelCompleted)
                return;

            Vector3 currentWorldPos = transform.position;
            Vector2Int currentGridPos = gridManager.WorldToGridPosition(currentWorldPos);

            if (currentGridPos != lastGridPos)
            {
                var traversedTiles = GridTraversalUtility.GetTraversedTiles(lastGridPos, currentGridPos);

                for (int i = 0; i < traversedTiles.Count; i++)
                {
                    var tilePos = traversedTiles[i];

                    // 시작 타일은 이미 지난 프레임에 처리했으므로 스킵
                    if (i == 0 && tilePos == lastGridPos)
                        continue;

                    EnqueuePendingTile(tilePos);
                }

                ProcessPendingHits(lastWorldPos, currentWorldPos);

                lastGridPos = currentGridPos;
                lastWorldPos = currentWorldPos;
            }
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
                // 시작 타일(origin)은 사거리 계산에서 제외
                if (pos != origin)
                {
                    traveledSteps++;
                }

                int rangeLimit = InstantLaserEffectiveRange;

                // Range 제한 (InstantLaser 유효 사거리 기반)
                if (!gridManager.IsValidPosition(pos) || traveledSteps > rangeLimit)
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

                PlayHitSound();
                OnHitTargetTile?.Invoke(this, health, pos);

                if (!piercing)
                {
                    break;
                }
            }

            FinishProjectile();
        }

        private void EnqueuePendingTile(Vector2Int tilePos)
        {
            if (gridManager == null)
                return;

            var worldPos = gridManager.GridToWorldPosition(tilePos);
            var pending = new PendingTileHit
            {
                GridPos = tilePos,
                CenterWorldPos = worldPos
            };
            pendingHitTiles.Add(pending);
        }

        private void ProcessPendingHits(Vector3 fromWorld, Vector3 toWorld)
        {
            if (pendingHitTiles.Count == 0)
                return;

            Vector2 p0 = new Vector2(fromWorld.x, fromWorld.z);
            Vector2 p1 = new Vector2(toWorld.x, toWorld.z);
            Vector2 seg = p1 - p0;
            float segLenSq = seg.sqrMagnitude;

            // 뒤에서부터 검사하며 통과한 타일은 제거
            for (int i = pendingHitTiles.Count - 1; i >= 0; i--)
            {
                var pending = pendingHitTiles[i];
                Vector3 centerWorld = pending.CenterWorldPos;
                Vector2 c = new Vector2(centerWorld.x, centerWorld.z);

                bool passed = false;

                if (segLenSq <= Mathf.Epsilon)
                {
                    // 이동이 거의 없는 경우, 현재 위치와의 거리로 판정
                    float distToPoint = Vector2.Distance(c, p1);
                    passed = distToPoint <= 0.05f;
                }
                else
                {
                    float t = Vector2.Dot(c - p0, seg) / segLenSq;
                    if (t >= 0f && t <= 1f)
                    {
                        Vector2 closest = p0 + seg * t;
                        float distance = Vector2.Distance(c, closest);
                        passed = distance <= 0.05f;
                    }
                }

                if (!passed)
                    continue;

                pendingHitTiles.RemoveAt(i);
                OnTileCenterPassed(pending.GridPos);
            }
        }

        /// <summary>
        /// InstantLaser 모드에서, 이번 공격으로 실제로 맞게 될 적들 중
        /// 가장 먼 타일까지의 타일 수를 계산한다.
        /// </summary>
        private int ComputeInstantLaserEffectiveRange(Vector2Int origin, System.Collections.Generic.IList<Vector2Int> pathTiles)
        {
            if (gridManager == null)
                return 0;

            int stepIndex = 0;
            int farthestHitStep = 0;

            var seenHealth = new HashSet<HealthComponent>();

            for (int i = 0; i < pathTiles.Count; i++)
            {
                var pos = pathTiles[i];

                if (pos == origin)
                    continue;

                stepIndex++;

                // 이론상 Range 및 맵 범위 제한
                if (!gridManager.IsValidPosition(pos) || stepIndex > maxRange)
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

                if (!seenHealth.Add(health))
                    continue;

                // 실제로 맞는 적 한 명 발견
                farthestHitStep = stepIndex;

                // 비관통: 첫 적에서 바로 종료
                if (!piercing)
                    break;
            }

            // 한 명도 맞지 않는 경우: 기존 최대 사거리 / 경로 길이 기반으로 보정
            if (farthestHitStep == 0)
            {
                int maxPossibleSteps = 0;
                int tmpIndex = 0;

                for (int i = 0; i < pathTiles.Count; i++)
                {
                    var pos = pathTiles[i];

                    if (pos == origin)
                        continue;

                    tmpIndex++;

                    if (!gridManager.IsValidPosition(pos) || tmpIndex > maxRange)
                        break;

                    maxPossibleSteps = tmpIndex;
                }

                farthestHitStep = maxPossibleSteps;
            }

            return farthestHitStep;
        }

        private void OnTileCenterPassed(Vector2Int tilePos)
        {
            traveledSteps++;

            if (!gridManager.IsValidPosition(tilePos) || traveledSteps > maxRange)
            {
                if (executionType == ProjectileExecutionType.Moving ||
                    executionType == ProjectileExecutionType.BallisticEqualTime)
                {
                    FinishProjectile();
                }
                else if (executionType == ProjectileExecutionType.MovingDelayed)
                {
                    travelCompleted = true;
                    if (pendingDelayedHits <= 0)
                    {
                        FinishProjectile();
                    }
                }
                return;
            }

            if (!targetTiles.Contains(tilePos))
                return;

            HandleHitAtTile(tilePos);
        }

        private void HandleHitAtTile(Vector2Int tilePos)
        {
            if (gridManager == null)
                return;

            // 타일 위 공격 가능한 타겟 찾기 (Grid 기반)
            var targetGO = gridManager.GetAttackableTargetAtPosition(tilePos);
            if (targetGO == null)
                return;

            var targetTeam = targetGO.GetComponent<ITeamComponent>();
            var health = targetGO.GetComponent<HealthComponent>();

            if (targetTeam == null || health == null || !health.IsAlive)
                return;

            if (TeamRelationMatrix.GetRelation(ownerTeam, targetTeam.Team) != TeamRelation.Enemy)
                return;

            if (!alreadyHitTargets.Add(health))
                return;

            if (executionType == ProjectileExecutionType.MovingDelayed)
            {
                // VFX 생성
                SpawnHitVfx(tilePos);

                // 지연 후 실제 데미지 적용
                StartCoroutine(DelayedHitRoutine(health, tilePos));

                if (!piercing)
                {
                    // 비관통인 경우 첫 타겟에서 이동 종료
                    travelCompleted = true;
                    // 아직 대기 중인 히트가 없다면 바로 종료
                    if (pendingDelayedHits <= 0)
                    {
                        FinishProjectile();
                    }
                }
            }
            else
            {
                PlayHitSound();
                OnHitTargetTile?.Invoke(this, health, tilePos);

                if (!piercing)
                {
                    FinishProjectile();
                }
            }
        }

        /// <summary>
        /// MovingDelayed 모드에서 유효 타일에 도착했을 때 히트 VFX를 생성한다.
        /// </summary>
        private void SpawnHitVfx(Vector2Int gridPos)
        {
            if (hitVfxPrefab == null || gridManager == null)
                return;

            Vector3 worldPos = gridManager.CalculateWorldPositionWithHeight(gridPos);
            Instantiate(hitVfxPrefab, worldPos, Quaternion.identity);
        }

        /// <summary>
        /// MovingDelayed 모드에서, VFX가 생성된 이후 hitDelay 만큼 기다렸다가
        /// 실제 데미지를 적용한다.
        /// </summary>
        private System.Collections.IEnumerator DelayedHitRoutine(HealthComponent health, Vector2Int hitPos)
        {
            pendingDelayedHits++;

            if (hitDelay > 0f)
            {
                yield return new WaitForSeconds(hitDelay);
            }

            if (health != null && health.IsAlive)
            {
                PlayHitSound();
                OnHitTargetTile?.Invoke(this, health, hitPos);
            }

            pendingDelayedHits--;

            // 이동이 끝났고(사거리 초과 또는 비관통 첫 타겟),
            // 더 이상 대기 중인 히트가 없다면 투사체 수명 종료
            if (executionType == ProjectileExecutionType.MovingDelayed &&
                travelCompleted &&
                pendingDelayedHits <= 0)
            {
                FinishProjectile();
            }
        }

        private void FinishProjectile()
        {
            OnFinished?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
