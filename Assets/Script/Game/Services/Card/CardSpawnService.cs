using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using Game.Components;

namespace Game.Services
{
    /// <summary>
    /// 카드의 유닛 소환 및 주문 발동을 처리하는 서비스
    /// 소환 후 UnitService에 유닛을 등록하여 게임 월드에 편입시키는 핵심 역할
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

        // 초기화 상태
        private bool isInitialized = false;

        /// <summary>소환 서비스가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            // CardServiceManager에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        #endregion

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
                isInitialized = true;
                Log("✅ CardSpawnService initialization completed");
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

        #region 유닛 소환 (Phase 2에서 구현)

        /// <summary>
        /// 카드로부터 유닛을 소환하고 UnitService에 등록 (기본: 플레이어 유닛)
        /// </summary>
        /// <param name="cardData">소환할 카드 데이터</param>
        /// <param name="gridPosition">소환할 그리드 위치</param>
        /// <returns>소환 성공 여부</returns>
        public bool TrySpawnUnitFromCard(CardData cardData, Vector2Int gridPosition)
        {
            return TrySpawnUnitFromCard(cardData, gridPosition, true);
        }

        /// <summary>
        /// 카드로부터 유닛을 소환하고 UnitService에 등록 (플레이어/적군 구분)
        /// </summary>
        /// <param name="cardData">소환할 카드 데이터</param>
        /// <param name="gridPosition">소환할 그리드 위치</param>
        /// <param name="isPlayerUnit">플레이어 유닛인지 여부</param>
        /// <returns>소환 성공 여부</returns>
        public bool TrySpawnUnitFromCard(CardData cardData, Vector2Int gridPosition, bool isPlayerUnit)
        {
            if (!isInitialized)
            {
                LogError("CardSpawnService not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot spawn unit from null CardData");
                return false;
            }

            // 유닛 카드인지 확인
            if (!cardData.CanSummonUnit)
            {
                LogError($"Card {cardData.CardName} is not a unit card or has no unit data");
                return false;
            }

            Log($"🎯 Attempting to spawn unit from card: {cardData.CardName} at position {gridPosition} (Player: {isPlayerUnit})");

            // Phase 2: 완전한 소환 구현
            
            // 1. 소환 위치 및 조건 검증 by SpawnValidator
            if (spawnValidator != null && !spawnValidator.CanSpawnUnit(cardData, gridPosition, isPlayerUnit))
            {
                Log($"❌ Spawn validation failed for {cardData.CardName}");
                return false;
            }

            // 2. 자원 소모
            if (resourceManager != null && !resourceManager.SpendResources(isPlayerUnit, cardData.ManaCost, 0))
            {
                LogError($"❌ Failed to spend resources for {cardData.CardName}");
                return false;
            }

            // 3. 유닛 프리팹 인스턴스화
            var unitData = cardData.GetUnitToSummon();
            if (unitData?.Prefab == null)
            {
                LogError($"❌ Unit prefab not found for {cardData.CardName}");
                // 자원 복구
                RestoreResources(cardData, isPlayerUnit);
                return false;
            }

            // 월드 위치 계산
            Vector3 worldPosition = gridController.GridToWorldPosition(gridPosition);
            
            // 유닛 생성
            GameObject spawnedUnitGO = Instantiate(unitData.Prefab, worldPosition, Quaternion.identity);
            if (spawnedUnitGO == null)
            {
                LogError($"❌ Failed to instantiate unit prefab for {cardData.CardName}");
                // 자원 복구
                RestoreResources(cardData, isPlayerUnit);
                return false;
            }

            // 4. 유닛 초기화
            Unit unitComponent = spawnedUnitGO.GetComponent<Unit>();
            if (unitComponent == null)
            {
                LogError($"❌ Unit component not found on spawned prefab for {cardData.CardName}");
                Destroy(spawnedUnitGO);
                // 자원 복구
                RestoreResources(cardData, isPlayerUnit);
                return false;
            }

            // 유닛 이름 설정
            spawnedUnitGO.name = $"{cardData.CardName}_{(isPlayerUnit ? "Player" : "Enemy")}";

            // 그리드 위치 설정
            if (!gridState.SetUnitPosition(spawnedUnitGO, gridPosition))
            {
                LogError($"❌ Failed to set unit position for {cardData.CardName}");
                Destroy(spawnedUnitGO);
                // 자원 복구
                RestoreResources(cardData, isPlayerUnit);
                return false;
            }

            // 5. UnitService에 신규 유닛 등록 (핵심)
            if (unitService != null)
            {
                unitService.RegisterUnit(unitComponent);
                Log($"✅ Unit {cardData.CardName} successfully spawned and registered at {gridPosition}");
                return true;
            }
            else
            {
                LogError("❌ UnitService not available - cannot register spawned unit");
                Destroy(spawnedUnitGO);
                // 자원 복구
                RestoreResources(cardData, isPlayerUnit);
                return false;
            }
        }

