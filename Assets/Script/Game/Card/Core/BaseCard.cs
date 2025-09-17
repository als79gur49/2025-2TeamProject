using UnityEngine;

public abstract class BaseCard : MonoBehaviour, ICard
{
    [Header("Card Base Info")]
    [SerializeField] private string cardName;
    [SerializeField] private string description;
    [SerializeField] private int manaCost;
    [SerializeField] private CardType cardType;
    [SerializeField] private Sprite cardImage;
    
    public string CardName => cardName;
    public string Description => description;
    public int ManaCost => manaCost;
    public CardType CardType => cardType;
    public Sprite CardImage => cardImage;
    
    protected BaseCard(string name, string desc, int cost, CardType type)
    {
        cardName = name;
        description = desc;
        manaCost = cost;
        cardType = type;
    }
    
    public abstract void Use();
    
    public virtual bool CanUse()
    {
        return true;
    }
    
    public virtual bool CanUse(out string failureReason)
    {
        failureReason = string.Empty;
        return CanUse();
    }
    
    protected virtual void OnCardUsed()
    {
        Debug.Log($"카드 사용됨: {cardName} (비용: {manaCost})");
    }
}