/*
 using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using Game.Interfaces;
using Game.Data;
using Game.Core;
using Moq;

namespace Game.Tests
{
    /// <summary>
    /// Phase 3 Grid System Unit Tests
    /// Tests all grid interfaces for proper functionality and integration
    /// </summary>
    [TestFixture]
    public class Phase3GridSystemTests
    {
        private Mock<IGridState> mockGridState;
        private Mock<IGridController> mockGridController;
        private Mock<IGridRenderer> mockGridRenderer;
        private Mock<IGridServices> mockGridServices;
        
        [SetUp]
        public void SetUp()
        {
            // Initialize mocks for each interface
            mockGridState = new Mock<IGridState>();
            mockGridController = new Mock<IGridController>();
            mockGridRenderer = new Mock<IGridRenderer>();
            mockGridServices = new Mock<IGridServices>();
            
            // Setup basic mock behaviors
            SetupMockDefaults();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up any resources
            ServiceLocator.UnregisterAll();
        }
        
        private void SetupMockDefaults()
        {
            // Setup IGridState defaults
            mockGridState.Setup(x => x.GridWidth).Returns(10);
            mockGridState.Setup(x => x.GridHeight).Returns(10);
            mockGridState.Setup(x => x.IsValidPosition(It.IsAny<Vector2Int>())).Returns(true);
            mockGridState.Setup(x => x.IsOccupied(It.IsAny<Vector2Int>())).Returns(false);
            mockGridState.Setup(x => x.GetUnitAtPosition(It.IsAny<Vector2Int>())).Returns((GameObject)null);
            
            // Setup IGridController defaults
            mockGridController.Setup(x => x.CanMoveUnit(It.IsAny<GameObject>(), It.IsAny<Vector2Int>())).Returns(true);
            mockGridController.Setup(x => x.MoveUnit(It.IsAny<GameObject>(), It.IsAny<Vector2Int>())).Returns(true);
            mockGridController.Setup(x => x.IsValidPosition(It.IsAny<Vector2Int>())).Returns(true);
            mockGridController.Setup(x => x.FindPath(It.IsAny<Vector2Int>(), It.IsAny<Vector2Int>(), It.IsAny<GameObject>()))
                .Returns(new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 1) });
            
            // Setup IGridServices defaults
            mockGridServices.Setup(x => x.GridState).Returns(mockGridState.Object);
            mockGridServices.Setup(x => x.GridController).Returns(mockGridController.Object);
            mockGridServices.Setup(x => x.GridRenderer).Returns(mockGridRenderer.Object);
        }
        
        #region IReadOnlyGridState Tests
        
        [Test]
        public void IReadOnlyGridState_GridDimensions_ReturnsCorrectValues()
        {
            // Arrange
            var gridState = mockGridState.Object;
            
            // Act & Assert
            Assert.AreEqual(10, gridState.GridWidth);
            Assert.AreEqual(10, gridState.GridHeight);
        }
        
        [Test]
        public void IReadOnlyGridState_IsValidPosition_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var validPosition = new Vector2Int(5, 5);
            mockGridState.Setup(x => x.IsValidPosition(validPosition)).Returns(true);
            
            // Act
            bool result = gridState.IsValidPosition(validPosition);
            
            // Assert
            Assert.IsTrue(result);
            mockGridState.Verify(x => x.IsValidPosition(validPosition), Times.Once);
        }
        
        [Test]
        public void IReadOnlyGridState_IsValidPosition_InvalidPosition_ReturnsFalse()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var invalidPosition = new Vector2Int(-1, -1);
            mockGridState.Setup(x => x.IsValidPosition(invalidPosition)).Returns(false);
            
            // Act
            bool result = gridState.IsValidPosition(invalidPosition);
            
            // Assert
            Assert.IsFalse(result);
            mockGridState.Verify(x => x.IsValidPosition(invalidPosition), Times.Once);
        }
        
        [Test]
        public void IReadOnlyGridState_IsOccupied_EmptyPosition_ReturnsFalse()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var emptyPosition = new Vector2Int(3, 3);
            mockGridState.Setup(x => x.IsOccupied(emptyPosition)).Returns(false);
            
            // Act
            bool result = gridState.IsOccupied(emptyPosition);
            
            // Assert
            Assert.IsFalse(result);
            mockGridState.Verify(x => x.IsOccupied(emptyPosition), Times.Once);
        }
        
        [Test]
        public void IReadOnlyGridState_GetUnitAtPosition_EmptyPosition_ReturnsNull()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var emptyPosition = new Vector2Int(2, 2);
            mockGridState.Setup(x => x.GetUnitAtPosition(emptyPosition)).Returns((GameObject)null);
            
            // Act
            GameObject result = gridState.GetUnitAtPosition(emptyPosition);
            
            // Assert
            Assert.IsNull(result);
            mockGridState.Verify(x => x.GetUnitAtPosition(emptyPosition), Times.Once);
        }
        
        #endregion
        
        #region IGridState Tests
        
        [Test]
        public void IGridState_PlaceUnit_ValidPosition_CallsOnUnitPlaced()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var testUnit = new GameObject("TestUnit");
            var position = new Vector2Int(1, 1);
            bool eventCalled = false;
            
            gridState.OnUnitPlaced += (pos, unit) => eventCalled = true;
            mockGridState.Setup(x => x.PlaceUnit(position, testUnit)).Returns(true);
            
            // Act
            bool result = gridState.PlaceUnit(position, testUnit);
            
            // Assert
            Assert.IsTrue(result);
            mockGridState.Verify(x => x.PlaceUnit(position, testUnit), Times.Once);
            
            // Cleanup
            Object.DestroyImmediate(testUnit);
        }
        
        [Test]
        public void IGridState_RemoveUnit_ValidPosition_CallsOnUnitRemoved()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var testUnit = new GameObject("TestUnit");
            var position = new Vector2Int(1, 1);
            bool eventCalled = false;
            
            gridState.OnUnitRemoved += (pos, unit) => eventCalled = true;
            mockGridState.Setup(x => x.RemoveUnit(position)).Returns(testUnit);
            
            // Act
            GameObject result = gridState.RemoveUnit(position);
            
            // Assert
            Assert.AreEqual(testUnit, result);
            mockGridState.Verify(x => x.RemoveUnit(position), Times.Once);
            
            // Cleanup
            Object.DestroyImmediate(testUnit);
        }
        
        #endregion
        
        #region IGridManager Tests
        
        [Test]
        public void IGridManager_CanMoveUnit_ValidMove_ReturnsTrue()
        {
            // Arrange
            var gridManager = mockGridController.Object;
            var testUnit = new GameObject("TestUnit");
            var targetPosition = new Vector2Int(2, 2);
            
            mockGridController.Setup(x => x.CanMoveUnit(testUnit, targetPosition)).Returns(true);
            
            // Act
            bool result = gridManager.CanMoveUnit(testUnit, targetPosition);
            
            // Assert
            Assert.IsTrue(result);
            mockGridController.Verify(x => x.CanMoveUnit(testUnit, targetPosition), Times.Once);
            
            // Cleanup
            Object.DestroyImmediate(testUnit);
        }
        
        [Test]
        public void IGridManager_MoveUnit_ValidMove_ReturnsTrue()
        {
            // Arrange
            var gridManager = mockGridController.Object;
            var testUnit = new GameObject("TestUnit");
            var targetPosition = new Vector2Int(3, 3);
            
            mockGridController.Setup(x => x.MoveUnit(testUnit, targetPosition)).Returns(true);
            
            // Act
            bool result = gridManager.MoveUnit(testUnit, targetPosition);
            
            // Assert
            Assert.IsTrue(result);
            mockGridController.Verify(x => x.MoveUnit(testUnit, targetPosition), Times.Once);
            
            // Cleanup
            Object.DestroyImmediate(testUnit);
        }
        
        [Test]
        public void IGridManager_FindPath_ValidPathfinding_ReturnsPath()
        {
            // Arrange
            var gridManager = mockGridController.Object;
            var startPos = new Vector2Int(0, 0);
            var endPos = new Vector2Int(2, 2);
            var testUnit = new GameObject("TestUnit");
            var expectedPath = new List<Vector2Int> { startPos, new Vector2Int(1, 1), endPos };
            
            mockGridController.Setup(x => x.FindPath(startPos, endPos, testUnit)).Returns(expectedPath);
            
            // Act
            List<Vector2Int> result = gridManager.FindPath(startPos, endPos, testUnit);
            
            // Assert
            Assert.AreEqual(expectedPath.Count, result.Count);
            Assert.AreEqual(startPos, result[0]);
            Assert.AreEqual(endPos, result[result.Count - 1]);
            mockGridController.Verify(x => x.FindPath(startPos, endPos, testUnit), Times.Once);
            
            // Cleanup
            Object.DestroyImmediate(testUnit);
        }
        
        [Test]
        public void IGridManager_GetPositionsInRange_ValidRange_ReturnsPositions()
        {
            // Arrange
            var gridManager = mockGridController.Object;
            var center = new Vector2Int(5, 5);
            int range = 2;
            var expectedPositions = new List<Vector2Int>
            {
                new Vector2Int(3, 5), new Vector2Int(4, 5), new Vector2Int(5, 5),
                new Vector2Int(6, 5), new Vector2Int(7, 5)
            };
            
            mockGridController.Setup(x => x.GetPositionsInRange(center, range)).Returns(expectedPositions);
            
            // Act
            List<Vector2Int> result = gridManager.GetPositionsInRange(center, range);
            
            // Assert
            Assert.AreEqual(expectedPositions.Count, result.Count);
            mockGridController.Verify(x => x.GetPositionsInRange(center, range), Times.Once);
        }
        
        #endregion
        
        #region IGridController Tests
        
        [Test]
        public void IGridController_GetPerformanceStats_ReturnsValidStats()
        {
            // Arrange
            var gridController = mockGridController.Object;
            var expectedStats = (1000, 850, 15.5, 85.0);
            
            mockGridController.Setup(x => x.GetPerformanceStats()).Returns(expectedStats);
            
            // Act
            var (totalCalls, cacheHits, avgTime, hitRate) = gridController.GetPerformanceStats();
            
            // Assert
            Assert.AreEqual(1000, totalCalls);
            Assert.AreEqual(850, cacheHits);
            Assert.AreEqual(15.5, avgTime, 0.1);
            Assert.AreEqual(85.0, hitRate, 0.1);
            mockGridController.Verify(x => x.GetPerformanceStats(), Times.Once);
        }
        
        [Test]
        public void IGridController_ClearCache_CallsSuccessfully()
        {
            // Arrange
            var gridController = mockGridController.Object;
            
            // Act
            gridController.ClearCache();
            
            // Assert
            mockGridController.Verify(x => x.ClearCache(), Times.Once);
        }
        
        #endregion
        
        #region IGridRenderer Tests
        
        [Test]
        public void IGridRenderer_UpdateVisuals_CallsSuccessfully()
        {
            // Arrange
            var gridRenderer = mockGridRenderer.Object;
            
            // Act
            gridRenderer.UpdateVisuals();
            
            // Assert
            mockGridRenderer.Verify(x => x.UpdateVisuals(), Times.Once);
        }
        
        [Test]
        public void IGridRenderer_HighlightPositions_ValidPositions_CallsSuccessfully()
        {
            // Arrange
            var gridRenderer = mockGridRenderer.Object;
            var positions = new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(2, 2) };
            var color = Color.blue;
            
            // Act
            gridRenderer.HighlightPositions(positions, color);
            
            // Assert
            mockGridRenderer.Verify(x => x.HighlightPositions(positions, color), Times.Once);
        }
        
        [Test]
        public void IGridRenderer_ClearHighlights_CallsSuccessfully()
        {
            // Arrange
            var gridRenderer = mockGridRenderer.Object;
            
            // Act
            gridRenderer.ClearHighlights();
            
            // Assert
            mockGridRenderer.Verify(x => x.ClearHighlights(), Times.Once);
        }
        
        #endregion
        
        #region IGridServices Tests
        
        [Test]
        public void IGridServices_Properties_ReturnCorrectInstances()
        {
            // Arrange
            var gridServices = mockGridServices.Object;
            
            // Act
            var gridState = gridServices.GridState;
            var gridController = gridServices.GridController;
            var gridRenderer = gridServices.GridRenderer;
            
            // Assert
            Assert.IsNotNull(gridState);
            Assert.IsNotNull(gridController);
            Assert.IsNotNull(gridRenderer);
            Assert.AreEqual(mockGridState.Object, gridState);
            Assert.AreEqual(mockGridController.Object, gridController);
            Assert.AreEqual(mockGridRenderer.Object, gridRenderer);
        }
        
        #endregion
        
        #region IGridDependent Integration Tests
        
        [Test]
        public void IGridDependent_Initialize_ValidServices_InitializesCorrectly()
        {
            // Arrange
            var testGameObject = new GameObject("TestGridDependent");
            var testComponent = testGameObject.AddComponent<TestGridDependentComponent>();
            var gridServices = mockGridServices.Object;
            
            // Act
            testComponent.Initialize(gridServices);
            
            // Assert
            Assert.IsTrue(testComponent.IsInitialized);
            Assert.AreEqual(gridServices, testComponent.GridServices);
            
            // Cleanup
            Object.DestroyImmediate(testGameObject);
        }
        
        [Test]
        public void IGridDependent_Initialize_NullServices_DoesNotInitialize()
        {
            // Arrange
            var testGameObject = new GameObject("TestGridDependent");
            var testComponent = testGameObject.AddComponent<TestGridDependentComponent>();
            
            // Act
            testComponent.Initialize(null);
            
            // Assert
            Assert.IsFalse(testComponent.IsInitialized);
            Assert.IsNull(testComponent.GridServices);
            
            // Cleanup
            Object.DestroyImmediate(testGameObject);
        }
        
        #endregion
        
        #region ServiceLocator Integration Tests
        
        [Test]
        public void ServiceLocator_RegisterAndGet_GridServices_WorksCorrectly()
        {
            // Arrange
            var gridServices = mockGridServices.Object;
            
            // Act
            ServiceLocator.Register<IGridServices>(gridServices);
            var retrievedServices = ServiceLocator.Get<IGridServices>();
            
            // Assert
            Assert.IsNotNull(retrievedServices);
            Assert.AreEqual(gridServices, retrievedServices);
        }
        
        [Test]
        public void ServiceLocator_RegisterAndGet_MultipleInterfaces_WorksCorrectly()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var gridController = mockGridController.Object;
            var gridRenderer = mockGridRenderer.Object;
            
            // Act
            ServiceLocator.Register<IGridState>(gridState);
            ServiceLocator.Register<IGridController>(gridController);
            ServiceLocator.Register<IGridRenderer>(gridRenderer);
            
            var retrievedState = ServiceLocator.Get<IGridState>();
            var retrievedController = ServiceLocator.Get<IGridController>();
            var retrievedRenderer = ServiceLocator.Get<IGridRenderer>();
            
            // Assert
            Assert.AreEqual(gridState, retrievedState);
            Assert.AreEqual(gridController, retrievedController);
            Assert.AreEqual(gridRenderer, retrievedRenderer);
        }
        
        #endregion
        
        #region Event System Tests
        
        [Test]
        public void GridState_Events_FireCorrectly()
        {
            // Arrange
            var gridState = mockGridState.Object;
            var testUnit = new GameObject("TestUnit");
            var position = new Vector2Int(1, 1);
            var newPosition = new Vector2Int(2, 2);
            
            bool unitPlacedFired = false;
            bool unitMovedFired = false;
            bool unitRemovedFired = false;
            
            // Subscribe to events
            gridState.OnUnitPlaced += (pos, unit) => unitPlacedFired = true;
            gridState.OnUnitMoved += (unit, oldPos, newPos) => unitMovedFired = true;
            gridState.OnUnitRemoved += (pos, unit) => unitRemovedFired = true;
            
            // Act - Trigger mock events
            mockGridState.Raise(x => x.OnUnitPlaced += null, position, testUnit);
            mockGridState.Raise(x => x.OnUnitMoved += null, testUnit, position, newPosition);
            mockGridState.Raise(x => x.OnUnitRemoved += null, position, testUnit);
            
            // Assert
            Assert.IsTrue(unitPlacedFired);
            Assert.IsTrue(unitMovedFired);
            Assert.IsTrue(unitRemovedFired);
            
            // Cleanup
            Object.DestroyImmediate(testUnit);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Test implementation of IGridDependent for testing purposes
    /// </summary>
    public class TestGridDependentComponent : MonoBehaviour, IGridDependent
    {
        public bool IsInitialized { get; private set; }
        public IGridServices GridServices { get; private set; }
        
        public void Initialize(IGridServices gridServices)
        {
            if (gridServices != null)
            {
                GridServices = gridServices;
                IsInitialized = true;
            }
            else
            {
                IsInitialized = false;
            }
        }
    }
}
 */ 