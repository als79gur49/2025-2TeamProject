using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Interfaces;
using Game.Card.Effects;

namespace Game.VFX
{
    /// <summary>
    /// VFX 기반 효과 실행 인터페이스
    /// ServiceLocator 패턴으로 의존성 주입 지원
    /// </summary>
    public interface ISpellEffectExecutor
    {
        /// <summary>
        /// 여러 효과를 단일 VFX로 실행 (TriggerData 기반)
        /// </summary>
        /// <param name="effectDataList">읽기 전용 효과 데이터 리스트</param>
        /// <param name="targetPos">타겟 그리드 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        void ExecuteBatch(IReadOnlyList<EffectData> effectDataList, Vector2Int targetPos, GameContext context);
    }
}
