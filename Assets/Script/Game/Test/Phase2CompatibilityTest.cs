using UnityEngine;
using Game.Data;
using Game.Card.Core;

namespace Game.Test
{
    /// <summary>
    /// Phase 2 CardData-CardSpawnService 호환성 검증 스크립트
    /// CardData의 주문 속성들이 CardSpawnService와 완전히 호환되는지 테스트
    /// </summary>
    public class Phase2CompatibilityTest : MonoBehaviour
    {
        [Header("Phase 2 호환성 테스트")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("테스트용 CardData들")]
        [SerializeField] private CardData[] testSpellCards;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunPhase2ValidationTests();
            }
        }

        [ContextMenu("Run Phase 2 Validation Tests")]
        public void RunPhase2ValidationTests()
        {
            Log("🧪 === Phase 2 CardData-CardSpawnService 호환성 테스트 시작 ===");
            
            bool allTestsPassed = true;
            
            allTestsPassed &= Test_CardData_SpellPropertiesExist();
            allTestsPassed &= Test_CardData_SpellPropertiesAccessible();
            allTestsPassed &= Test_SpellType_EnumCompatibility();
            allTestsPassed &= Test_CardSpawnService_PropertyAccess();
            allTestsPassed &= Test_ValidationMethods();
            allTestsPassed &= Test_FactoryMethods();
            
            if (testSpellCards != null && testSpellCards.Length > 0)
            {
                allTestsPassed &= Test_RealSpellCards();
            }
            
            Log($"🏁 === Phase 2 테스트 완료: {(allTestsPassed ? "✅ 모든 테스트 통과" : "❌ 일부 테스트 실패")} ===");
        }
        
        /// <summary>
        /// CardData에 주문 관련 속성들이 모두 존재하는지 확인
        /// </summary>
        private bool Test_CardData_SpellPropertiesExist()
        {
            Log("📝 Test 1: CardData 주문 속성 존재 확인");
            
            try
            {
                // 런타임에서 테스트용 주문 카드 생성
                var testCard = CardData.CreateSpellCard(
                    "Test Fire Bolt",
                    "테스트용 화염구",
                    2, 1,
                    SpellType.Damage,
                    50,
                    3f,
                    1f
                );
                
                // 모든 주문 속성이 정의되어 있는지 확인
                bool hasSpellType = testCard.SpellType != default;
                bool hasSpellEffectValue = testCard.SpellEffectValue >= 0;
                bool hasSpellRange = testCard.SpellRange >= 0;
                bool hasSpellCooldown = testCard.SpellCooldown >= 0;
                bool hasSpellEffectPrefab = true; // nullable이므로 항상 true
                
                bool testResult = hasSpellType && hasSpellEffectValue && hasSpellRange && hasSpellCooldown && hasSpellEffectPrefab;
                
                if (enableDetailedLogging)
                {
                    Log($"  - SpellType: {testCard.SpellType} ({hasSpellType})");
                    Log($"  - SpellEffectValue: {testCard.SpellEffectValue} ({hasSpellEffectValue})");
                    Log($"  - SpellRange: {testCard.SpellRange} ({hasSpellRange})");
                    Log($"  - SpellCooldown: {testCard.SpellCooldown} ({hasSpellCooldown})");
                    Log($"  - SpellEffectPrefab: {testCard.SpellEffectPrefab} ({hasSpellEffectPrefab})");
                }
                
                Log($"  결과: {(testResult ? "✅ 통과" : "❌ 실패")}");
                return testResult;
            }
            catch (System.Exception ex)
            {
                LogError($"  예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// CardSpawnService가 요구하는 방식으로 속성에 접근 가능한지 확인
        /// </summary>
        private bool Test_CardData_SpellPropertiesAccessible()
        {
            Log("📝 Test 2: CardSpawnService 호환 속성 접근");
            
            try
            {
                var testCard = CardData.CreateSpellCard(
                    "Test Ice Shard",
                    "테스트용 얼음 조각",
                    1, 1,
                    SpellType.Damage,
                    30,
                    2.5f,
                    0.5f
                );
                
                // CardSpawnService.ExecuteSpellEffect()에서 사용하는 방식 시뮬레이션
                SpellType spellType = testCard.SpellType;     // line 332 시뮬레이션
                int effectValue = testCard.SpellEffectValue;  // line 333 시뮬레이션
                float effectRange = testCard.SpellRange;      // line 334 시뮬레이션
                
                bool accessTest = spellType == SpellType.Damage &&
                                 effectValue == 30 &&
                                 effectRange == 2.5f;
                
                if (enableDetailedLogging)
                {
                    Log($"  - SpellType 접근: {spellType}");
                    Log($"  - SpellEffectValue 접근: {effectValue}");
                    Log($"  - SpellRange 접근: {effectRange}");
                }
                
                Log($"  결과: {(accessTest ? "✅ 통과" : "❌ 실패")}");
                return accessTest;
            }
            catch (System.Exception ex)
            {
                LogError($"  예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// SpellType 열거형 호환성 확인
        /// </summary>
        private bool Test_SpellType_EnumCompatibility()
        {
            Log("📝 Test 3: SpellType 열거형 호환성");
            
            try
            {
                // SpellType의 모든 값들이 정의되어 있는지 확인
                var allSpellTypes = System.Enum.GetValues(typeof(SpellType));
                bool enumTest = allSpellTypes.Length >= 7; // Damage, Heal, Buff, Debuff, Shield, Teleport, Summon
                
                if (enableDetailedLogging)
                {
                    Log($"  - SpellType 개수: {allSpellTypes.Length}");
                    foreach (SpellType spellType in allSpellTypes)
                    {
                        Log($"    • {spellType}");
                    }
                }
                
                Log($"  결과: {(enumTest ? "✅ 통과" : "❌ 실패")}");
                return enumTest;
            }
            catch (System.Exception ex)
            {
                LogError($"  예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// CardSpawnService에서의 속성 접근 패턴 시뮬레이션
        /// </summary>
        private bool Test_CardSpawnService_PropertyAccess()
        {
            Log("📝 Test 4: CardSpawnService 접근 패턴 시뮬레이션");
            
            try
            {
                var testCard = CardData.CreateSpellCard(
                    "Test Lightning Bolt",
                    "테스트용 번개",
                    3, 1,
                    SpellType.Damage,
                    80,
                    4f,
                    2f
                );
                
                // ExecuteSpellEffect 메서드의 line 332-334 시뮬레이션
                bool simulationResult = SimulateExecuteSpellEffect(testCard);
                
                Log($"  결과: {(simulationResult ? "✅ 통과" : "❌ 실패")}");
                return simulationResult;
            }
            catch (System.Exception ex)
            {
                LogError($"  예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// ExecuteSpellEffect 메서드의 속성 접근 부분을 시뮬레이션
        /// </summary>
        private bool SimulateExecuteSpellEffect(CardData cardData)
        {
            // CardSpawnService.ExecuteSpellEffect() line 332-334 시뮬레이션
            SpellType spellType = cardData.SpellType;
            int effectValue = cardData.SpellEffectValue;
            float effectRange = cardData.SpellRange;
            
            if (enableDetailedLogging)
            {
                Log($"    시뮬레이션 - Executing {spellType} spell with value {effectValue} and range {effectRange}");
            }
            
            // 값들이 예상대로 접근되었는지 확인
            return spellType != default &&
                   effectValue > 0 &&
                   effectRange >= 0;
        }
        
        /// <summary>
        /// 주문 관련 검증 메서드들 테스트
        /// </summary>
        private bool Test_ValidationMethods()
        {
            Log("📝 Test 5: 주문 검증 메서드 테스트");
            
            try
            {
                var spellCard = CardData.CreateSpellCard(
                    "Test Heal",
                    "테스트용 힐",
                    2, 1,
                    SpellType.Heal,
                    40,
                    3f,
                    1f
                );
                
                var nonSpellCard = CardData.CreateUnitCard("Test Unit", null, 2, 1);
                
                // 검증 메서드들 테스트
                bool hasValidSpellData = spellCard.HasValidSpellData;
                bool isSpellCard = spellCard.IsSpellCard;
                bool nonSpellIsValid = !nonSpellCard.HasValidSpellData;
                
                string spellDescription = spellCard.GetSpellDescription();
                bool hasDescription = !string.IsNullOrEmpty(spellDescription);
                
                // 범위 검사 테스트
                Vector2Int pos1 = new Vector2Int(0, 0);
                Vector2Int pos2 = new Vector2Int(2, 2); // 거리: ~2.83
                bool inRange = spellCard.IsSpellInRange(pos1, pos2); // 3f 범위 내
                
                bool validationTest = hasValidSpellData && isSpellCard && nonSpellIsValid && hasDescription && inRange;
                
                if (enableDetailedLogging)
                {
                    Log($"  - HasValidSpellData: {hasValidSpellData}");
                    Log($"  - IsSpellCard: {isSpellCard}");
                    Log($"  - 비주문카드 검증: {nonSpellIsValid}");
                    Log($"  - GetSpellDescription: '{spellDescription}'");
                    Log($"  - IsSpellInRange: {inRange}");
                }
                
                Log($"  결과: {(validationTest ? "✅ 통과" : "❌ 실패")}");
                return validationTest;
            }
            catch (System.Exception ex)
            {
                LogError($"  예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 팩토리 메서드 테스트
        /// </summary>
        private bool Test_FactoryMethods()
        {
            Log("📝 Test 6: 팩토리 메서드 테스트");
            
            try
            {
                // CreateSpellCard 오버로드 1
                var spellCard1 = CardData.CreateSpellCard(
                    "Factory Test 1",
                    "팩토리 테스트 1",
                    1, 1,
                    SpellType.Shield,
                    25,
                    2f,
                    0.8f
                );
                
                bool factory1Test = spellCard1.IsSpellCard &&
                                   spellCard1.SpellType == SpellType.Shield &&
                                   spellCard1.SpellEffectValue == 25 &&
                                   spellCard1.SpellRange == 2f &&
                                   spellCard1.SpellCooldown == 0.8f;
                
                if (enableDetailedLogging)
                {
                    Log($"  - Factory Method 1: {(factory1Test ? "✅" : "❌")}");
                    Log($"    • Name: {spellCard1.CardName}");
                    Log($"    • Type: {spellCard1.SpellType}");
                    Log($"    • Value: {spellCard1.SpellEffectValue}");
                }
                
                Log($"  결과: {(factory1Test ? "✅ 통과" : "❌ 실패")}");
                return factory1Test;
            }
            catch (System.Exception ex)
            {
                LogError($"  예외 발생: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 실제 주문 카드들로 테스트 (Inspector에서 할당된 경우)
        /// </summary>
        private bool Test_RealSpellCards()
        {
            Log($"📝 Test 7: 실제 주문 카드 테스트 ({testSpellCards.Length}개)");
            
            bool allCardsValid = true;
            int validCardCount = 0;
            
            foreach (var card in testSpellCards)
            {
                if (card == null) continue;
                
                try
                {
                    bool cardTest = card.IsSpellCard && card.HasValidSpellData;
                    
                    if (cardTest)
                    {
                        validCardCount++;
                        
                        // CardSpawnService 접근 패턴 시뮬레이션
                        bool accessTest = SimulateExecuteSpellEffect(card);
                        if (!accessTest)
                        {
                            allCardsValid = false;
                        }
                        
                        if (enableDetailedLogging)
                        {
                            Log($"  - {card.CardName}: ✅ ({card.SpellType}, {card.SpellEffectValue}, {card.SpellRange})");
                        }
                    }
                    else
                    {
                        allCardsValid = false;
                        if (enableDetailedLogging)
                        {
                            Log($"  - {card.CardName}: ❌ (주문카드 검증 실패)");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    allCardsValid = false;
                    LogError($"  - {card.CardName}: 예외 발생 - {ex.Message}");
                }
            }
            
            Log($"  유효한 주문 카드: {validCardCount}/{testSpellCards.Length}");
            Log($"  결과: {(allCardsValid ? "✅ 통과" : "❌ 실패")}");
            return allCardsValid;
        }
        
        private void Log(string message)
        {
            Debug.Log($"[Phase2Validation] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[Phase2Validation] {message}");
        }
    }
}