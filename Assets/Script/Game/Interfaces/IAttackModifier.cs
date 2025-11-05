namespace Game.Interfaces
{
    public interface IAttackModifier : IActionModifier
    {
        int CalculateDamage(ActionContext context);
    }
}
