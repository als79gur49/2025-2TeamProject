using Game.Core;
using Game.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class UnitGameSerivceTest : MonoBehaviour
    {
        [SerializeField]
        private Unit unitPrefab;
        [SerializeField]
        private Vector2Int startPosition = new Vector2Int(1, 1);

        private Unit unitObject;
        private void Start()
        {
            unitObject = Instantiate(unitPrefab, new Vector3(0,0,0), Quaternion.identity);
            
            var gridManager = ServiceLocator.Get<IGridManager>();
            var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
            if(gridManager != null && gameServiceManager != null )
            {
                unitObject.Init(gridManager, gameServiceManager);
                unitObject?.SetPosition(startPosition.x, startPosition.y);
                unitObject.OnTurnStart();
            }
        }
    }
}
