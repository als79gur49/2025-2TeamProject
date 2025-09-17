using UnityEngine;
using System;

public class Spell : BaseCard
{
    [Header("Spell Properties")]
    [SerializeField] private SpellType spellType;
    [SerializeField] private int effectValue;
    [SerializeField] private float effectRange;
    [SerializeField] private float cooldown;
    [SerializeField] private GameObject spellEffectPrefab;
    
    public SpellType SpellType => spellType;
    public int EffectValue => effectValue;
    public float EffectRange => effectRange;
    public float Cooldown => cooldown;
    
    private float lastUsedTime = -1f;
    
    public static event Action<Spell, Vector3> OnSpellCast;
    
    public Spell(string name, string desc, int cost, SpellType type, int effectValue, float range = 0f) 
        : base(name, desc, cost, CardType.Spell)
    {
        this.spellType = type;
        this.effectValue = effectValue;
        this.effectRange = range;
    }
    
    public override void Use()
    {
        if (!CanUse(out string failureReason))
        {
            Debug.LogWarning($"스펠 사용 실패: {failureReason}");
            return;
        }
        
        OnCardUsed();
        CastSpell();
        UpdateCooldown();
    }
    
    public override bool CanUse()
    {
        return CanUse(out _);
    }
    
    public override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason))
        {
            return false;
        }
        
        if (effectValue < 0)
        {
            failureReason = "스펠의 효과 값이 유효하지 않습니다.";
            return false;
        }
        
        if (effectRange < 0)
        {
            failureReason = "스펠의 효과 범위가 유효하지 않습니다.";
            return false;
        }
        
        if (cooldown < 0)
        {
            failureReason = "스펠의 쿨다운 시간이 유효하지 않습니다.";
            return false;
        }
        
        if (IsOnCooldown())
        {
            float remainingCooldown = cooldown - (Time.time - lastUsedTime);
            failureReason = $"스펠이 쿨다운 중입니다. (남은 시간: {remainingCooldown:F1}초)";
            return false;
        }
        
        if (!ValidateSpellTypeRequirements(out string typeFailureReason))
        {
            failureReason = typeFailureReason;
            return false;
        }
        
        failureReason = string.Empty;
        return true;
    }
    
    private bool ValidateSpellTypeRequirements(out string failureReason)
    {
        switch (spellType)
        {
            case SpellType.Damage:
                if (effectValue <= 0)
                {
                    failureReason = "데미지 스펠의 피해량은 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            case SpellType.Heal:
                if (effectValue <= 0)
                {
                    failureReason = "힐 스펠의 회복량은 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            case SpellType.Buff:
                if (effectValue <= 0)
                {
                    failureReason = "버프 스펠의 강화량은 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            case SpellType.Debuff:
                if (effectValue <= 0)
                {
                    failureReason = "디버프 스펠의 약화량은 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            case SpellType.Shield:
                if (effectValue <= 0)
                {
                    failureReason = "실드 스펠의 보호량은 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            case SpellType.Teleport:
                if (effectRange <= 0)
                {
                    failureReason = "텔레포트 스펠의 범위는 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            case SpellType.Summon:
                if (effectValue <= 0)
                {
                    failureReason = "소환 스펠의 소환 수는 0보다 커야 합니다.";
                    return false;
                }
                break;
                
            default:
                failureReason = $"알 수 없는 스펠 타입입니다: {spellType}";
                return false;
        }
        
        failureReason = string.Empty;
        return true;
    }
    
    private bool IsOnCooldown()
    {
        return lastUsedTime >= 0 && Time.time - lastUsedTime < cooldown;
    }
    
    private void UpdateCooldown()
    {
        lastUsedTime = Time.time;
    }
    
    private void CastSpell()
    {
        Vector3 targetPosition = GetTargetPosition();
        
        ApplySpellEffect(targetPosition);
        PlaySpellEffect(targetPosition);
        
        OnSpellCast?.Invoke(this, targetPosition);
        
        Debug.Log($"스펠 시전: {CardName} (타입: {spellType}, 효과: {effectValue})");
    }
    
    private void ApplySpellEffect(Vector3 targetPosition)
    {
        var effectHandler = SpellEffectFactory.CreateEffect(spellType);
        effectHandler?.Execute(targetPosition, effectValue, effectRange);
    }
    
    private void PlaySpellEffect(Vector3 position)
    {
        if (spellEffectPrefab != null)
        {
            GameObject effect = Instantiate(spellEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }
    
    private Vector3 GetTargetPosition()
    {
        return Vector3.zero;
    }
}