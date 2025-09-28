using UnityEngine;
using Game.Data;

/// <summary>
/// CardData Phase 1 Implementation Test
/// Verifies that CardSpawnService can now access SpellType, SpellEffectValue, and SpellRange properties
/// </summary>
public class CardDataPhase1Test : MonoBehaviour
{
    [System.Serializable]
    public class TestResult
    {
        public bool spellTypeAccessible;
        public bool spellEffectValueAccessible;
        public bool spellRangeAccessible;
        public bool factoryMethodWorks;
        public bool validationWorks;
        public string testSummary;
    }

    [Header("Test Results")]
    public TestResult testResult = new TestResult();

    void Start()
    {
        RunPhase1Tests();
    }

    void RunPhase1Tests()
    {
        Debug.Log("=== CardData Phase 1 Implementation Test ===");

        try
        {
            // Test 1: Create spell card using new factory method
            var testSpellCard = CardData.CreateSpellCard(
                "Fire Bolt", 
                "Burns target", 
                2, 1, 
                CardData.SpellType.Damage, 
                50, 
                3f, 
                2f
            );

            testResult.factoryMethodWorks = testSpellCard != null;
            Debug.Log($"✅ Factory Method Test: {(testResult.factoryMethodWorks ? "PASS" : "FAIL")}");

            if (testSpellCard != null)
            {
                // Test 2: Verify CardSpawnService compatibility - access spell properties
                CardData.SpellType spellType = testSpellCard.SpellCategory;
                int effectValue = testSpellCard.SpellEffectValue;
                float effectRange = testSpellCard.SpellRange;

                testResult.spellTypeAccessible = spellType == CardData.SpellType.Damage;
                testResult.spellEffectValueAccessible = effectValue == 50;
                testResult.spellRangeAccessible = effectRange == 3f;

                Debug.Log($"🎯 SpellType Access Test: {(testResult.spellTypeAccessible ? "PASS" : "FAIL")} (Value: {spellType})");
                Debug.Log($"💥 SpellEffectValue Access Test: {(testResult.spellEffectValueAccessible ? "PASS" : "FAIL")} (Value: {effectValue})");
                Debug.Log($"📏 SpellRange Access Test: {(testResult.spellRangeAccessible ? "PASS" : "FAIL")} (Value: {effectRange})");

                // Test 3: Verify validation works
                testResult.validationWorks = testSpellCard.IsValid() && testSpellCard.HasValidSpellData;
                Debug.Log($"✅ Validation Test: {(testResult.validationWorks ? "PASS" : "FAIL")}");

                // Test 4: Verify helper methods
                string spellDesc = testSpellCard.GetSpellDescription();
                Debug.Log($"📝 Spell Description: {spellDesc}");

                bool inRange = testSpellCard.IsSpellInRange(Vector2Int.zero, new Vector2Int(2, 2));
                Debug.Log($"📍 Range Test: {inRange} (should be true for distance 2.83 within range 3)");
            }

            // Generate test summary
            int passedTests = 0;
            int totalTests = 5;

            if (testResult.factoryMethodWorks) passedTests++;
            if (testResult.spellTypeAccessible) passedTests++;
            if (testResult.spellEffectValueAccessible) passedTests++;
            if (testResult.spellRangeAccessible) passedTests++;
            if (testResult.validationWorks) passedTests++;

            testResult.testSummary = $"Phase 1 Tests: {passedTests}/{totalTests} PASSED";
            
            Debug.Log($"🎯 {testResult.testSummary}");
            
            if (passedTests == totalTests)
            {
                Debug.Log("🎉 ALL TESTS PASSED! CardSpawnService compatibility confirmed!");
                Debug.Log("✅ CardData now properly supports spell properties:");
                Debug.Log("   • cardData.SpellCategory");
                Debug.Log("   • cardData.SpellEffectValue");
                Debug.Log("   • cardData.SpellRange");
            }
            else
            {
                Debug.LogWarning($"⚠️ {totalTests - passedTests} tests failed. Review implementation.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test failed with exception: {ex.Message}");
            testResult.testSummary = "FAILED: Exception occurred";
        }
    }
}