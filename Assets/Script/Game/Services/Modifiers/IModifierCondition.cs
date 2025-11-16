using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Services.Modifiers
{
    /// <summary>
    /// Modifier 실행 조건 인터페이스
    /// Strategy Pattern으로 조건 체크 로직 분리
    /// </summary>
    public interface IModifierCondition
    {
        /// <summary>
        /// 조건 평가
        /// </summary>
        /// <param name="owner">조건을 평가할 유닛</param>
        /// <param name="context">액션 컨텍스트</param>
        /// <returns>조건을 만족하면 true</returns>
        bool Evaluate(Unit owner, ActionContext context);

        /// <summary>
        /// 조건 설명 (디버깅/UI용)
        /// </summary>
        string Description { get; }
    }
}
