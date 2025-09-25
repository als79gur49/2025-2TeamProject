using UnityEngine;
using Game.Data;
using Game.Card.Core;
using Game.Tools;

namespace Game.Tests
{
    /// <summary>
    /// Phase 3 런타임 검증 스크립트
    /// 게임 실행 중에 Phase 3 마이그레이션 결과를 검증합니다.
    /// </summary>
    public class Phase3RuntimeValidator : MonoBehaviour
    {
        [Header("Runtime Validation Settings")]
        [SerializeField] private bool runValidationOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;

        [Header("Test Results")]
        [SerializeField] private bool migrationTestPassed;
        [SerializeField] private bool compatibilityTestPassed;
        [SerializeField] private bool validationTestPassed;
        [SerializeField] private int totalSpellCardsCreated;

        private void Start()
        {
            if (runValidationOnStart)
            {
                StartCoroutine(RunRuntimeValidation());
            }
        }

        private System.Collections.IEnumerator RunRuntimeValidation()
        {
            Log("🚀 Starting Phase 3 Runtime Validation...");
            yield return new WaitForSeconds(0.1f);

            // Test 1: Migration System
            migrationTestPassed = TestMigrationSystem();
            yield return new WaitForSeconds(0.1f);

            // Test 2: CardSpawnService Compatibility
            compatibilityTestPassed = TestCardSpawnServiceCompatibility();
            yield return new WaitForSeconds(0.1f);

            // Test 3: Data Validation
            validationTestPassed = TestDataValidation();
            yield return new WaitForSeconds(0.1f);

            // Final Report
            GenerateFinalReport();
        }

        private bool TestMigrationSystem()
        {
            Log("🔄 Testing Migration System...");

            try
            {
                // Execute migration to temp folder
                bool migrationResult = Phase3DataMigration.ExecutePhase3Migration("Assets/Temp/RuntimeTest");
                
                if (migrationResult)
                {
                    Log("✅ Migration system test PASSED");
                    return true;
                }
                else
                {
                    LogError("❌ Migration system test FAILED");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Migration system test FAILED with exception: {ex.Message}");
                return false;
            }
        }

        private bool TestCardSpawnServiceCompatibility()
        {
            Log("🔗 Testing CardSpawnService Compatibility...");

            try
            {
                // Create test spell cards
                var testCards = CreateTestSpellCards();
                totalSpellCardsCreated = testCards.Length;

                foreach (var card in testCards)
                {
                    // Test the exact property access pattern used in CardSpawnService
                    SpellType spellType = card.SpellType;                 // CardSpawnService line 332
                    int effectValue = card.SpellEffectValue;              // CardSpawnService line 333  
                    float effectRange = card.SpellRange;                  // CardSpawnService line 334

                    // Verify properties are accessible
                    if (spellType == default || effectValue <= 0)
                    {
                        LogError($"❌ Card {card.CardName} has invalid spell properties");
                        CleanupTestCards(testCards);
                        return false;
                    }

                    Log($"✅ Card {card.CardName}: Type={spellType}, Value={effectValue}, Range={effectRange}");
                }

                CleanupTestCards(testCards);
                Log("✅ CardSpawnService compatibility test PASSED");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"❌ CardSpawnService compatibility test FAILED: {ex.Message}");
                return false;
            }
        }

        private bool TestDataValidation()
        {
            Log("✅ Testing Data Validation...");

            try
            {
                // Test various spell types
                SpellType[] spellTypes = (SpellType[])System.Enum.GetValues(typeof(SpellType));
                int validCards = 0;

                foreach (SpellType spellType in spellTypes)
                {
                    CardData testCard = CardData.CreateSpellCard(
                        $"Validation Test {spellType}",
                        $"Testing {spellType} validation",
                        2, 1, spellType, 50, 3f, 1f
                    );

                    if (Phase3DataMigration.ValidateMigratedCardData(testCard))
                    {
                        validCards++;
                        Log($"✅ {spellType} validation passed");
                    }
                    else
                    {
                        LogError($"❌ {spellType} validation failed");
                    }

                    Destroy(testCard);
                }

                bool allValid = validCards == spellTypes.Length;
                Log(allValid ? "✅ Data validation test PASSED" : "❌ Data validation test FAILED");
                return allValid;
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Data validation test FAILED: {ex.Message}");
                return false;
            }
        }

        private CardData[] CreateTestSpellCards()
        {
            return new CardData[]
            {
                CardData.CreateSpellCard("Fire Bolt", "Damage spell", 2, 1, SpellType.Damage, 50, 3f, 0f),
                CardData.CreateSpellCard("Heal", "Healing spell", 1, 1, SpellType.Heal, 30, 2f, 0f),
                CardData.CreateSpellCard("Shield", "Protection spell", 2, 1, SpellType.Shield, 40, 0f, 0f),
                CardData.CreateSpellCard("Lightning", "High damage spell", 4, 2, SpellType.Damage, 100, 5f, 1f),
                CardData.CreateSpellCard("Teleport", "Movement spell", 3, 1, SpellType.Teleport, 1, 4f, 0f)
            };
        }

        private void CleanupTestCards(CardData[] cards)
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
        }

        private void GenerateFinalReport()
        {
            Log("📊 Phase 3 Runtime Validation Report");
            Log("=====================================");
            
            string migrationStatus = migrationTestPassed ? "✅ PASSED" : "❌ FAILED";
            string compatibilityStatus = compatibilityTestPassed ? "✅ PASSED" : "❌ FAILED";
            string validationStatus = validationTestPassed ? "✅ PASSED" : "❌ FAILED";

            Log($"🔄 Migration System: {migrationStatus}");
            Log($"🔗 CardSpawnService Compatibility: {compatibilityStatus}");
            Log($"✅ Data Validation: {validationStatus}");
            Log($"📈 Total Spell Cards Created: {totalSpellCardsCreated}");

            bool allTestsPassed = migrationTestPassed && compatibilityTestPassed && validationTestPassed;
            
            if (allTestsPassed)
            {
                Log("🎉 ALL TESTS PASSED - Phase 3 Implementation Complete!");
                Log("✅ CardData spell properties are working correctly");
                Log("✅ CardSpawnService can access all required properties");
                Log("✅ Migration and validation systems are functional");
            }
            else
            {
                LogError("⚠️  SOME TESTS FAILED - Check individual test results above");
            }

            Log("=====================================");
        }

        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[Phase3RuntimeValidator] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[Phase3RuntimeValidator] {message}");
        }

        /// <summary>
        /// Unity Inspector에서 수동으로 검증 실행
        /// </summary>
        [ContextMenu("Run Validation")]
        public void RunValidationManually()
        {
            StartCoroutine(RunRuntimeValidation());
        }

        /// <summary>
        /// 개별 마이그레이션 테스트 실행
        /// </summary>
        [ContextMenu("Test Migration Only")]
        public void TestMigrationOnly()
        {
            migrationTestPassed = TestMigrationSystem();
        }

        /// <summary>
        /// 개별 호환성 테스트 실행
        /// </summary>
        [ContextMenu("Test Compatibility Only")]
        public void TestCompatibilityOnly()
        {
            compatibilityTestPassed = TestCardSpawnServiceCompatibility();
        }
    }
}