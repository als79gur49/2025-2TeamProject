using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Game.Data;
using Game.Card.Effects;

namespace Game.Tests
{
    /// <summary>
    /// Phase 3.16: UI 시스템 업데이트 테스트
    /// GetDetailedDescription() 메서드와 새로운 카드 구조의 UI 반영 테스트
    /// </summary>
    public class Phase316UISystemTest
    {
        private CardData testCard;

        [SetUp]
        public void Setup()
        {
            // 테스트용 카드 생성
            testCard = ScriptableObject.CreateInstance<CardData>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testCard != null)
            {
                Object.DestroyImmediate(testCard);
            }
        }

        [Test]
        public void GetDetailedDescription_WithEffectData_ReturnsFormattedDescription()
        {
            // Arrange
            var testCardName = "Test Fire Spell";
            var testManaCost = 3;
            var damageValue = 5;

            // Create test card using reflection to set private fields
            SetPrivateField(testCard, "cardName", testCardName);
            SetPrivateField(testCard, "description", "A powerful fire spell");
            SetPrivateField(testCard, "cardType", CardData.CardType.Spell);
            SetPrivateField(testCard, "manaCost", testManaCost);
            SetPrivateField(testCard, "rarity", CardData.CardRarity.Rare);

            // Create EffectData
            var effectData = new EffectData(EffectType.Damage, damageValue, AffectedType.Enemy, 1);
            var effectDataList = new List<EffectData> { effectData };
            SetPrivateField(testCard, "effectDataList", effectDataList);

            // Act
            string description = testCard.GetDetailedDescription();

            // Assert
            Assert.IsNotNull(description);
            Assert.IsTrue(description.Contains(testCardName), "Description should contain card name");
            Assert.IsTrue(description.Contains(testManaCost.ToString()), "Description should contain mana cost");
            Assert.IsTrue(description.Contains("효과:"), "Description should contain effect section");
            Assert.IsTrue(description.Contains("피해"), "Description should contain damage effect");
            Assert.IsTrue(description.Contains(damageValue.ToString()), "Description should contain damage value");

            Debug.Log($"Generated Description:\n{description}");
        }

        [Test]
        public void GetDetailedDescription_WithMultipleEffects_ShowsAllEffects()
        {
            // Arrange
            SetPrivateField(testCard, "cardName", "Multi-Effect Card");
            SetPrivateField(testCard, "cardType", CardData.CardType.Spell);
            SetPrivateField(testCard, "manaCost", 4);

            var effects = new List<EffectData>
            {
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 2, AffectedType.Ally, 1)
            };
            SetPrivateField(testCard, "effectDataList", effects);

            // Act
            string description = testCard.GetDetailedDescription();

            // Assert
            Assert.IsTrue(description.Contains("피해"), "Should contain damage effect");
            Assert.IsTrue(description.Contains("회복"), "Should contain heal effect");
            Assert.IsTrue(description.Contains("3"), "Should contain damage value");
            Assert.IsTrue(description.Contains("2"), "Should contain heal value");

