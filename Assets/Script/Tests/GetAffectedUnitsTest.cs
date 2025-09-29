using UnityEngine;
using System.Collections.Generic;
using Game.Components;
using Game.Card.Effects;
using Game.Core;
using Game.Interfaces;

namespace Game.Tests
{
    /// <summary>
    /// Phase 2.12: GetAffectedUnits() 메서드 테스트 스크립트
    /// GridController의 새로운 GetAffectedUnits 메서드가 올바르게 동작하는지 검증합니다.
    /// </summary>
    public class GetAffectedUnitsTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private GridController gridController;
        [SerializeField] private GameObject testUnitPrefab;

        [Header("테스트 매개변수")]
        [SerializeField] private Vector2Int testTargetPosition = new Vector2Int(5, 5);
        [SerializeField] private AffectedType testAffectedType = AffectedType.Enemy;
        [SerializeField] private int testAffectedRange = 1;
        [SerializeField] private int testOriginPlayerId = 0;

        private List<GameObject> testUnits = new List<GameObject>();

        void Start()
        {
            Debug.Log("GetAffectedUnitsTest: 테스트 시작");

            if (gridController == null)
            {
                Debug.LogError("GetAffectedUnitsTest: GridController가 설정되지 않았습니다.");
                return;
            }

            // 자동 테스트 실행 (필요시 주석 처리)
            // RunAutomaticTests();
        }

        [ContextMenu("Run All Tests")]
        public void RunAutomaticTests()
        {
            Debug.Log("=== GetAffectedUnits() 자동 테스트 시작 ===");

            SetupTestUnits();
            TestSingleTargetAffectedRange();
            TestRangeTargetAffectedRange();
            TestAffectedTypeFiltering();
            TestEmptyPositions();
            CleanupTestUnits();

            Debug.Log("=== GetAffectedUnits() 자동 테스트 완료 ===");
        }

        /// <summary>
        /// 테스트용 유닛들을 배치합니다.
        /// </summary>
        private void SetupTestUnits()
        {
            Debug.Log("GetAffectedUnitsTest: 테스트 유닛 배치 시작");

            if (testUnitPrefab == null)
            {
                Debug.LogWarning("GetAffectedUnitsTest: testUnitPrefab이 null입니다. 기본 GameObject 생성");
            }

            // 테스트 유닛들을 다양한 위치에 배치
            var testPositions = new List<(Vector2Int pos, TeamType team)>
            {
                (testTargetPosition, TeamType.Enemy), // 중심
                (testTargetPosition + Vector2Int.up, TeamType.Enemy), // 위
                (testTargetPosition + Vector2Int.down, TeamType.Player), // 아래
                (testTargetPosition + Vector2Int.left, TeamType.Enemy), // 왼쪽
                (testTargetPosition + Vector2Int.right, TeamType.Player), // 오른쪽
                (testTargetPosition + new Vector2Int(2, 2), TeamType.Enemy), // 범위 밖
            };

            foreach (var (pos, team) in testPositions)
            {
                var testUnit = CreateTestUnit($"TestUnit_{pos.x}_{pos.y}", pos, team);
                if (testUnit != null)
                {
                    testUnits.Add(testUnit);
                }
            }

            Debug.Log($"GetAffectedUnitsTest: {testUnits.Count}개 테스트 유닛 배치 완료");
        }

        /// <summary>
        /// 테스트용 유닛을 생성합니다.
        /// </summary>
        private GameObject CreateTestUnit(string unitName, Vector2Int gridPos, TeamType team)
        {
            GameObject unit;

            if (testUnitPrefab != null)
            {
                unit = Instantiate(testUnitPrefab);
            }
            else
            {
                unit = new GameObject();
            }

            unit.name = unitName;

            // 팀 컴포넌트 추가
            var teamComponent = unit.GetComponent<ITeamComponent>();
            if (teamComponent == null)
            {
                var testTeamComp = unit.AddComponent<TestTeamComponent>();
                testTeamComp.Team = team;
            }

            // GridController에 유닛 등록 (실제 구현에 따라 방법이 다를 수 있음)
            // TODO: 실제 프로젝트의 유닛 배치 로직에 맞게 수정
            var worldPos = gridController.GridToWorldPosition(gridPos);
            unit.transform.position = worldPos;

            Debug.Log($"GetAffectedUnitsTest: 유닛 생성 - {unitName} at {gridPos} (Team: {team})");

            return unit;
        }

        /// <summary>
        /// 단일 대상 효과 테스트 (AffectedRange = 0)
        /// </summary>
        [ContextMenu("Test Single Target")]
        public void TestSingleTargetAffectedRange()
        {
            Debug.Log("--- 테스트 1: 단일 대상 (AffectedRange = 0) ---");

            var result = gridController.GetAffectedUnits(
                testTargetPosition,
                AffectedType.Enemy,
                0, // 단일 대상
                testOriginPlayerId
            );

            Debug.Log($"단일 대상 테스트 결과: {result.Count}개 유닛 발견");
            foreach (var unit in result)
            {
                Debug.Log($"  - {unit.name}");
            }

            // 검증: 정확히 중심 위치의 Enemy 유닛만 나와야 함
            bool testPassed = result.Count == 1 && result[0].name.Contains($"{testTargetPosition.x}_{testTargetPosition.y}");
            Debug.Log($"단일 대상 테스트: {(testPassed ? "PASS" : "FAIL")}");
        }

