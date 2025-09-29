using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Game.Data;
using Game.Card.Effects;
using Game.Services.Card;
using Game.Components;

namespace Game.Tests
{
    /// <summary>
    /// SpawnValidator 통합 테스트
    /// Phase 4.17: 새로운 TargetType과 TargetRange 시스템 통합 검증
    /// </summary>
    [TestFixture]
    public class SpawnValidatorIntegrationTests
    {
        private SpawnValidator spawnValidator;
        private IGridController mockGridController;
        private CardData testCard;
        private GameContext testContext;

        [SetUp]
        public void SetUp()
        {
            // Mock GridController 생성
            mockGridController = CreateMockGridController();

            // SpawnValidator 초기화
            spawnValidator = new SpawnValidator(mockGridController);

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

        #region TargetType 검증 테스트

        [Test]
        public void ValidatePlacementTarget_TargetTypeNone_ShouldAlwaysAllowPlacement()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.None);
            var targetPos = new Vector2Int(5, 5);

            // Act
            var result = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, targetPos, true);

            // Assert
            Assert.IsTrue(result.IsValid, "TargetType.None should always allow placement");
            Assert.IsEmpty(result.ErrorMessage, "Should have no error message");
        }

        [Test]
        public void ValidatePlacementTarget_TargetTypeGround_ShouldRequireEmptyTile()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.Ground);
            var emptyPos = new Vector2Int(3, 3);
            var occupiedPos = new Vector2Int(4, 4);

            SetupMockGridTile(emptyPos, isEmpty: true);
            SetupMockGridTile(occupiedPos, isEmpty: false);

            // Act
            var validResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, emptyPos, true);
            var invalidResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, occupiedPos, true);

            // Assert
            Assert.IsTrue(validResult.IsValid, "Empty tile should be valid for Ground type");
            Assert.IsFalse(invalidResult.IsValid, "Occupied tile should be invalid for Ground type");
            Assert.That(invalidResult.ErrorMessage, Does.Contain("타일이 이미 점유"), "Should indicate tile occupation");
        }

        [Test]
        public void ValidatePlacementTarget_TargetTypeAlly_ShouldRequireAllyPosition()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.Ally);
            var allyPos = new Vector2Int(2, 2);
            var enemyPos = new Vector2Int(6, 6);
            var emptyPos = new Vector2Int(4, 4);

            SetupMockGridUnit(allyPos, isPlayerUnit: true);
            SetupMockGridUnit(enemyPos, isPlayerUnit: false);
            SetupMockGridTile(emptyPos, isEmpty: true);

            // Act
            var validResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, allyPos, true);
            var enemyResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, enemyPos, true);
            var emptyResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, emptyPos, true);

            // Assert
            Assert.IsTrue(validResult.IsValid, "Ally position should be valid for Ally type");
            Assert.IsFalse(enemyResult.IsValid, "Enemy position should be invalid for Ally type");
            Assert.IsFalse(emptyResult.IsValid, "Empty position should be invalid for Ally type");
        }

        [Test]
        public void ValidatePlacementTarget_TargetTypeEnemy_ShouldRequireEnemyPosition()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.Enemy);
            var allyPos = new Vector2Int(2, 2);
            var enemyPos = new Vector2Int(6, 6);

            SetupMockGridUnit(allyPos, isPlayerUnit: true);
            SetupMockGridUnit(enemyPos, isPlayerUnit: false);

            // Act
            var allyResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, allyPos, true);
            var validResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, enemyPos, true);

            // Assert
            Assert.IsFalse(allyResult.IsValid, "Ally position should be invalid for Enemy type");
            Assert.IsTrue(validResult.IsValid, "Enemy position should be valid for Enemy type");
        }

        [Test]
        public void ValidatePlacementTarget_TargetTypeAny_ShouldAllowAnyUnitPosition()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.Any);
            var allyPos = new Vector2Int(2, 2);
            var enemyPos = new Vector2Int(6, 6);
            var emptyPos = new Vector2Int(4, 4);

            SetupMockGridUnit(allyPos, isPlayerUnit: true);
            SetupMockGridUnit(enemyPos, isPlayerUnit: false);
            SetupMockGridTile(emptyPos, isEmpty: true);

            // Act
            var allyResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, allyPos, true);
            var enemyResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, enemyPos, true);
            var emptyResult = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, emptyPos, true);

            // Assert
            Assert.IsTrue(allyResult.IsValid, "Ally position should be valid for Any type");
            Assert.IsTrue(enemyResult.IsValid, "Enemy position should be valid for Any type");
            Assert.IsFalse(emptyResult.IsValid, "Empty position should be invalid for Any type (requires unit)");
        }

        #endregion

        #region TargetRange 검증 테스트

        [Test]
        public void ValidatePlacementTarget_TargetRange_ShouldEnforceDistanceLimit()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: 3);
            var casterPos = new Vector2Int(0, 0);
            var nearPos = new Vector2Int(2, 1); // Distance 3
            var farPos = new Vector2Int(3, 1);  // Distance 4

            // Act
            var nearResult = spawnValidator.ValidatePlacementTarget(testCard, casterPos, nearPos, true);
            var farResult = spawnValidator.ValidatePlacementTarget(testCard, casterPos, farPos, true);

            // Assert
            Assert.IsTrue(nearResult.IsValid, "Position within range should be valid");
            Assert.IsFalse(farResult.IsValid, "Position outside range should be invalid");
            Assert.That(farResult.ErrorMessage, Does.Contain("범위"), "Should indicate range limitation");
        }

        [Test]
        public void ValidatePlacementTarget_PlayerBasedRange_ShouldCalculateFromLeftmostUnit()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: 2);
            var leftmostPlayerPos = new Vector2Int(1, 0);
            var rightPlayerPos = new Vector2Int(4, 0);
            var targetPos = new Vector2Int(3, 0); // Distance 2 from leftmost

            SetupPlayerUnits(leftmostPlayerPos, rightPlayerPos);

            // Act
            var result = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, targetPos, true);

            // Assert
            Assert.IsTrue(result.IsValid, "Should calculate range from leftmost player unit");
        }

        [Test]
        public void ValidatePlacementTarget_EnemyBasedRange_ShouldCalculateFromRightmostUnit()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: 2);
            var leftEnemyPos = new Vector2Int(6, 0);
            var rightmostEnemyPos = new Vector2Int(9, 0);
            var targetPos = new Vector2Int(7, 0); // Distance 2 from rightmost

            SetupEnemyUnits(leftEnemyPos, rightmostEnemyPos);

            // Act
            var result = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, targetPos, false);

            // Assert
            Assert.IsTrue(result.IsValid, "Should calculate range from rightmost enemy unit");
        }

        [Test]
        public void ValidatePlacementTarget_UnlimitedRange_ShouldAllowAnyDistance()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: -1);
            var casterPos = new Vector2Int(0, 0);
            var distantPos = new Vector2Int(100, 100);

            // Act
            var result = spawnValidator.ValidatePlacementTarget(testCard, casterPos, distantPos, true);

            // Assert
            Assert.IsTrue(result.IsValid, "Unlimited range should allow any distance");
        }

        #endregion

        #region 복합 검증 테스트

        [Test]
        public void ValidatePlacementTarget_CombinedConstraints_ShouldValidateBoth()
        {
            // Arrange
            testCard = CreateTestCard(targetType: CardData.TargetType.Ally, targetRange: 2);
            var casterPos = new Vector2Int(0, 0);
            var nearAllyPos = new Vector2Int(1, 1); // Distance 2, ally
            var farAllyPos = new Vector2Int(3, 0);  // Distance 3, ally
            var nearEnemyPos = new Vector2Int(0, 2); // Distance 2, enemy

            SetupMockGridUnit(nearAllyPos, isPlayerUnit: true);
            SetupMockGridUnit(farAllyPos, isPlayerUnit: true);
            SetupMockGridUnit(nearEnemyPos, isPlayerUnit: false);

            // Act
            var validResult = spawnValidator.ValidatePlacementTarget(testCard, casterPos, nearAllyPos, true);
            var rangeResult = spawnValidator.ValidatePlacementTarget(testCard, casterPos, farAllyPos, true);
            var typeResult = spawnValidator.ValidatePlacementTarget(testCard, casterPos, nearEnemyPos, true);

            // Assert
            Assert.IsTrue(validResult.IsValid, "Near ally should be valid");
            Assert.IsFalse(rangeResult.IsValid, "Far ally should be invalid due to range");
            Assert.IsFalse(typeResult.IsValid, "Near enemy should be invalid due to type");
        }

        [Test]
        public void ValidatePlacementTarget_EdgeOfRange_ShouldBeValid()
        {
            // Arrange
            testCard = CreateTestCard(targetRange: 3);
            var casterPos = new Vector2Int(0, 0);
            var edgePos = new Vector2Int(3, 0); // Exactly distance 3

            // Act
            var result = spawnValidator.ValidatePlacementTarget(testCard, casterPos, edgePos, true);

            // Assert
            Assert.IsTrue(result.IsValid, "Position at exact range limit should be valid");
        }

        #endregion

        #region 오류 처리 테스트

        [Test]
        public void ValidatePlacementTarget_InvalidGridPosition_ShouldReturnError()
        {
            // Arrange
            testCard = CreateTestCard();
            var invalidPos = new Vector2Int(-1, -1);

            // Act
            var result = spawnValidator.ValidatePlacementTarget(testCard, Vector2Int.zero, invalidPos, true);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid grid position should be invalid");
            Assert.That(result.ErrorMessage, Does.Contain("유효하지 않은"), "Should indicate invalid position");
        }

        [Test]
        public void ValidatePlacementTarget_NullCard_ShouldHandleGracefully()
        {
            // Act
            var result = spawnValidator.ValidatePlacementTarget(null, Vector2Int.zero, Vector2Int.zero, true);

            // Assert
            Assert.IsFalse(result.IsValid, "Null card should be invalid");
            Assert.IsNotEmpty(result.ErrorMessage, "Should have error message");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 테스트용 카드 생성
        /// </summary>
        private CardData CreateTestCard(CardData.TargetType targetType = CardData.TargetType.None, int targetRange = -1)
        {
            var card = ScriptableObject.CreateInstance<CardData>();

            // Use reflection to set private fields
            SetPrivateField(card, "cardName", "Test Card");
            SetPrivateField(card, "targetType", targetType);
            SetPrivateField(card, "targetRange", targetRange);

            return card;
        }

        /// <summary>
        /// Mock GridController 생성
        /// </summary>
        private IGridController CreateMockGridController()
        {
            // Note: This would need to be implemented based on actual IGridController interface
            // For testing purposes, create a mock that handles basic tile/unit queries
            return new MockGridController();
        }

        /// <summary>
        /// 테스트용 GameContext 생성
        /// </summary>
        private GameContext CreateTestGameContext()
        {
            return new GameContext(
                null, // unitService
                mockGridController,
                null, // cardSpawnService
                spawnValidator,
                0,    // playerId
                Vector2Int.zero // originPosition
            );
        }

        /// <summary>
        /// Mock 그리드 타일 설정
        /// </summary>
        private void SetupMockGridTile(Vector2Int position, bool isEmpty)
        {
            var mockGrid = (MockGridController)mockGridController;
            mockGrid.SetTile(position, isEmpty);
        }

        /// <summary>
        /// Mock 그리드 유닛 설정
        /// </summary>
        private void SetupMockGridUnit(Vector2Int position, bool isPlayerUnit)
        {
            var mockGrid = (MockGridController)mockGridController;
            mockGrid.SetUnit(position, isPlayerUnit);
        }

        /// <summary>
        /// 플레이어 유닛들 설정
        /// </summary>
        private void SetupPlayerUnits(params Vector2Int[] positions)
        {
            var mockGrid = (MockGridController)mockGridController;
            foreach (var pos in positions)
            {
                mockGrid.SetUnit(pos, true);
            }
        }

        /// <summary>
        /// 적 유닛들 설정
        /// </summary>
        private void SetupEnemyUnits(params Vector2Int[] positions)
        {
            var mockGrid = (MockGridController)mockGridController;
            foreach (var pos in positions)
            {
                mockGrid.SetUnit(pos, false);
            }
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
    /// 테스트용 MockGridController
    /// </summary>
    public class MockGridController : IGridController
    {
        private Dictionary<Vector2Int, bool> tiles = new Dictionary<Vector2Int, bool>();
        private Dictionary<Vector2Int, bool> units = new Dictionary<Vector2Int, bool>();

        public void SetTile(Vector2Int position, bool isEmpty)
        {
            tiles[position] = isEmpty;
        }

        public void SetUnit(Vector2Int position, bool isPlayerUnit)
        {
            units[position] = isPlayerUnit;
            tiles[position] = false; // Unit occupies tile
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.y >= 0;
        }

        public bool IsEmptyTile(Vector2Int position)
        {
            return tiles.TryGetValue(position, out bool isEmpty) && isEmpty;
        }

        public bool HasUnitAt(Vector2Int position)
        {
            return units.ContainsKey(position);
        }

        public bool IsPlayerUnit(Vector2Int position)
        {
            return units.TryGetValue(position, out bool isPlayer) && isPlayer;
        }

        public Vector2Int GetLeftmostPlayerUnit()
        {
            var playerUnits = units.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
            return playerUnits.Any() ? playerUnits.OrderBy(pos => pos.x).First() : Vector2Int.zero;
        }

        public Vector2Int GetRightmostEnemyUnit()
        {
            var enemyUnits = units.Where(kvp => !kvp.Value).Select(kvp => kvp.Key);
            return enemyUnits.Any() ? enemyUnits.OrderByDescending(pos => pos.x).First() : Vector2Int.zero;
        }

        // Implement other IGridController methods as needed for testing
        public void PlaceUnit(Vector2Int position, object unit) { }
        public void RemoveUnit(Vector2Int position) { }
        public object GetUnitAt(Vector2Int position) { return null; }
        public List<Vector2Int> GetUnitsInRange(Vector2Int center, int range) { return new List<Vector2Int>(); }
    }
}