            Debug.Log($"Multi-Effect Description:\n{description}");
        }

        [Test]
        public void GetDetailedDescription_WithSummonEffect_ShowsUnitName()
        {
            // Arrange
            SetPrivateField(testCard, "cardName", "Summon Knight");
            SetPrivateField(testCard, "cardType", CardData.CardType.Unit);
            SetPrivateField(testCard, "manaCost", 5);

            // Create mock UnitData
            var unitData = ScriptableObject.CreateInstance<UnitData>();
            SetPrivateField(unitData, "unitName", "Knight Warrior");

            var effectData = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            SetPrivateField(effectData, "unitToSummon", unitData);

            var effectDataList = new List<EffectData> { effectData };
            SetPrivateField(testCard, "effectDataList", effectDataList);

            // Act
            string description = testCard.GetDetailedDescription();

            // Assert
            Assert.IsTrue(description.Contains("Knight Warrior"), "Should contain unit name");
            Assert.IsTrue(description.Contains("소환"), "Should contain summon text");

            Debug.Log($"Summon Effect Description:\n{description}");

            // Cleanup
            Object.DestroyImmediate(unitData);
        }

        [Test]
        public void GetDetailedDescription_WithTargetingInfo_ShowsTargetingDetails()
        {
            // Arrange
            SetPrivateField(testCard, "cardName", "Targeted Spell");
            SetPrivateField(testCard, "cardType", CardData.CardType.Spell);
            SetPrivateField(testCard, "manaCost", 2);
            SetPrivateField(testCard, "targetType", CardData.TargetType.Enemy);
            SetPrivateField(testCard, "targetRange", 3);

            var effectData = new EffectData(EffectType.Damage, 4, AffectedType.Enemy, 2);
            var effectDataList = new List<EffectData> { effectData };
            SetPrivateField(testCard, "effectDataList", effectDataList);

            // Act
            string description = testCard.GetDetailedDescription();

            // Assert
            Assert.IsTrue(description.Contains("배치:"), "Should contain placement info");
            Assert.IsTrue(description.Contains("적군 위치"), "Should contain enemy targeting");
            Assert.IsTrue(description.Contains("거리 제한: 3"), "Should contain range limit");
            Assert.IsTrue(description.Contains("효과 범위"), "Should contain effect range info");

            Debug.Log($"Targeting Description:\n{description}");
        }

        [Test]
        public void GetDetailedDescription_WithRarityColors_ContainsColorTags()
        {
            // Arrange - Test different rarities
            var rarities = new[]
            {
                CardData.CardRarity.Common,
                CardData.CardRarity.Rare,
                CardData.CardRarity.Legendary
            };

            foreach (var rarity in rarities)
            {
                SetPrivateField(testCard, "cardName", $"Test {rarity} Card");
                SetPrivateField(testCard, "rarity", rarity);
                SetPrivateField(testCard, "cardType", CardData.CardType.Spell);

                // Act
                string description = testCard.GetDetailedDescription();

                // Assert
                Assert.IsTrue(description.Contains("<color="), "Should contain color tags");
                Assert.IsTrue(description.Contains("</color>"), "Should contain color closing tags");

                Debug.Log($"{rarity} Card Description:\n{description}");
            }
        }

        [Test]
        public void GetDetailedDescription_LegacySystem_FallbackWorks()
        {
            // Arrange - Card without EffectData (legacy system)
            SetPrivateField(testCard, "cardName", "Legacy Card");
            SetPrivateField(testCard, "description", "Old system description");
            SetPrivateField(testCard, "cardType", CardData.CardType.Spell);
            SetPrivateField(testCard, "manaCost", 1);

            // Empty EffectDataList to simulate legacy card
            SetPrivateField(testCard, "effectDataList", new List<EffectData>());

            // Act
            string description = testCard.GetDetailedDescription();

            // Assert
            Assert.IsTrue(description.Contains("Legacy Card"), "Should contain card name");
            Assert.IsTrue(description.Contains("Old system description"), "Should contain basic description");
            Assert.IsFalse(description.Contains("효과:"), "Should not contain effect section");

            Debug.Log($"Legacy Card Description:\n{description}");
        }

        [Test]
        public void IsEffectBasedCard_WithEffects_ReturnsTrue()
        {
            // Arrange
            var effectDataList = new List<EffectData>
            {
                new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 0)
            };
            SetPrivateField(testCard, "effectDataList", effectDataList);

            // Act & Assert
            Assert.IsTrue(testCard.IsEffectBasedCard, "Card with EffectData should be effect-based");
        }

        [Test]
        public void IsEffectBasedCard_WithoutEffects_ReturnsFalse()
        {
            // Arrange
            SetPrivateField(testCard, "effectDataList", new List<EffectData>());

            // Act & Assert
            Assert.IsFalse(testCard.IsEffectBasedCard, "Card without EffectData should not be effect-based");
        }

        [Test]
        public void GetPrimaryEffectType_WithMultipleEffects_ReturnsHighestValue()
        {
            // Arrange
            var effectDataList = new List<EffectData>
            {
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0),
                new EffectData(EffectType.Heal, 5, AffectedType.Ally, 0), // Highest value
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 1)
            };
            SetPrivateField(testCard, "effectDataList", effectDataList);

            // Act
            var primaryEffect = testCard.GetPrimaryEffectType();

            // Assert
            Assert.AreEqual(EffectType.Heal, primaryEffect, "Primary effect should be Heal (highest value)");
        }

        /// <summary>
        /// Helper method to set private fields using reflection
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
            }
        }
    }
}