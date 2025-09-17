using UnityEngine;

public class CardSpellTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runTestsOnStart = true;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            RunAllTests();
        }
    }
    
    public void RunAllTests()
    {
        Debug.Log("=== Card Spell Test 시작 ===");
        
        TestSpellCardCreation();
        TestSpellEffects();
        TestSpellValidation();
        TestSpellCooldown();
        
        Debug.Log("=== Card Spell Test 완료 ===");
    }
    
    private void TestSpellCardCreation()
    {
        Debug.Log("--- Spell Card 생성 테스트 ---");
        
        GameObject cardObject = new GameObject("TestSpellCard");
        Spell spellCard = cardObject.AddComponent<Spell>();
        
        if (spellCard != null)
        {
            Debug.Log("✓ Spell Card 생성 성공");
            
            if (spellCard.CardType == CardType.Spell)
            {
                Debug.Log("✓ Card Type 설정 성공");
            }
            else
            {
                Debug.LogError("✗ Card Type 설정 실패");
            }
        }
        else
        {
            Debug.LogError("✗ Spell Card 생성 실패");
        }
        
        Destroy(cardObject);
    }
    
    private void TestSpellEffects()
    {
        Debug.Log("--- Spell Effects 테스트 ---");
        
        TestSpellEffectType(SpellType.Damage);
        TestSpellEffectType(SpellType.Heal);
        TestSpellEffectType(SpellType.Buff);
        TestSpellEffectType(SpellType.Debuff);
        TestSpellEffectType(SpellType.Shield);
        TestSpellEffectType(SpellType.Teleport);
        TestSpellEffectType(SpellType.Summon);
    }
    
    private void TestSpellEffectType(SpellType spellType)
    {
        var effect = SpellEffectFactory.CreateEffect(spellType);
        if (effect != null)
        {
            Debug.Log($"✓ {spellType} Effect 생성 성공");
            effect.Execute(Vector3.zero, 10, 5f);
        }
        else
        {
            Debug.LogError($"✗ {spellType} Effect 생성 실패");
        }
    }
    
    private void TestSpellValidation()
    {
        Debug.Log("--- Spell Validation 테스트 ---");
        
        GameObject cardObject = new GameObject("TestSpellCard");
        Spell spellCard = cardObject.AddComponent<Spell>();
        
        bool canUseResult = spellCard.CanUse(out string failureReason);
        Debug.Log($"Spell 사용 가능 여부: {canUseResult}");
        
        if (!canUseResult)
        {
            Debug.Log($"사용 불가 이유: {failureReason}");
        }
        
        Destroy(cardObject);
    }
    
    private void TestSpellCooldown()
    {
        Debug.Log("--- Spell Cooldown 테스트 ---");
        
        GameObject cardObject = new GameObject("TestSpellCard");
        Spell spellCard = cardObject.AddComponent<Spell>();
        
        spellCard.Use();
        
        Debug.Log("첫 번째 사용 후 즉시 재사용 시도:");
        bool canUseAgain = spellCard.CanUse(out string reason);
        Debug.Log($"재사용 가능: {canUseAgain}, 이유: {reason}");
        
        Destroy(cardObject);
    }
    
    public void TestSpecificSpell(SpellType spellType, int effectValue, float range, float cooldown)
    {
        Debug.Log($"--- {spellType} 특정 테스트 ---");
        
        GameObject cardObject = new GameObject($"Test{spellType}Spell");
        Spell spellCard = cardObject.AddComponent<Spell>();
        
        Debug.Log($"스펠 타입: {spellType}");
        Debug.Log($"효과 값: {effectValue}");
        Debug.Log($"범위: {range}");
        Debug.Log($"쿨다운: {cooldown}");
        
        if (spellCard.CanUse())
        {
            spellCard.Use();
            Debug.Log($"✓ {spellType} 스펠 사용 성공");
        }
        else
        {
            Debug.LogError($"✗ {spellType} 스펠 사용 실패");
        }
        
        Destroy(cardObject);
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(220, 10, 200, 200));
        GUILayout.Label("=== Card Spell Test ===");
        
        if (GUILayout.Button("Run Spell Tests"))
        {
            RunAllTests();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Individual Tests:");
        
        if (GUILayout.Button("Test Damage Spell"))
        {
            TestSpecificSpell(SpellType.Damage, 50, 3f, 2f);
        }
        
        if (GUILayout.Button("Test Heal Spell"))
        {
            TestSpecificSpell(SpellType.Heal, 30, 2f, 1f);
        }
        
        if (GUILayout.Button("Test Buff Spell"))
        {
            TestSpecificSpell(SpellType.Buff, 20, 4f, 3f);
        }
        
        GUILayout.EndArea();
    }
}