using Game.Data.Modifiers;

namespace Game.Interfaces
{
    /// <summary>
    /// 투사체 기반 공격 Modifier 마커 인터페이스
    /// </summary>
    public interface IProjectileAttackModifier : IAttackModifier
    {
        AttackConfig AttackConfig { get; }
    }
}
