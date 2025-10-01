using Game.Core;
using Game.Data;
using Game.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class UnitGameSerivceTest : MonoBehaviour
    {
        [SerializeField]
        private Unit playerUnitPrefab;
        [SerializeField]
        private Unit enemyUnitPrefab;
        [SerializeField]
        private UnitData playerUnitData;
        [SerializeField]
        private UnitData enemyUnitData;
        [SerializeField]
        private List<Vector2Int> playerStartPositions;
        [SerializeField]
        private List<Vector2Int> enemyStartPositions;

        private Unit unitObject;
        private void Start()
        {

            var gridManager = ServiceLocator.Get<IGridManager>();
            var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();

            if (gridManager == null && gameServiceManager == null) return;

            foreach (var playerStartPosition in playerStartPositions)
            {
                if (playerUnitPrefab == null || playerUnitData == null) break;

                unitObject = Instantiate(playerUnitPrefab, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));

                unitObject.Init(playerUnitData, playerStartPosition, true);

                unitObject?.SetPosition(playerStartPosition.x, playerStartPosition.y);
                unitObject.OnTurnStart();
            }
            foreach (var enemyStartPosition in enemyStartPositions)
            {
                if (enemyUnitPrefab == null || enemyUnitData == null) break;

                unitObject = Instantiate(enemyUnitPrefab, new Vector3(0, 0, 0), Quaternion.Euler(0, 180, 0));

                unitObject.Init(enemyUnitData, enemyStartPosition, false);

                unitObject?.SetPosition(enemyStartPosition.x, enemyStartPosition.y);
                unitObject.OnTurnStart();
            }
        }
    }
}
