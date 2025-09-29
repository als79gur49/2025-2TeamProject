using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Card.Effects;

namespace Game.Tests
{
    /// <summary>
    /// CardData 리팩토링 Phase 4.17 단위 테스트
    /// 새로운 효과 기반 아키텍처 시스템 전체에 대한 포괄적인 테스트
    /// </summary>
    [TestFixture]
    public class CardDataRefactoringPhase417Tests
    {
        private CardData testCard;
        private GameContext mockContext;

        [SetUp]
        public void SetUp()
        {
            // 각 테스트 전 초기화
            testCard = null;
            mockContext = CreateMockGameContext();
        }

        [TearDown]
        public void TearDown()
        {
            // 각 테스트 후 정리
            if (testCard != null)
            {
                Object.DestroyImmediate(testCard);
            }
        }

        #region EffectType 시스템 테스트

        [Test]
        public void EffectType_ShouldHaveThreeValidValues()
        {
            // Arrange & Act
            var effectTypes = System.Enum.GetValues(typeof(EffectType));

            // Assert
            Assert.AreEqual(3, effectTypes.Length, "EffectType should have exactly 3 values");
            Assert.IsTrue(System.Enum.IsDefined(typeof(EffectType), EffectType.Damage), "Damage should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(EffectType), EffectType.Heal), "Heal should be defined");
            Assert.IsTrue(System.Enum.IsDefined(typeof(EffectType), EffectType.Summon), "Summon should be defined");
        }

        [Test]
        public void EffectData_Creation_ShouldSetPropertiesCorrectly()
        {
            // Arrange & Act
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 2);

            // Assert
            Assert.AreEqual(EffectType.Damage, effectData.Type);
            Assert.AreEqual(5, effectData.Value);
            Assert.AreEqual(AffectedType.Enemy, effectData.AffectedType);
            Assert.AreEqual(2, effectData.AffectedRange);
        }

        [Test]
        public void EffectData_IsValid_ShouldReturnTrueForValidData()
        {
            // Arrange
            var damageEffect = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 1);
            var healEffect = new EffectData(EffectType.Heal, 2, AffectedType.Ally, 0);

            // Act & Assert
            Assert.IsTrue(damageEffect.IsValid(), "Valid damage effect should return true");
            Assert.IsTrue(healEffect.IsValid(), "Valid heal effect should return true");
        }

        [Test]
        public void EffectData_IsValid_ShouldReturnFalseForInvalidData()
        {
            // Arrange
            var invalidEffect1 = new EffectData(EffectType.Damage, 0, AffectedType.Enemy, 0); // 0 value
            var invalidEffect2 = new EffectData(EffectType.Damage, 1, AffectedType.Enemy, -1); // negative range

            // Act & Assert
            Assert.IsFalse(invalidEffect1.IsValid(), "Effect with 0 value should be invalid");
            Assert.IsFalse(invalidEffect2.IsValid(), "Effect with negative range should be invalid");
        }

