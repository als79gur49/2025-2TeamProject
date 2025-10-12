using UnityEngine;
using System.Collections.Generic;

namespace Game.Interfaces
{
    /// <summary>
    /// 유닛 AI 의사결정 인터페이스
    /// 타겟팅 및 행동 결정 책임을 Unit에서 분리
    /// - Unit과 Base 모두 공격 가능 (IHealthComponent 기반)
    /// - CombatComponent와 MovementComponent 활용
    /// - Strategy Pattern 지원
    /// </summary>
    public interface IUnitAI
    {
        /// <summary>현재 턴에 수행할 행동 결정</summary>
        ActionDecision DecideAction();

        /// <summary>
        /// 공격 가능한 최적의 타겟 찾기
        /// Unit과 Base 모두 공격 가능 (IHealthComponent 반환)
        /// </summary>
        IHealthComponent FindBestTarget();

        /// <summary>AI 전략 설정 (향후 확장용)</summary>
        void SetStrategy(AIStrategy strategy);
    }

    /// <summary>
    /// AI가 결정한 행동 정보
    /// </summary>
    public struct ActionDecision
    {
        public ActionType Type;              // 행동 타입
        public IHealthComponent TargetHealth; // 공격 대상 (IHealthComponent)
        public GameObject TargetObject;      // 타겟의 GameObject
        public Vector2Int MovePosition;      // 이동 목표 위치

        /// <summary>공격 행동 생성</summary>
        public static ActionDecision Attack(IHealthComponent target)
        {
            return new ActionDecision
            {
                Type = ActionType.Attack,
                TargetHealth = target,
                TargetObject = (target as Component)?.gameObject
            };
        }

        /// <summary>이동 행동 생성</summary>
        public static ActionDecision Move(Vector2Int position)
        {
            return new ActionDecision
            {
                Type = ActionType.Move,
                MovePosition = position
            };
        }

        /// <summary>대기 행동 생성</summary>
        public static ActionDecision Idle()
        {
            return new ActionDecision { Type = ActionType.Idle };
        }
    }

    /// <summary>행동 타입</summary>
    public enum ActionType
    {
        Idle,    // 대기
        Attack,  // 공격
        Move     // 이동
    }

    /// <summary>AI 전략 (향후 확장용)</summary>
    public enum AIStrategy
    {
        Basic,       // 기본 전략 (가장 가까운 적 공격)
        Aggressive,  // 공격적 (높은 공격력 우선)
        Defensive,   // 방어적 (낮은 HP 아군 보호)
        Optimal      // 최적화 (피해 최대화 계산)
    }
}
