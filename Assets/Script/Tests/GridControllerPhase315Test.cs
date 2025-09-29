using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using Game.Components;
using Game.Data;
using Game.Services;
using Game.Interfaces;
using Game.Core;
using NUnit.Framework;

namespace Tests
{
    /// <summary>
    /// Phase 3.15: GridController 연동 테스트
    /// TargetRange 거리 계산 및 SpawnValidator 통합 검증
    /// </summary>
    public class GridControllerPhase315Test : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 6;

        // Test Dependencies
        private IGridController gridController;
        private ISpawnValidator spawnValidator;
        private MockGridState mockGridState;

        // Test Data
        private CardData testPlayerCard;
        private CardData testEnemyCard;
        private CardData testLongRangeCard;

        #region Unity Lifecycle

        void Start()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[GridControllerPhase315Test] 테스트 시작");
                StartCoroutine(RunAllTests());
            }
        }

        #endregion

        #region Test Setup

        private IEnumerator RunAllTests()
        {
            yield return StartCoroutine(SetupTestEnvironment());

            if (mockGridState == null || gridController == null)
            {
                Debug.LogError("[GridControllerPhase315Test] 테스트 환경 설정 실패");
                yield break;
            }

            // Phase 3.15 핵심 테스트들
            yield return StartCoroutine(TestPlayerBasePositionCalculation());
            yield return StartCoroutine(TestEnemyBasePositionCalculation());
            yield return StartCoroutine(TestTargetRangeValidation());
            yield return StartCoroutine(TestSpawnValidatorIntegration());
            yield return StartCoroutine(TestPositionsWithinRange());

            Debug.Log("[GridControllerPhase315Test] 모든 테스트 완료");
        }

        private IEnumerator SetupTestEnvironment()
        {
            Debug.Log("[GridControllerPhase315Test] 테스트 환경 설정 중...");

            // MockGridState 생성
            mockGridState = new MockGridState(gridWidth, gridHeight);

            // GridController 생성 및 초기화
            gridController = new GridController(mockGridState);

            // 테스트 유닛들 배치
            SetupTestUnits();

            // 테스트용 카드 데이터 생성
            SetupTestCards();

            Debug.Log("[GridControllerPhase315Test] 테스트 환경 설정 완료");
            yield return null;
        }

        private void SetupTestUnits()
        {
            // 플레이어 유닛들 (왼쪽 열에 배치)
            var playerUnit1 = CreateMockUnit("PlayerUnit1", TeamType.Player);
            var playerUnit2 = CreateMockUnit("PlayerUnit2", TeamType.Player);

            mockGridState.SetUnitPosition(playerUnit1, new Vector2Int(0, 2));
            mockGridState.SetUnitPosition(playerUnit2, new Vector2Int(0, 4));

            // 적군 유닛들 (오른쪽 열에 배치)
            var enemyUnit1 = CreateMockUnit("EnemyUnit1", TeamType.Enemy);
            var enemyUnit2 = CreateMockUnit("EnemyUnit2", TeamType.Enemy);

            mockGridState.SetUnitPosition(enemyUnit1, new Vector2Int(gridWidth - 1, 1));
            mockGridState.SetUnitPosition(enemyUnit2, new Vector2Int(gridWidth - 1, 3));

            Debug.Log($"[GridControllerPhase315Test] 테스트 유닛 배치 완료 - Player: (0,2), (0,4) Enemy: ({gridWidth-1},1), ({gridWidth-1},3)");
        }

        private GameObject CreateMockUnit(string name, TeamType team)
        {
            var unit = new GameObject(name);
            var teamComponent = unit.AddComponent<MockTeamComponent>();
            teamComponent.Team = team;
            return unit;
        }

        private void SetupTestCards()
        {
            // 플레이어용 단거리 카드 (TargetRange: 2)
            testPlayerCard = ScriptableObject.CreateInstance<CardData>();
            SetCardDataFields(testPlayerCard, "Player Short Range", CardData.CardType.Spell, 2, CardData.TargetType.Ground, 2);

            // 적군용 중거리 카드 (TargetRange: 4)
            testEnemyCard = ScriptableObject.CreateInstance<CardData>();
            SetCardDataFields(testEnemyCard, "Enemy Medium Range", CardData.CardType.Spell, 3, CardData.TargetType.Ground, 4);

            // 장거리 카드 (TargetRange: -1, 무제한)
            testLongRangeCard = ScriptableObject.CreateInstance<CardData>();
            SetCardDataFields(testLongRangeCard, "Long Range Unlimited", CardData.CardType.Spell, 5, CardData.TargetType.Ground, -1);

            Debug.Log("[GridControllerPhase315Test] 테스트 카드 생성 완료");
        }

        private void SetCardDataFields(CardData card, string name, CardData.CardType type, int manaCost, CardData.TargetType targetType, int targetRange)
        {
            // Reflection을 사용하여 private 필드 설정
            var cardNameField = typeof(CardData).GetField("cardName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cardTypeField = typeof(CardData).GetField("cardType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var manaCostField = typeof(CardData).GetField("manaCost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetTypeField = typeof(CardData).GetField("targetType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetRangeField = typeof(CardData).GetField("targetRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            cardNameField?.SetValue(card, name);
            cardTypeField?.SetValue(card, type);
            manaCostField?.SetValue(card, manaCost);
            targetTypeField?.SetValue(card, targetType);
            targetRangeField?.SetValue(card, targetRange);
        }

        #endregion

        #region Phase 3.15 Tests

        private IEnumerator TestPlayerBasePositionCalculation()
        {
            Debug.Log("[GridControllerPhase315Test] === 플레이어 기준점 계산 테스트 ===");

            Vector2Int playerBase = gridController.GetPlayerBasePosition();
            Debug.Log($"플레이어 기준점: {playerBase}");

            // 플레이어 유닛이 (0,2)와 (0,4)에 있으므로 기준점은 (0,2) 또는 (0,4) 중 하나여야 함
            bool isValidPlayerBase = (playerBase.x == 0) && (playerBase.y == 2 || playerBase.y == 4);

            if (isValidPlayerBase)
            {
                Debug.Log("✅ 플레이어 기준점 계산 성공");
            }
            else
            {
                Debug.LogError($"❌ 플레이어 기준점 계산 실패 - 예상: (0,2) 또는 (0,4), 실제: {playerBase}");
            }

            yield return null;
        }

        private IEnumerator TestEnemyBasePositionCalculation()
        {
            Debug.Log("[GridControllerPhase315Test] === 적군 기준점 계산 테스트 ===");

            Vector2Int enemyBase = gridController.GetEnemyBasePosition();
            Debug.Log($"적군 기준점: {enemyBase}");

            // 적군 유닛이 (7,1)과 (7,3)에 있으므로 기준점은 (7,1) 또는 (7,3) 중 하나여야 함
            bool isValidEnemyBase = (enemyBase.x == gridWidth - 1) && (enemyBase.y == 1 || enemyBase.y == 3);

            if (isValidEnemyBase)
            {
                Debug.Log("✅ 적군 기준점 계산 성공");
            }
            else
            {
                Debug.LogError($"❌ 적군 기준점 계산 실패 - 예상: ({gridWidth-1},1) 또는 ({gridWidth-1},3), 실제: {enemyBase}");
            }

            yield return null;
        }

        private IEnumerator TestTargetRangeValidation()
        {
            Debug.Log("[GridControllerPhase315Test] === TargetRange 검증 테스트 ===");

            // 테스트 위치들 정의
            var testPositions = new List<Vector2Int>
            {
                new Vector2Int(1, 2), // 플레이어 기준 거리 1
                new Vector2Int(2, 2), // 플레이어 기준 거리 2
                new Vector2Int(3, 2), // 플레이어 기준 거리 3
                new Vector2Int(4, 2), // 중간 지점
                new Vector2Int(5, 2), // 적군 기준 거리 2-3
                new Vector2Int(6, 2), // 적군 기준 거리 1-2
            };

            // 플레이어 카드 테스트 (TargetRange: 2)
            Debug.Log("-- 플레이어 카드 (TargetRange: 2) 테스트 --");
            foreach (var pos in testPositions)
            {
                bool isValid = gridController.ValidateCardTargetRange(testPlayerCard, pos, true);
                int distance = gridController.GetDistanceFromPlayerBase(pos);
                Debug.Log($"위치 {pos}: 거리={distance}, 유효={isValid}");
            }

            // 적군 카드 테스트 (TargetRange: 4)
            Debug.Log("-- 적군 카드 (TargetRange: 4) 테스트 --");
            foreach (var pos in testPositions)
            {
                bool isValid = gridController.ValidateCardTargetRange(testEnemyCard, pos, false);
                int distance = gridController.GetDistanceFromEnemyBase(pos);
                Debug.Log($"위치 {pos}: 거리={distance}, 유효={isValid}");
            }

            // 무제한 거리 카드 테스트 (TargetRange: -1)
            Debug.Log("-- 무제한 거리 카드 (TargetRange: -1) 테스트 --");
            bool unlimitedValid = gridController.ValidateCardTargetRange(testLongRangeCard, new Vector2Int(10, 10), true);
            Debug.Log($"무제한 거리 카드 검증 결과: {unlimitedValid} (true여야 함)");

            yield return null;
        }

        private IEnumerator TestSpawnValidatorIntegration()
        {
            Debug.Log("[GridControllerPhase315Test] === SpawnValidator 통합 테스트 ===");

            // SpawnValidator는 실제 게임 환경에서 테스트해야 하므로
            // 여기서는 GridController 메서드들이 올바르게 노출되었는지만 확인

            // GridController 메서드 존재 확인
            bool hasPlayerBaseMethod = gridController.GetType().GetMethod("GetPlayerBasePosition") != null;
            bool hasEnemyBaseMethod = gridController.GetType().GetMethod("GetEnemyBasePosition") != null;
            bool hasValidateMethod = gridController.GetType().GetMethod("ValidateCardTargetRange") != null;
            bool hasDistanceMethod = gridController.GetType().GetMethod("GetDistanceFromPlayerBase") != null;

            Debug.Log($"GetPlayerBasePosition 메서드 존재: {hasPlayerBaseMethod}");
            Debug.Log($"GetEnemyBasePosition 메서드 존재: {hasEnemyBaseMethod}");
            Debug.Log($"ValidateCardTargetRange 메서드 존재: {hasValidateMethod}");
            Debug.Log($"GetDistanceFromPlayerBase 메서드 존재: {hasDistanceMethod}");

            bool allMethodsExist = hasPlayerBaseMethod && hasEnemyBaseMethod && hasValidateMethod && hasDistanceMethod;

            if (allMethodsExist)
            {
                Debug.Log("✅ SpawnValidator 통합에 필요한 모든 메서드가 존재합니다");
            }
            else
            {
                Debug.LogError("❌ SpawnValidator 통합에 필요한 메서드가 누락되었습니다");
            }

            yield return null;
        }

        private IEnumerator TestPositionsWithinRange()
        {
            Debug.Log("[GridControllerPhase315Test] === 범위 내 위치 검색 테스트 ===");

            // 플레이어 기준 거리 2 내의 위치들
            var playerPositions = gridController.GetPositionsWithinRange(2, true, true);
            Debug.Log($"플레이어 기준 거리 2 내 위치 개수: {playerPositions.Count}");

            // 적군 기준 거리 3 내의 위치들
            var enemyPositions = gridController.GetPositionsWithinRange(3, false, true);
            Debug.Log($"적군 기준 거리 3 내 위치 개수: {enemyPositions.Count}");

            // 위치들이 실제로 거리 조건을 만족하는지 검증
            Vector2Int playerBase = gridController.GetPlayerBasePosition();
            bool playerPositionsValid = true;
            foreach (var pos in playerPositions)
            {
                int distance = GridController.CalculateManhattanDistance(playerBase, pos);
                if (distance > 2)
                {
                    Debug.LogError($"❌ 플레이어 기준 위치 {pos}가 거리 조건을 위반: {distance} > 2");
                    playerPositionsValid = false;
                }
            }

            if (playerPositionsValid)
            {
                Debug.Log("✅ 플레이어 기준 범위 내 위치 검색 성공");
            }

            yield return null;
        }

        #endregion

        #region Mock Classes

        /// <summary>
        /// 테스트용 MockGridState 구현
        /// </summary>
        private class MockGridState : IGridState
        {
            private readonly Vector2Int gridSize;
            private readonly Dictionary<Vector2Int, GameObject> units;
            private readonly HashSet<Vector2Int> blockedPositions;

            public Vector2Int GridSize => gridSize;
            public float TileSize => 1f;

            public event System.Action<GameObject, Vector2Int, Vector2Int> OnUnitMoved;
            public event System.Action<Vector2Int, GameObject> OnUnitPlaced;
            public event System.Action<Vector2Int, GameObject> OnUnitRemoved;

            public MockGridState(int width, int height)
            {
                gridSize = new Vector2Int(width, height);
                units = new Dictionary<Vector2Int, GameObject>();
                blockedPositions = new HashSet<Vector2Int>();
            }

            public bool IsValidPosition(Vector2Int gridPosition)
            {
                return gridPosition.x >= 0 && gridPosition.x < gridSize.x &&
                       gridPosition.y >= 0 && gridPosition.y < gridSize.y;
            }

            public bool IsPositionOccupied(Vector2Int gridPosition)
            {
                return units.ContainsKey(gridPosition);
            }

            public bool IsPositionBlocked(Vector2Int gridPosition)
            {
                return blockedPositions.Contains(gridPosition);
            }

            public GameObject GetUnitAtPosition(Vector2Int gridPosition)
            {
                units.TryGetValue(gridPosition, out GameObject unit);
                return unit;
            }

            public Vector2Int GetUnitPosition(GameObject unit)
            {
                foreach (var kvp in units)
                {
                    if (kvp.Value == unit)
                        return kvp.Key;
                }
                return new Vector2Int(-1, -1);
            }

            public bool TryGetUnitPosition(GameObject unit, out Vector2Int position)
            {
                position = GetUnitPosition(unit);
                return position != new Vector2Int(-1, -1);
            }

            public bool SetUnitPosition(GameObject unit, Vector2Int newPosition)
            {
                if (!IsValidPosition(newPosition)) return false;

                // 기존 위치에서 제거
                RemoveUnit(unit);

                // 새 위치에 설정
                units[newPosition] = unit;
                OnUnitPlaced?.Invoke(newPosition, unit);
                return true;
            }

            public bool RemoveUnit(GameObject unit)
            {
                Vector2Int oldPosition = GetUnitPosition(unit);
                if (oldPosition != new Vector2Int(-1, -1))
                {
                    units.Remove(oldPosition);
                    OnUnitRemoved?.Invoke(oldPosition, unit);
                    return true;
                }
                return false;
            }

            public Vector3 GridToWorldPosition(Vector2Int gridPosition)
            {
                return new Vector3(gridPosition.x * TileSize, 0, gridPosition.y * TileSize);
            }

            public Vector2Int WorldToGridPosition(Vector3 worldPosition)
            {
                return new Vector2Int(
                    Mathf.RoundToInt(worldPosition.x / TileSize),
                    Mathf.RoundToInt(worldPosition.z / TileSize)
                );
            }

            public void SetTileBlocked(Vector2Int position, bool blocked)
            {
                if (blocked)
                    blockedPositions.Add(position);
                else
                    blockedPositions.Remove(position);
            }

            public List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeOccupied = true)
            {
                var positions = new List<Vector2Int>();

                for (int x = center.x - range; x <= center.x + range; x++)
                {
                    for (int y = center.y - range; y <= center.y + range; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);

                        if (!IsValidPosition(pos)) continue;
                        if (!includeOccupied && IsPositionOccupied(pos)) continue;

                        int distance = Mathf.Abs(pos.x - center.x) + Mathf.Abs(pos.y - center.y);
                        if (distance <= range)
                        {
                            positions.Add(pos);
                        }
                    }
                }

                return positions;
            }

            public List<GameObject> GetUnitsInRange(Vector2Int center, int range)
            {
                var unitsInRange = new List<GameObject>();
                var positions = GetPositionsInRange(center, range, true);

                foreach (var pos in positions)
                {
                    var unit = GetUnitAtPosition(pos);
                    if (unit != null)
                    {
                        unitsInRange.Add(unit);
                    }
                }

                return unitsInRange;
            }
        }

        /// <summary>
        /// 테스트용 MockTeamComponent 구현
        /// </summary>
        private class MockTeamComponent : MonoBehaviour, ITeamComponent
        {
            public TeamType Team { get; set; } = TeamType.None;
            public string TeamName => Team.ToString();
            public Color TeamColor => Team == TeamType.Player ? Color.blue : Color.red;

            public event System.Action<TeamType, TeamType> OnTeamChanged;

            public TeamRelation GetRelationTo(ITeamComponent other)
            {
                if (other == null) return TeamRelation.Neutral;
                if (other == this) return TeamRelation.Self;

                if (Team == TeamType.Player && other.Team == TeamType.Enemy) return TeamRelation.Enemy;
                if (Team == TeamType.Enemy && other.Team == TeamType.Player) return TeamRelation.Enemy;
                if (Team == other.Team && Team != TeamType.None) return TeamRelation.Ally;

                return TeamRelation.Neutral;
            }

            public bool IsSameTeam(ITeamComponent other)
            {
                return other != null && Team == other.Team && Team != TeamType.None;
            }

            public bool IsEnemy(ITeamComponent other)
            {
                return GetRelationTo(other) == TeamRelation.Enemy;
            }

            public bool IsAlly(ITeamComponent other)
            {
                var relation = GetRelationTo(other);
                return relation == TeamRelation.Ally || relation == TeamRelation.Self;
            }
        }

        #endregion

        #region Cleanup

        void OnDestroy()
        {
            // 테스트 데이터 정리
            if (testPlayerCard != null) DestroyImmediate(testPlayerCard);
            if (testEnemyCard != null) DestroyImmediate(testEnemyCard);
            if (testLongRangeCard != null) DestroyImmediate(testLongRangeCard);
        }

        #endregion
    }
}