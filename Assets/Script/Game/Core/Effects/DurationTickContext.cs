using Game.Components;

namespace Game.Core.Effects
{
    /// <summary>
    /// Duration Tick이 어떤 소스에서 호출되었는지를 나타냅니다.
    /// </summary>
    public enum DurationTickSource
    {
        /// <summary>
        /// 유닛의 OnTurnEnd() 이후, 전역 턴 종료 시점에서 호출됨.
        /// </summary>
        TurnEnd,

        /// <summary>
        /// 유닛의 행동 턴(Action Turn) 처리 완료 시점에서 호출됨.
        /// </summary>
        ActionEnd
    }

    /// <summary>
    /// Duration Tick 호출 컨텍스트입니다.
    /// </summary>
    public readonly struct DurationTickContext
    {
        public DurationTickSource Source { get; }
        public Unit ActingUnit { get; }
        public ActionTurnOutcome? ActionOutcome { get; }

        public DurationTickContext(DurationTickSource source, Unit actingUnit, ActionTurnOutcome? actionOutcome = null)
        {
            Source = source;
            ActingUnit = actingUnit;
            ActionOutcome = actionOutcome;
        }
    }
}

