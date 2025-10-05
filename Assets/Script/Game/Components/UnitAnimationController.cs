using System;
using System.Collections;
using UnityEngine;
using Game.Interfaces;
using Game.Services;
using Game.Core;

namespace Game.Components
{
    /// <summary>
    /// 유닛 애니메이션 컨트롤러
    /// - 이동: BlendTree 기반 (BlendTreeAnimationController 사용)
    /// - 공격: Animation Event 기반 (Animator Trigger + Animation Event)
    /// </summary>
    public class UnitAnimationController : MonoBehaviour, IAnimationController
    {
        #region Serialized Fields

        [Header("Animator Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool useAnimator = true;

        [Header("Advanced Settings")]
        [SerializeField] private float animationSpeedMultiplier = 1f;
        [SerializeField] private bool skipAnimations = false;

        [Header("Debug")]
        [SerializeField] private bool logAnimationEvents = true;

        [Header("BlendTree Animation")]
        [SerializeField] private BlendTreeAnimationController blendTreeController;

        #endregion

        #region Grid Manager Reference

        private IGridManager gridManager;

        #endregion

        #region Animation Parameter Names

        private const string ANIMATION_SPEED = "AnimSpeed";
        private const string ATTACK_TRIGGER = "Attack";

        #endregion

        #region Runtime State

        private bool isAnimationPlaying;
        private float currentAnimationProgress;
        private GameObject currentTarget;

        #endregion

        #region IAnimationController Events

        /// <summary>
        /// 이동 시작 이벤트 (from, to)
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnMoveStart;

        /// <summary>
        /// 이동 종료 이벤트 (targetPosition)
        /// </summary>
        public event Action<Vector2Int> OnMoveEnd;

        /// <summary>
        /// 공격 시작 이벤트 (target)
        /// </summary>
        public event Action<GameObject> OnAttackStart;

        /// <summary>
        /// 공격 종료 이벤트 (target)
        /// </summary>
        public event Action<GameObject> OnAttackEnd;

        /// <summary>
        /// 애니메이션 중단 이벤트
        /// </summary>
        public event Action OnAnimationInterrupted;

        /// <summary>
        /// 공격 타격 순간 이벤트 (공격 진행도 60% 지점)
        /// </summary>
        public event Action<GameObject> OnAttackHit;

        /// <summary>
        /// VFX 효과 요청 이벤트 (효과 타입, 위치, 방향)
        /// </summary>
        public event Action<string, Vector3, Vector3> OnVFXRequested;

        /// <summary>
        /// SFX 사운드 요청 이벤트 (사운드 타입, 위치)
        /// </summary>
        public event Action<string, Vector3> OnSFXRequested;

        #endregion

        #region IAnimationController Properties

        public bool IsAnimationPlaying => isAnimationPlaying;
        public float CurrentAnimationProgress => currentAnimationProgress;
        public GameObject CurrentTarget => currentTarget;

        #endregion

        #region IAnimationController Methods

        /// <summary>
        /// BlendTree 기반 이동 애니메이션 재생
        /// 상태 관리는 Handler에서 자동으로 처리됨
        /// </summary>
        public void PlayMoveAnimation(Vector2Int from, Vector2Int to)
        {
            if (skipAnimations)
            {
                OnMoveEnd?.Invoke(to);
                return;
            }

            if (blendTreeController == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: BlendTreeAnimationController is not assigned!");
                OnMoveEnd?.Invoke(to);
                return;
            }

            if (gridManager == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: GridManager is not assigned!");
                OnMoveEnd?.Invoke(to);
                return;
            }

            // 상태 관리는 HandleBlendTreeMoveStart에서 처리
            // isAnimationPlaying과 currentAnimationProgress는 Handler가 관리

            // Grid 좌표를 World 좌표로 변환 (GridManager 사용)
            Vector3 startWorldPos = gridManager.GridToWorldPosition(from);
            Vector3 targetWorldPos = gridManager.GridToWorldPosition(to);

            // VFX/SFX 요청
            if (GameSettings.EnableVFX)
            {
                Vector3 direction = (targetWorldPos - startWorldPos).normalized;
                OnVFXRequested?.Invoke("Move_Trail", startWorldPos, direction);
            }

            if (GameSettings.EnableSFX)
            {
                OnSFXRequested?.Invoke("Footstep", transform.position);
            }

            // BlendTree 이동 시작 (이벤트는 BlendTreeAnimationController에서 발생)
            blendTreeController.StartBlendTreeMove(startWorldPos, targetWorldPos);

            // 이동 완료 모니터링 시작
            StartCoroutine(MonitorBlendTreeMove(to));

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: BlendTree move started from {from} to {to}");
        }

        /// <summary>
        /// Animation Event 기반 공격 애니메이션 재생
        /// Animator의 Attack Trigger를 통해 애니메이션 시작
        /// 실제 타격은 Animation Event에서 AttackHit() 호출로 처리
        /// </summary>
        public void PlayAttackAnimation(GameObject target)
        {
            if (skipAnimations)
            {
                currentTarget = target;
                OnAttackHit?.Invoke(target);
                OnAttackEnd?.Invoke(target);
                currentTarget = null;
                return;
            }

            if (animator == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: Animator is not assigned!");
                return;
            }

            // 공격 상태 시작
            isAnimationPlaying = true;
            currentAnimationProgress = 0f;
            currentTarget = target;

            // 공격 시작 이벤트 발생
            OnAttackStart?.Invoke(target);

            // VFX/SFX 요청
            if (GameSettings.EnableVFX)
            {
                Vector3 pos = transform.position;
                Vector3 direction = target != null ? (target.transform.position - pos).normalized : transform.forward;
                OnVFXRequested?.Invoke("Attack_Swing", pos, direction);
            }

            if (GameSettings.EnableSFX)
            {
                Vector3 pos = transform.position;
                OnSFXRequested?.Invoke("Attack_Swing", pos);
            }

            // Animator Attack Trigger 실행
            animator.SetTrigger(ATTACK_TRIGGER);

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: Attack animation started on {target?.name}");
        }

        public void StopCurrentAnimation()
        {
            isAnimationPlaying = false;
            currentTarget = null;

            OnAnimationInterrupted?.Invoke();
        }

        public void SetAnimationSpeed(float speed)
        {
            animationSpeedMultiplier = Mathf.Max(0.1f, speed);

            if (useAnimator && animator != null)
            {
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }
        }

        #endregion

        #region BlendTree Movement Monitoring

        /// <summary>
        /// BlendTree 이동 완료를 모니터링하는 코루틴
        /// 완료 처리는 BlendTreeAnimationController.UpdateBlendTreeSpeed에서 자동으로 수행됨
        /// 이 코루틴은 단순히 완료 대기만 담당
        /// </summary>
        private IEnumerator MonitorBlendTreeMove(Vector2Int targetPos)
        {
            while (blendTreeController.GetMoveProgress() < 1.0f)
            {
                yield return null;
            }

            // 완료 처리는 BlendTreeAnimationController가 자체적으로 수행
            // (UpdateBlendTreeSpeed에서 elapsed >= moveDuration 체크)
            // 여기서는 완료 대기만 하고 이벤트는 HandleBlendTreeMoveEnd에서 수신

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: BlendTree move monitor completed to {targetPos}");
        }

        #endregion

        #region Animation Event Handlers

        /// <summary>
        /// Animation Event에서 호출되는 공격 타격 이벤트
        /// 공격 애니메이션의 타격 프레임에서 Unity Animation Event로 호출됨
        /// </summary>
        public void AttackHit()
        {
            if (currentTarget == null)
            {
                if (logAnimationEvents)
                    Debug.LogWarning($"[UnitAnimationController] {gameObject.name}: AttackHit called but currentTarget is null");
                return;
            }

            // 공격 타격 이벤트 발생
            OnAttackHit?.Invoke(currentTarget);

            // VFX/SFX 효과
            if (GameSettings.EnableVFX && currentTarget != null)
            {
                Vector3 hitPos = currentTarget.transform.position;
                Vector3 direction = (hitPos - transform.position).normalized;
                OnVFXRequested?.Invoke("Attack_Hit", hitPos, direction);
            }

            if (GameSettings.EnableSFX && currentTarget != null)
            {
                Vector3 hitPos = currentTarget.transform.position;
                OnSFXRequested?.Invoke("Attack_Hit", hitPos);
            }

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: Attack hit on {currentTarget?.name}");
        }

        /// <summary>
        /// 공격 종료 이벤트
        /// StateMachineBehaviour.OnStateExit에서 호출됨 (이전: Animation Event)
        ///
        /// 변경 이유:
        /// - Unity Animation Event는 마지막 프레임에서 신뢰성 문제가 있음
        /// - StateMachineBehaviour.OnStateExit는 Transition과 독립적으로 확실하게 호출됨
        /// - AttackStateBehaviour에서 이 메서드를 호출하여 상태 초기화 보장
        /// </summary>
        public void AttackEnd()
        {
            GameObject target = currentTarget;

            // 상태 초기화
            isAnimationPlaying = false;
            currentAnimationProgress = 1f;
            currentTarget = null;

            // 공격 종료 이벤트 발생
            OnAttackEnd?.Invoke(target);

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: Attack animation ended on {target?.name}");
        }

        #endregion

        #region BlendTree Event Handlers (이동 애니메이션 전용)

        /// <summary>
        /// BlendTree 이동 시작 핸들러
        /// BlendTreeAnimationController.OnBlendTreeMoveStart 이벤트를 구독
        /// 이벤트 기반으로 애니메이션 상태 관리
        /// </summary>
        private void HandleBlendTreeMoveStart(Vector3 startPos, Vector3 targetPos)
        {
            // 애니메이션 상태 시작
            isAnimationPlaying = true;
            currentAnimationProgress = 0f;

            if (gridManager == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: GridManager is null in HandleBlendTreeMoveStart!");
                return;
            }

            // World 좌표를 Grid 좌표로 변환 (GridManager 사용)
            Vector2Int from = gridManager.WorldToGridPosition(startPos);
            Vector2Int to = gridManager.WorldToGridPosition(targetPos);

            // 이동 시작 이벤트 재발행
            OnMoveStart?.Invoke(from, to);

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: Move Start {from} → {to} (isAnimationPlaying: {isAnimationPlaying})");
        }

        /// <summary>
        /// BlendTree 이동 완료 핸들러
        /// BlendTreeAnimationController.OnBlendTreeMoveEnd 이벤트를 구독
        /// 이벤트 기반으로 애니메이션 상태 관리
        /// </summary>
        private void HandleBlendTreeMoveEnd(Vector3 finalPos)
        {
            // 애니메이션 상태 완료
            isAnimationPlaying = false;
            currentAnimationProgress = 1f;

            if (gridManager == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: GridManager is null in HandleBlendTreeMoveEnd!");
                return;
            }

            // World 좌표를 Grid 좌표로 변환 (GridManager 사용)
            Vector2Int targetPos = gridManager.WorldToGridPosition(finalPos);

            // 이동 종료 이벤트 재발행
            OnMoveEnd?.Invoke(targetPos);

            if (logAnimationEvents)
                Debug.Log($"[UnitAnimationController] {gameObject.name}: Move End at {targetPos} (isAnimationPlaying: {isAnimationPlaying})");
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"[UnitAnimationController] No Animator on {gameObject.name}, animations will be skipped");
                skipAnimations = true;
            }

            // BlendTreeAnimationController 자동 검색
            if (blendTreeController == null)
            {
                blendTreeController = GetComponent<BlendTreeAnimationController>();
            }

            if (blendTreeController == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: BlendTreeAnimationController not found! Adding component...");
                blendTreeController = gameObject.AddComponent<BlendTreeAnimationController>();
            }
            
            // GridManager 참조 가져오기 (ServiceLocator 패턴)
            gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager == null)
            {
                Debug.LogError($"[UnitAnimationController] {gameObject.name}: GridManager not found in ServiceLocator!");
            }

