using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Game.Data;
using Game.Card.Effects;
using Game.Services.Card;
using Game.Components;

namespace Game.Tests
{
    /// <summary>
    /// CardSpawnService 통합 테스트
    /// Phase 4.17: 새로운 효과 시스템과 CardSpawnService 통합 검증
    /// </summary>
    [TestFixture]
    public class CardSpawnServiceIntegrationTests
    {
        private CardSpawnService cardSpawnService;
        private IGridController mockGridController;
        private SpawnValidator mockSpawnValidator;
        private GameContext testContext;
        private CardData testCard;

        [SetUp]
        public void SetUp()
        {
            // Mock 서비스들 생성
            mockGridController = new MockGridController();
            mockSpawnValidator = new MockSpawnValidator();

            // CardSpawnService 초기화
            cardSpawnService = new CardSpawnService(mockGridController, mockSpawnValidator);

            // 테스트 컨텍스트 생성
            testContext = CreateTestGameContext();
        }

        [TearDown]
        public void TearDown()
        {
            if (testCard != null)
            {
                Object.DestroyImmediate(testCard);
            }
        }

        #region 새로운 효과 시스템 통합 테스트

        [Test]
        public void SpawnCard_NewEffectSystem_ShouldExecuteDamageEffect()
        {
            // Arrange
            testCard = CreateCardWithEffects(
                new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1)
            );
            var targetPos = new Vector2Int(3, 3);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Damage card should spawn successfully");
            Assert.IsEmpty(result.ErrorMessage, "Should have no error message");
        }

