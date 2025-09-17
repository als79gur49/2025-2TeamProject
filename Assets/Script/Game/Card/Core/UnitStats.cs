using UnityEngine;

[System.Serializable]
public struct UnitStats
{
    [SerializeField] private int maxHP;
    [SerializeField] private int attack;
    [SerializeField] private int movementRange;
    [SerializeField] private int attackRange;
    
    public int MaxHP => maxHP;
    public int Attack => attack;
    public int MovementRange => movementRange;
    public int AttackRange => attackRange;
    
    public UnitStats(int hp, int atk, int moveRange, int atkRange)
    {
        maxHP = hp;
        attack = atk;
        movementRange = moveRange;
        attackRange = atkRange;
    }
}

public enum UnitRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum SpellType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Shield,
    Teleport,
    Summon
}