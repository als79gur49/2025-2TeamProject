using System;
using UnityEngine;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// Animation Event 기반 유닛 애니메이션 컨트롤러
    /// Unity Animator의 Animation Event를 수신하여 게임 로직에 전달
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

        #endregion

        #region Animation Parameter Names

        private const string MOVE_TRIGGER = "Move";
        private const string ATTACK_TRIGGER = "Attack";
        private const string IDLE_TRIGGER = "Idle";
        private const string ANIMATION_SPEED = "AnimSpeed";

        // Animation Type Constants (for currentAnimationType)
        private const string ANIM_TYPE_MOVE = "Move";
        private const string ANIM_TYPE_ATTACK = "Attack";

        #endregion

        #region Runtime State

        private bool isAnimationPlaying;
        private float currentAnimationProgress;
        private GameObject currentTarget;              // 공격 대상 추적
        private Vector2Int moveStartPosition;
        private Vector2Int moveTargetPosition;
        private string currentAnimationType;           // "Move", "Attack" 등

        #endregion

        #region IAnimationController Events

        public event Action<string> OnAnimationStarted;
        public event Action OnAnimationComplete;
        public event Action OnAnimationInterrupted;
        public event Action<GameObject> OnAttackHit;
        public event Action<Vector2Int> OnMovementFinished;
        public event Action OnSkillCast;

        #endregion

        #region VFX Events

        /// <summary>
        /// VFX 효과 요청 이벤트
        /// (효과 타입, 위치, 방향)
        /// </summary>
        public event Action<string, Vector3, Vector3> OnVFXRequested;

        /// <summary>
        /// SFX 사운드 요청 이벤트
        /// (사운드 타입, 위치)
        /// </summary>
        public event Action<string, Vector3> OnSFXRequested;

        #endregion

        #region Phase 2: Transform Movement Events

        /// <summary>
        /// Transform 이동 시작 이벤트 (Phase 2)
        /// AnimEvent_OnAnimationStart()에서 Move 타입일 때 발생
        /// </summary>
        public event Action<Vector2Int, Vector2Int> OnTransformMoveStart;

        /// <summary>
        /// Transform 이동 종료 이벤트 (Phase 2)
        /// AnimEvent_OnAnimationEnd()에서 Move 타입일 때 발생
        /// </summary>
        public event Action<Vector2Int> OnTransformMoveEnd;

        #endregion

        #region IAnimationController Properties

        public bool IsAnimationPlaying => isAnimationPlaying;
        public float CurrentAnimationProgress => currentAnimationProgress;
        public GameObject CurrentTarget => currentTarget;

        #endregion

        #region IAnimationController Methods

        /// <summary>
        /// 이동 애니메이션 재생 (트리거만, Animation Event가 나머지 처리)
        /// </summary>
        public void PlayMoveAnimation(Vector2Int from, Vector2Int to)
        {
            if (skipAnimations)
            {
                // 애니메이션 스킵 시 즉시 완료 이벤트 발생
                OnMovementFinished?.Invoke(to);
                return;
            }

            moveStartPosition = from;
            moveTargetPosition = to;
            currentAnimationType = ANIM_TYPE_MOVE;

            if (useAnimator && animator != null)
            {
                animator.SetTrigger(MOVE_TRIGGER);
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }

            // VFX 요청 (이동 먼지 효과 등)
            if (GameSettings.EnableVFX)
            {
                Vector3 startPos = new Vector3(from.x, 0, from.y);
                Vector3 direction = new Vector3(to.x - from.x, 0, to.y - from.y).normalized;
                OnVFXRequested?.Invoke("Move_Trail", startPos, direction);
            }

            // SFX 요청 (발소리 등)
            if (GameSettings.EnableSFX)
            {
                Vector3 pos = transform.position;
                OnSFXRequested?.Invoke("Footstep", pos);
            }

            // isAnimationPlaying은 AnimEvent_OnAnimationStart()에서 true로 설정됨
        }

        /// <summary>
        /// 공격 애니메이션 재생 (트리거만, Animation Event가 나머지 처리)
        /// </summary>
        public void PlayAttackAnimation(GameObject target)
        {
            if (skipAnimations)
            {
                // 애니메이션 스킵 시 즉시 공격 히트 및 완료
                currentTarget = target;
                OnAttackHit?.Invoke(target);
                currentTarget = null;
                return;
            }

            currentTarget = target;
            currentAnimationType = ANIM_TYPE_ATTACK;

            if (useAnimator && animator != null)
            {
                animator.SetTrigger(ATTACK_TRIGGER);
                animator.SetFloat(ANIMATION_SPEED, animationSpeedMultiplier);
            }

            // VFX 요청 (공격 휘두르기 효과)
            if (GameSettings.EnableVFX)
            {
                Vector3 pos = transform.position;
                Vector3 direction = target != null ? (target.transform.position - pos).normalized : transform.forward;
                OnVFXRequested?.Invoke("Attack_Swing", pos, direction);
            }

            // SFX 요청 (무기 휘두르는 소리)
            if (GameSettings.EnableSFX)
            {
                Vector3 pos = transform.position;
                OnSFXRequested?.Invoke("Attack_Swing", pos);
            }

            // 순서: AnimEvent_OnAnimationStart → AnimEvent_OnAttackImpact → AnimEvent_OnAnimationEnd
        }

        public void StopCurrentAnimation()
        {
            isAnimationPlaying = false;
            currentTarget = null;

            if (useAnimator && animator != null)
            {
                animator.SetTrigger(IDLE_TRIGGER);
            }

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

        #region Animation Event Callbacks
        // 🎬 이 메서드들은 Unity Animation Event에서 호출됨

        /// <summary>
        /// Animation Event: 애니메이션 시작 시 호출
        /// Animation Clip의 첫 프레임에 설정
        /// Transition 완료 후 실제 애니메이션이 시작되는 시점
        /// </summary>
        public void AnimEvent_OnAnimationStart()
        {
            isAnimationPlaying = true;
            currentAnimationProgress = 0f;

            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Animation Started - {currentAnimationType}");

            // Phase 2: Transform 보간 시작 신호
            if (currentAnimationType == ANIM_TYPE_MOVE)
            {
                OnTransformMoveStart?.Invoke(moveStartPosition, moveTargetPosition);
            }

            OnAnimationStarted?.Invoke(currentAnimationType);
        }

        /// <summary>
        /// Animation Event: 애니메이션 종료 시 호출
        /// Animation Clip의 마지막 프레임에 설정
        /// </summary>
        public void AnimEvent_OnAnimationEnd()
        {
            isAnimationPlaying = false;
            currentAnimationProgress = 1f;

            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Animation Ended - {currentAnimationType}");

            // Phase 2: Transform 보간 종료 신호 (이동 애니메이션만)
            if (currentAnimationType == ANIM_TYPE_MOVE)
            {
                OnTransformMoveEnd?.Invoke(moveTargetPosition);
                OnMovementFinished?.Invoke(moveTargetPosition);
            }

            OnAnimationComplete?.Invoke();

            // 정리
            currentAnimationType = null;
            currentTarget = null;
        }

        /// <summary>
        /// Animation Event: 공격이 실제로 타격하는 프레임에 호출
        /// Attack Animation Clip의 타격 프레임 (보통 60% 지점)에 설정
        /// CombatComponent가 이 이벤트를 구독하여 데미지 적용
        /// </summary>
        public void AnimEvent_OnAttackImpact()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Attack Impact on {currentTarget?.name}");

            if (currentTarget != null)
            {
                OnAttackHit?.Invoke(currentTarget);

                // VFX 요청 (타격 효과)
                if (GameSettings.EnableVFX)
                {
                    Vector3 hitPos = currentTarget.transform.position;
                    Vector3 direction = (hitPos - transform.position).normalized;
                    OnVFXRequested?.Invoke("Attack_Hit", hitPos, direction);
                }

                // SFX 요청 (타격 소리)
                if (GameSettings.EnableSFX)
                {
                    Vector3 hitPos = currentTarget.transform.position;
                    OnSFXRequested?.Invoke("Attack_Hit", hitPos);
                }
            }
            else
            {
                Debug.LogWarning($"[AnimationController] {gameObject.name}: Attack impact but no target set!");
            }
        }

        /// <summary>
        /// Animation Event: 스킬 발동 시점 (추후 확장용)
        /// </summary>
        public void AnimEvent_OnSkillCast()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Skill Cast");

            OnSkillCast?.Invoke();
        }

        /// <summary>
        /// Animation Event: 이동 완료 시점 (선택적, AnimEvent_OnAnimationEnd와 별도 타이밍 필요시)
        /// </summary>
        public void AnimEvent_OnMoveComplete()
        {
            if (logAnimationEvents)
                Debug.Log($"[AnimationController] {gameObject.name}: Move Complete to {moveTargetPosition}");

            OnMovementFinished?.Invoke(moveTargetPosition);
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

            // GameSettings 연동
            ApplyGameSettings();
            GameSettings.OnAnimationSettingsChanged += ApplyGameSettings;
        }

        private void Update()
        {
            // Phase 2: 애니메이션 진행도 실시간 추적 (Transform 보간용)
            if (isAnimationPlaying && useAnimator && animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                // Transition 중이 아닐 때만 진행도 업데이트 (정확도 보장)
                if (!animator.IsInTransition(0))
                {
                    currentAnimationProgress = Mathf.Clamp01(stateInfo.normalizedTime);
                }
            }
        }

        private void OnDestroy()
        {
            StopCurrentAnimation();
            GameSettings.OnAnimationSettingsChanged -= ApplyGameSettings;
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