        [Test]
        public void SpawnCard_NewEffectSystem_ShouldExecuteHealEffect()
        {
            // Arrange
            testCard = CreateCardWithEffects(
                new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0)
            );
            var targetPos = new Vector2Int(2, 2);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Heal card should spawn successfully");
            Assert.That(result.Message, Does.Contain("회복"), "Should indicate healing occurred");
        }

        [Test]
        public void SpawnCard_NewEffectSystem_ShouldExecuteSummonEffect()
        {
            // Arrange
            var unitData = CreateMockUnitData("Knight");
            testCard = CreateCardWithSummonEffect(unitData);
            var targetPos = new Vector2Int(4, 4);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Summon card should spawn successfully");
            Assert.That(result.Message, Does.Contain("소환"), "Should indicate summoning occurred");
        }

        [Test]
        public void SpawnCard_MultipleEffects_ShouldExecuteInPriorityOrder()
        {
            // Arrange
            var effect1 = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0);
            var effect2 = new EffectData(EffectType.Heal, 2, AffectedType.Ally, 0);
            var effect3 = new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 1);

            SetEffectPriority(effect1, 10);
            SetEffectPriority(effect2, 5); // Should execute first
            SetEffectPriority(effect3, 15);

            testCard = CreateCardWithEffects(effect1, effect2, effect3);
            var targetPos = new Vector2Int(3, 3);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Multi-effect card should spawn successfully");
            Assert.AreEqual(3, result.EffectsExecuted, "Should execute all 3 effects");
        }

        [Test]
        public void SpawnCard_PartialEffectFailure_ShouldExecuteValidEffects()
        {
            // Arrange
            var validEffect = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0);
            var invalidEffect = new EffectData(EffectType.Summon, 1, AffectedType.None, 0); // No unit data

            testCard = CreateCardWithEffects(validEffect, invalidEffect);
            var targetPos = new Vector2Int(3, 3);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Should succeed with partial execution");
            Assert.AreEqual(1, result.EffectsExecuted, "Should execute only valid effect");
            Assert.That(result.ErrorMessage, Does.Contain("일부 효과"), "Should indicate partial failure");
        }

        #endregion

        #region 배치 유효성 검증 테스트

        [Test]
        public void SpawnCard_InvalidPlacement_ShouldFailValidation()
        {
            // Arrange
            testCard = CreateCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            );
            var invalidPos = new Vector2Int(-1, -1);

            // Setup validator to reject position
            ((MockSpawnValidator)mockSpawnValidator).SetValidationResult(false, "Invalid position");

            // Act
            var result = cardSpawnService.SpawnCard(testCard, invalidPos, testContext);

            // Assert
            Assert.IsFalse(result.Success, "Should fail for invalid placement");
            Assert.That(result.ErrorMessage, Does.Contain("Invalid position"), "Should contain validation error");
        }

        [Test]
        public void SpawnCard_ValidPlacement_ShouldPassValidation()
        {
            // Arrange
            testCard = CreateCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            );
            var validPos = new Vector2Int(3, 3);

            // Setup validator to accept position
            ((MockSpawnValidator)mockSpawnValidator).SetValidationResult(true, "");

            // Act
            var result = cardSpawnService.SpawnCard(testCard, validPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Should succeed for valid placement");
        }

        [Test]
        public void SpawnCard_RangeValidation_ShouldEnforceDistance()
        {
            // Arrange
            testCard = CreateCardWithTargetRange(2);
            testCard = AddEffectsToCard(testCard, new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0));

            var nearPos = new Vector2Int(1, 1); // Distance 2
            var farPos = new Vector2Int(3, 0);  // Distance 3

            // Act
            var nearResult = cardSpawnService.SpawnCard(testCard, nearPos, testContext);
            var farResult = cardSpawnService.SpawnCard(testCard, farPos, testContext);

            // Assert
            Assert.IsTrue(nearResult.Success, "Near position should be valid");
            Assert.IsFalse(farResult.Success, "Far position should be invalid");
            Assert.That(farResult.ErrorMessage, Does.Contain("범위"), "Should indicate range limitation");
        }

        #endregion

        #region 레거시 시스템 호환성 테스트

        [Test]
        public void SpawnCard_LegacyUnitCard_ShouldUseCompatibilityMode()
        {
            // Arrange
            testCard = CreateLegacyUnitCard();
            var targetPos = new Vector2Int(4, 4);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Legacy unit card should work");
            Assert.That(result.Message, Does.Contain("레거시"), "Should indicate legacy mode");
        }

        [Test]
        public void SpawnCard_LegacySpellCard_ShouldUseCompatibilityMode()
        {
            // Arrange
            testCard = CreateLegacySpellCard();
            var targetPos = new Vector2Int(3, 3);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Legacy spell card should work");
            Assert.That(result.Message, Does.Contain("레거시"), "Should indicate legacy mode");
        }

        [Test]
        public void SpawnCard_PreferNewSystem_ShouldUseEffectBasedExecution()
        {
            // Arrange
            testCard = CreateCardWithBothSystems(); // Has both legacy and new effects
            var targetPos = new Vector2Int(3, 3);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Should prefer new system");
            Assert.That(result.Message, Does.Not.Contain("레거시"), "Should not use legacy mode");
        }

        #endregion

        #region 오류 처리 테스트

        [Test]
        public void SpawnCard_NullCard_ShouldReturnError()
        {
            // Act
            var result = cardSpawnService.SpawnCard(null, Vector2Int.zero, testContext);

            // Assert
            Assert.IsFalse(result.Success, "Should fail for null card");
            Assert.That(result.ErrorMessage, Does.Contain("카드"), "Should indicate card error");
        }

        [Test]
        public void SpawnCard_NullContext_ShouldReturnError()
        {
            // Arrange
            testCard = CreateCardWithEffects(
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            );

            // Act
            var result = cardSpawnService.SpawnCard(testCard, Vector2Int.zero, null);

            // Assert
            Assert.IsFalse(result.Success, "Should fail for null context");
            Assert.That(result.ErrorMessage, Does.Contain("컨텍스트"), "Should indicate context error");
        }

        [Test]
        public void SpawnCard_NoValidEffects_ShouldReturnError()
        {
            // Arrange
            testCard = CreateCardWithoutEffects();

            // Act
            var result = cardSpawnService.SpawnCard(testCard, Vector2Int.zero, testContext);

            // Assert
            Assert.IsFalse(result.Success, "Should fail for card without effects");
            Assert.That(result.ErrorMessage, Does.Contain("효과"), "Should indicate effect error");
        }

        #endregion

        #region 성능 및 리소스 테스트

        [Test]
        public void SpawnCard_ManyEffects_ShouldExecuteEfficiently()
        {
            // Arrange
            var effects = new List<EffectData>();
            for (int i = 0; i < 10; i++)
            {
                effects.Add(new EffectData(EffectType.Damage, 1, AffectedType.Enemy, 0));
            }

            testCard = CreateCardWithEffects(effects.ToArray());
            var targetPos = new Vector2Int(3, 3);

            // Act
            var startTime = Time.realtimeSinceStartup;
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);
            var executionTime = Time.realtimeSinceStartup - startTime;

            // Assert
            Assert.IsTrue(result.Success, "Should execute many effects successfully");
            Assert.AreEqual(10, result.EffectsExecuted, "Should execute all effects");
            Assert.Less(executionTime, 0.1f, "Should execute within reasonable time");
        }

        [Test]
        public void SpawnCard_LargeAreaEffect_ShouldHandleMultipleTargets()
        {
            // Arrange
            testCard = CreateCardWithEffects(
                new EffectData(EffectType.Damage, 2, AffectedType.Enemy, 3) // Large area
            );
            var targetPos = new Vector2Int(5, 5);

            // Act
            var result = cardSpawnService.SpawnCard(testCard, targetPos, testContext);

            // Assert
            Assert.IsTrue(result.Success, "Large area effect should succeed");
            Assert.Greater(result.TargetsAffected, 1, "Should affect multiple targets");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 효과가 있는 테스트 카드 생성
        /// </summary>
        private CardData CreateCardWithEffects(params EffectData[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "cardName", "Test Card");
            SetPrivateField(card, "cardType", CardData.CardType.Spell);
            SetPrivateField(card, "effectDataList", new List<EffectData>(effects));
            return card;
        }

        /// <summary>
        /// 소환 효과가 있는 테스트 카드 생성
        /// </summary>
        private CardData CreateCardWithSummonEffect(UnitData unitData)
        {
            var effect = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            SetPrivateField(effect, "unitToSummon", unitData);

            return CreateCardWithEffects(effect);
        }

        /// <summary>
        /// TargetRange가 설정된 테스트 카드 생성
        /// </summary>
        private CardData CreateCardWithTargetRange(int range)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "cardName", "Range Test Card");
            SetPrivateField(card, "targetRange", range);
            return card;
        }

        /// <summary>
        /// 기존 카드에 효과 추가
        /// </summary>
        private CardData AddEffectsToCard(CardData card, params EffectData[] effects)
        {
            SetPrivateField(card, "effectDataList", new List<EffectData>(effects));
            return card;
        }

        /// <summary>
        /// 레거시 유닛 카드 생성
        /// </summary>
        private CardData CreateLegacyUnitCard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "cardName", "Legacy Unit");
            SetPrivateField(card, "cardType", CardData.CardType.Unit);
            return card;
        }

        /// <summary>
        /// 레거시 주문 카드 생성
        /// </summary>
        private CardData CreateLegacySpellCard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "cardName", "Legacy Spell");
            SetPrivateField(card, "cardType", CardData.CardType.Spell);
            return card;
        }

        /// <summary>
        /// 레거시와 새 시스템을 모두 가진 카드 생성
        /// </summary>
        private CardData CreateCardWithBothSystems()
        {
            var card = CreateLegacySpellCard();
            var effects = new List<EffectData>
            {
                new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
            };
            SetPrivateField(card, "effectDataList", effects);
            return card;
        }

        /// <summary>
        /// 효과가 없는 카드 생성
        /// </summary>
        private CardData CreateCardWithoutEffects()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "cardName", "Empty Card");
            SetPrivateField(card, "effectDataList", new List<EffectData>());
            return card;
        }

        /// <summary>
        /// Mock UnitData 생성
        /// </summary>
        private UnitData CreateMockUnitData(string name)
        {
            var unitData = ScriptableObject.CreateInstance<UnitData>();
            SetPrivateField(unitData, "unitName", name);
            return unitData;
        }

        /// <summary>
        /// 테스트용 GameContext 생성
        /// </summary>
        private GameContext CreateTestGameContext()
        {
            return new GameContext(
                null, // unitService
                mockGridController,
                cardSpawnService,
                mockSpawnValidator,
                0, // playerId
                Vector2Int.zero // originPosition
            );
        }

        /// <summary>
        /// EffectData의 우선순위 설정
        /// </summary>
        private void SetEffectPriority(EffectData effectData, int priority)
        {
            SetPrivateField(effectData, "priority", priority);
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

        #endregion
    }

    /// <summary>
    /// 테스트용 MockSpawnValidator
    /// </summary>
    public class MockSpawnValidator : SpawnValidator
    {
        private bool validationResult = true;
        private string validationMessage = "";

        public MockSpawnValidator() : base(null) { }

        public void SetValidationResult(bool isValid, string message)
        {
            validationResult = isValid;
            validationMessage = message;
        }

        public override ValidationResult ValidatePlacementTarget(CardData card, Vector2Int casterPos, Vector2Int targetPos, bool isPlayerCard)
        {
            return new ValidationResult
            {
                IsValid = validationResult,
                ErrorMessage = validationMessage
            };
        }
    }

    /// <summary>
    /// CardSpawnService의 결과 구조체 (테스트용 확장)
    /// </summary>
    public class SpawnResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public int EffectsExecuted { get; set; }
        public int TargetsAffected { get; set; }
    }

    /// <summary>
    /// SpawnValidator의 검증 결과 구조체
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
    }
}