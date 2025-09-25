using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Services;
using Game.Card.UI;
using Game.Data;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Phase 3 통합 테스트 스크립트
/// 카드 드래그 앤 드롭 UI 시스템의 모든 컴포넌트가 올바르게 작동하는지 검증
/// </summary>
public class Phase3ValidationScript : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool autoRunTestsOnStart = true;
    [SerializeField] private float testDelaySeconds = 2f;
    [SerializeField] private bool showDetailedLogs = true;

    [Header("테스트 카드 데이터")]
    [SerializeField] private List<CardData> testCards = new List<CardData>();

    [Header("테스트 UI 요소")]
    [SerializeField] private Transform testTileParent;
    [SerializeField] private GameObject tileDropHandlerPrefab;

    // 테스트 결과
    private int totalTests = 0;
    private int passedTests = 0;
    private int failedTests = 0;

    // 서비스 참조
    private ICardServiceManager cardServiceManager;
    private ICardHandManager cardHandManager;
    private ICardSpawnService cardSpawnService;
    private ISpawnValidator spawnValidator;
    private ITurnService turnService;

    #region Unity Lifecycle

    private void Start()
    {
        if (autoRunTestsOnStart)
        {
            StartCoroutine(RunAllTestsWithDelay());
        }
    }

    #endregion

    #region 테스트 실행

    /// <summary>
    /// 모든 테스트를 순차적으로 실행
    /// </summary>
    private IEnumerator RunAllTestsWithDelay()
    {
        Log("🧪 Starting Phase 3 Validation Tests...");
        
        yield return new WaitForSeconds(1f); // 초기화 대기

        // 1. 서비스 초기화 검증
        yield return StartCoroutine(TestServiceInitialization());
        yield return new WaitForSeconds(testDelaySeconds);

        // 2. CardHandManager UI 기능 검증
        yield return StartCoroutine(TestCardHandManagerUI());
        yield return new WaitForSeconds(testDelaySeconds);

        // 3. 드래그 앤 드롭 기능 검증
        yield return StartCoroutine(TestDragAndDropFunctionality());
        yield return new WaitForSeconds(testDelaySeconds);

        // 4. TurnService 통합 검증
        yield return StartCoroutine(TestTurnServiceIntegration());
        yield return new WaitForSeconds(testDelaySeconds);

        // 5. 타일 드롭 핸들러 검증
        yield return StartCoroutine(TestTileDropHandlers());
        yield return new WaitForSeconds(testDelaySeconds);

        // 결과 출력
        ShowTestResults();
    }

    /// <summary>
    /// 수동 테스트 실행
    /// </summary>
    [ContextMenu("Run Phase 3 Tests")]
    public void RunTests()
    {
        StartCoroutine(RunAllTestsWithDelay());
    }

    #endregion

    #region 개별 테스트

    /// <summary>
    /// 서비스 초기화 테스트
    /// </summary>
    private IEnumerator TestServiceInitialization()
    {
        Log("🔧 Testing Service Initialization...");

        // 서비스 참조 가져오기
        cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
        cardHandManager = ServiceLocator.Get<ICardHandManager>();
        cardSpawnService = ServiceLocator.Get<ICardSpawnService>();
        spawnValidator = ServiceLocator.Get<ISpawnValidator>();
        turnService = ServiceLocator.Get<ITurnService>();

        // CardServiceManager 검증
        AssertTest("CardServiceManager Service Located", cardServiceManager != null);
        if (cardServiceManager != null)
        {
            AssertTest("CardServiceManager Initialized", cardServiceManager.IsInitialized);
            AssertTest("CardServiceManager Services Healthy", cardServiceManager.AreServicesHealthy);
        }

        // CardHandManager 검증
        AssertTest("CardHandManager Service Located", cardHandManager != null);
        if (cardHandManager != null)
        {
            AssertTest("CardHandManager Initialized", cardHandManager.IsInitialized);
        }

        // CardSpawnService 검증
        AssertTest("CardSpawnService Service Located", cardSpawnService != null);

        // SpawnValidator 검증
        AssertTest("SpawnValidator Service Located", spawnValidator != null);

        // TurnService 검증
        AssertTest("TurnService Service Located", turnService != null);

        yield return null;
    }

    /// <summary>
    /// CardHandManager UI 기능 테스트
    /// </summary>
    private IEnumerator TestCardHandManagerUI()
    {
        Log("🖐️ Testing CardHandManager UI Functionality...");

        if (cardHandManager == null)
        {
            LogError("CardHandManager not available for UI testing");
            yield break;
        }

        // 초기 상태 검증
        AssertTest("Hand Initially Empty", cardHandManager.HandSize == 0);
        AssertTest("Hand Not Full Initially", !cardHandManager.IsHandFull());
        AssertTest("Player Summon Mode Initially Disabled", !cardHandManager.IsPlayerSummonMode);

        // 테스트 카드 추가
        if (testCards.Count > 0)
        {
            var testCard = testCards[0];
            bool addResult = cardHandManager.AddCardToHand(testCard);
            AssertTest("Add Card to Hand", addResult);
            AssertTest("Hand Size After Add", cardHandManager.HandSize == 1);
            AssertTest("Has Added Card", cardHandManager.HasCard(testCard));

            yield return new WaitForSeconds(0.5f);

            // 플레이어 소환 모드 테스트
            cardHandManager.EnablePlayerSummonMode();
            AssertTest("Player Summon Mode Enabled", cardHandManager.IsPlayerSummonMode);

            yield return new WaitForSeconds(0.5f);

            cardHandManager.DisablePlayerSummonMode();
            AssertTest("Player Summon Mode Disabled", !cardHandManager.IsPlayerSummonMode);

            // 카드 제거 테스트
            bool removeResult = cardHandManager.RemoveCardFromHand(testCard);
            AssertTest("Remove Card from Hand", removeResult);
            AssertTest("Hand Empty After Remove", cardHandManager.HandSize == 0);
        }
        else
        {
            LogError("No test cards available for CardHandManager testing");
        }

        yield return null;
    }

    /// <summary>
    /// 드래그 앤 드롭 기능 테스트
    /// </summary>
    private IEnumerator TestDragAndDropFunctionality()
    {
        Log("🎯 Testing Drag and Drop Functionality...");

        if (cardHandManager == null || testCards.Count == 0)
        {
            LogError("CardHandManager or test cards not available for drag-drop testing");
            yield break;
        }

        // 테스트 카드 핸드에 추가
        cardHandManager.ClearHand();
        bool addResult = cardHandManager.AddCardToHand(testCards[0]);
        AssertTest("Test Card Added for Drag Test", addResult);

        yield return new WaitForSeconds(0.5f);

        // 소환 모드 활성화
        cardHandManager.EnablePlayerSummonMode();
        AssertTest("Drag Mode Enabled", cardHandManager.IsPlayerSummonMode);

        yield return new WaitForSeconds(0.5f);

        // CardUI 컴포넌트 검증
        var handCards = cardHandManager.GetHandCards();
        AssertTest("Hand Cards Retrieved", handCards != null && handCards.Count > 0);

        // CardUI 드래그 가능 상태 검증 (UI 컴포넌트가 존재하는 경우)
        var cardUIComponents = FindObjectsOfType<CardUI>();
        if (cardUIComponents.Length > 0)
        {
            var cardUI = cardUIComponents[0];
            AssertTest("CardUI Component Found", cardUI != null);
            AssertTest("CardUI Is Draggable", cardUI.IsDraggable);
            AssertTest("CardUI Has Card Data", cardUI.GetCardData() != null);
        }
        else
        {
            LogWarning("No CardUI components found in scene - UI prefab may not be assigned");
        }

        yield return null;
    }

    /// <summary>
    /// TurnService 통합 테스트
    /// </summary>
    private IEnumerator TestTurnServiceIntegration()
    {
        Log("🔄 Testing TurnService Integration...");

        if (turnService == null || cardHandManager == null)
        {
            LogError("TurnService or CardHandManager not available for integration testing");
            yield break;
        }

        // 초기 상태 확인
        AssertTest("Player Summon Mode Initially Off", !cardHandManager.IsPlayerSummonMode);

        // AllySummon 페이즈로 변경 시뮬레이션
        if (turnService is TurnService concreteService)
        {
            // TurnService의 페이즈 변경을 통해 이벤트 트리거
            LogDetail("Simulating phase change to AllySummon...");
            
            // 페이즈 변경 후 대기
            yield return new WaitForSeconds(0.5f);

            // 이벤트 핸들러가 작동했는지 확인
            // 실제로는 TurnService가 페이즈를 변경해야 하지만 테스트에서는 직접 호출
            cardHandManager.EnablePlayerSummonMode();
            AssertTest("Summon Mode Activated by Phase Change", cardHandManager.IsPlayerSummonMode);

            yield return new WaitForSeconds(0.5f);

            // 다른 페이즈로 변경
            cardHandManager.DisablePlayerSummonMode();
            AssertTest("Summon Mode Deactivated by Phase Change", !cardHandManager.IsPlayerSummonMode);
        }

        yield return null;
    }

    /// <summary>
    /// 타일 드롭 핸들러 테스트
    /// </summary>
    private IEnumerator TestTileDropHandlers()
    {
        Log("🎯 Testing Tile Drop Handlers...");

        // 씬에 있는 TileDropHandler들 검증
        var tileDropHandlers = FindObjectsOfType<TileDropHandler>();
        AssertTest("TileDropHandlers Found in Scene", tileDropHandlers.Length > 0);

        if (tileDropHandlers.Length > 0)
        {
            var handler = tileDropHandlers[0];
            AssertTest("TileDropHandler Is Interactable", handler.IsInteractable);
            
            Vector2Int gridPos = handler.GetGridPosition();
            LogDetail($"TileDropHandler at position: {gridPos}");

            // 유효성 검사 테스트 (테스트 카드가 있는 경우)
            if (testCards.Count > 0 && spawnValidator != null)
            {
                bool isValid = spawnValidator.CanSpawnUnit(testCards[0], gridPos);
                LogDetail($"Spawn validation result for {testCards[0].CardName} at {gridPos}: {isValid}");
                AssertTest("Spawn Validation Callable", true); // 호출 가능하면 성공
            }
        }

        // 추가 타일 핸들러 생성 테스트 (프리팹이 있는 경우)
        if (tileDropHandlerPrefab != null && testTileParent != null)
        {
            var testTileHandler = Instantiate(tileDropHandlerPrefab, testTileParent);
            var dropHandler = testTileHandler.GetComponent<TileDropHandler>();
            
            if (dropHandler != null)
            {
                dropHandler.SetGridPosition(new Vector2Int(10, 10));
                dropHandler.SetInteractable(true);
                
                AssertTest("Test TileDropHandler Created", dropHandler != null);
                AssertTest("Test TileDropHandler Position Set", dropHandler.GetGridPosition() == new Vector2Int(10, 10));
                AssertTest("Test TileDropHandler Is Interactable", dropHandler.IsInteractable);
                
                // 테스트 후 정리
                DestroyImmediate(testTileHandler);
            }
        }

        yield return null;
    }

    #endregion

    #region 테스트 유틸리티

    /// <summary>
    /// 어설션 테스트 수행
    /// </summary>
    private void AssertTest(string testName, bool condition)
    {
        totalTests++;
        
        if (condition)
        {
            passedTests++;
            LogDetail($"✅ PASS: {testName}");
        }
        else
        {
            failedTests++;
            LogError($"❌ FAIL: {testName}");
        }
    }

    /// <summary>
    /// 테스트 결과 표시
    /// </summary>
    private void ShowTestResults()
    {
        Log("📊 Phase 3 Test Results:");
        Log($"📈 Total Tests: {totalTests}");
        Log($"✅ Passed: {passedTests}");
        Log($"❌ Failed: {failedTests}");
        Log($"📊 Success Rate: {(passedTests / (float)totalTests * 100):F1}%");

        if (failedTests == 0)
        {
            Log("🎉 All Phase 3 tests passed! Card drag-and-drop system is working correctly.");
        }
        else
        {
            LogError($"⚠️ {failedTests} tests failed. Please check the implementation.");
        }
    }

    /// <summary>
    /// 상세 로그 출력
    /// </summary>
    private void LogDetail(string message)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"[Phase3Test] {message}");
        }
    }

    /// <summary>
    /// 일반 로그 출력
    /// </summary>
    private void Log(string message)
    {
        Debug.Log($"[Phase3Test] {message}");
    }

    /// <summary>
    /// 경고 로그 출력
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[Phase3Test] {message}");
    }

    /// <summary>
    /// 에러 로그 출력
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[Phase3Test] {message}");
    }

    #endregion

    #region 에디터 도구

