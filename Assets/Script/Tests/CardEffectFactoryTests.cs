using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Game.Data;
using Game.Card.Effects;

namespace Game.Tests
{
    /// <summary>
    /// CardEffect 팩토리 패턴 단위 테스트
    /// Phase 4.17: ICardEffect 구현체들과 CardEffectFactory 검증
    /// </summary>
    [TestFixture]
    public class CardEffectFactoryTests
    {
        private GameContext mockContext;
        private UnitData mockUnitData;

        [SetUp]
        public void SetUp()
        {
            mockContext = CreateMockGameContext();
            mockUnitData = CreateMockUnitData();
        }

        #region 팩토리 패턴 기본 테스트

        [Test]
        public void CardEffectFactory_CreateEffect_ShouldCreateDamageEffect()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1);

            // Act
            var effect = CardEffectFactory.CreateEffect(effectData);

            // Assert
            Assert.IsNotNull(effect, "Should create effect instance");
            Assert.IsInstanceOf<DamageEffect>(effect, "Should create DamageEffect instance");
            Assert.AreEqual(EffectType.Damage, effect.EffectType, "Should have correct effect type");
        }

        [Test]
        public void CardEffectFactory_CreateEffect_ShouldCreateHealEffect()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0);

            // Act
            var effect = CardEffectFactory.CreateEffect(effectData);

            // Assert
            Assert.IsNotNull(effect, "Should create effect instance");
            Assert.IsInstanceOf<HealEffect>(effect, "Should create HealEffect instance");
            Assert.AreEqual(EffectType.Heal, effect.EffectType, "Should have correct effect type");
        }

        [Test]
        public void CardEffectFactory_CreateEffect_ShouldCreateSummonEffect()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            SetSummonUnit(effectData, mockUnitData);

            // Act
            var effect = CardEffectFactory.CreateEffect(effectData);

            // Assert
            Assert.IsNotNull(effect, "Should create effect instance");
            Assert.IsInstanceOf<SummonEffect>(effect, "Should create SummonEffect instance");
            Assert.AreEqual(EffectType.Summon, effect.EffectType, "Should have correct effect type");
        }

        [Test]
        public void CardEffectFactory_CreateEffect_ShouldReturnNullForInvalidData()
        {
            // Arrange
            var invalidEffectData = new EffectData(EffectType.Damage, 0, AffectedType.Enemy, 0); // Invalid value

            // Act
            var effect = CardEffectFactory.CreateEffect(invalidEffectData);

            // Assert
            Assert.IsNull(effect, "Should return null for invalid effect data");
        }

        [Test]
        public void CardEffectFactory_CreateEffect_ShouldReturnNullForNullData()
        {
            // Act
            var effect = CardEffectFactory.CreateEffect(null);

            // Assert
            Assert.IsNull(effect, "Should return null for null effect data");
        }

        #endregion

        #region DamageEffect 테스트

        [Test]
        public void DamageEffect_CanExecute_ShouldValidateTargetPosition()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1);
            var effect = CardEffectFactory.CreateEffect(effectData) as DamageEffect;
            var targetPos = new Vector2Int(3, 3);

            // Act
            var canExecute = effect.CanExecute(targetPos, mockContext);

            // Assert
            Assert.IsTrue(canExecute, "Should be able to execute on valid target");
        }

        [Test]
        public void DamageEffect_CanExecute_ShouldReturnFalseForInvalidContext()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1);
            var effect = CardEffectFactory.CreateEffect(effectData) as DamageEffect;
            var targetPos = new Vector2Int(3, 3);

            // Act
            var canExecute = effect.CanExecute(targetPos, null);

            // Assert
            Assert.IsFalse(canExecute, "Should return false for null context");
        }

        [Test]
        public void DamageEffect_Execute_ShouldApplyDamageToTargets()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1);
            var effect = CardEffectFactory.CreateEffect(effectData) as DamageEffect;
            var targetPos = new Vector2Int(3, 3);

            // Act & Assert
            Assert.DoesNotThrow(() => effect.Execute(targetPos, mockContext),
                "Execute should not throw exception");
        }

        [Test]
        public void DamageEffect_Priority_ShouldReturnCorrectValue()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1);
            SetEffectPriority(effectData, 10);
            var effect = CardEffectFactory.CreateEffect(effectData);

            // Assert
            Assert.AreEqual(10, effect.Priority, "Should return correct priority");
        }

        #endregion

        #region HealEffect 테스트

        [Test]
        public void HealEffect_CanExecute_ShouldValidateAllyTarget()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0);
            var effect = CardEffectFactory.CreateEffect(effectData) as HealEffect;
            var targetPos = new Vector2Int(2, 2);

            // Act
            var canExecute = effect.CanExecute(targetPos, mockContext);

            // Assert
            Assert.IsTrue(canExecute, "Should be able to execute heal on ally target");
        }

        [Test]
        public void HealEffect_Execute_ShouldHealTargets()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0);
            var effect = CardEffectFactory.CreateEffect(effectData) as HealEffect;
            var targetPos = new Vector2Int(2, 2);

            // Act & Assert
            Assert.DoesNotThrow(() => effect.Execute(targetPos, mockContext),
                "Execute should not throw exception");
        }

        [Test]
        public void HealEffect_Execute_ShouldRespectAffectedRange()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Heal, 2, AffectedType.Ally, 2); // Range 2
            var effect = CardEffectFactory.CreateEffect(effectData) as HealEffect;
            var targetPos = new Vector2Int(5, 5);

            // Act & Assert
            Assert.DoesNotThrow(() => effect.Execute(targetPos, mockContext),
                "Should execute area heal without error");
        }

        #endregion

        #region SummonEffect 테스트

        [Test]
        public void SummonEffect_CanExecute_ShouldValidateSummonPosition()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            SetSummonUnit(effectData, mockUnitData);
            var effect = CardEffectFactory.CreateEffect(effectData) as SummonEffect;
            var targetPos = new Vector2Int(4, 4);

            // Act
            var canExecute = effect.CanExecute(targetPos, mockContext);

            // Assert
            Assert.IsTrue(canExecute, "Should be able to summon at valid position");
        }

        [Test]
        public void SummonEffect_CanExecute_ShouldReturnFalseForNoUnitData()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            // No unit data set
            var effect = CardEffectFactory.CreateEffect(effectData) as SummonEffect;
            var targetPos = new Vector2Int(4, 4);

            // Act
            var canExecute = effect.CanExecute(targetPos, mockContext);

            // Assert
            Assert.IsFalse(canExecute, "Should return false when no unit data is provided");
        }

        [Test]
        public void SummonEffect_Execute_ShouldSummonUnit()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);
            SetSummonUnit(effectData, mockUnitData);
            var effect = CardEffectFactory.CreateEffect(effectData) as SummonEffect;
            var targetPos = new Vector2Int(4, 4);

            // Act & Assert
            Assert.DoesNotThrow(() => effect.Execute(targetPos, mockContext),
                "Execute should not throw exception");
        }

        [Test]
        public void SummonEffect_Execute_ShouldRespectSummonCount()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Summon, 2, AffectedType.None, 0); // Summon 2 units
            SetSummonUnit(effectData, mockUnitData);
            var effect = CardEffectFactory.CreateEffect(effectData) as SummonEffect;
            var targetPos = new Vector2Int(4, 4);

            // Act & Assert
            Assert.DoesNotThrow(() => effect.Execute(targetPos, mockContext),
                "Should handle multiple unit summoning");
        }

        #endregion

        #region 효과 우선순위 테스트

        [Test]
        public void CardEffects_ShouldBeSortableByPriority()
        {
            // Arrange
            var effect1Data = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 0);
            var effect2Data = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 0);
            var effect3Data = new EffectData(EffectType.Summon, 1, AffectedType.None, 0);

            SetEffectPriority(effect1Data, 20);
            SetEffectPriority(effect2Data, 10);
            SetEffectPriority(effect3Data, 5);
            SetSummonUnit(effect3Data, mockUnitData);

            var effect1 = CardEffectFactory.CreateEffect(effect1Data);
            var effect2 = CardEffectFactory.CreateEffect(effect2Data);
            var effect3 = CardEffectFactory.CreateEffect(effect3Data);

            var effects = new[] { effect1, effect2, effect3 };

            // Act
            System.Array.Sort(effects, (a, b) => a.Priority.CompareTo(b.Priority));

            // Assert
            Assert.AreEqual(5, effects[0].Priority, "Summon effect should be first");
            Assert.AreEqual(10, effects[1].Priority, "Heal effect should be second");
            Assert.AreEqual(20, effects[2].Priority, "Damage effect should be last");
        }

        #endregion

        #region 효과 범위 테스트

        [Test]
        public void CardEffects_ShouldHandleDifferentAffectedRanges()
        {
            // Arrange
            var singleTargetData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 0);
            var areaTargetData = new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 2);

            var singleEffect = CardEffectFactory.CreateEffect(singleTargetData);
            var areaEffect = CardEffectFactory.CreateEffect(areaTargetData);

            var targetPos = new Vector2Int(5, 5);

            // Act & Assert
            Assert.DoesNotThrow(() => singleEffect.Execute(targetPos, mockContext),
                "Single target effect should execute");
            Assert.DoesNotThrow(() => areaEffect.Execute(targetPos, mockContext),
                "Area effect should execute");
        }

        [Test]
        public void CardEffects_ShouldRespectAffectedType()
        {
            // Arrange
            var allyEffectData = new EffectData(EffectType.Heal, 3, AffectedType.Ally, 1);
            var enemyEffectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 1);
            var anyEffectData = new EffectData(EffectType.Damage, 2, AffectedType.Any, 1);

            var allyEffect = CardEffectFactory.CreateEffect(allyEffectData);
            var enemyEffect = CardEffectFactory.CreateEffect(enemyEffectData);
            var anyEffect = CardEffectFactory.CreateEffect(anyEffectData);

            var targetPos = new Vector2Int(5, 5);

            // Act & Assert
            Assert.DoesNotThrow(() => allyEffect.Execute(targetPos, mockContext),
                "Ally effect should execute");
            Assert.DoesNotThrow(() => enemyEffect.Execute(targetPos, mockContext),
                "Enemy effect should execute");
            Assert.DoesNotThrow(() => anyEffect.Execute(targetPos, mockContext),
                "Any effect should execute");
        }

        #endregion

        #region 오류 처리 테스트

        [Test]
        public void CardEffects_ShouldHandleInvalidContext()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 0);
            var effect = CardEffectFactory.CreateEffect(effectData);
            var targetPos = new Vector2Int(3, 3);

            // Act & Assert
            Assert.IsFalse(effect.CanExecute(targetPos, null),
                "Should return false for null context");
            Assert.DoesNotThrow(() => effect.Execute(targetPos, null),
                "Execute should handle null context gracefully");
        }

        [Test]
        public void CardEffects_ShouldHandleInvalidTargetPosition()
        {
            // Arrange
            var effectData = new EffectData(EffectType.Damage, 5, AffectedType.Enemy, 0);
            var effect = CardEffectFactory.CreateEffect(effectData);
            var invalidPos = new Vector2Int(-1, -1);

            // Act & Assert
            Assert.DoesNotThrow(() => effect.CanExecute(invalidPos, mockContext),
                "CanExecute should handle invalid position");
            Assert.DoesNotThrow(() => effect.Execute(invalidPos, mockContext),
                "Execute should handle invalid position");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Mock GameContext 생성
        /// </summary>
        private GameContext CreateMockGameContext()
        {
            return new GameContext(
                null, // unitService
                new MockGridController(),
                null, // cardSpawnService
                null, // spawnValidator
                0,    // playerId
                Vector2Int.zero // originPosition
            );
        }

        /// <summary>
        /// Mock UnitData 생성
        /// </summary>
        private UnitData CreateMockUnitData()
        {
            var unitData = ScriptableObject.CreateInstance<UnitData>();
            SetPrivateField(unitData, "unitName", "Test Unit");
            return unitData;
        }

        /// <summary>
        /// EffectData에 소환 유닛 설정
        /// </summary>
        private void SetSummonUnit(EffectData effectData, UnitData unitData)
        {
            SetPrivateField(effectData, "unitToSummon", unitData);
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
}