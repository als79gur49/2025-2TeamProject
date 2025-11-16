using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Effect가 반응하는 타이밍을 정의합니다.
    /// </summary>
    public enum EffectTrigger
    {
        OnDeploy,
        OnTurnStart,
        OnTurnEnd,
        OnAttack,
        OnDamaged,
        OnDeath,
        OnMove
    }
}

