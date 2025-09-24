using UnityEngine;
using Game.Interfaces;
using Game.Services;

namespace Game.Test
{
    /// <summary>
    /// Unit 이동 시 Tile 점유 상태가 정확히 동기화되는지 테스트
    /// GridState와 물리적 Tile 컴포넌트 간의 통합 검증
    /// </summary>
    public class TileOccupancyIntegrationTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool autoRunTest = true;
        [SerializeField] private float testInterval = 3f;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("테스트 대상")]
        [SerializeField] private Unit testUnit;
        [SerializeField] private Vector2Int startPosition = new Vector2Int(2, 2);
        [SerializeField] private Vector2Int[] testPositions = { 
            new Vector2Int(3, 2), 
            new Vector2Int(3, 3), 
            new Vector2Int(2, 3) 
        };
        
        // 테스트 상태
        private IGridManager gridManager;
        private int currentTestIndex = 0;
        private float nextTestTime;
        
        // 테스트 결과
        private int totalTests = 0;
        private int passedTests = 0;
        private int failedTests = 0;

        private void Start()
        {
            if (autoRunTest)
            {
                InitializeTest();
                nextTestTime = Time.time + 1f; // 1초 후 시작
            }
        }

        private void Update()
        {
            if (autoRunTest && Time.time >= nextTestTime && gridManager != null)
            {
                RunNextTest();
                nextTestTime = Time.time + testInterval;
            }
        }

        /// <summary>
        /// 테스트 초기화
        /// </summary>
        private void InitializeTest()
        {
            // GameServiceManager에서 GridManager 가져오기
            var gameServiceManager = GameServiceManager.Instance;
            if (gameServiceManager != null)
            {
                gridManager = gameServiceManager.GetService<IGridManager>();
                if (gridManager == null)
                {
                    Debug.LogError("[TileOccupancyIntegrationTest] GridManager not found in GameServiceManager");
                    return;
                }
            }
            else
            {
                Debug.LogError("[TileOccupancyIntegrationTest] GameServiceManager not found");
                return;
            }

            // 테스트 유닛이 없으면 자동으로 찾기
            if (testUnit == null)
            {
                testUnit = FindObjectOfType<Unit>();
                if (testUnit == null)
                {
                    Debug.LogError("[TileOccupancyIntegrationTest] No Unit found for testing");
                    return;
                }
            }

            Debug.Log($"[TileOccupancyIntegrationTest] ===== TILE OCCUPANCY INTEGRATION TEST STARTED =====");
            Debug.Log($"[TileOccupancyIntegrationTest] Test Unit: {testUnit.name}");
            Debug.Log($"[TileOccupancyIntegrationTest] Start Position: ({startPosition.x}, {startPosition.y})");
            
            // 시작 위치로 유닛 이동
            MoveUnitToPosition(startPosition);
            ValidateTileOccupancy(startPosition, testUnit.gameObject, "Initial Position");
        }

        /// <summary>
        /// 다음 테스트 실행
        /// </summary>
        private void RunNextTest()
        {
            if (currentTestIndex >= testPositions.Length)
            {
                // 모든 테스트 완료
                ShowTestResults();
                autoRunTest = false;
                return;
            }

            Vector2Int targetPosition = testPositions[currentTestIndex];
            Vector2Int previousPosition = gridManager.GetUnitPosition(testUnit.gameObject);
            
            Debug.Log($"[TileOccupancyIntegrationTest] ===== TEST {currentTestIndex + 1}/{testPositions.Length} =====");
            Debug.Log($"[TileOccupancyIntegrationTest] Moving unit from ({previousPosition.x}, {previousPosition.y}) to ({targetPosition.x}, {targetPosition.y})");

            // 이전 위치 검증
            ValidateTileOccupancy(previousPosition, testUnit.gameObject, $"Before Move Test {currentTestIndex + 1}");
            
            // 유닛 이동
            bool moveSuccess = MoveUnitToPosition(targetPosition);
            
            if (moveSuccess)
            {
                // 이동 후 검증
                ValidateTileOccupancy(targetPosition, testUnit.gameObject, $"After Move Test {currentTestIndex + 1} - New Position");
                ValidateTileOccupancy(previousPosition, null, $"After Move Test {currentTestIndex + 1} - Old Position");
            }
            else
            {
                Debug.LogError($"[TileOccupancyIntegrationTest] Move failed for test {currentTestIndex + 1}");
                failedTests++;
                totalTests++;
            }

            currentTestIndex++;
        }

        /// <summary>
        /// 유닛을 지정된 위치로 이동
        /// </summary>
        private bool MoveUnitToPosition(Vector2Int position)
        {
            if (gridManager.CanMoveUnit(testUnit.gameObject, position))
            {
                bool result = gridManager.MoveUnit(testUnit.gameObject, position);
                if (result)
                {
                    Debug.Log($"[TileOccupancyIntegrationTest] Successfully moved unit to ({position.x}, {position.y})");
                }
                else
                {
                    Debug.LogError($"[TileOccupancyIntegrationTest] Failed to move unit to ({position.x}, {position.y})");
                }
                return result;
            }
            else
            {
                Debug.LogError($"[TileOccupancyIntegrationTest] Cannot move unit to ({position.x}, {position.y}) - position not available");
                return false;
            }
        }

        /// <summary>
        /// 특정 위치의 Tile 점유 상태 검증
        /// </summary>
        private void ValidateTileOccupancy(Vector2Int position, GameObject expectedUnit, string testName)
        {
            totalTests++;
            bool testPassed = true;
            string errorMessage = "";

            // 1. GridState 데이터 검증
            GameObject gridStateUnit = gridManager.GetUnitAtPosition(position);
            bool gridStateOccupied = gridManager.IsPositionOccupied(position);

            // 2. 물리적 Tile 컴포넌트 검증
            GameObject tileObject = GameObject.Find($"Tile_{position.x}_{position.y}");
            Tile physicalTile = null;
            bool physicalTileOccupied = false;
            Unit physicalTileUnit = null;

            if (tileObject != null)
            {
                physicalTile = tileObject.GetComponent<Tile>();
                if (physicalTile != null)
                {
                    physicalTileOccupied = physicalTile.IsOccupied;
                    physicalTileUnit = physicalTile.OccupyingUnit;
                }
            }

            // 검증 로직
            if (expectedUnit == null)
            {
                // 빈 타일이어야 함
                if (gridStateOccupied)
                {
                    testPassed = false;
                    errorMessage += $"GridState shows occupied but should be empty. Unit: {gridStateUnit?.name}. ";
                }
                
                if (physicalTileOccupied)
                {
                    testPassed = false;
                    errorMessage += $"Physical Tile shows occupied but should be empty. Unit: {physicalTileUnit?.name}. ";
                }
            }
            else
            {
                // 특정 유닛이 점유하고 있어야 함
                if (!gridStateOccupied || gridStateUnit != expectedUnit)
                {
                    testPassed = false;
                    errorMessage += $"GridState mismatch. Expected: {expectedUnit.name}, Found: {gridStateUnit?.name}. ";
                }
                
                if (!physicalTileOccupied || physicalTileUnit == null || physicalTileUnit.gameObject != expectedUnit)
                {
                    testPassed = false;
                    errorMessage += $"Physical Tile mismatch. Expected: {expectedUnit.name}, Found: {physicalTileUnit?.gameObject.name}. ";
                }
            }

            // 결과 로깅
            if (testPassed)
            {
                passedTests++;
                if (enableDetailedLogging)
                {
                    Debug.Log($"[TileOccupancyIntegrationTest] ✅ PASS - {testName} at ({position.x}, {position.y})");
                }
            }
            else
            {
                failedTests++;
                Debug.LogError($"[TileOccupancyIntegrationTest] ❌ FAIL - {testName} at ({position.x}, {position.y}): {errorMessage}");
                
                // 상세 디버깅 정보
                Debug.LogError($"    GridState - Occupied: {gridStateOccupied}, Unit: {gridStateUnit?.name}");
                Debug.LogError($"    Physical Tile - Occupied: {physicalTileOccupied}, Unit: {physicalTileUnit?.gameObject.name}");
            }
        }

        /// <summary>
        /// 테스트 결과 요약
        /// </summary>
        private void ShowTestResults()
        {
            Debug.Log($"[TileOccupancyIntegrationTest] ===== TEST RESULTS SUMMARY =====");
            Debug.Log($"[TileOccupancyIntegrationTest] Total Tests: {totalTests}");
            Debug.Log($"[TileOccupancyIntegrationTest] Passed: {passedTests}");
            Debug.Log($"[TileOccupancyIntegrationTest] Failed: {failedTests}");
            
            float successRate = totalTests > 0 ? (float)passedTests / totalTests * 100f : 0f;
            Debug.Log($"[TileOccupancyIntegrationTest] Success Rate: {successRate:F1}%");
            
            if (failedTests == 0)
            {
                Debug.Log($"[TileOccupancyIntegrationTest] 🎉 ALL TESTS PASSED! Tile occupancy integration is working correctly.");
            }
            else
            {
                Debug.LogWarning($"[TileOccupancyIntegrationTest] ⚠️ {failedTests} test(s) failed. Please check the implementation.");
            }
        }

        /// <summary>
        /// 수동 테스트 실행 (Inspector에서 호출)
        /// </summary>
        [ContextMenu("Run Manual Test")]
        public void RunManualTest()
        {
            autoRunTest = false;
            currentTestIndex = 0;
            totalTests = 0;
            passedTests = 0;
            failedTests = 0;
            
            InitializeTest();
            
            // 모든 테스트 위치에 대해 즉시 테스트 실행
            for (int i = 0; i < testPositions.Length; i++)
            {
                currentTestIndex = i;
                RunNextTest();
            }
            
            ShowTestResults();
        }

        /// <summary>
        /// 현재 상태 디버그 출력
        /// </summary>
        [ContextMenu("Debug Current State")]
        public void DebugCurrentState()
        {
            if (gridManager == null || testUnit == null)
            {
                Debug.LogWarning("[TileOccupancyIntegrationTest] GridManager or TestUnit not initialized");
                return;
            }

            Vector2Int currentPos = gridManager.GetUnitPosition(testUnit.gameObject);
            Debug.Log($"[TileOccupancyIntegrationTest] Current Unit Position: ({currentPos.x}, {currentPos.y})");
            
            ValidateTileOccupancy(currentPos, testUnit.gameObject, "Current State Debug");
        }
    }
}