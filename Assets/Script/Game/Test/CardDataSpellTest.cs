using UnityEngine;
using Game.Data;
using Game.Card.Core;
using Game.Services;

namespace Game.Tests
{
    /// <summary>
    /// Phase 4: CardData 기반 주문 시스템 테스트
    /// 기존 Spell 클래스를 완전히 대체하는 CardData 중심 테스트
    /// </summary>
    public class CardDataSpellTest : MonoBehaviour
    {
        [Header("Phase 4 Test Settings")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;

        private CardData[] testSpellCards;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        public void RunAllTests()
        {
            Log("🎭 === Phase 4 CardData Spell Test 시작 ===");
            
            CreateTestSpellCards();
            TestCardDataSpellCreation();
            TestSpellEffectFactory();
            TestSpellValidation();
            TestSpellRangeAndDescription();
            TestCardSpawnServiceIntegration();
            CleanupTestCards();
            
            Log("✅ === Phase 4 CardData Spell Test 완료 ===");
        }

        private void CreateTestSpellCards()
        {
            Log("📋 Creating test spell cards...");

            testSpellCards = new CardData[]
            {
                CardData.CreateSpellCard("Test Fire Bolt", "Test damage spell", 2, 1, SpellType.Damage, 50, 3f, 0f),
                CardData.CreateSpellCard("Test Healing Light", "Test heal spell", 1, 1, SpellType.Heal, 30, 2f, 0f),
                CardData.CreateSpellCard("Test Shield", "Test shield spell", 2, 1, SpellType.Shield, 40, 0f, 0f),
                CardData.CreateSpellCard("Test Lightning Strike", "Test powerful spell", 4, 2, SpellType.Damage, 100, 5f, 1f),
                CardData.CreateSpellCard("Test Teleport", "Test teleport spell", 3, 1, SpellType.Teleport, 1, 4f, 0f),
                CardData.CreateSpellCard("Test Buff", "Test buff spell", 3, 2, SpellType.Buff, 25, 2f, 0f),
                CardData.CreateSpellCard("Test Debuff", "Test debuff spell", 2, 1, SpellType.Debuff, 20, 3f, 0f),
                CardData.CreateSpellCard("Test Summon", "Test summon spell", 5, 3, SpellType.Summon, 2, 0f, 2f)
            };

            Log($"✅ Created {testSpellCards.Length} test spell cards");
        }

        private void TestCardDataSpellCreation()
        {
            Log("🧪 --- CardData Spell Creation Test ---");

            foreach (var card in testSpellCards)
            {
                if (card != null && card.IsSpellCard && card.HasValidSpellData)
                {
                    Log($"✅ {card.CardName}: Type={card.SpellType}, Value={card.SpellEffectValue}, Range={card.SpellRange}");
                }
                else
                {
                    LogError($"❌ Failed to create valid spell card: {card?.CardName}");
                }
            }

            Log("✅ CardData spell creation test completed");
        }

        private void TestSpellEffectFactory()
        {
            Log("🧪 --- SpellEffectFactory Integration Test ---");

            foreach (var card in testSpellCards)
            {
                // Test new CardData-based factory method
                var effect = SpellEffectFactory.CreateEffect(card);
                if (effect != null)
                {
                    Log($"✅ SpellEffectFactory created effect for {card.CardName} ({card.SpellType})");
                    
                    // Test execution
                    effect.Execute(Vector3.zero, card.SpellEffectValue, card.SpellRange);
                }
                else
                {
                    LogError($"❌ SpellEffectFactory failed to create effect for {card.CardName}");
                }

                // Test new integrated execution method
                bool executeSuccess = SpellEffectFactory.ExecuteSpellEffect(card, Vector3.zero);
                if (executeSuccess)
                {
                    Log($"✅ SpellEffectFactory.ExecuteSpellEffect succeeded for {card.CardName}");
                }
                else
                {
                    LogError($"❌ SpellEffectFactory.ExecuteSpellEffect failed for {card.CardName}");
                }
            }

            Log("✅ SpellEffectFactory integration test completed");
        }

        private void TestSpellValidation()
        {
            Log("🧪 --- Spell Validation Test ---");

            foreach (var card in testSpellCards)
            {
                // Test basic validation
                bool isValid = card.IsValid();
                Log($"CardData validation for {card.CardName}: {(isValid ? "✅ VALID" : "❌ INVALID")}");

                // Test spell-specific validation
                bool hasValidSpellData = card.HasValidSpellData;
                Log($"Spell data validation for {card.CardName}: {(hasValidSpellData ? "✅ VALID" : "❌ INVALID")}");

                // Test cost validation
                bool canAfford = card.CanAfford(10, 5); // Assume sufficient resources
                Log($"Cost validation for {card.CardName}: {(canAfford ? "✅ AFFORDABLE" : "❌ TOO EXPENSIVE")}");
            }

            // Test invalid spell card
            CardData invalidCard = ScriptableObject.CreateInstance<CardData>();
            // This card has no spell properties set, so should be invalid
            bool invalidResult = invalidCard.HasValidSpellData;
            Log($"Invalid spell card test: {(invalidResult ? "❌ SHOULD BE INVALID" : "✅ CORRECTLY INVALID")}");
            
            Destroy(invalidCard);
            Log("✅ Spell validation test completed");
        }

        private void TestSpellRangeAndDescription()
        {
            Log("🧪 --- Spell Range and Description Test ---");

            Vector2Int casterPos = new Vector2Int(0, 0);
            Vector2Int nearTarget = new Vector2Int(2, 0);
            Vector2Int farTarget = new Vector2Int(10, 0);

            foreach (var card in testSpellCards)
            {
                // Test range validation
                bool nearInRange = card.IsSpellInRange(casterPos, nearTarget);
                bool farInRange = card.IsSpellInRange(casterPos, farTarget);
                
                Log($"{card.CardName} range test: Near(2)={nearInRange}, Far(10)={farInRange}, Range={card.SpellRange}");

                // Test description generation
                string spellDesc = card.GetSpellDescription();
                string detailedDesc = card.GetDetailedDescription();
                
                Log($"{card.CardName} descriptions generated: Basic='{spellDesc}', Detailed length={detailedDesc.Length}");

                if (string.IsNullOrEmpty(spellDesc))
                {
                    LogError($"❌ Empty spell description for {card.CardName}");
                }
            }

            Log("✅ Spell range and description test completed");
        }

        private void TestCardSpawnServiceIntegration()
        {
            Log("🧪 --- CardSpawnService Integration Test ---");

            // This tests the exact integration that Phase 4 is designed to support
            foreach (var card in testSpellCards)
            {
                // Simulate the CardSpawnService property access pattern
                try
                {
                    SpellType spellType = card.SpellType;       // CardSpawnService line 332
                    int effectValue = card.SpellEffectValue;    // CardSpawnService line 333
                    float effectRange = card.SpellRange;        // CardSpawnService line 334

                    Log($"✅ CardSpawnService integration test for {card.CardName}:");
                    Log($"   Type: {spellType}, Value: {effectValue}, Range: {effectRange}");

                    // Test spell card identification
                    bool isSpellCard = card.IsSpellCard;        // CardSpawnService line 278
                    if (!isSpellCard)
                    {
                        LogError($"❌ Card {card.CardName} not identified as spell card");
                    }

                    // Test spell range validation pattern
                    Vector2Int testPos1 = Vector2Int.zero;
                    Vector2Int testPos2 = new Vector2Int(2, 0);
                    bool inRange = card.IsSpellInRange(testPos1, testPos2);
                    
                    Log($"   Range validation: {testPos1} to {testPos2} = {inRange}");
                }
                catch (System.Exception ex)
                {
                    LogError($"❌ CardSpawnService integration failed for {card.CardName}: {ex.Message}");
                }
            }

            Log("✅ CardSpawnService integration test completed");
        }

        private void CleanupTestCards()
        {
            Log("🧹 Cleaning up test cards...");

            if (testSpellCards != null)
            {
                foreach (var card in testSpellCards)
                {
                    if (card != null)
                    {
                        Destroy(card);
                    }
                }
                testSpellCards = null;
            }

            Log("✅ Test cards cleanup completed");
        }

        #region Individual Test Methods for Unity Inspector

        [ContextMenu("Test Single Damage Spell")]
        public void TestDamageSpell()
        {
            Log("⚔️ Testing single damage spell...");
            
            var damageCard = CardData.CreateSpellCard("Test Damage", "Single test", 2, 1, SpellType.Damage, 75, 4f, 0f);
            
            Log($"Created: {damageCard.CardName}");
            Log($"Properties: {damageCard.SpellType}, {damageCard.SpellEffectValue}, {damageCard.SpellRange}");
            Log($"Description: {damageCard.GetSpellDescription()}");
            
            var effect = SpellEffectFactory.CreateEffect(damageCard);
            if (effect != null)
            {
                effect.Execute(Vector3.zero, damageCard.SpellEffectValue, damageCard.SpellRange);
                Log("✅ Effect executed successfully");
            }
            
            Destroy(damageCard);
        }

        [ContextMenu("Test Single Heal Spell")]
        public void TestHealSpell()
        {
            Log("💚 Testing single heal spell...");
            
            var healCard = CardData.CreateSpellCard("Test Heal", "Single test", 1, 1, SpellType.Heal, 45, 3f, 0f);
            
            Log($"Created: {healCard.CardName}");
            Log($"Properties: {healCard.SpellType}, {healCard.SpellEffectValue}, {healCard.SpellRange}");
            Log($"Description: {healCard.GetSpellDescription()}");
            
            bool executeSuccess = SpellEffectFactory.ExecuteSpellEffect(healCard, Vector3.zero);
            Log($"Execution result: {(executeSuccess ? "✅ SUCCESS" : "❌ FAILED")}");
            
            Destroy(healCard);
        }

        [ContextMenu("Test All Spell Types")]
        public void TestAllSpellTypes()
        {
            Log("🎭 Testing all spell types...");

            SpellType[] allTypes = (SpellType[])System.Enum.GetValues(typeof(SpellType));
            
            foreach (SpellType spellType in allTypes)
            {
                var testCard = CardData.CreateSpellCard($"Test {spellType}", $"Testing {spellType}", 2, 1, spellType, 50, 3f, 1f);
                
                Log($"Testing {spellType}: Valid={testCard.HasValidSpellData}, Desc='{testCard.GetSpellDescription()}'");
                
                bool factorySuccess = SpellEffectFactory.ExecuteSpellEffect(testCard, Vector3.zero);
                Log($"{spellType} factory execution: {(factorySuccess ? "✅" : "❌")}");
                
                Destroy(testCard);
            }
        }

        #endregion

        #region Utility Methods

        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[CardDataSpellTest] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CardDataSpellTest] {message}");
        }

        #endregion

        #region Unity GUI

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(430, 10, 250, 300));
            GUILayout.Label("=== Phase 4 CardData Spell Test ===");
            
            if (GUILayout.Button("Run All Tests"))
            {
                RunAllTests();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Individual Tests:");
            
            if (GUILayout.Button("Test Damage Spell"))
            {
                TestDamageSpell();
            }
            
            if (GUILayout.Button("Test Heal Spell"))
            {
                TestHealSpell();
            }
            
            if (GUILayout.Button("Test All Spell Types"))
            {
                TestAllSpellTypes();
            }
            
            GUILayout.Space(10);
            
            enableDetailedLogging = GUILayout.Toggle(enableDetailedLogging, "Detailed Logging");
            runTestsOnStart = GUILayout.Toggle(runTestsOnStart, "Run on Start");
            
            GUILayout.EndArea();
        }

        #endregion
    }
}