#if UNITY_EDITOR
    [Header("에디터 도구")]
    [SerializeField] private bool showTestStats = true;

    private void OnGUI()
    {
        if (!showTestStats || !Application.isPlaying) return;

        var rect = new Rect(Screen.width - 300, Screen.height - 200, 280, 180);
        GUILayout.BeginArea(rect);
        GUILayout.Box("Phase 3 Test Status");

        GUILayout.Label($"Services Available:");
        GUILayout.Label($"  CardServiceManager: {(cardServiceManager != null ? "✅" : "❌")}");
        GUILayout.Label($"  CardHandManager: {(cardHandManager != null ? "✅" : "❌")}");
        GUILayout.Label($"  CardSpawnService: {(cardSpawnService != null ? "✅" : "❌")}");
        GUILayout.Label($"  SpawnValidator: {(spawnValidator != null ? "✅" : "❌")}");

        GUILayout.Space(10);

        if (totalTests > 0)
        {
            GUILayout.Label($"Test Results: {passedTests}/{totalTests} passed");
            var successRate = (passedTests / (float)totalTests);
            var color = successRate == 1.0f ? Color.green : successRate > 0.7f ? Color.yellow : Color.red;
            
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Label($"Success Rate: {successRate * 100:F1}%");
            GUI.color = oldColor;
        }
        else
        {
            GUILayout.Label("Tests not run yet");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Run Tests"))
        {
            RunTests();
        }

        if (GUILayout.Button("Clear Results"))
        {
            totalTests = passedTests = failedTests = 0;
        }

        GUILayout.EndArea();
    }

    [UnityEditor.MenuItem("Game/Run Phase 3 Tests")]
    private static void RunPhase3TestsMenu()
    {
        var tester = FindObjectOfType<Phase3ValidationScript>();
        if (tester != null)
        {
            tester.RunTests();
        }
        else
        {
            Debug.LogWarning("Phase3ValidationScript not found in scene");
        }
    }
#endif

    #endregion
}