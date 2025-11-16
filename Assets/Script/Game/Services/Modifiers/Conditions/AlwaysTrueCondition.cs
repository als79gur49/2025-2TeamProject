using Game.Core;
using Game.Interfaces;

namespace Game.Services.Modifiers.Conditions
{
    /// <summary>
    /// 항상 통과하는 조건
    /// 조건이 없는 Modifier용 (Null Object Pattern)
    /// </summary>
    public class AlwaysTrueCondition : IModifierCondition
    {
        public static readonly AlwaysTrueCondition Instance = new AlwaysTrueCondition();

        public string Description => "조건 없음";

        private AlwaysTrueCondition() { }

        public bool Evaluate(Unit owner, ActionContext context)
        {
            return true;
        }
    }
}
