using UnityEngine;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// VFX TriggerData를 활용하는 고급 효과 인터페이스
    /// 공격 성공/실패, 타겟 유효성 등의 동적 정보에 반응
    /// </summary>
    public interface IVFXAwareEffect : ICardEffect
    {
        /// <summary>
        /// VFX TriggerData를 포함한 효과 실행
        /// </summary>
        /// <param name="targetPos">타겟 그리드 좌표</param>
        /// <param name="context">게임 컨텍스트</param>
        /// <param name="triggerData">VFX 동적 데이터 (공격 성공/실패, 타겟 정보 등)</param>
        void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData);
    }
}
