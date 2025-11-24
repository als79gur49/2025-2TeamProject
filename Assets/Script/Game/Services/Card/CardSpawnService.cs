using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using Game.Components;
using Game.Card.Effects;
using System.Collections.Generic;
using System.Linq;
using Game.VFX;
using static Game.Interfaces.ITeamComponent;

namespace Game.Services
{
    /// <summary>
    /// 리팩토링된 카드 소환 서비스
    /// EffectDefinition 기반의 통합 카드 처리 시스템
    /// </summary>
    public class CardSpawnService : MonoBehaviour, ICardSpawnService
    {
        [Header("소환 서비스 설정")]
        [SerializeField] private bool enableLogging = true;

        // ServiceLocator를 통해 주입받을 의존성들
        private IUnitService unitService;
        private IGridController gridController;
        private IGridState gridState;
        private ISpawnValidator spawnValidator;
        private IResourceManager resourceManager;

        // GameContext for effect execution
        private GameContext gameContext;

        // 초기화 상태
        private bool isInitialized = false;

        /// <summary>소환 서비스가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        #region CardServiceManager 호출 메서드

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드
        /// Unit의 Init() 패턴을 따라 외부에서 의존성을 주입받음
        /// </summary>
        /// <param name="iUnitService">유닛 서비스</param>
        /// <param name="iGridController">그리드 컨트롤러</param>
        /// <param name="iGridState">그리드 상태</param>
        /// <param name="iSpawnValidator">소환 검증기</param>
        /// <param name="iResourceManager">자원 매니저</param>
        public void Init(IUnitService iUnitService, IGridController iGridController,
                        IGridState iGridState, ISpawnValidator iSpawnValidator,
                        IResourceManager iResourceManager)
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[CardSpawnService] {gameObject.name} already initialized");
                return;
            }

            Log("⭐ Initializing CardSpawnService...");

            // 외부에서 주입받은 의존성 설정
            InjectDependencies(iUnitService, iGridController, iGridState, iSpawnValidator, iResourceManager);

            // 필수 의존성들이 모두 주입되었는지 확인
            bool allDependenciesResolved = unitService != null &&
                                         gridController != null &&
                                         gridState != null &&
                                         spawnValidator != null &&
                                         resourceManager != null;

