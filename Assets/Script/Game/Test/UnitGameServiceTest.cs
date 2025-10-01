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

            if (gridManager == null || gameServiceManager == null)
            {
                Debug.LogError("[UnitGameServiceTest] GridManager 또는 GameServiceManager를 찾을 수 없습니다.");
                return;
            }

            // Player 유닛 소환 (외부 등록 패턴 적용)
            foreach (var playerStartPosition in playerStartPositions)
            {
                if (playerUnitPrefab == null || playerUnitData == null) break;

                SpawnUnit(playerUnitPrefab, playerUnitData, playerStartPosition, true, gridManager, gameServiceManager);
            }

            // Enemy 유닛 소환 (외부 등록 패턴 적용)
            foreach (var enemyStartPosition in enemyStartPositions)
            {
                if (enemyUnitPrefab == null || enemyUnitData == null) break;

                SpawnUnit(enemyUnitPrefab, enemyUnitData, enemyStartPosition, false, gridManager, gameServiceManager);
            }
        }

        /// <summary>
        /// 유닛 소환 및 외부 등록 패턴 적용 (SummonEffect와 동일한 패턴)
        /// </summary>
        private void SpawnUnit(Unit prefab, UnitData unitData, Vector2Int position, bool isPlayer, IGridManager gridManager, IGameServiceManager gameServiceManager)
        {
            // 1. 유닛 인스턴스화
            Quaternion rotation = isPlayer ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
            Unit unit = Instantiate(prefab, Vector3.zero, rotation);

            // 2. Unit 초기화 (등록은 Unit 내부에서 하지 않음)
            unit.Init(unitData, position, isPlayer);

            // 3. GridController.MoveUnit()으로 GridState + Transform 위치 동시 업데이트
            var gridController = gridManager.GetGridController();
            if (gridController != null)
            {
                bool moved = gridController.MoveUnit(unit.gameObject, position);
                if (!moved)
                {
                    Debug.LogError($"[UnitGameServiceTest] {position}에 유닛 배치 실패 - GridController.MoveUnit() failed");
                    Destroy(unit.gameObject);
                    return;
                }
            }
            else
            {
                Debug.LogError("[UnitGameServiceTest] GridController를 찾을 수 없습니다.");
                Destroy(unit.gameObject);
                return;
            }

            // 4. GameServiceManager를 통한 UnitService 등록 (외부 등록)
            gameServiceManager.RegisterUnit(unit);

            // 5. currentTile 설정 (GridController를 통해 Tile 찾기)
            var tile = gridController.GetTileAtPosition(position);
            if (tile != null)
            {
                unit.SetCurrentTile(tile);
                Debug.Log($"[UnitGameServiceTest] {unit.name}의 currentTile 설정 완료 → {tile.name} ({position.x}, {position.y})");
            }
            else
            {
                Debug.LogWarning($"[UnitGameServiceTest] 위치 ({position.x}, {position.y})에서 Tile을 찾을 수 없습니다.");
            }

            // 6. 턴 시작
            unit.OnTurnStart();

            Debug.Log($"[UnitGameServiceTest] {(isPlayer ? "Player" : "Enemy")} 유닛 소환 완료: {unit.name} at ({position.x}, {position.y})");
        }
    }
}
