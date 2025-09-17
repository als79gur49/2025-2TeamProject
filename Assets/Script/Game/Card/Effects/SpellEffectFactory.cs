public static class SpellEffectFactory
{
    public static ISpellEffect CreateEffect(SpellType type)
    {
        return type switch
        {
            SpellType.Damage => new DamageEffect(),
            SpellType.Heal => new HealEffect(),
            SpellType.Buff => new BuffEffect(),
            SpellType.Debuff => new DebuffEffect(),
            SpellType.Shield => new ShieldEffect(),
            SpellType.Teleport => new TeleportEffect(),
            SpellType.Summon => new SummonEffect(),
            _ => null
        };
    }
}