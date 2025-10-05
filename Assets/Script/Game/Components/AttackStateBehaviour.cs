using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// Attack State 전용 StateMachineBehaviour
    /// Transition과 독립적으로 AttackEnd 이벤트를 신뢰성 있게 호출
    ///
    /// 사용법:
    /// 1. Animator Controller의 모든 Attack State에 이 Behaviour 추가
    /// 2. OnStateExit에서 자동으로 AttackEnd() 호출
    /// 3. 콤보 공격의 경우 중간 State는 이 Behaviour 제거
    /// </summary>
    public class AttackStateBehaviour : StateMachineBehaviour
    {
        /// <summary>
        /// UnitAnimationController 캐싱 (GetComponent 오버헤드 제거)
        /// </summary>
        private UnitAnimationController cachedController;

        /// <summary>
        /// State 진입 시 UnitAnimationController 참조 캐싱
        /// </summary>
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            Debug.Log($"[AttackStateBehaviour] call AttackEnter - on {animator.gameObject.name}");

            if (cachedController == null)
            {
                cachedController = animator.GetComponent<UnitAnimationController>();
                Debug.Log($"[AttackStateBehaviour] OnStateEnterSecond {cachedController}");
                if (cachedController == null)
                {
                    Debug.LogError($"[AttackStateBehaviour] UnitAnimationController not found on {animator.gameObject.name}");
                }
            }
        }

        /// <summary>
        /// State 종료 시 AttackEnd 호출
        /// Transition 시작 시점에 확실하게 호출됨 (Exit Time 도달 시)
        /// </summary>
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (cachedController != null)
            {
                cachedController.AttackEnd();

                Debug.LogWarning($"[AttackStateBehaviour] call AttackEnd - on {animator.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[AttackStateBehaviour] Cannot call AttackEnd - controller is null on {animator.gameObject.name}");
            }
        }

        /// <summary>
        /// State 초기화 시 캐시 제거
        /// </summary>
        private void OnDisable()
        {
            cachedController = null;
        }
    }
}
