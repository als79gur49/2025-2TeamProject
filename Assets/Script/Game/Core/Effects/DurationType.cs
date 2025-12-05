using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Effect 지속시간이 어떤 시간축을 기준으로 감소하는지 정의합니다.
    /// </summary>
    public enum DurationType
    {
        /// <summary>
        /// 전역 턴 종료(TurnEnd)를 기준으로 감소합니다.
        /// </summary>
        TimeBased,

        /// <summary>
        /// 유닛의 행동 턴(Action Turn) 처리를 기준으로 감소합니다.
        /// </summary>
        ActionBased
    }
}