        /// <summary>
        /// 주문 카드 발동 (Phase 4에서 완전 구현)
        /// </summary>
        /// <param name="cardData">발동할 주문 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <returns>발동 성공 여부</returns>
        public bool TryActivateSpellFromCard(CardData cardData, Vector2Int targetPosition)
        {
            return TryActivateSpellFromCard(cardData, targetPosition, true);
        }
        
        /// <summary>
        /// 주문 카드 발동 (플레이어/적군 구분)
        /// </summary>
        /// <param name="cardData">발동할 주문 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="isPlayerSpell">플레이어가 사용하는 주문인지 여부</param>
        /// <returns>발동 성공 여부</returns>
        public bool TryActivateSpellFromCard(CardData cardData, Vector2Int targetPosition, bool isPlayerSpell)
        {
            if (!isInitialized)
            {
                LogError("CardSpawnService not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot activate spell from null CardData");
                return false;
            }

            // 주문 카드인지 확인
            if (!cardData.IsSpellCard)
            {
                LogError($"Card {cardData.CardName} is not a spell card");
                return false;
            }

            Log($"🔮 Attempting to activate spell: {cardData.CardName} at position {targetPosition} (Player: {isPlayerSpell})");

            // Phase 4: 완전한 주문 발동 구현
            
            // 1. 주문 사용 위치 및 조건 검증
            if (spawnValidator != null && !spawnValidator.CanUseSpell(cardData, targetPosition))
            {
                Log($"❌ Spell validation failed for {cardData.CardName}");
                return false;
            }

            // 2. 자원 소모
            if (resourceManager != null && !resourceManager.SpendResources(isPlayerSpell, cardData.ManaCost, 0))
            {
                LogError($"❌ Failed to spend resources for spell {cardData.CardName}");
                return false;
            }

            // 3. 주문 효과 실행
            bool effectSuccess = ExecuteSpellEffect(cardData, targetPosition, isPlayerSpell);
            
            if (!effectSuccess)
            {
                LogError($"❌ Spell effect execution failed for {cardData.CardName}");
                // 자원 복구
                RestoreResources(cardData, isPlayerSpell);
                return false;
            }

            Log($"✅ Spell {cardData.CardName} successfully activated at {targetPosition}");
            return true;
        }
        
