using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 카드 효과 실행을 위한 인터페이스
    /// 팩토리 패턴의 핵심 인터페이스로, 모든 카드 효과 구현체가 상속받습니다.
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// 효과를 실행할 수 있는지 검증합니다.
        /// </summary>
        /// <param name="targetPos">목표 위치</param>
        /// <param name="context">게임 컨텍스트 (서비스 참조)</param>
        /// <returns>실행 가능하면 true, 아니면 false</returns>
        bool CanExecute(Vector2Int targetPos, GameContext context);

        /// <summary>
        /// 카드 효과를 실행합니다.
        /// </summary>
        /// <param name="targetPos">목표 위치</param>
        /// <param name="context">게임 컨텍스트 (서비스 참조)</param>
        void Execute(Vector2Int targetPos, GameContext context);

        /// <summary>
        /// 효과 타입을 반환합니다.
        /// </summary>
        EffectType EffectType { get; }

        /// <summary>
        /// 효과의 우선순위를 반환합니다. (낮은 값일수록 먼저 실행)
        /// </summary>
        int Priority { get; }
    }
}