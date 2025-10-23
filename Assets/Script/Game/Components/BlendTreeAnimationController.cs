using System.Collections;
using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// BlendTree 기반 2D 애니메이션 컨트롤러
    /// 이동(MoveSpeed) 및 공격(AttackTrigger) 애니메이션을 2D Blend Tree로 제어
    /// </summary>
    public class BlendTreeAnimationController : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// BlendTree 이동 시작 이벤트 (startPos, targetPos)
        /// </summary>
        public event System.Action<Vector3, Vector3> OnBlendTreeMoveStart;

        /// <summary>
        /// BlendTree 이동 완료 이벤트 (finalPos)
        /// </summary>
        public event System.Action<Vector3> OnBlendTreeMoveEnd;

        /// <summary>
        /// BlendTree 공격 시작 이벤트 (target)
        /// </summary>
        public event System.Action<GameObject> OnBlendTreeAttackStart;

        /// <summary>
        /// BlendTree 공격 완료 이벤트 (target)
        /// </summary>
        public event System.Action<GameObject> OnBlendTreeAttackEnd;

        #endregion

        [Header("Movement Timing")]
        [SerializeField] [Range(0.1f, 5.0f)] [Tooltip("애니메이션 속도 배율 (높을수록 빠름)")]
        private float speed = 1.0f;

        [SerializeField] [Range(0.5f, 3.0f)] [Tooltip("기본 이동 시간 (speed로 나누어짐)")]
        private float baseMoveDuration = 1.0f;

        [SerializeField] [Range(0.1f, 0.5f)] [Tooltip("기본 가속 시간 (speed로 나누어짐)")]
        private float baseMoveFrontTransitionDuration = 0.2f;

        [SerializeField] [Range(0.1f, 0.5f)] [Tooltip("기본 감속 시간 (speed로 나누어짐)")]
        private float baseMoveBackTransitionDuration = 0.3f;

        // 실제 계산된 값 (speed 적용)
        private float moveDuration => baseMoveDuration / speed;
        private float moveFrontTransitionDuration => baseMoveFrontTransitionDuration / speed;
        private float moveBackTransitionDuration => baseMoveBackTransitionDuration / speed;

        [Header("Attack Timing")]
        [SerializeField] [Range(0.3f, 2.0f)]
        private float attackDuration = 0.6f;

        [SerializeField] [Range(0.05f, 0.3f)]
        private float attackFrontTransitionDuration = 0.1f;

        [SerializeField] [Range(0.05f, 0.3f)]
        private float attackBackTransitionDuration = 0.15f;

        [Header("Speed Curve")]
        [SerializeField]
        private AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Transform Sync Settings")]
        [SerializeField] [Tooltip("Transform과 Grid 위치 오차 허용 임계값 (Unity units)")]
        private float positionErrorThreshold = 0.1f;

        [SerializeField] [Tooltip("최종 위치 스냅 거리 임계값 (Unity units)")]
        private float snapDistanceThreshold = 0.05f;

        // 내부 상태
        private Animator animator;
        private float moveStartTime;
        private float attackStartTime;
        private bool isBlendTreeMoving;
        private bool isFirstMoveStep; // 첫 칸 여부 (가속 제어용)
        private bool isLastMoveStep; // 마지막 칸 여부 (감속 제어용)
        private Coroutine updateSpeedCoroutine;
        private Coroutine updateAttackCoroutine;
        private Coroutine transformSyncCoroutine;

        // Transform 동기화 상태
        private Vector3 transformStartPosition;
        private Vector3 transformTargetPosition;
        private bool isTransformSyncing;

        // Animator 파라미터 상수
        private const string MOVE_SPEED_PARAM = "MoveSpeed";
        private const string ATTACK_TRIGGER_PARAM = "AttackTrigger";

        // 공격 상태
        private bool isBlendTreeAttacking;
        private GameObject currentAttackTarget;
        private Coroutine attackCoroutine;

        #region Unity Lifecycle

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogError($"[BlendTreeAnimationController] {gameObject.name}: Animator component not found!");
            }
        }

        private void OnValidate()
        {
            // Speed 값 검증
            speed = Mathf.Clamp(speed, 0.1f, 5.0f);

            // Movement 기본 값 검증
            baseMoveDuration = Mathf.Clamp(baseMoveDuration, 0.5f, 3.0f);
            baseMoveFrontTransitionDuration = Mathf.Clamp(baseMoveFrontTransitionDuration, 0.1f, 0.5f);
            baseMoveBackTransitionDuration = Mathf.Clamp(baseMoveBackTransitionDuration, 0.1f, 0.5f);

            float minBaseMoveDuration = baseMoveFrontTransitionDuration + baseMoveBackTransitionDuration + 0.1f;
            if (baseMoveDuration < minBaseMoveDuration)
            {
                baseMoveDuration = minBaseMoveDuration;
                Debug.LogWarning($"[BlendTreeAnimationController] baseMoveDuration adjusted to {baseMoveDuration:F2}s to fit transition durations");
            }

            // Attack 값 검증
            attackDuration = Mathf.Clamp(attackDuration, 0.3f, 2.0f);
            attackFrontTransitionDuration = Mathf.Clamp(attackFrontTransitionDuration, 0.05f, 0.3f);
            attackBackTransitionDuration = Mathf.Clamp(attackBackTransitionDuration, 0.05f, 0.3f);

            float minAttackDuration = attackFrontTransitionDuration + attackBackTransitionDuration + 0.1f;
            if (attackDuration < minAttackDuration)
            {
                attackDuration = minAttackDuration;
                Debug.LogWarning($"[BlendTreeAnimationController] attackDuration adjusted to {attackDuration:F2}s to fit transition durations");
            }
        }

        #endregion

        public void SetAnimationSpeed(float speed)
        {
            this.speed = speed;
        }

        #region Public Methods

        /// <summary>
        /// BlendTree 이동 시작 (외부에서 호출)
        /// MoveSpeed 초기화 및 실시간 속도 업데이트 시작
        /// Phase 3-3: Transform 동기화 동시 시작
        /// </summary>
        /// <param name="startWorldPos">시작 월드 위치 (옵션)</param>
        /// <param name="targetWorldPos">목표 월드 위치 (옵션)</param>
        /// <param name="isFirstStep">첫 칸 여부 (true: MoveSpeed=0에서 시작, false: 현재 값 유지)</param>
        /// <param name="isLastStep">마지막 칸 여부 (true: 감속 적용, false: 감속 스킵하여 Walk 유지)</param>
        public void StartBlendTreeMove(Vector3? startWorldPos = null, Vector3? targetWorldPos = null,
                                      bool isFirstStep = true, bool isLastStep = true)
        {
            if (animator == null)
            {
                Debug.LogError($"[BlendTreeAnimationController] Cannot start move: Animator is null");
                return;
            }

            // 이전 업데이트 중단
            if (updateSpeedCoroutine != null)
            {
                StopCoroutine(updateSpeedCoroutine);
            }

            if (transformSyncCoroutine != null)
            {
                StopCoroutine(transformSyncCoroutine);
            }

            // 초기화
            moveStartTime = Time.time;
            isBlendTreeMoving = true;
            isFirstMoveStep = isFirstStep; // 첫 칸 여부 저장
            isLastMoveStep = isLastStep;   // 마지막 칸 여부 저장

            // 첫 칸일 때만 MoveSpeed 0으로 초기화 (Idle 상태)
            // 중간 칸은 이전 값(1.0) 유지 (Walk 상태)
            if (isFirstStep)
            {
                animator.SetFloat(MOVE_SPEED_PARAM, 0.0f);
            }

            // Animator Apply Root Motion 비활성화 검증
            if (animator.applyRootMotion)
            {
                Debug.LogWarning($"[BlendTreeMovementController] {gameObject.name}: Apply Root Motion is enabled! " +
                                "Disabling it for script-controlled Transform movement.");
                animator.applyRootMotion = false;
            }

            // Phase 3-3: Transform 동기화 시작 (위치 정보가 제공된 경우)
            if (startWorldPos.HasValue && targetWorldPos.HasValue)
            {
                transformStartPosition = startWorldPos.Value;
                transformTargetPosition = targetWorldPos.Value;
                isTransformSyncing = true;
                transformSyncCoroutine = StartCoroutine(SyncTransformWithBlendTree());
            }

            // 실시간 속도 업데이트 시작
            updateSpeedCoroutine = StartCoroutine(UpdateBlendTreeSpeed());

            // 이동 시작 이벤트 발생
            OnBlendTreeMoveStart?.Invoke(transformStartPosition, transformTargetPosition);

            Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: BlendTree move started (Duration: {moveDuration:F2}s, " +
                     $"Transform Sync: {isTransformSyncing})");
        }

        /// <summary>
        /// BlendTree 이동 완료 (외부에서 호출)
        /// MoveSpeed를 0.0으로 설정하고 상태 초기화
        /// Phase 3-3: Transform 최종 위치 스냅 보장
        /// </summary>
        public void CompleteBlendTreeMove()
        {
            if (updateSpeedCoroutine != null)
            {
                StopCoroutine(updateSpeedCoroutine);
                updateSpeedCoroutine = null;
            }

            if (transformSyncCoroutine != null)
            {
                StopCoroutine(transformSyncCoroutine);
                transformSyncCoroutine = null;
            }

            isBlendTreeMoving = false;

            // Phase 3-3: Transform 최종 위치 스냅 (목표 위치와 정확히 일치하도록)
            Vector3 finalPosition = transform.position;
            if (isTransformSyncing)
            {
                transform.position = transformTargetPosition;
                finalPosition = transformTargetPosition;
                Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: Transform snapped to final position {transformTargetPosition}");
            }

            isTransformSyncing = false;

            // 마지막 칸일 때만 MoveSpeed를 0으로 설정 (Idle 복귀)
            // 중간 칸일 때는 1.0 유지 (Walk 상태 지속)
            if (animator != null)
            {
                if (isLastMoveStep)
                {
                    animator.SetFloat(MOVE_SPEED_PARAM, 0.0f);
                }
                else
                {
                    animator.SetFloat(MOVE_SPEED_PARAM, 1.0f);
                }
            }

            // 이동 완료 이벤트 발생
            OnBlendTreeMoveEnd?.Invoke(finalPosition);

            Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: BlendTree move completed");
        }

        /// <summary>
        /// 현재 이동 진행도 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetMoveProgress()
        {
            if (!isBlendTreeMoving) return 0.0f;

            float elapsed = Time.time - moveStartTime;
            return Mathf.Clamp01(elapsed / moveDuration);
        }

        /// <summary>
        /// 현재 MoveSpeed 값 반환 (디버깅용)
        /// </summary>
        public float GetCurrentMoveSpeed()
        {
            if (animator == null) return 0.0f;
            return animator.GetFloat(MOVE_SPEED_PARAM);
        }

        /// <summary>
        /// BlendTree 공격 시작 (외부에서 호출)
        /// AttackTrigger를 0.0으로 초기화하고 실시간 공격 업데이트 시작
        /// </summary>
        /// <param name="target">공격 대상</param>
        public void StartBlendTreeAttack(GameObject target)
        {
            if (animator == null)
            {
                Debug.LogError($"[BlendTreeAnimationController] Cannot start attack: Animator is null");
                return;
            }

            if (isBlendTreeAttacking)
            {
                Debug.LogWarning($"[BlendTreeAnimationController] {gameObject.name}: Already attacking!");
                return;
            }

            // 이전 업데이트 중단
            if (updateAttackCoroutine != null)
            {
                StopCoroutine(updateAttackCoroutine);
            }

            // 초기화
            attackStartTime = Time.time;
            isBlendTreeAttacking = true;
            currentAttackTarget = target;

            // AttackTrigger 0.0으로 설정 (시작)
            animator.SetFloat(ATTACK_TRIGGER_PARAM, 0.0f);

            // 공격 시작 이벤트 발생
            OnBlendTreeAttackStart?.Invoke(target);

            // 실시간 공격 업데이트 시작
            updateAttackCoroutine = StartCoroutine(UpdateBlendTreeAttack());

            Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: BlendTree attack started on {target?.name} (Duration: {attackDuration:F2}s)");
        }

        /// <summary>
        /// BlendTree 공격 완료 (외부에서 호출)
        /// AttackTrigger를 0.0으로 설정하고 상태 초기화
        /// </summary>
        public void CompleteBlendTreeAttack()
        {
            if (updateAttackCoroutine != null)
            {
                StopCoroutine(updateAttackCoroutine);
                updateAttackCoroutine = null;
            }

            isBlendTreeAttacking = false;

            // AttackTrigger 0.0으로 설정 (정지)
            if (animator != null)
            {
                animator.SetFloat(ATTACK_TRIGGER_PARAM, 0.0f);
            }

            // 공격 완료 이벤트 발생
            OnBlendTreeAttackEnd?.Invoke(currentAttackTarget);

            Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: BlendTree attack completed");

            currentAttackTarget = null;
        }

        /// <summary>
        /// 현재 공격 진행도 반환 (0.0 ~ 1.0)
        /// </summary>
        public float GetAttackProgress()
        {
            if (!isBlendTreeAttacking) return 0.0f;

            float elapsed = Time.time - attackStartTime;
            return Mathf.Clamp01(elapsed / attackDuration);
        }

        /// <summary>
        /// 현재 AttackTrigger 값 반환 (디버깅용)
        /// </summary>
        public float GetCurrentAttackTrigger()
        {
            if (animator == null) return 0.0f;
            return animator.GetFloat(ATTACK_TRIGGER_PARAM);
        }

        /// <summary>
        /// 공격 중인지 반환
        /// </summary>
        public bool IsAttacking => isBlendTreeAttacking;

        #endregion

        #region Private Methods

        /// <summary>
        /// 실시간 속도 업데이트 코루틴
        /// CalculateCurrentSpeed()를 통해 3단계 속도를 계산하고 Animator에 적용
        /// </summary>
        private IEnumerator UpdateBlendTreeSpeed()
        {
            while (isBlendTreeMoving)
            {
                float elapsed = Time.time - moveStartTime;
                float speed = CalculateCurrentSpeed(elapsed);

                // Animator MoveSpeed 파라미터 업데이트
                animator.SetFloat(MOVE_SPEED_PARAM, speed);

                // 이동 시간 완료 체크
                if (elapsed >= moveDuration)
                {
                    CompleteBlendTreeMove();
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 3단계 구조 이동 속도 계산
        /// 첫 칸: frontTransition(0→1) → move(1.0) → [backTransition(1→0) if last]
        /// 중간 칸: move(1.0) → [backTransition(1→0) if last]
        /// </summary>
        /// <param name="elapsed">이동 시작부터 경과 시간</param>
        /// <returns>현재 MoveSpeed 값 (0.0 ~ 1.0)</returns>
        private float CalculateCurrentSpeed(float elapsed)
        {
            // 중간 칸: 가속 단계 스킵, 바로 정속(1.0)부터 시작
            if (!isFirstMoveStep)
            {
                // Phase 3: Back Transition (마지막 칸만)
                if (isLastMoveStep)
                {
                    float backTransitionStartTime = moveDuration - moveBackTransitionDuration;
                    if (elapsed >= backTransitionStartTime)
                    {
                        float t = (elapsed - backTransitionStartTime) / moveBackTransitionDuration;
                        float curveValue = speedCurve.Evaluate(t);
                        return Mathf.Lerp(1.0f, 0.0f, curveValue);
                    }
                }

                // Phase 2: Move (정속 유지)
                return 1.0f;
            }

            // 첫 칸: 정상 3단계 구조
            // Phase 1: Front Transition (가속)
            if (elapsed < moveFrontTransitionDuration)
            {
                float t = elapsed / moveFrontTransitionDuration;
                float curveValue = speedCurve.Evaluate(t);
                return Mathf.Lerp(0.0f, 1.0f, curveValue);
            }

            // Phase 3: Back Transition (감속) - 마지막 칸만
            if (isLastMoveStep)
            {
                float backTransitionStartTime = moveDuration - moveBackTransitionDuration;
                if (elapsed >= backTransitionStartTime)
                {
                    float t = (elapsed - backTransitionStartTime) / moveBackTransitionDuration;
                    float curveValue = speedCurve.Evaluate(t);
                    return Mathf.Lerp(1.0f, 0.0f, curveValue);
                }
            }

            // Phase 2: Move (정속)
            return 1.0f;
        }

        /// <summary>
        /// 실시간 공격 업데이트 코루틴
        /// CalculateCurrentAttackTrigger()를 통해 3단계 속도를 계산하고 Animator에 적용
        /// </summary>
        private IEnumerator UpdateBlendTreeAttack()
        {
            while (isBlendTreeAttacking)
            {
                float elapsed = Time.time - attackStartTime;
                float trigger = CalculateCurrentAttackTrigger(elapsed);

                // Animator AttackTrigger 파라미터 업데이트
                animator.SetFloat(ATTACK_TRIGGER_PARAM, trigger);

                // 공격 시간 완료 체크
                if (elapsed >= attackDuration)
                {
                    CompleteBlendTreeAttack();
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 3단계 구조 공격 트리거 계산
        /// frontTransition: 0.0 → 1.0 가속
        /// attack: 1.0 정속 유지
        /// backTransition: 1.0 → 0.0 감속
        /// </summary>
        /// <param name="elapsed">공격 시작부터 경과 시간</param>
        /// <returns>현재 AttackTrigger 값 (0.0 ~ 1.0)</returns>
        private float CalculateCurrentAttackTrigger(float elapsed)
        {
            // Phase 1: Front Transition (가속)
            if (elapsed < attackFrontTransitionDuration)
            {
                float t = elapsed / attackFrontTransitionDuration;
                float curveValue = speedCurve.Evaluate(t);
                return Mathf.Lerp(0.0f, 1.0f, curveValue);
            }

            // Phase 3: Back Transition (감속)
            float backTransitionStartTime = attackDuration - attackBackTransitionDuration;
            if (elapsed >= backTransitionStartTime)
            {
                float t = (elapsed - backTransitionStartTime) / attackBackTransitionDuration;
                float curveValue = speedCurve.Evaluate(t);
                return Mathf.Lerp(1.0f, 0.0f, curveValue);
            }

            // Phase 2: Attack (정속)
            return 1.0f;
        }

        #endregion

        #region Phase 3-3: Transform Synchronization System

        /// <summary>
        /// BlendTree 애니메이션과 Transform 위치를 실시간으로 동기화하는 코루틴
        /// 시간 기반 진행도 계산 + Vector3.Lerp를 통한 부드러운 보간 구현
        /// </summary>
        private IEnumerator SyncTransformWithBlendTree()
        {
            if (!isTransformSyncing)
            {
                Debug.LogWarning($"[BlendTreeAnimationController] {gameObject.name}: Transform sync started but flag is false");
                yield break;
            }

            float totalDistance = Vector3.Distance(transformStartPosition, transformTargetPosition);
            Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: Transform sync started - " +
                     $"Distance: {totalDistance:F2}, Duration: {moveDuration:F2}s");

            while (isBlendTreeMoving && isTransformSyncing)
            {
                // 1. 시간 기반 진행도 계산 (0.0 ~ 1.0)
                float elapsed = Time.time - moveStartTime;
                float progress = Mathf.Clamp01(elapsed / moveDuration);

                // 2. Vector3.Lerp를 통한 부드러운 위치 보간
                Vector3 interpolatedPosition = Vector3.Lerp(transformStartPosition, transformTargetPosition, progress);
                transform.position = interpolatedPosition;

                // 3. 동기화 검증 (Grid 위치와 Transform 위치 오차 모니터링)
                ValidateTransformSync(interpolatedPosition, progress);

                // 4. 최종 위치 근접 시 스냅 처리
                float remainingDistance = Vector3.Distance(transform.position, transformTargetPosition);
                if (remainingDistance <= snapDistanceThreshold)
                {
                    transform.position = transformTargetPosition;
                    Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: Transform snapped to target (distance: {remainingDistance:F4})");
                    break;
                }

                yield return null;
            }

            // 최종 위치 보장
            if (isTransformSyncing)
            {
                transform.position = transformTargetPosition;
                Debug.Log($"[BlendTreeAnimationController] {gameObject.name}: Transform sync completed - Final position: {transformTargetPosition}");
            }

            isTransformSyncing = false;
            transformSyncCoroutine = null;
        }

        /// <summary>
        /// Transform과 Grid 위치 동기화 검증 시스템
        /// 오차가 임계값을 초과하면 경고 로그 출력
        /// </summary>
        /// <param name="currentPosition">현재 Transform 위치</param>
        /// <param name="progress">이동 진행도 (0.0 ~ 1.0)</param>
        private void ValidateTransformSync(Vector3 currentPosition, float progress)
        {
            // 예상 위치 계산 (선형 보간 기준)
            Vector3 expectedPosition = Vector3.Lerp(transformStartPosition, transformTargetPosition, progress);

            // 실제 위치와 예상 위치 오차 계산
            float positionError = Vector3.Distance(currentPosition, expectedPosition);

            // 임계값 초과 시 경고
            if (positionError > positionErrorThreshold)
            {
                Debug.LogWarning($"[BlendTreeAnimationController] {gameObject.name}: Transform sync error detected! " +
                                $"Error: {positionError:F4} (Threshold: {positionErrorThreshold:F4}), " +
                                $"Progress: {progress:F2}, Expected: {expectedPosition}, Actual: {currentPosition}");
            }
        }

        /// <summary>
        /// 현재 Transform과 목표 위치 간 오차 반환 (디버깅용)
        /// </summary>
        public float GetPositionError()
        {
            if (!isTransformSyncing) return 0f;
            return Vector3.Distance(transform.position, transformTargetPosition);
        }

        /// <summary>
        /// Transform 동기화 활성 상태 반환
        /// </summary>
        public bool IsTransformSyncing => isTransformSyncing;

        #endregion

        #region Debug Visualization

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 labelPosition = transform.position + Vector3.up * 2.0f;

            // 이동 상태 시각화
            if (isBlendTreeMoving)
            {
                float progress = GetMoveProgress();
                float speed = GetCurrentMoveSpeed();

                // Transform 동기화 상태 시각화
                string syncStatus = isTransformSyncing ?
                    $"Transform Sync: ON\nPosition Error: {GetPositionError():F4}" :
                    "Transform Sync: OFF";

                UnityEngine.GUIStyle moveStyle = new UnityEngine.GUIStyle();
                moveStyle.normal.textColor = isTransformSyncing ? Color.green : Color.white;

                UnityEditor.Handles.Label(labelPosition,
                    $"[MOVE] Progress: {progress:F2}\nSpeed: {speed:F2}\n{syncStatus}",
                    moveStyle);

                // Transform 동기화 경로 시각화
                if (isTransformSyncing)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transformStartPosition, transformTargetPosition);

                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transformTargetPosition, 0.2f);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position, 0.1f);
                }
            }

            // 공격 상태 시각화
            if (isBlendTreeAttacking)
            {
                float attackProgress = GetAttackProgress();
                float attackTrigger = GetCurrentAttackTrigger();

                UnityEngine.GUIStyle attackStyle = new UnityEngine.GUIStyle();
                attackStyle.normal.textColor = Color.red;

                string targetInfo = currentAttackTarget != null ?
                    $"Target: {currentAttackTarget.name}" :
                    "Target: None";

                UnityEditor.Handles.Label(labelPosition,
                    $"[ATTACK] Progress: {attackProgress:F2}\nTrigger: {attackTrigger:F2}\n{targetInfo}",
                    attackStyle);

                // 공격 대상 방향 시각화
                if (currentAttackTarget != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, currentAttackTarget.transform.position);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(currentAttackTarget.transform.position, 0.3f);
                }
            }
        }
#endif

        #endregion
    }
}
