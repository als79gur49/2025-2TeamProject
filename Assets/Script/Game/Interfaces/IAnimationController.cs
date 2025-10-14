using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interfaces
{
    /// <summary>
    /// BlendTree 기반 유닛 애니메이션 제어 인터페이스
    /// BlendTreeAnimationController를 통한 정밀한 타이밍 제어
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
        /// BlendTree 기반 이동 애니메이션 재생
        /// </summary>
        /// <param name="from">시작 그리드 위치</param>
        /// <param name="to">목표 그리드 위치</param>
        void PlayMoveAnimation(Vector2Int from, Vector2Int to);

        /// <summary>
        /// BlendTree 기반 공격 애니메이션 재생
        /// </summary>
        /// <param name="target">공격 대상 GameObject</param>
        void PlayAttackAnimation(GameObject target);

        /// <summary>
        /// BlendTree 기반 공격 애니메이션 재생 (다중 타겟)
        /// </summary>
        /// <param name="targets">공격 대상 GameObject 리스트</param>
        void PlayAttackAnimation(List<GameObject> targets);

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
        // BlendTree Animation Events
        // ========================================

        /// <summary>
        /// 이동 시작 이벤트 (from, to)
        /// BlendTreeAnimationController.OnBlendTreeMoveStart에서 전달
        /// </summary>
        event Action<Vector2Int, Vector2Int> OnMoveStart;

        /// <summary>
        /// 이동 종료 이벤트 (targetPosition)
        /// BlendTreeAnimationController.OnBlendTreeMoveEnd에서 전달
        /// </summary>
        event Action<Vector2Int> OnMoveEnd;

        /// <summary>
        /// 공격 시작 이벤트 (targets)
        /// BlendTreeAnimationController.OnBlendTreeAttackStart에서 전달
        /// </summary>
        event Action<List<GameObject>> OnAttackStart;

        /// <summary>
        /// 공격 종료 이벤트 (targets)
        /// BlendTreeAnimationController.OnBlendTreeAttackEnd에서 전달
        /// </summary>
        event Action<List<GameObject>> OnAttackEnd;

        /// <summary>
        /// 애니메이션 중단 이벤트
        /// StopCurrentAnimation() 호출 시 발생
        /// </summary>
        event Action OnAnimationInterrupted;

        /// <summary>
        /// 공격 타격 순간 이벤트 (공격 진행도 60% 지점)
        /// List 기반으로 단일/다중 타겟 모두 지원
        /// CombatComponent가 이 이벤트를 구독하여 데미지 적용
        /// </summary>
        event Action<List<GameObject>> OnAttackHit;

        // ========================================
        // VFX/SFX Events
        // ========================================

        /// <summary>
        /// VFX 효과 요청 이벤트 (효과 타입, 위치, 방향)
        /// </summary>
        event Action<string, Vector3, Vector3> OnVFXRequested;

        /// <summary>
        /// SFX 사운드 요청 이벤트 (사운드 타입, 위치)
        /// </summary>
        event Action<string, Vector3> OnSFXRequested;
    }
}
