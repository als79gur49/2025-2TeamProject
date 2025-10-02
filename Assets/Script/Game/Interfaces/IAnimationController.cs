using System;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// Animation Event 기반 유닛 애니메이션 제어 인터페이스
    /// Unity Animation Event를 통해 정확한 타이밍에 게임플레이 이벤트 발생
    /// </summary>
    public interface IAnimationController
    {
        // ========================================
        // Properties
        // ========================================

        /// <summary>
        /// 현재 애니메이션이 재생 중인지 여부
        /// UnitService가 이 값을 체크하여 애니메이션 완료 대기
        /// </summary>
        bool IsAnimationPlaying { get; }

        /// <summary>
        /// 현재 애니메이션의 진행도 (0.0 ~ 1.0)
        /// </summary>
        float CurrentAnimationProgress { get; }

        /// <summary>
        /// 현재 공격 대상 GameObject
        /// 공격 애니메이션 재생 중 타겟 추적용
        /// </summary>
        GameObject CurrentTarget { get; }

        // ========================================
        // Animation Methods
        // ========================================

        /// <summary>
        /// 이동 애니메이션 재생 (트리거만, Animation Event가 나머지 처리)
        /// </summary>
        /// <param name="from">시작 그리드 위치</param>
        /// <param name="to">목표 그리드 위치</param>
        void PlayMoveAnimation(Vector2Int from, Vector2Int to);

        /// <summary>
        /// 공격 애니메이션 재생 (트리거만, Animation Event가 나머지 처리)
        /// </summary>
        /// <param name="target">공격 대상 GameObject</param>
        void PlayAttackAnimation(GameObject target);

        /// <summary>
        /// 현재 재생 중인 애니메이션 중단
        /// </summary>
        void StopCurrentAnimation();

        /// <summary>
        /// 애니메이션 속도 배율 설정
        /// </summary>
        /// <param name="speed">속도 배율 (1.0 = 정상 속도)</param>
        void SetAnimationSpeed(float speed);

        // ========================================
        // Animation Lifecycle Events
        // (Animation Event에서 호출)
        // ========================================

        /// <summary>
        /// 애니메이션 시작 이벤트
        /// AnimEvent_OnAnimationStart()에서 호출
        /// </summary>
        event Action<string> OnAnimationStarted;

        /// <summary>
        /// 애니메이션 완료 이벤트
        /// AnimEvent_OnAnimationEnd()에서 호출
        /// UnitService가 이 이벤트를 대기
        /// </summary>
        event Action OnAnimationComplete;

        /// <summary>
        /// 애니메이션 중단 이벤트
        /// </summary>
        event Action OnAnimationInterrupted;

        // ========================================
        // Gameplay Events
        // (특정 타이밍에 게임플레이 로직 실행)
        // ========================================

        /// <summary>
        /// 공격 타격 순간 이벤트
        /// AnimEvent_OnAttackImpact()에서 호출
        /// CombatComponent가 이 이벤트를 구독하여 데미지 적용
        /// </summary>
        event Action<GameObject> OnAttackHit;

        /// <summary>
        /// 이동 완료 이벤트
        /// AnimEvent_OnMoveComplete()에서 호출
        /// MovementComponent가 이 이벤트를 구독하여 추가 처리 (선택적)
        /// </summary>
        event Action<Vector2Int> OnMovementFinished;

        /// <summary>
        /// 스킬 시전 이벤트 (추후 확장용)
        /// AnimEvent_OnSkillCast()에서 호출
        /// </summary>
        event Action OnSkillCast;
    }
}