        [Test]
        public void EffectData_GetDescription_ShouldFormatCorrectly()
        {
            // Arrange
            var damageEffect = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 2);
            var healEffect = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0);

            // Act
            var damageDesc = damageEffect.GetDescription();
            var healDesc = healEffect.GetDescription();

            // Assert
            Assert.That(damageDesc, Does.Contain("5 피해"), "Damage description should contain damage value");
            Assert.That(damageDesc, Does.Contain("범위: 2"), "Damage description should contain range info");
            Assert.That(healDesc, Does.Contain("3 회복"), "Heal description should contain heal value");
            Assert.That(healDesc, Does.Not.Contain("범위"), "Single target heal should not show range");
        }

        #endregion

        #region AffectedType 시스템 테스트

        [Test]
        public void AffectedType_ShouldHaveFourValidValues()
        {
            // Arrange & Act
            var affectedTypes = System.Enum.GetValues(typeof(AffectedType));

            // Assert
            Assert.AreEqual(4, affectedTypes.Length, "AffectedType should have exactly 4 values");
            Assert.IsTrue(System.Enum.IsDefined(typeof(AffectedType), AffectedType.None));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AffectedType), AffectedType.Ally));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AffectedType), AffectedType.Enemy));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AffectedType), AffectedType.Any));
        }

        [Test]
        public void CardData_GetAffectedPositions_ShouldReturnCorrectPositionsForSingleTarget()
        {
            // Arrange
            testCard = CreateTestCard();
            var effectData = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0);
            var targetPos = new Vector2Int(5, 5);

            // Act
            var positions = testCard.GetAffectedPositions(targetPos, effectData);

            // Assert
            Assert.AreEqual(1, positions.Count, "Single target should return 1 position");
            Assert.AreEqual(targetPos, positions[0], "Should return target position");
        }

        [Test]
        public void CardData_GetAffectedPositions_ShouldReturnCorrectPositionsForAreaEffect()
        {
            // Arrange
            testCard = CreateTestCard();
            var effectData = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 1); // Range 1
            var targetPos = new Vector2Int(5, 5);

            // Act
            var positions = testCard.GetAffectedPositions(targetPos, effectData);

            // Assert
            Assert.AreEqual(9, positions.Count, "Range 1 should return 3x3 = 9 positions");
            Assert.IsTrue(positions.Contains(targetPos), "Should contain target position");
            Assert.IsTrue(positions.Contains(new Vector2Int(4, 4)), "Should contain adjacent positions");
            Assert.IsTrue(positions.Contains(new Vector2Int(6, 6)), "Should contain diagonal positions");
        }

        [Test]
        public void CardData_IsPositionInAffectedRange_ShouldValidateCorrectly()
        {
            // Arrange
            testCard = CreateTestCard();
            var effectData = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 2);
            var targetPos = new Vector2Int(5, 5);

            // Act & Assert
            Assert.IsTrue(testCard.IsPositionInAffectedRange(targetPos, targetPos, effectData), "Target position should be in range");
            Assert.IsTrue(testCard.IsPositionInAffectedRange(targetPos, new Vector2Int(6, 6), effectData), "Adjacent position should be in range");
            Assert.IsTrue(testCard.IsPositionInAffectedRange(targetPos, new Vector2Int(7, 5), effectData), "Distance 2 position should be in range");
            Assert.IsFalse(testCard.IsPositionInAffectedRange(targetPos, new Vector2Int(8, 5), effectData), "Distance 3 position should be out of range");
        }

        #endregion

        #region TargetType 및 TargetRange 검증 테스트

        [Test]
        public void CardData_IsValidTarget_ShouldValidateTargetTypeNone()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.None);

            // Act & Assert
            Assert.IsTrue(testCard.IsValidTarget(Vector2Int.zero, new Vector2Int(10, 10)),
                "TargetType.None should allow any position");
        }

        [Test]
        public void CardData_IsValidTarget_ShouldValidateTargetRange()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: 3);
            var casterPos = Vector2Int.zero;

            // Act & Assert
            Assert.IsTrue(testCard.IsValidTarget(casterPos, new Vector2Int(2, 1)), // Distance 3
                "Position within range should be valid");
            Assert.IsTrue(testCard.IsValidTarget(casterPos, new Vector2Int(0, 3)), // Distance 3
                "Position at exact range should be valid");
            Assert.IsFalse(testCard.IsValidTarget(casterPos, new Vector2Int(2, 2)), // Distance 4
                "Position outside range should be invalid");
        }

        [Test]
        public void CardData_IsValidTarget_ShouldValidateUnlimitedRange()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: -1); // Unlimited range
            var casterPos = Vector2Int.zero;

            // Act & Assert
            Assert.IsTrue(testCard.IsValidTarget(casterPos, new Vector2Int(100, 100)),
                "Unlimited range should allow distant positions");
        }

        [Test]
        public void CardData_CalculateManhattanDistance_ShouldCalculateCorrectly()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0, CardData.CalculateManhattanDistance(Vector2Int.zero, Vector2Int.zero));
            Assert.AreEqual(2, CardData.CalculateManhattanDistance(Vector2Int.zero, new Vector2Int(1, 1)));
            Assert.AreEqual(5, CardData.CalculateManhattanDistance(Vector2Int.zero, new Vector2Int(2, 3)));
            Assert.AreEqual(8, CardData.CalculateManhattanDistance(new Vector2Int(1, 1), new Vector2Int(5, 5)));
        }

        #endregion

        #region 팩토리 패턴 테스트

        [Test]
        public void CardData_CreateEffectInstances_ShouldCreateCorrectEffects()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1),
                new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0)
            );

            // Act
            var effects = testCard.CreateEffectInstances();

            // Assert
            Assert.AreEqual(2, effects.Count, "Should create 2 effect instances");
            Assert.IsTrue(effects.Any(e => e.EffectType == EffectType.Damage), "Should contain damage effect");
            Assert.IsTrue(effects.Any(e => e.EffectType == EffectType.Heal), "Should contain heal effect");
        }

        [Test]
        public void CardData_CreateEffectInstances_ShouldSortByPriority()
        {
            // Arrange
            var effect1 = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 0);
            var effect2 = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0);

            // Set different priorities using reflection (since Priority is read-only)
            SetEffectPriority(effect1, 10);
            SetEffectPriority(effect2, 5);

            testCard = CreateTestCardWithEffects(effect1, effect2);

            // Act
            var effects = testCard.CreateEffectInstances();

            // Assert
            Assert.AreEqual(2, effects.Count);
            Assert.IsTrue(effects[0].Priority <= effects[1].Priority, "Effects should be sorted by priority");
        }

        [Test]
        public void CardData_HasEffectType_ShouldReturnCorrectValues()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0)
            );

            // Act & Assert
            Assert.IsTrue(testCard.HasEffectType(EffectType.Damage), "Should have damage effect");
            Assert.IsTrue(testCard.HasEffectType(EffectType.Heal), "Should have heal effect");
            Assert.IsFalse(testCard.HasEffectType(EffectType.Summon), "Should not have summon effect");
        }

        [Test]
        public void CardData_GetTotalEffectValue_ShouldSumValuesCorrectly()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0),
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 1),
                new EffectData(EffectType.Heal, 5, AffectedType.Ally, 0)
            );

            // Act & Assert
            Assert.AreEqual(5, testCard.GetTotalEffectValue(EffectType.Damage), "Should sum damage values");
            Assert.AreEqual(5, testCard.GetTotalEffectValue(EffectType.Heal), "Should return heal value");
            Assert.AreEqual(0, testCard.GetTotalEffectValue(EffectType.Summon), "Should return 0 for missing type");
        }

        #endregion

        #region 카드 실행 테스트

        [Test]
        public void CardData_ExecuteEffects_ShouldReturnCorrectCount()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 2, AffectedType.Ally, 0)
            );
            var targetPos = new Vector2Int(1, 1);

            // Act
            int executedCount = testCard.ExecuteEffects(targetPos, mockContext);

            // Assert
            Assert.AreEqual(2, executedCount, "Should execute all valid effects");
        }

        [Test]
        public void CardData_CanExecuteAllEffects_ShouldValidateCorrectly()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            );
            var targetPos = new Vector2Int(1, 1);

            // Act & Assert
            Assert.IsTrue(testCard.CanExecuteAllEffects(targetPos, mockContext),
                "Should return true for valid execution context");
            Assert.IsFalse(testCard.CanExecuteAllEffects(targetPos, null),
                "Should return false for null context");
        }

        [Test]
        public void CardData_GetExecutableEffects_ShouldFilterCorrectly()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 2, AffectedType.Ally, 0)
            );
            var targetPos = new Vector2Int(1, 1);

            // Act
            var executableEffects = testCard.GetExecutableEffects(targetPos, mockContext);

            // Assert
            Assert.AreEqual(2, executableEffects.Count, "Should return all executable effects");
        }

        #endregion

        #region UI 시스템 테스트

        [Test]
        public void CardData_GetDetailedDescription_ShouldIncludeEffectData()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1)
            );

            // Act
            var description = testCard.GetDetailedDescription();

            // Assert
            Assert.That(description, Does.Contain("효과:"), "Should contain effect section");
            Assert.That(description, Does.Contain("피해"), "Should contain damage info");
            Assert.That(description, Does.Contain("5"), "Should contain effect value");
        }

        [Test]
        public void CardData_GetMaxAffectedRange_ShouldReturnMaximumRange()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 1),
                new EffectData(EffectType.Heal, 2, AffectedType.Ally, 3)
            );

            // Act
            int maxRange = testCard.GetMaxAffectedRange();

            // Assert
            Assert.AreEqual(3, maxRange, "Should return maximum affected range");
        }

        [Test]
        public void CardData_GetAffectedRangeForEffectType_ShouldReturnCorrectRange()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 1),
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 2),
                new EffectData(EffectType.Heal, 2, AffectedType.Ally, 3)
            );

            // Act & Assert
            Assert.AreEqual(2, testCard.GetAffectedRangeForEffectType(EffectType.Damage),
                "Should return max range for damage effects");
            Assert.AreEqual(3, testCard.GetAffectedRangeForEffectType(EffectType.Heal),
                "Should return range for heal effect");
            Assert.AreEqual(0, testCard.GetAffectedRangeForEffectType(EffectType.Summon),
                "Should return 0 for missing effect type");
        }

        #endregion

        #region 데이터 검증 테스트

        [Test]
        public void CardData_IsValid_ShouldValidateNewSystem()
        {
            // Arrange
            var validCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            );
            var invalidCard = CreateTestCard(); // No effects

            // Act & Assert
            Assert.IsTrue(validCard.IsValid(), "Card with valid effects should be valid");
            Assert.IsFalse(invalidCard.IsValid(), "Card without effects should be invalid");
        }

        [Test]
        public void CardData_IsEffectBasedCard_ShouldReturnCorrectValue()
        {
            // Arrange
            var effectCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            );
            var emptyCard = CreateTestCard();

            // Act & Assert
            Assert.IsTrue(effectCard.IsEffectBasedCard, "Card with effects should be effect-based");
            Assert.IsFalse(emptyCard.IsEffectBasedCard, "Card without effects should not be effect-based");
        }

        [Test]
        public void CardData_GetPrimaryEffectType_ShouldReturnHighestValueEffect()
        {
            // Arrange
            testCard = CreateTestCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 5, AffectedType.Ally, 0), // Highest value
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 1)
            );

            // Act
            var primaryType = testCard.GetPrimaryEffectType();

            // Assert
            Assert.AreEqual(EffectType.Heal, primaryType, "Should return effect type with highest value");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 테스트용 기본 카드 생성
        /// </summary>
        private CardData CreateTestCard(CardData.TargetType targetType = CardData.TargetType.None, int targetRange = -1)
        {
            var card = ScriptableObject.CreateInstance<CardData>();

            // Use reflection to set private fields
            SetPrivateField(card, "cardName", "Test Card");
            SetPrivateField(card, "cardType", CardData.CardType.Spell);
            SetPrivateField(card, "manaCost", 3);
            SetPrivateField(card, "targetType", targetType);
            SetPrivateField(card, "targetRange", targetRange);

            return card;
        }

        /// <summary>
        /// 효과가 있는 테스트 카드 생성
        /// </summary>
        private CardData CreateTestCardWithEffects(params EffectData[] effects)
        {
            var card = CreateTestCard();
            var effectList = new List<EffectData>(effects);

            SetPrivateField(card, "effectDataList", effectList);

            return card;
        }

        /// <summary>
        /// Mock GameContext 생성
        /// </summary>
        private GameContext CreateMockGameContext()
        {
            // Note: This would need to be implemented based on the actual GameContext structure
            // For now, return a basic mock that allows testing
            return new GameContext(
                null, // unitService
                null, // gridController
                null, // cardSpawnService
                null, // spawnValidator
                0,    // playerId
                Vector2Int.zero // originPosition
            );
        }

        /// <summary>
        /// Reflection을 사용한 private 필드 설정
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// EffectData의 Priority 설정 (테스트용)
        /// </summary>
        private void SetEffectPriority(EffectData effectData, int priority)
        {
            SetPrivateField(effectData, "priority", priority);
        }

        #endregion
    }
}