using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Services;
using Game.Data;
using Game.Card.Core;
using Game.Interfaces;

namespace Game.Test
{
    /// <summary>
    /// Phase 4 전체 워크플로우 테스트 - 카드 드로우부터 UnitService 등록까지의 완전한 흐름을 테스트합니다.
    /// </summary>
    public class Phase4WorkflowTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private float testDelay = 1f;
        
        [Header("Test Data")]
        [SerializeField] private CardData[] testCards;
        [SerializeField] private Vector2Int[] testPositions;
        
        // 서비스 참조
        private ICardHandManager cardHandManager;
        private ICardSpawnService cardSpawnService;
        private IUnitService unitService;
        private ITurnService turnService;
        
        private bool servicesResolved = false;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(InitializeAndRunTests());
            }
        }
        
        /// <summary>
        /// 서비스들이 초기화되기를 기다린 후 테스트를 실행합니다.
        /// </summary>
        private IEnumerator InitializeAndRunTests()
        {
            Debug.Log("🧪 [Phase4WorkflowTest] Initializing workflow test...");
            
            // 서비스들이 초기화되기를 잠시 기다림
            yield return new WaitForSeconds(1f);
            
            // 서비스 의존성 해결
            ResolveServiceDependencies();
            
            if (servicesResolved)
            {
                Debug.Log("✅ [Phase4WorkflowTest] All services resolved. Starting tests...");
                yield return StartCoroutine(RunCompleteWorkflowTest());
            }
            else
            {
                Debug.LogError("❌ [Phase4WorkflowTest] Failed to resolve services. Cannot run tests.");
            }
        }
        
        /// <summary>
        /// ServiceLocator를 통해 필요한 서비스들을 해결합니다.
        /// </summary>
        private void ResolveServiceDependencies()
        {
            Debug.Log("🔧 [Phase4WorkflowTest] Resolving service dependencies...");
            
            cardHandManager = ServiceLocator.Get<ICardHandManager>();
            cardSpawnService = ServiceLocator.Get<ICardSpawnService>();
            unitService = ServiceLocator.Get<IUnitService>();
            turnService = ServiceLocator.Get<ITurnService>();
            
            bool allResolved = cardHandManager != null && 
                              cardSpawnService != null && 
                              unitService != null && 
                              turnService != null;
            
            if (allResolved)
            {
                servicesResolved = true;
                Debug.Log("✅ [Phase4WorkflowTest] All services resolved successfully");
            }
            else
            {
                Debug.LogError($"❌ [Phase4WorkflowTest] Service resolution failed:");
                Debug.LogError($"   - CardHandManager: {(cardHandManager != null ? "✅" : "❌")}");
                Debug.LogError($"   - CardSpawnService: {(cardSpawnService != null ? "✅" : "❌")}");
                Debug.LogError($"   - UnitService: {(unitService != null ? "✅" : "❌")}");
                Debug.LogError($"   - TurnService: {(turnService != null ? "✅" : "❌")}");
            }
        }
        
        /// <summary>
        /// 완전한 워크플로우 테스트를 실행합니다.
        /// 카드 드로우 → 핸드 표시 → 드래그 → 유효성 검사 → 드롭 → 소환 → UnitService 등록
        /// </summary>
        private IEnumerator RunCompleteWorkflowTest()
        {
            Debug.Log("🎯 [Phase4WorkflowTest] Starting complete workflow test...");
            
            // Step 1: 게임을 AllySummon 페이즈로 설정
            yield return StartCoroutine(SetupAllySummonPhase());
            
            // Step 2: 테스트 카드 생성 및 핸드에 추가
            yield return StartCoroutine(TestCardDrawAndHandDisplay());
            
            // Step 3: 카드 드래그 앤 드롭 시뮬레이션
            yield return StartCoroutine(TestCardDragAndDrop());
            
            // Step 4: 유닛 소환 및 UnitService 등록 검증
            yield return StartCoroutine(TestUnitSpawnAndRegistration());
            
            // Step 5: 전체 워크플로우 결과 검증
            ValidateCompleteWorkflow();
            
            Debug.Log("🏁 [Phase4WorkflowTest] Complete workflow test finished");
        }
        
        /// <summary>
        /// AllySummon 페이즈로 설정
        /// </summary>
        private IEnumerator SetupAllySummonPhase()
        {
            Debug.Log("⚙️ [Phase4WorkflowTest] Setting up AllySummon phase...");
            
            if (turnService != null)
            {
                // 현재 페이즈가 AllySummon이 아니라면 전환
                if (turnService.CurrentPhase != TurnPhase.AllySummon)
                {
                    // 페이즈를 AllySummon으로 강제 전환 (테스트 목적)
                    while (turnService.CurrentPhase != TurnPhase.AllySummon)
                    {
                        turnService.EndCurrentPhase();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                
                Debug.Log($"✅ [Phase4WorkflowTest] Current phase is now: {turnService.CurrentPhase}");
            }
            else
            {
                Debug.LogError("❌ [Phase4WorkflowTest] TurnService not available for phase setup");
            }
            
            yield return new WaitForSeconds(testDelay);
        }
        
        /// <summary>
        /// 카드 드로우 및 핸드 표시 테스트
        /// </summary>
        private IEnumerator TestCardDrawAndHandDisplay()
        {
            Debug.Log("🃏 [Phase4WorkflowTest] Testing card draw and hand display...");
            
            // 테스트용 카드 데이터가 없다면 기본 카드 생성
            if (testCards == null || testCards.Length == 0)
            {
                CreateTestCards();
            }
            
            if (cardHandManager != null && testCards.Length > 0)
            {
                // 핸드에 테스트 카드들 추가
                foreach (var cardData in testCards)
                {
                    if (cardData != null)
                    {
                        cardHandManager.AddCardToHand(cardData);
                        Debug.Log($"📥 [Phase4WorkflowTest] Added {cardData.CardName} to hand");
                        yield return new WaitForSeconds(0.2f);
                    }
                }
                
                Debug.Log($"✅ [Phase4WorkflowTest] Added {testCards.Length} cards to hand");
            }
            else
            {
                Debug.LogError("❌ [Phase4WorkflowTest] CardHandManager not available or no test cards");
            }
            
            yield return new WaitForSeconds(testDelay);
        }
        
        /// <summary>
        /// 카드 드래그 앤 드롭 시뮬레이션 테스트
        /// </summary>
        private IEnumerator TestCardDragAndDrop()
        {
            Debug.Log("🎯 [Phase4WorkflowTest] Testing card drag and drop simulation...");
            
            // 드래그 앤 드롭은 UI 이벤트이므로 실제로는 CardSpawnService를 직접 호출하여 시뮬레이션
            if (cardSpawnService != null && testCards.Length > 0 && testPositions.Length > 0)
            {
                var testCard = testCards[0]; // 첫 번째 카드 사용
                var testPosition = testPositions[0]; // 첫 번째 위치 사용
                
                Debug.Log($"🎪 [Phase4WorkflowTest] Simulating drag and drop of {testCard.CardName} to {testPosition}");
                
                // 실제 드래그 앤 드롭 대신 CardSpawnService 직접 호출
                bool spawnSuccess = cardSpawnService.TrySpawnUnitFromCard(testCard, testPosition);
                
                if (spawnSuccess)
                {
                    Debug.Log("✅ [Phase4WorkflowTest] Card spawn simulation successful");
                }
                else
                {
                    Debug.LogWarning("⚠️ [Phase4WorkflowTest] Card spawn simulation failed");
                }
            }
            else
            {
                Debug.LogError("❌ [Phase4WorkflowTest] Cannot simulate drag and drop - missing dependencies");
            }
            
            yield return new WaitForSeconds(testDelay);
        }
        
        /// <summary>
        /// 유닛 소환 및 UnitService 등록 검증
        /// </summary>
        private IEnumerator TestUnitSpawnAndRegistration()
        {
            Debug.Log("👥 [Phase4WorkflowTest] Testing unit spawn and registration...");
            
            if (unitService != null)
            {
                // UnitService에서 활성 유닛 수 확인
                int activeUnits = unitService.ActiveUnitCount;
                Debug.Log($"📊 [Phase4WorkflowTest] Current active units: {activeUnits}");
                
                // 플레이어 유닛과 적군 유닛 수 확인
                int playerUnits = unitService.GetUnitCount(true);
                int enemyUnits = unitService.GetUnitCount(false);
                
                Debug.Log($"📊 [Phase4WorkflowTest] Player units: {playerUnits}, Enemy units: {enemyUnits}");
                
                if (activeUnits > 0)
                {
                    Debug.Log("✅ [Phase4WorkflowTest] Units successfully registered with UnitService");
                    
                    // 등록된 유닛들의 세부 정보 확인
                    var allUnits = unitService.GetActiveUnits();
                    foreach (var unit in allUnits)
                    {
                        Debug.Log($"🔍 [Phase4WorkflowTest] Registered unit: {unit.name} at ({unit.X}, {unit.Y})");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ [Phase4WorkflowTest] No units registered with UnitService");
                }
            }
            else
            {
                Debug.LogError("❌ [Phase4WorkflowTest] UnitService not available for verification");
            }
            
            yield return new WaitForSeconds(testDelay);
        }
        
        /// <summary>
        /// 전체 워크플로우 결과 검증
        /// </summary>
        private void ValidateCompleteWorkflow()
        {
            Debug.Log("📋 [Phase4WorkflowTest] Validating complete workflow results...");
            
            bool workflowSuccess = true;
            var issues = new List<string>();
            
            // 1. 서비스 가용성 확인
            if (!servicesResolved)
            {
                workflowSuccess = false;
                issues.Add("Services not properly resolved");
            }
            
            // 2. 페이즈 상태 확인
            if (turnService != null && turnService.CurrentPhase != TurnPhase.AllySummon)
            {
                issues.Add($"Expected AllySummon phase, but current is {turnService.CurrentPhase}");
            }
            
            // 3. UnitService 등록 확인
            if (unitService != null && unitService.ActiveUnitCount == 0)
            {
                issues.Add("No units registered with UnitService after workflow");
            }
            
            // 결과 출력
            if (workflowSuccess && issues.Count == 0)
            {
                Debug.Log("🎉 [Phase4WorkflowTest] Complete workflow validation PASSED");
                Debug.Log("✅ [Phase4WorkflowTest] All systems working correctly from card draw to unit registration");
            }
            else
            {
                Debug.LogWarning($"⚠️ [Phase4WorkflowTest] Workflow validation completed with {issues.Count} issues:");
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"  - {issue}");
                }
            }
        }
        
        /// <summary>
        /// 테스트용 카드 데이터 생성
        /// </summary>
        private void CreateTestCards()
        {
            Debug.Log("🃏 [Phase4WorkflowTest] Creating test cards...");
            
            // ScriptableObject를 런타임에 생성할 수 없으므로, 기존 카드를 찾거나 기본값 사용
            var foundCards = Resources.FindObjectsOfTypeAll<CardData>();
            if (foundCards.Length > 0)
            {
                var cardList = new List<CardData>();
                foreach (var card in foundCards)
                {
                    if (card.CanSummonUnit)
                    {
                        cardList.Add(card);
                        if (cardList.Count >= 3) break; // 최대 3장만
                    }
                }
                testCards = cardList.ToArray();
                Debug.Log($"✅ [Phase4WorkflowTest] Found {testCards.Length} test cards");
            }
            else
            {
                Debug.LogWarning("⚠️ [Phase4WorkflowTest] No CardData assets found. Workflow test may be limited.");
                testCards = new CardData[0];
            }
            
            // 테스트 위치 설정
            if (testPositions == null || testPositions.Length == 0)
            {
                testPositions = new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2)
                };
                Debug.Log($"✅ [Phase4WorkflowTest] Created {testPositions.Length} test positions");
            }
        }
        
        #region Manual Test Triggers
        
        [ContextMenu("Run Complete Workflow Test")]
        public void ManualRunWorkflowTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("⚠️ [Phase4WorkflowTest] Manual test can only run in Play mode");
                return;
            }
            
            StopAllCoroutines(); // 이전 테스트 중단
            StartCoroutine(InitializeAndRunTests());
        }
        
        [ContextMenu("Test Card Spawn Only")]
        public void ManualTestCardSpawn()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("⚠️ [Phase4WorkflowTest] Manual test can only run in Play mode");
                return;
            }
            
            ResolveServiceDependencies();
            if (servicesResolved)
            {
                StartCoroutine(TestCardDragAndDrop());
            }
        }
        
        [ContextMenu("Check UnitService Registration")]
        public void ManualCheckUnitRegistration()
        {
            ResolveServiceDependencies();
            if (unitService != null)
            {
                Debug.Log($"📊 [Phase4WorkflowTest] Active units: {unitService.ActiveUnitCount}");
                Debug.Log($"📊 [Phase4WorkflowTest] Player units: {unitService.GetUnitCount(true)}");
                Debug.Log($"📊 [Phase4WorkflowTest] Enemy units: {unitService.GetUnitCount(false)}");
            }
        }
        
        #endregion
        
        private void Update()
        {
            // F7 키로 수동 워크플로우 테스트 실행
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ManualRunWorkflowTest();
            }
            
            // F8 키로 유닛 등록 상태 확인
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ManualCheckUnitRegistration();
            }
        }
    }
}