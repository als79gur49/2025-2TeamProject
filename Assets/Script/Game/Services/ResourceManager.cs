using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Services;
using Game;
using System;

namespace Game.Services
{
    /// <summary>
    /// 플레이어의 자원(Mana, ActionPoint) 관리 서비스
    /// Phase 2: 카드 소환 비용 검증 및 차감을 담당
    /// </summary>
    public class ResourceManager : MonoBehaviour, IResourceManager
    {
        [Header("자원 관리 설정")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool enableDebugGUI = true;

        [Header("플레이어 자원")]
        [SerializeField] private int playerMana = 5;
        [SerializeField] private int playerMaxMana = 10;
        [SerializeField] private int playerActionPoints = 3;
        [SerializeField] private int playerMaxActionPoints = 5;

        [Header("적군 자원")]
        [SerializeField] private int enemyMana = 5;
        [SerializeField] private int enemyMaxMana = 10;
        [SerializeField] private int enemyActionPoints = 3;
        [SerializeField] private int enemyMaxActionPoints = 5;

        // 초기화 상태
        private bool isInitialized = false;

        /// <summary>자원 매니저가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        #region 플레이어 자원 프로퍼티

        /// <summary>플레이어 현재 마나</summary>
        public int PlayerMana => playerMana;

        /// <summary>플레이어 최대 마나</summary>
        public int PlayerMaxMana => playerMaxMana;

        /// <summary>플레이어 현재 행동력</summary>
        public int PlayerActionPoints => playerActionPoints;

        /// <summary>플레이어 최대 행동력</summary>
        public int PlayerMaxActionPoints => playerMaxActionPoints;

        #endregion

        #region 적군 자원 프로퍼티

        /// <summary>적군 현재 마나</summary>
        public int EnemyMana => enemyMana;

        /// <summary>적군 최대 마나</summary>
        public int EnemyMaxMana => enemyMaxMana;

        /// <summary>적군 현재 행동력</summary>
        public int EnemyActionPoints => enemyActionPoints;

        /// <summary>적군 최대 행동력</summary>
        public int EnemyMaxActionPoints => enemyMaxActionPoints;

        #endregion

        #region 이벤트

        /// <summary>플레이어 자원 변경 이벤트 (마나, 행동력)</summary>
        public event System.Action<int, int> OnPlayerResourcesChanged;

        /// <summary>적군 자원 변경 이벤트 (마나, 행동력)</summary>
        public event System.Action<int, int> OnEnemyResourcesChanged;

        /// <summary>자원 부족 이벤트 (플레이어 여부, 필요한 마나, 필요한 행동력)</summary>
        public event System.Action<bool, int, int> OnInsufficientResources;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // GameInitializer에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        #endregion

        #region GameInitializer 호출 메서드

        /// <summary>
        /// GameInitializer에 의해 호출되는 초기화 메서드
        /// 자원 시스템을 초기화하고 기본값을 설정
        /// </summary>
        public void Initialize()
        {
            Log("💰 Initializing ResourceManager...");

            // 초기 자원 설정 검증
            ValidateResourceLimits();

            // 턴 서비스 이벤트 연결
            ConnectToTurnService();

            isInitialized = true;
            Log("✅ ResourceManager initialized successfully");

            // 초기 상태 이벤트 발송
            NotifyResourceChanged(true);
            NotifyResourceChanged(false);
        }

        #endregion

        #region 자원 검증 메서드

        /// <summary>
        /// 플레이어가 지정된 비용을 지불할 수 있는지 확인
        /// </summary>
        /// <param name="manaCost">필요한 마나</param>
        /// <param name="actionCost">필요한 행동력</param>
        /// <returns>지불 가능 여부</returns>
        public bool CanPlayerAfford(int manaCost, int actionCost)
        {
            bool canAfford = playerMana >= manaCost && playerActionPoints >= actionCost;
            
            if (!canAfford)
            {
                Log($"❌ Player cannot afford cost: Need {manaCost}M/{actionCost}A, Have {playerMana}M/{playerActionPoints}A");
                OnInsufficientResources?.Invoke(true, manaCost, actionCost);
            }
            
            return canAfford;
        }

        /// <summary>
        /// 적군이 지정된 비용을 지불할 수 있는지 확인
        /// </summary>
        /// <param name="manaCost">필요한 마나</param>
        /// <param name="actionCost">필요한 행동력</param>
        /// <returns>지불 가능 여부</returns>
        public bool CanEnemyAfford(int manaCost, int actionCost)
        {
            bool canAfford = enemyMana >= manaCost && enemyActionPoints >= actionCost;
            
            if (!canAfford)
            {
                Log($"❌ Enemy cannot afford cost: Need {manaCost}M/{actionCost}A, Have {enemyMana}M/{enemyActionPoints}A");
                OnInsufficientResources?.Invoke(false, manaCost, actionCost);
            }
            
            return canAfford;
        }

        /// <summary>
        /// 팀에 따라 자원 지불 가능 여부 확인
        /// </summary>
        /// <param name="isPlayerTeam">플레이어 팀인지 여부</param>
        /// <param name="manaCost">필요한 마나</param>
        /// <param name="actionCost">필요한 행동력</param>
        /// <returns>지불 가능 여부</returns>
        public bool CanAfford(bool isPlayerTeam, int manaCost, int actionCost)
        {
            return isPlayerTeam ? CanPlayerAfford(manaCost, actionCost) : CanEnemyAfford(manaCost, actionCost);
        }

        #endregion

        #region 자원 소모 메서드

        /// <summary>
        /// 플레이어 자원 소모
        /// </summary>
        /// <param name="manaCost">소모할 마나</param>
        /// <param name="actionCost">소모할 행동력</param>
        /// <returns>소모 성공 여부</returns>
        public bool SpendPlayerResources(int manaCost, int actionCost)
        {
            if (!CanPlayerAfford(manaCost, actionCost))
            {
                return false;
            }

            playerMana -= manaCost;
            playerActionPoints -= actionCost;

            Log($"💸 Player spent {manaCost}M/{actionCost}A, Remaining: {playerMana}M/{playerActionPoints}A");
            NotifyResourceChanged(true);
            return true;
        }

        /// <summary>
        /// 적군 자원 소모
        /// </summary>
        /// <param name="manaCost">소모할 마나</param>
        /// <param name="actionCost">소모할 행동력</param>
        /// <returns>소모 성공 여부</returns>
        public bool SpendEnemyResources(int manaCost, int actionCost)
        {
            if (!CanEnemyAfford(manaCost, actionCost))
            {
                return false;
            }

            enemyMana -= manaCost;
            enemyActionPoints -= actionCost;

            Log($"💸 Enemy spent {manaCost}M/{actionCost}A, Remaining: {enemyMana}M/{enemyActionPoints}A");
            NotifyResourceChanged(false);
            return true;
        }

        /// <summary>
        /// 팀에 따라 자원 소모
        /// </summary>
        /// <param name="isPlayerTeam">플레이어 팀인지 여부</param>
        /// <param name="manaCost">소모할 마나</param>
        /// <param name="actionCost">소모할 행동력</param>
        /// <returns>소모 성공 여부</returns>
        public bool SpendResources(bool isPlayerTeam, int manaCost, int actionCost)
        {
            return isPlayerTeam ? SpendPlayerResources(manaCost, actionCost) : SpendEnemyResources(manaCost, actionCost);
        }

        #endregion

        #region 자원 회복/설정 메서드

        /// <summary>
        /// 플레이어 자원 회복
        /// </summary>
        /// <param name="manaAmount">회복할 마나</param>
        /// <param name="actionAmount">회복할 행동력</param>
        public void RestorePlayerResources(int manaAmount, int actionAmount)
        {
            playerMana = Mathf.Min(playerMana + manaAmount, playerMaxMana);
            playerActionPoints = Mathf.Min(playerActionPoints + actionAmount, playerMaxActionPoints);

            Log($"💚 Player restored {manaAmount}M/{actionAmount}A, Current: {playerMana}M/{playerActionPoints}A");
            NotifyResourceChanged(true);
        }

        /// <summary>
        /// 적군 자원 회복
        /// </summary>
        /// <param name="manaAmount">회복할 마나</param>
        /// <param name="actionAmount">회복할 행동력</param>
        public void RestoreEnemyResources(int manaAmount, int actionAmount)
        {
            enemyMana = Mathf.Min(enemyMana + manaAmount, enemyMaxMana);
            enemyActionPoints = Mathf.Min(enemyActionPoints + actionAmount, enemyMaxActionPoints);

            Log($"💚 Enemy restored {manaAmount}M/{actionAmount}A, Current: {enemyMana}M/{enemyActionPoints}A");
            NotifyResourceChanged(false);
        }

        /// <summary>
        /// 플레이어 자원을 최대치로 설정
        /// </summary>
        public void RefillPlayerResources()
        {
            playerMana = playerMaxMana;
            playerActionPoints = playerMaxActionPoints;

            Log($"🔋 Player resources refilled: {playerMana}M/{playerActionPoints}A");
            NotifyResourceChanged(true);
        }

        /// <summary>
        /// 적군 자원을 최대치로 설정
        /// </summary>
        public void RefillEnemyResources()
        {
            enemyMana = enemyMaxMana;
            enemyActionPoints = enemyMaxActionPoints;

            Log($"🔋 Enemy resources refilled: {enemyMana}M/{enemyActionPoints}A");
            NotifyResourceChanged(false);
        }

        #endregion

        #region 턴 시스템 연동

        /// <summary>
        /// TurnService 이벤트에 연결 (GameServiceManager를 통해 간접 접근)
        /// </summary>
        private void ConnectToTurnService()
        {
            var gameServiceManager = ServiceLocator.Get<IGameServiceManager>();
            if (gameServiceManager != null)
            {
                var turnService = gameServiceManager.GetTurnService();
                if (turnService != null)
                {
                    // TODO: 턴 시작 시 자원 회복 로직 추가
                    Log("🔗 Connected to TurnService through GameServiceManager for resource management");
                }
                else
                {
                    LogError("❌ TurnService not found in GameServiceManager - resource per-turn logic won't work");
                }
            }
            else
            {
                LogError("❌ GameServiceManager not found - cannot connect to TurnService");
            }
        }

        /// <summary>
        /// 턴 변경 시 호출되는 핸들러
        /// </summary>
        /// <param name="newPhase">새로운 턴 페이즈</param>
        private void HandlePhaseChanged(TurnPhase newPhase)
        {
            switch (newPhase)
            {
                case TurnPhase.TurnStart:
                    // 턴 시작 시 자원 회복
                    HandleTurnStart();
                    break;
                
                case TurnPhase.EnemySummon:
                    // 적군 소환 페이즈 시작 시 일부 자원 회복
                    RestoreEnemyResources(1, 1);
                    break;
                
                case TurnPhase.AllySummon:
                    // 플레이어 소환 페이즈 시작 시 일부 자원 회복
                    RestorePlayerResources(1, 1);
                    break;
                    
                case TurnPhase.TurnEnd:
                    // 턴 종료 시 자원 정리
                    HandleTurnEnd();
                    break;
            }
        }
        
        /// <summary>
        /// 턴 시작 시 자원 회복 및 초기화
        /// </summary>
        private void HandleTurnStart()
        {
            // 매 턴 사용가능 Cost 1증가 (요구사항)
            IncreaseTurnlyMana();
            
            // 액션 포인트 전체 회복
            playerActionPoints = playerMaxActionPoints;
            enemyActionPoints = enemyMaxActionPoints;
            
            Log("🔄 Turn start - resources restored for new turn");
        }

        /// <summary>
        /// 매 턴 사용가능 마나를 1증가시킴
        /// CardServiceManager에서 호출됨
        /// </summary>
        public void IncreaseTurnlyMana()
        {
            // 플레이어 마나 최대치 및 현재 마나 1증가 (최대 10까지)
            if (playerMaxMana < 10)
            {
                playerMaxMana++;
                Log($"💰 Player max mana increased to {playerMaxMana}");
            }
            
            // 현재 마나를 최대치로 회복
            playerMana = playerMaxMana;
            
            // 적군 마나도 동일하게 증가
            if (enemyMaxMana < 10)
            {
                enemyMaxMana++;
                Log($"💰 Enemy max mana increased to {enemyMaxMana}");
            }
            
            enemyMana = enemyMaxMana;
            
            Log($"⚡ Turn mana increased - Player: {playerMana}/{playerMaxMana}, Enemy: {enemyMana}/{enemyMaxMana}");
            
            // 자원 변경 이벤트 발송
            NotifyResourceChanged(true);
            NotifyResourceChanged(false);
        }
        
        /// <summary>
        /// 턴 종료 시 자원 정리
        /// </summary>
        private void HandleTurnEnd()
        {
            // 필요시 턴 종료 시 자원 정리 로직 구현
            Log("🏁 Turn end - resource cleanup completed");
        }

        #endregion

        #region 내부 유틸리티 메서드

        /// <summary>
        /// 자원 한계값 검증
        /// </summary>
        private void ValidateResourceLimits()
        {
            playerMana = Mathf.Clamp(playerMana, 0, playerMaxMana);
            playerActionPoints = Mathf.Clamp(playerActionPoints, 0, playerMaxActionPoints);
            enemyMana = Mathf.Clamp(enemyMana, 0, enemyMaxMana);
            enemyActionPoints = Mathf.Clamp(enemyActionPoints, 0, enemyMaxActionPoints);
        }

        /// <summary>
        /// 자원 변경 알림
        /// </summary>
        /// <param name="isPlayerTeam">플레이어 팀인지 여부</param>
        private void NotifyResourceChanged(bool isPlayerTeam)
        {
            if (isPlayerTeam)
            {
                OnPlayerResourcesChanged?.Invoke(playerMana, playerActionPoints);
            }
            else
            {
                OnEnemyResourcesChanged?.Invoke(enemyMana, enemyActionPoints);
            }
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[ResourceManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[ResourceManager] {System.DateTime.Now:HH:mm:ss.fff} {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 자원 매니저 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"ResourceManager Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Player: {playerMana}M/{playerMaxMana}M, {playerActionPoints}A/{playerMaxActionPoints}A\n" +
                   $"- Enemy: {enemyMana}M/{enemyMaxMana}M, {enemyActionPoints}A/{enemyMaxActionPoints}A\n";
        }

        /// <summary>
        /// 자원 상태 초기화 (테스트용)
        /// </summary>
        public void ResetResources()
        {
            playerMana = playerMaxMana;
            playerActionPoints = playerMaxActionPoints;
            enemyMana = enemyMaxMana;
            enemyActionPoints = enemyMaxActionPoints;

            Log("🔄 Resources reset to default values");
            NotifyResourceChanged(true);
            NotifyResourceChanged(false);
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("에디터 도구")]
        [SerializeField] private bool showDebugInfo = true;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying || !enableDebugGUI) return;

            GUILayout.BeginArea(new Rect(650, 10, 250, 300));
            GUILayout.Box("Resource Manager Debug");

            if (isInitialized)
            {
                GUILayout.Label("✅ ResourceManager Initialized");
            }
            else
            {
                GUILayout.Label("❌ ResourceManager Not Initialized");
            }

            GUILayout.Space(10);

            // 플레이어 자원
            GUILayout.Label($"🔵 Player Resources:");
            GUILayout.Label($"  Mana: {playerMana}/{playerMaxMana}");
            GUILayout.Label($"  Action: {playerActionPoints}/{playerMaxActionPoints}");

            if (GUILayout.Button("Player +1M/+1A"))
            {
                RestorePlayerResources(1, 1);
            }
            if (GUILayout.Button("Player Refill"))
            {
                RefillPlayerResources();
            }

            GUILayout.Space(10);

            // 적군 자원
            GUILayout.Label($"🔴 Enemy Resources:");
            GUILayout.Label($"  Mana: {enemyMana}/{enemyMaxMana}");
            GUILayout.Label($"  Action: {enemyActionPoints}/{enemyMaxActionPoints}");

            if (GUILayout.Button("Enemy +1M/+1A"))
            {
                RestoreEnemyResources(1, 1);
            }
            if (GUILayout.Button("Enemy Refill"))
            {
                RefillEnemyResources();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Reset All Resources"))
            {
                ResetResources();
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}