using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Interfaces;

namespace Game.Coordinators
{
    /// <summary>
    /// 유닛이 그리드에 처음 배치될 때 Unit.OnPlaced를 호출하여
    /// OnDeploy 트리거 기반 이펙트가 항상 한 번 실행되도록 보장하는 Coordinator입니다.
    /// </summary>
    public class UnitPlacementCoordinator : MonoBehaviour
    {
        private IGridManager gridManager;
        private bool isInitialized;

        // 안전장치: 동일 유닛에 대해 OnPlaced가 중복 호출되는 것을 방지
        private readonly HashSet<Unit> placedUnits = new HashSet<Unit>();

        /// <summary>
        /// ServiceLocator에서 IGridManager를 가져와 OnUnitPlaced 이벤트를 구독합니다.
        /// GameInitializer에서 호출됩니다.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            gridManager = ServiceLocator.Get<IGridManager>();
            if (gridManager == null)
            {
                Debug.LogError("[UnitPlacementCoordinator] IGridManager not found in ServiceLocator - cannot initialize");
                return;
            }

            gridManager.OnUnitPlaced += HandleUnitPlaced;
            isInitialized = true;

            Debug.Log("[UnitPlacementCoordinator] Initialized and subscribed to IGridManager.OnUnitPlaced");
        }

        private void OnDestroy()
        {
            if (isInitialized && gridManager != null)
            {
                gridManager.OnUnitPlaced -= HandleUnitPlaced;
            }
        }

        private void HandleUnitPlaced(UnityEngine.Vector2Int position, GameObject unitObject)
        {
            if (unitObject == null) return;

            var unit = unitObject.GetComponent<Unit>();
            if (unit == null) return;

            // 이미 처리한 유닛이면 스킵
            if (placedUnits.Contains(unit))
            {
                return;
            }
            placedUnits.Add(unit);

            var controller = gridManager.GetGridController();
            if (controller == null)
            {
                Debug.LogWarning("[UnitPlacementCoordinator] GridController is null - cannot resolve Tile for unit placement");
                return;
            }

            var tile = controller.GetTileAtPosition(position);
            if (tile == null)
            {
                Debug.LogWarning($"[UnitPlacementCoordinator] No Tile found at position {position} - OnPlaced will not be called");
                return;
            }

            // 배치 시점 콜백 및 OnDeploy 트리거 실행
            unit.OnPlaced(tile);
            Debug.LogWarning("[UnitPlacementCoordinator] unit.OnPlaced() is called");
        }
    }
}