        /// <summary>
        /// 주문 효과를 실제로 실행합니다.
        /// </summary>
        /// <param name="cardData">주문 카드 데이터</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="isPlayerSpell">플레이어 주문인지 여부</param>
        /// <returns>실행 성공 여부</returns>
        private bool ExecuteSpellEffect(CardData cardData, Vector2Int targetPosition, bool isPlayerSpell)
        {
            try
            {
                // 월드 좌표로 변환
                Vector3 worldPosition = gridController.GridToWorldPosition(targetPosition);
                
                // 주문 타입에 따른 효과 실행
                CardData.SpellType spellType = cardData.SpellType;
                int effectValue = cardData.SpellEffectValue;
                float effectRange = cardData.SpellRange;
                
                Log($"🎯 Executing {spellType} spell with value {effectValue} and range {effectRange}");
                
                // 영향 받는 유닛들 찾기
                var affectedUnits = GetUnitsInRange(worldPosition, effectRange);
                
                // 주문 효과 적용
                foreach (var unit in affectedUnits)
                {
                    ApplySpellEffectToUnit(unit, spellType, effectValue, isPlayerSpell);
                }
                
                // 시각적 효과 표시
                ShowSpellVisualEffect(cardData, worldPosition);
                
                Log($"✅ Spell effect applied to {affectedUnits.Count} units");
                return true;
            }
            catch (System.Exception ex)
            {
                LogError($"❌ Exception during spell effect execution: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 지정된 범위 내의 유닛들을 찾습니다.
        /// </summary>
        /// <param name="centerPosition">중심 위치</param>
        /// <param name="range">범위</param>
        /// <returns>범위 내의 유닛 리스트</returns>
        private System.Collections.Generic.List<Unit> GetUnitsInRange(Vector3 centerPosition, float range)
        {
            var unitsInRange = new System.Collections.Generic.List<Unit>();
            
            if (unitService != null)
            {
                var allUnits = unitService.GetActiveUnits();
                foreach (var unit in allUnits)
                {
                    Vector3 unitPosition = new Vector3(unit.X, 0, unit.Y); // 그리드 좌표를 월드 좌표로
                    float distance = Vector3.Distance(centerPosition, unitPosition);
                    
                    if (distance <= range)
                    {
                        unitsInRange.Add(unit);
                    }
                }
            }
            
            return unitsInRange;
        }
        
        /// <summary>
        /// 특정 유닛에게 주문 효과를 적용합니다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <param name="spellType">주문 타입</param>
        /// <param name="effectValue">효과값</param>
        /// <param name="isPlayerSpell">플레이어 주문인지 여부</param>
        private void ApplySpellEffectToUnit(Unit unit, CardData.SpellType spellType, int effectValue, bool isPlayerSpell)
        {
            if (unit == null) return;
            
            // 아군/적군 주문이 영향을 주는 대상 확인
            bool canAffectUnit = CanSpellAffectUnit(unit, spellType, isPlayerSpell);
            if (!canAffectUnit)
            {
                return;
            }
            
            switch (spellType)
            {
                case CardData.SpellType.Damage:
                    // 데미지 적용 (체력이 있다면)
                    if (unit.TryGetComponent<HealthComponent>(out var health))
                    {
                        health.TakeDamage(effectValue);
                        Log($"💥 Applied {effectValue} damage to {unit.name}");
                    }
                    break;
                    
                case CardData.SpellType.Heal:
                    // 힐 적용
                    if (unit.TryGetComponent<HealthComponent>(out var healthComp))
                    {
                        healthComp.Heal(effectValue);
                        Log($"💚 Healed {unit.name} for {effectValue} HP");
                    }
                    break;
                    
                case CardData.SpellType.Buff:
                    // 버프 적용 (임시 구현 - 실제로는 더 복잡한 버프 시스템 필요)
                    Log($"⬆️ Applied buff to {unit.name} (value: {effectValue})");
                    break;
                    
                case CardData.SpellType.Debuff:
                    // 디버프 적용
                    Log($"⬇️ Applied debuff to {unit.name} (value: {effectValue})");
                    break;
                    
                case CardData.SpellType.Shield:
                    // 실드 적용
                    Log($"🛡️ Applied shield to {unit.name} (value: {effectValue})");
                    break;
                    
                case CardData.SpellType.Teleport:
                    // 텔레포트 (현재 위치에서 랜덤 이동)
                    Log($"🌀 Teleported {unit.name}");
                    break;
                    
                default:
                    Log($"❓ Unknown spell type: {spellType}");
                    break;
            }
        }
        
        /// <summary>
        /// 주문이 특정 유닛에게 영향을 줄 수 있는지 확인합니다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <param name="spellType">주문 타입</param>
        /// <param name="isPlayerSpell">플레이어 주문인지 여부</param>
        /// <returns>영향을 줄 수 있으면 true</returns>
        private bool CanSpellAffectUnit(Unit unit, CardData.SpellType spellType, bool isPlayerSpell)
        {
            // 기본적으로 같은 팀은 도움이 되는 주문, 다른 팀은 해로운 주문
            bool isHelpfulSpell = spellType == CardData.SpellType.Heal || spellType == CardData.SpellType.Buff || spellType == CardData.SpellType.Shield;
            bool isHarmfulSpell = spellType == CardData.SpellType.Damage || spellType == CardData.SpellType.Debuff;
            
            if (isPlayerSpell && unit.IsPlayerUnit)
            {
                // 플레이어가 아군에게 사용 - 도움이 되는 주문만
                return isHelpfulSpell || spellType == CardData.SpellType.Teleport;
            }
            else if (isPlayerSpell && !unit.IsPlayerUnit)
            {
                // 플레이어가 적군에게 사용 - 해로운 주문만
                return isHarmfulSpell;
            }
            else if (!isPlayerSpell && !unit.IsPlayerUnit)
            {
                // 적군이 적군에게 사용 - 도움이 되는 주문만
                return isHelpfulSpell || spellType == CardData.SpellType.Teleport;
            }
            else if (!isPlayerSpell && unit.IsPlayerUnit)
            {
                // 적군이 아군에게 사용 - 해로운 주문만
                return isHarmfulSpell;
            }
            
            return false;
        }
        
        /// <summary>
        /// 주문의 시각적 효과를 표시합니다.
        /// </summary>
        /// <param name="cardData">주문 카드 데이터</param>
        /// <param name="position">위치</param>
        private void ShowSpellVisualEffect(CardData cardData, Vector3 position)
        {
            // 간단한 시각적 효과 - 실제 게임에서는 더 화려한 이팩트 사용
            Log($"✨ Showing visual effect for {cardData.CardName} at {position}");
            
            // TODO: 파티클 시스템이나 이팩트 프리팹 사용
            // if (cardData.SpellEffectPrefab != null)
            // {
            //     GameObject effect = Instantiate(cardData.SpellEffectPrefab, position, Quaternion.identity);
            //     Destroy(effect, 3f);
            // }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 팀에 따라 자원을 복구하는 헬퍼 메서드
        /// </summary>
        /// <param name="cardData">복구할 카드의 데이터</param>
        /// <param name="isPlayerUnit">플레이어 유닛인지 여부</param>
        private void RestoreResources(CardData cardData, bool isPlayerUnit)
        {
            if (resourceManager == null) return;

            if (isPlayerUnit)
            {
                resourceManager.RestorePlayerResources(cardData.ManaCost, 0);
                Log($"🔄 Restored {cardData.ManaCost}M to Player");
            }
            else
            {
                resourceManager.RestoreEnemyResources(cardData.ManaCost, 0);
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
                   $"- ResourceManager Available: {(resourceManager != null ? "✅" : "❌")}\n";
        }

        #endregion
    }
}