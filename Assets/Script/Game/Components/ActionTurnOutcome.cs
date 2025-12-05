using Game.Core;
using Game.Interfaces;

namespace Game.Components
{
    /// <summary>
    /// 한 유닛의 행동 턴이 어떻게 종료되었는지 요약하는 타입입니다.
    /// </summary>
    public enum ActionOutcomeType
    {
        None,
        Idle,
        Skipped,
        Attack,
        Move
    }

    /// <summary>
    /// 행동 턴 종료 결과 요약 구조체입니다.
    /// </summary>
    public readonly struct ActionTurnOutcome
    {
        public ActionOutcomeType Type { get; }
        public bool Success { get; }
        public ActionResult LastAction { get; }

        public ActionTurnOutcome(ActionOutcomeType type, bool success, ActionResult lastAction = null)
        {
            Type = type;
            Success = success;
            LastAction = lastAction;
        }
    }
}

