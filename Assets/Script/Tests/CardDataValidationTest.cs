using UnityEngine;
using Game.Data;
using Game.Card.Effects;
using System.Collections.Generic;

namespace Game.Tests
{
    /// <summary>
    /// Phase 2.11: CardData IsValidTarget() 업데이트 테스트
    /// 새로운 TargetType과 TargetRange 시스템 검증을 위한 테스트 스크립트
    /// </summary>
    public class CardDataValidationTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;

        [Header("테스트 카드 데이터")]
        [SerializeField] private CardData testFireballCard;  // 원거리 공격 주문
        [SerializeField] private CardData testHealCard;      // 근거리 회복 주문
        [SerializeField] private CardData testSummonCard;    // 유닛 소환 카드

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        /// <summary>
        /// 모든 테스트 실행
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("🧪 === CardData Phase 2.11 Validation Tests Starting ===");

            TestBasicDistanceCalculation();
            TestTargetRangeValidation();
            TestTargetTypeValidation();
            TestLegacyCompatibility();

            Debug.Log("✅ === All CardData Validation Tests Completed ===");
        }

        /// <summary>
        /// 기본 거리 계산 테스트
        /// </summary>
        private void TestBasicDistanceCalculation()
        {
            Debug.Log("📏 Testing basic distance calculation...");

            // 맨하탄 거리 계산 테스트
            var testCases = new Dictionary<(Vector2Int from, Vector2Int to), int>
            {
                { (Vector2Int.zero, new Vector2Int(1, 1)), 2 },
                { (Vector2Int.zero, new Vector2Int(3, 0)), 3 },
                { (new Vector2Int(2, 2), new Vector2Int(5, 7)), 8 },
                { (Vector2Int.zero, Vector2Int.zero), 0 }
            };

            foreach (var testCase in testCases)
            {
                int calculated = CardData.CalculateManhattanDistance(testCase.Key.from, testCase.Key.to);
                bool passed = calculated == testCase.Value;

                Log($"{(passed ? "✅" : "❌")} Distance {testCase.Key.from} → {testCase.Key.to}: " +
                    $"Expected {testCase.Value}, Got {calculated}");
            }
        }

        /// <summary>
        /// TargetRange 검증 테스트
        /// </summary>
        private void TestTargetRangeValidation()
        {
            Debug.Log("🎯 Testing TargetRange validation...");

            // 테스트용 카드 데이터 생성
            var shortRangeCard = CreateTestCard("Short Range", targetRange: 2);
            var longRangeCard = CreateTestCard("Long Range", targetRange: 5);
            var unlimitedRangeCard = CreateTestCard("Unlimited Range", targetRange: -1);

            Vector2Int casterPos = Vector2Int.zero;

            // 거리 2 제한 카드 테스트
            TestCardAtPositions(shortRangeCard, casterPos, new[]
            {
                (new Vector2Int(1, 1), true),   // 거리 2, 유효
                (new Vector2Int(2, 0), true),   // 거리 2, 유효
                (new Vector2Int(2, 1), false),  // 거리 3, 무효
                (new Vector2Int(3, 0), false)   // 거리 3, 무효
            });

            // 거리 5 제한 카드 테스트
            TestCardAtPositions(longRangeCard, casterPos, new[]
            {
                (new Vector2Int(3, 2), true),   // 거리 5, 유효
                (new Vector2Int(5, 0), true),   // 거리 5, 유효
                (new Vector2Int(3, 3), false),  // 거리 6, 무효
                (new Vector2Int(4, 2), false)   // 거리 6, 무효
            });

            // 무제한 거리 카드 테스트
            TestCardAtPositions(unlimitedRangeCard, casterPos, new[]
            {
                (new Vector2Int(10, 10), true), // 거리 20, 유효 (무제한)
                (new Vector2Int(100, 0), true)  // 거리 100, 유효 (무제한)
            });
        }

        /// <summary>
        /// TargetType 검증 테스트
        /// </summary>
        private void TestTargetTypeValidation()
        {
            Debug.Log("🏷️ Testing TargetType validation...");

            var targetTypes = new[]
            {
                CardData.TargetType.None,
                CardData.TargetType.Ground,
                CardData.TargetType.Ally,
                CardData.TargetType.Enemy,
                CardData.TargetType.Any
            };

            foreach (var targetType in targetTypes)
            {
                var card = CreateTestCard($"Test {targetType}", targetType: targetType);
                bool result = card.IsValidTarget(Vector2Int.zero, new Vector2Int(1, 1));

                // TargetType.None은 항상 true여야 함
                if (targetType == CardData.TargetType.None)
                {
                    Log($"{(result ? "✅" : "❌")} TargetType.None should always be valid");
                }
                else
                {
                    Log($"ℹ️ TargetType.{targetType}: {result} (complex validation handled by SpawnValidator)");
                }
            }
        }

        /// <summary>
        /// 레거시 호환성 테스트
        /// </summary>
        private void TestLegacyCompatibility()
        {
            Debug.Log("🔄 Testing legacy compatibility...");

            // range 필드와 targetRange 필드가 모두 설정된 경우 테스트
            var legacyCard = CreateTestCard("Legacy Card", range: 3, targetRange: 2);

            Vector2Int casterPos = Vector2Int.zero;
            Vector2Int testPos = new Vector2Int(2, 1); // 맨하탄 거리 3, 유클리드 거리 ~2.24

            bool result = legacyCard.IsValidTarget(casterPos, testPos);

            // targetRange(2)가 더 제한적이므로 false여야 함
            Log($"{(!result ? "✅" : "❌")} Legacy compatibility: targetRange should take precedence when more restrictive");
        }

        /// <summary>
        /// 카드를 여러 위치에서 테스트
        /// </summary>
        private void TestCardAtPositions(CardData card, Vector2Int casterPos, (Vector2Int pos, bool expected)[] tests)
        {
            foreach (var (pos, expected) in tests)
            {
                bool result = card.IsValidTarget(casterPos, pos);
                bool passed = result == expected;

                int distance = CardData.CalculateManhattanDistance(casterPos, pos);
                Log($"{(passed ? "✅" : "❌")} {card.CardName} at {pos} (distance {distance}): " +
                    $"Expected {expected}, Got {result}");
            }
        }

        /// <summary>
        /// 테스트용 카드 데이터 생성
        /// </summary>
        private CardData CreateTestCard(string name, int range = 0, int targetRange = -1,
            CardData.TargetType targetType = CardData.TargetType.None)
        {
            var card = ScriptableObject.CreateInstance<CardData>();

            // Reflection을 사용하여 private 필드 설정
            var cardTypeField = typeof(CardData).GetField("cardName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cardTypeField?.SetValue(card, name);

            var rangeField = typeof(CardData).GetField("range",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rangeField?.SetValue(card, range);

            var targetRangeField = typeof(CardData).GetField("targetRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetRangeField?.SetValue(card, targetRange);

            var targetTypeField = typeof(CardData).GetField("targetType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetTypeField?.SetValue(card, targetType);

            return card;
        }

        /// <summary>
        /// 조건부 로깅
        /// </summary>
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[CardDataValidationTest] {message}");
            }
        }
    }
}