        /// <summary>
        /// 범위 효과 테스트 (AffectedRange = 1)
        /// </summary>
        [ContextMenu("Test Range Target")]
        public void TestRangeTargetAffectedRange()
        {
            Debug.Log("--- 테스트 2: 범위 효과 (AffectedRange = 1) ---");

            var result = gridController.GetAffectedUnits(
                testTargetPosition,
                AffectedType.Enemy,
                1, // 범위 1
                testOriginPlayerId
            );

            Debug.Log($"범위 효과 테스트 결과: {result.Count}개 유닛 발견");
            foreach (var unit in result)
            {
                Debug.Log($"  - {unit.name}");
            }

            // 검증: 범위 1 내의 Enemy 유닛들만 나와야 함 (중심, 위, 왼쪽)
            Debug.Log($"범위 효과 테스트: Enemy 유닛 {result.Count}개 발견");
        }

        /// <summary>
        /// AffectedType 필터링 테스트
        /// </summary>
        [ContextMenu("Test Affected Type Filtering")]
        public void TestAffectedTypeFiltering()
        {
            Debug.Log("--- 테스트 3: AffectedType 필터링 ---");

            // Enemy 유닛만 테스트
            var enemyResult = gridController.GetAffectedUnits(
                testTargetPosition,
                AffectedType.Enemy,
                1,
                testOriginPlayerId
            );

            // Ally 유닛만 테스트
            var allyResult = gridController.GetAffectedUnits(
                testTargetPosition,
                AffectedType.Ally,
                1,
                testOriginPlayerId
            );

            // 모든 유닛 테스트
            var anyResult = gridController.GetAffectedUnits(
                testTargetPosition,
                AffectedType.Any,
                1,
                testOriginPlayerId
            );

            Debug.Log($"Enemy 타겟팅: {enemyResult.Count}개 유닛");
            Debug.Log($"Ally 타겟팅: {allyResult.Count}개 유닛");
            Debug.Log($"Any 타겟팅: {anyResult.Count}개 유닛");

            // 검증: Any는 Enemy + Ally의 합과 같거나 커야 함
            bool testPassed = anyResult.Count >= enemyResult.Count + allyResult.Count;
            Debug.Log($"AffectedType 필터링 테스트: {(testPassed ? "PASS" : "FAIL")}");
        }

        /// <summary>
        /// 빈 위치 테스트
        /// </summary>
        [ContextMenu("Test Empty Positions")]
        public void TestEmptyPositions()
        {
            Debug.Log("--- 테스트 4: 빈 위치 테스트 ---");

            var emptyPos = new Vector2Int(20, 20); // 유닛이 없는 위치
            var result = gridController.GetAffectedUnits(
                emptyPos,
                AffectedType.Any,
                2,
                testOriginPlayerId
            );

            Debug.Log($"빈 위치 테스트 결과: {result.Count}개 유닛 발견");

            // 검증: 빈 위치에서는 0개가 나와야 함
            bool testPassed = result.Count == 0;
            Debug.Log($"빈 위치 테스트: {(testPassed ? "PASS" : "FAIL")}");
        }

        /// <summary>
        /// 디버그 정보 출력 테스트
        /// </summary>
        [ContextMenu("Test Debug Output")]
        public void TestDebugOutput()
        {
            Debug.Log("--- 테스트 5: 디버그 출력 ---");

            gridController.DebugLogAffectedUnits(
                testTargetPosition,
                AffectedType.Any,
                1,
                testOriginPlayerId
            );
        }

        /// <summary>
        /// 테스트용 유닛들을 정리합니다.
        /// </summary>
        private void CleanupTestUnits()
        {
            Debug.Log("GetAffectedUnitsTest: 테스트 유닛 정리");

            foreach (var unit in testUnits)
            {
                if (unit != null)
                {
                    DestroyImmediate(unit);
                }
            }

            testUnits.Clear();
            Debug.Log("GetAffectedUnitsTest: 테스트 유닛 정리 완료");
        }

        void OnDestroy()
        {
            CleanupTestUnits();
        }
    }

    /// <summary>
    /// 테스트용 팀 컴포넌트
    /// </summary>
    public class TestTeamComponent : MonoBehaviour, ITeamComponent
    {
        [SerializeField] private TeamType team = TeamType.None;

        public TeamType Team
        {
            get => team;
            set => team = value;
        }

        public string TeamName => team.ToString();
        public Color TeamColor => team == TeamType.Player ? Color.blue : Color.red;

        public event System.Action<TeamType, TeamType> OnTeamChanged;

        public TeamRelation GetRelationTo(ITeamComponent other)
        {
            if (other == null) return TeamRelation.Neutral;
            if (other == this) return TeamRelation.Self;

            return TeamRelationMatrix.GetRelation(Team, other.Team);
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
}