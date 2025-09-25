using UnityEngine;
using Game.Services;
using Game.Interfaces;
using Game.Data;
using Game.Core;

namespace Game.Test
{
    /// <summary>
    /// Phase 2 구현 검증 스크립트
    /// GameInitializer에 의한 서비스 등록 후 카드 소환 시스템 테스트
    /// </summary>
    public class Phase2ValidationScript : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("테스트 데이터")]
        [SerializeField] private CardData testCard;
        [SerializeField] private Vector2Int testPosition = new Vector2Int(0, 1);

        // 서비스 참조들
        private ICardServiceManager cardServiceManager;
        private IResourceManager resourceManager;
        private ISpawnValidator spawnValidator;
        private ICardSpawnService cardSpawnService;
        private IGridController gridController;
        private ITurnService turnService;

        private void Start()
        {
            if (runTestOnStart)
            {
                // 서비스가 초기화될 때까지 잠시 대기
                Invoke(nameof(RunPhase2ValidationTest), 1f);
            }
        }

        [ContextMenu("Run Phase 2 Validation Test")]
        public void RunPhase2ValidationTest()
        {
            Log("🧪 Starting Phase 2 Implementation Validation Test...");

            // 1. 서비스 로케이터에서 모든 서비스 가져오기
            if (!GetAllServices())
            {
                LogError("❌ Failed to get required services from ServiceLocator");
                return;
            }

            Log("✅ All services successfully retrieved from ServiceLocator");

            // 2. 서비스 상태 검증
            ValidateServiceStates();

            // 3. ResourceManager 테스트
            TestResourceManager();

            // 4. SpawnValidator 테스트
            TestSpawnValidator();

            // 5. 통합 소환 테스트 (실제 카드가 있는 경우에만)
            if (testCard != null)
            {
                TestCardSpawning();
            }
            else
            {
                Log("⚠️ No test card provided - skipping spawn test");
            }

            Log("🧪 Phase 2 Validation Test Completed!");
        }

        /// <summary>
        /// ServiceLocator에서 모든 필요한 서비스 가져오기
        /// </summary>
        private bool GetAllServices()
        {
            try
            {
                cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
                resourceManager = ServiceLocator.Get<IResourceManager>();
                spawnValidator = ServiceLocator.Get<ISpawnValidator>();
                cardSpawnService = ServiceLocator.Get<ICardSpawnService>();
                gridController = ServiceLocator.Get<IGridController>();
                turnService = ServiceLocator.Get<ITurnService>();

                return cardServiceManager != null && 
                       resourceManager != null && 
                       spawnValidator != null && 
                       cardSpawnService != null && 
                       gridController != null;
            }
            catch (System.Exception ex)
            {
                LogError($"Exception getting services: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 각 서비스의 초기화 상태 검증
        /// </summary>
        private void ValidateServiceStates()
        {
            Log("📋 Validating service states...");

            // CardServiceManager 상태
            if (cardServiceManager != null)
            {
                Log($"🃏 CardServiceManager - Initialized: {cardServiceManager.IsInitialized}");
                if (cardServiceManager.AreServicesHealthy)
                {
                    Log("✅ Card services are healthy");
                }
                else
                {
                    LogError("❌ Some card services are unhealthy");
                }
            }

            // ResourceManager 상태  
            if (resourceManager != null)
            {
                Log($"💰 ResourceManager - Initialized: {resourceManager.IsInitialized}");
                Log($"   Player Resources: {resourceManager.PlayerMana}M/{resourceManager.PlayerActionPoints}A");
                Log($"   Enemy Resources: {resourceManager.EnemyMana}M/{resourceManager.EnemyActionPoints}A");
            }

            // SpawnValidator 상태
            if (spawnValidator != null)
            {
                Log($"✔️ SpawnValidator - Initialized: {spawnValidator.IsInitialized}");
                if (enableDetailedLogging)
                {
                    Log(spawnValidator.GetStatus());
                }
            }

            // CardSpawnService 상태
            if (cardSpawnService != null)
            {
                Log($"⭐ CardSpawnService - Initialized: {cardSpawnService.IsInitialized}");
                if (enableDetailedLogging)
                {
                    Log(cardSpawnService.GetStatus());
                }
            }
        }

        /// <summary>
        /// ResourceManager 기능 테스트
        /// </summary>
        private void TestResourceManager()
        {
            Log("💰 Testing ResourceManager functionality...");

            if (resourceManager == null)
            {
                LogError("❌ ResourceManager not available");
                return;
            }

            // 자원 확인 테스트
            bool canAfford = resourceManager.CanPlayerAfford(2, 1);
            Log($"   Can player afford 2M/1A? {canAfford}");

            // 자원 소모 테스트
            int initialMana = resourceManager.PlayerMana;
            int initialActions = resourceManager.PlayerActionPoints;

            if (resourceManager.SpendPlayerResources(1, 1))
            {
                Log($"✅ Successfully spent 1M/1A - Before: {initialMana}M/{initialActions}A, After: {resourceManager.PlayerMana}M/{resourceManager.PlayerActionPoints}A");
                
                // 자원 복구 테스트
                resourceManager.RestorePlayerResources(1, 1);
                Log($"🔄 Restored 1M/1A - Current: {resourceManager.PlayerMana}M/{resourceManager.PlayerActionPoints}A");
            }
            else
            {
                LogError("❌ Failed to spend resources");
            }
        }

        /// <summary>
        /// SpawnValidator 기능 테스트
        /// </summary>
        private void TestSpawnValidator()
        {
            Log("✔️ Testing SpawnValidator functionality...");

            if (spawnValidator == null)
            {
                LogError("❌ SpawnValidator not available");
                return;
            }

            if (testCard == null)
            {
                Log("⚠️ No test card - creating dummy card for validation test");
                testCard = ScriptableObject.CreateInstance<CardData>();
            }

            // 위치 검증 테스트
            Vector2Int playerSpawnPos = new Vector2Int(0, 1); // 플레이어 소환 위치 (좌측 첫 열)
            Vector2Int invalidPos = new Vector2Int(5, 1); // 잘못된 위치

            bool canSpawnAtValid = spawnValidator.CanSpawnUnit(testCard, playerSpawnPos, true);
            bool canSpawnAtInvalid = spawnValidator.CanSpawnUnit(testCard, invalidPos, true);

            Log($"   Can spawn at player area (0,1)? {canSpawnAtValid}");
            Log($"   Can spawn at invalid area (5,1)? {canSpawnAtInvalid}");

            if (canSpawnAtValid && !canSpawnAtInvalid)
            {
                Log("✅ SpawnValidator correctly validates spawn positions");
            }
            else
            {
                LogError("❌ SpawnValidator validation may have issues");
            }
        }

        /// <summary>
        /// 카드 소환 통합 테스트
        /// </summary>
        private void TestCardSpawning()
        {
            Log("🎯 Testing card spawning integration...");

            if (cardSpawnService == null || testCard == null)
            {
                LogError("❌ CardSpawnService or test card not available");
                return;
            }

            // 현재 자원 상태 저장
            int initialMana = resourceManager?.PlayerMana ?? 0;
            int initialActions = resourceManager?.PlayerActionPoints ?? 0;

            Log($"   Initial resources: {initialMana}M/{initialActions}A");
            Log($"   Card cost: {testCard.ManaCost}M/{testCard.ActionCost}A");

            // 소환 시도
            bool spawnSuccess = cardSpawnService.TrySpawnUnitFromCard(testCard, testPosition);

            if (spawnSuccess)
            {
                Log("✅ Card spawn test successful!");
                int finalMana = resourceManager?.PlayerMana ?? 0;
                int finalActions = resourceManager?.PlayerActionPoints ?? 0;
                Log($"   Final resources: {finalMana}M/{finalActions}A");
            }
            else
            {
                Log("⚠️ Card spawn test failed (this may be expected if no valid unit data)");
            }
        }

        /// <summary>
        /// 로깅 메서드
        /// </summary>
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[Phase2Test] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[Phase2Test] {message}");
        }

        #region 에디터 디버깅

#if UNITY_EDITOR
        [Header("에디터 도구")]
        [SerializeField] private bool showDebugGUI = true;

        private void OnGUI()
        {
            if (!showDebugGUI || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 200, 300, 400));
            GUILayout.Box("Phase 2 Validation Tools");

            if (GUILayout.Button("Run Validation Test"))
            {
                RunPhase2ValidationTest();
            }

            GUILayout.Space(10);

            if (resourceManager != null)
            {
                GUILayout.Label("Resource Manager:");
                GUILayout.Label($"Player: {resourceManager.PlayerMana}M/{resourceManager.PlayerActionPoints}A");
                GUILayout.Label($"Enemy: {resourceManager.EnemyMana}M/{resourceManager.EnemyActionPoints}A");

                if (GUILayout.Button("Give Player +5M/+3A"))
                {
                    resourceManager.RestorePlayerResources(5, 3);
                }
            }

            GUILayout.Space(10);

            if (testCard != null && cardSpawnService != null)
            {
                GUILayout.Label($"Test Card: {testCard.CardName}");
                GUILayout.Label($"Cost: {testCard.ManaCost}M/{testCard.ActionCost}A");
                
                if (GUILayout.Button("Test Spawn at (0,1)"))
                {
                    bool success = cardSpawnService.TrySpawnUnitFromCard(testCard, new Vector2Int(0, 1));
                    Log($"Spawn test result: {success}");
                }
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}