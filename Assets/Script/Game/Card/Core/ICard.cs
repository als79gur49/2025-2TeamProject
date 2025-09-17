using UnityEngine;

public interface ICard
{
    string CardName { get; }
    int ManaCost { get; }
    CardType CardType { get; }
    void Use();
    bool CanUse();
    bool CanUse(out string failureReason);
}

public enum CardType
{
    Unit,
    Spell,
    Equipment,
    Environment
}