            if (allDependenciesResolved)
            {
                // Initialize GameContext for effect execution
                gameContext = new GameContext(
                    null,
                    unitService,
                    gridController,
                    this,  // CardSpawnService itself
                    spawnValidator,
                    TeamType.Player,  // CasterTeam - will be updated per card usage
                    Vector2Int.zero   // OriginPosition - will be updated per card usage
                );

                isInitialized = true;
                Log("✅ CardSpawnService initialization completed with GameContext");
            }
            else
            {
                LogError("❌ CardSpawnService initialization failed - missing dependencies");
            }
        }

        /// <summary>
        /// 외부에서 주입받은 서비스 의존성 설정
        /// </summary>
        private void InjectDependencies(IUnitService iUnitService, IGridController iGridController,
                                       IGridState iGridState, ISpawnValidator iSpawnValidator,
                                       IResourceManager iResourceManager)
        {
            unitService = iUnitService;
            if (unitService != null)
                Log("✅ UnitService dependency injected successfully");
            else
                LogError("❌ UnitService is null");

            gridController = iGridController;
            if (gridController != null)
                Log("✅ GridController dependency injected successfully");
            else
                LogError("❌ GridController is null");

            gridState = iGridState;
            if (gridState != null)
                Log("✅ GridState dependency injected successfully");
            else
                LogError("❌ GridState is null");

            spawnValidator = iSpawnValidator;
            if (spawnValidator != null)
                Log("✅ SpawnValidator dependency injected successfully");
            else
                LogError("❌ SpawnValidator is null");

            resourceManager = iResourceManager;
            if (resourceManager != null)
                Log("✅ ResourceManager dependency injected successfully");
            else
                LogError("❌ ResourceManager is null");
        }

        #endregion

        #region 통합 카드 처리 (효과 기반)

        /// <summary>
        /// 카드를 사용하여 모든 효과를 실행 (기본: 플레이어)
        /// </summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>실행 성공 여부</returns>
        public bool TryExecuteCard(CardData cardData, Vector2Int targetPosition)
        {
            return TryExecuteCard(cardData, targetPosition, TeamType.Player);
        }

        /// <summary>
        /// 카드를 사용하여 모든 효과를 실행 (팀 지정)
        /// </summary>
        /// <param name="cardData">사용할 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="casterTeam">카드를 사용한 팀</param>
        /// <returns>실행 성공 여부</returns>
        public bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, TeamType casterTeam)
        {
            if (!isInitialized)
            {
                LogError("CardSpawnService not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot execute null CardData");
                return false;
            }

            // Check if card uses effect system (EffectDefinition 기반)
            if (!cardData.IsEffectBasedCard)
            {
                LogError($"Card {cardData.CardName} does not use effect system (no EffectDefinitions)");
                return false;
            }

            Log($"🎯 Executing card: {cardData.CardName} at position {targetPosition} (Team: {casterTeam})");

            // Update GameContext for this card execution
            UpdateGameContext(casterTeam, targetPosition, cardData);

            // 1. Resource validation and spending
            bool isPlayerCard = (casterTeam == TeamType.Player);
            if (resourceManager != null && !resourceManager.SpendResources(isPlayerCard, cardData.ManaCost))
            {
                LogError($"❌ Failed to spend resources for {cardData.CardName}");
                return false;
            }

            // 2. Execute all card effects using factory pattern
            bool allEffectsSuccess = ExecuteAllCardEffects(cardData, targetPosition, casterTeam);

            if (!allEffectsSuccess)
            {
                LogError($"❌ One or more effects failed for {cardData.CardName}");
                // Restore resources on failure
                RestoreResources(cardData, casterTeam);
                return false;
            }

            Log($"✅ Card {cardData.CardName} successfully executed at {targetPosition}");
            return true;
        }

        /// <summary>
        /// GameContext 업데이트
        /// </summary>
        /// <param name="casterTeam">카드를 사용한 팀</param>
        /// <param name="originPosition">원점 위치</param>
        private void UpdateGameContext(TeamType casterTeam, Vector2Int originPosition, CardData cardData)
        {
            if (gameContext != null)
            {
                // GameContext는 immutable이므로 새로 생성
                gameContext = new GameContext(
                    cardData,
                    unitService,
                    gridController,
                    this,
                    spawnValidator,
                    casterTeam,
                    originPosition
                );
            }
        }

        /// <summary>
        /// <summary>
        /// 카드의 모든 효과를 실행하는 핵심 메서드
        /// EffectDefinition 기반 시스템만 사용합니다.
        /// </summary>
        private bool ExecuteAllCardEffects(CardData cardData, Vector2Int targetPosition, TeamType casterTeam)
        {
            var effectDefinitions = cardData.EffectDefinitions;

            if (effectDefinitions.Count > 0)
            {
                Log($"🔄 Executing {effectDefinitions.Count} EffectDefinitions for {cardData.CardName}");

                bool hasVFXNew = effectDefinitions.Any(e => e != null && e.VFX != null && e.VFX.VFXPrefab != null);
                if (hasVFXNew)
                {
                    var executor = ServiceLocator.Get<ISpellEffectExecutor>();
                    if (executor != null)
                    {
                        Log($"🎬 VFX detected (EffectDefinition) - delegating to SpellEffectExecutor for {cardData.CardName}");
                        executor.ExecuteBatch(effectDefinitions, targetPosition, gameContext);
                        return true;
                    }
                    else
                    {
                        LogError("❌ ISpellEffectExecutor not found in ServiceLocator (EffectDefinition), falling back to immediate execution");
                    }
                }

                Log($"⚡ No VFX or executor unavailable (EffectDefinition) - executing effects immediately for {cardData.CardName}");
                var cardEffects = CardEffectFactory.CreateEffects(effectDefinitions);
                if (cardEffects.Count == 0)
                {
                    LogError($"Failed to create any effects from EffectDefinitions for {cardData.CardName}");
                    return false;
                }

                int successCount = 0;
                foreach (var effect in cardEffects)
                {
                    try
                    {
                        if (!effect.CanExecute(targetPosition, gameContext))
                        {
                            LogError($"❌ Effect {effect.GetType().Name} cannot be executed at {targetPosition}");
                            continue;
                        }

                        effect.Execute(targetPosition, gameContext);
                        Log($"✅ Effect {effect.GetType().Name} executed successfully");
                        successCount++;
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"❌ Exception executing effect {effect.GetType().Name} (EffectDefinition): {ex.Message}");
                        return false;
                    }
                }

                Log($"📊 {successCount}/{cardEffects.Count} EffectDefinition-based effects executed successfully for {cardData.CardName}");
                return successCount > 0;
            }

            // EffectDefinitions가 비어있는 카드는 효과가 없는 것으로 간주
            LogError($"Card {cardData.CardName} has no EffectDefinitions to execute");
            return false;
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 팀에 따라 자원을 복구하는 헬퍼 메서드
        /// </summary>
        /// <param name="cardData">복구할 카드의 데이터</param>
        /// <param name="casterTeam">카드를 사용한 팀</param>
        private void RestoreResources(CardData cardData, TeamType casterTeam)
        {
            if (resourceManager == null) return;

            if (casterTeam == TeamType.Player)
            {
                resourceManager.RestorePlayerResources(cardData.ManaCost);
                Log($"🔄 Restored {cardData.ManaCost}M to Player");
            }
            else
            {
                resourceManager.RestoreEnemyResources(cardData.ManaCost);
                Log($"🔄 Restored {cardData.ManaCost}M to Enemy");
            }
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[CardSpawnService] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CardSpawnService] {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 소환 서비스 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"CardSpawnService Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- UnitService Available: {(unitService != null ? "✅" : "❌")}\n" +
                   $"- GridController Available: {(gridController != null ? "✅" : "❌")}\n" +
                   $"- SpawnValidator Available: {(spawnValidator != null ? "✅" : "❌")}\n" +
                   $"- ResourceManager Available: {(resourceManager != null ? "✅" : "❌")}\n" +
                   $"- GameContext Available: {(gameContext != null ? "✅" : "❌")}\n";
        }

        #endregion
    }
}