            // BlendTree 이벤트 구독 (GameServiceManager.ConnectServiceEvents() 패턴)
            ConnectBlendTreeEvents();

            // GameSettings 연동
            ApplyGameSettings();
            GameSettings.OnAnimationSettingsChanged += ApplyGameSettings;
        }

        private void Update()
        {
            // BlendTree 진행도 업데이트 (이동 애니메이션만)
            if (isAnimationPlaying && blendTreeController != null)
            {
                if (blendTreeController.GetMoveProgress() > 0f)
                {
                    currentAnimationProgress = blendTreeController.GetMoveProgress();
                }
            }

            // 공격 애니메이션 진행도는 Animator의 현재 상태 기반으로 업데이트
            if (isAnimationPlaying && animator != null && currentTarget != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsTag("Attack"))
                {
                    currentAnimationProgress = stateInfo.normalizedTime;
                }
            }
        }

        private void OnDestroy()
        {
            // BlendTree 이벤트 구독 해제
            DisconnectBlendTreeEvents();

            StopCurrentAnimation();
            GameSettings.OnAnimationSettingsChanged -= ApplyGameSettings;
        }

        /// <summary>
        /// BlendTreeAnimationController 이벤트를 UnitAnimationController 핸들러에 연결
        /// 이동 애니메이션만 BlendTree 사용, 공격 애니메이션은 Animation Event 사용
        /// </summary>
        private void ConnectBlendTreeEvents()
        {
            if (blendTreeController != null)
            {
                blendTreeController.OnBlendTreeMoveStart += HandleBlendTreeMoveStart;
                blendTreeController.OnBlendTreeMoveEnd += HandleBlendTreeMoveEnd;

                if (logAnimationEvents)
                    Debug.Log($"[UnitAnimationController] {gameObject.name}: BlendTree events connected (Move only)");
            }
        }

        /// <summary>
        /// BlendTreeAnimationController 이벤트 구독 해제
        /// </summary>
        private void DisconnectBlendTreeEvents()
        {
            if (blendTreeController != null)
            {
                blendTreeController.OnBlendTreeMoveStart -= HandleBlendTreeMoveStart;
                blendTreeController.OnBlendTreeMoveEnd -= HandleBlendTreeMoveEnd;

                if (logAnimationEvents)
                    Debug.Log($"[UnitAnimationController] {gameObject.name}: BlendTree events disconnected");
            }
        }

        /// <summary>
        /// GameSettings의 설정을 이 컨트롤러에 적용
        /// </summary>
        private void ApplyGameSettings()
        {
            skipAnimations = !GameSettings.EnableUnitAnimations;
            animationSpeedMultiplier = GameSettings.GlobalAnimationSpeed;

            if (useAnimator && animator != null)
            {
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }

            if (logAnimationEvents)
            {
                Debug.Log($"[UnitAnimationController] {gameObject.name}: Applied GameSettings - " +
                         $"Speed: {animationSpeedMultiplier}x, Skip: {skipAnimations}");
            }
        }

        #endregion
    }
}
