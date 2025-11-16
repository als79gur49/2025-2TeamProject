using Game.Core;
using Game.Data;
using Game.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

namespace Game.Components
{
    /// <summary>
    /// 전투 시스템 컴포넌트 구현
    /// </summary>
    [System.Serializable]
    public class CombatComponent : MonoBehaviour, IAdvancedCombatSystem
    {
        [Header("기본 공격 설정")]
        [SerializeField] private int baseAttackPower = 10;
        [SerializeField] private int attackRange = 1;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private DamageType attackType = DamageType.Physical;
        [SerializeField] private AttackRangeType rangeType = AttackRangeType.Single;

        [Header("고급 전투 설정")]
        [SerializeField] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 2f;
        [SerializeField] private float armorPenetration = 0f;
        [SerializeField] private bool canCounterAttack = false;
        [SerializeField] private float counterAttackChance = 0.2f;

        [Header("특수 공격")]
        [SerializeField] private bool hasSpecialAttack = false;
        [SerializeField] private int specialAttackCooldown = 3;
        [SerializeField] private int specialAttackDamageMultiplier = 2;

        [Header("Audio Configuration")]
        [SerializeField] private SoundEventChannelSO soundEventChannel;  // Event Channel
        [SerializeField] private AudioData attackSound;              // AudioData

        // 런타임 상태
        private bool isInCombat = false;
        private float lastAttackTime = -999f;
        private int lastSpecialAttackTurn = -999;
        private List<StatModifier> attackPowerModifiers = new List<StatModifier>();

        // 공격 상태 추적 (BlendTree 애니메이션 동기화용)
        private bool isAttacking = false;
        private List<GameObject> currentAttackTargets = null;
        private bool isSpecialAttackActive = false;
        private bool isForceCritical = false;

        // 새로운 Action System 필드
        private ActionResult currentAttackResult;
        private ActionContext currentAttackContext;

        // 캐시된 컴포넌트
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private IHealthComponent healthComponent;
        private IAnimationController animationController;

        #region Unity Lifecycle

        private void Awake()
        {
            // 캐시 컴포넌트 초기화
            teamComponent = GetComponent<ITeamComponent>();
            healthComponent = GetComponent<IHealthComponent>();
            animationController = GetComponent<IAnimationController>();

            // BlendTree 애니메이션 이벤트 구독
            if (animationController != null)
            {
                animationController.OnAttackStart += OnAnimationAttackStart;
                animationController.OnAttackHit += OnAnimationAttackHit;
                animationController.OnAttackEnd += OnAnimationAttackEnd;
            }
        }

        private void OnDestroy()
        {
            // BlendTree 애니메이션 이벤트 구독 해제
            if (animationController != null)
            {
                animationController.OnAttackStart -= OnAnimationAttackStart;
                animationController.OnAttackHit -= OnAnimationAttackHit;
                animationController.OnAttackEnd -= OnAnimationAttackEnd;
            }
        }

        private void Start()
        {
            // GridManager는 ServiceLocator에서 가져오기
            gridManager = ServiceLocator.Get<IGridManager>();
        }

        private void OnValidate()
        {
            // 에디터에서 값 검증
            baseAttackPower = Mathf.Max(0, baseAttackPower);
            attackRange = Mathf.Max(1, attackRange);
            attackSpeed = Mathf.Max(0.1f, attackSpeed);
            criticalChance = Mathf.Clamp01(criticalChance);
            criticalMultiplier = Mathf.Max(1f, criticalMultiplier);
            armorPenetration = Mathf.Clamp01(armorPenetration);
            counterAttackChance = Mathf.Clamp01(counterAttackChance);
        }

        #endregion

        #region ICombatSystem Implementation

        public int BaseAttackPower => baseAttackPower;
        public int CurrentAttackPower => GetModifiedAttackPower();
        public int AttackRange => attackRange;
        public float AttackSpeed => attackSpeed;
        public bool CanAttack => healthComponent?.IsAlive == true && Time.time >= NextAttackTime;
        public bool IsInCombat => isInCombat;
        public float LastAttackTime => lastAttackTime;
        public float NextAttackTime => lastAttackTime + (1f / attackSpeed);

        public bool CanAttackTarget(GameObject target)
        {
            Debug.Log($"[CombatComponent] CanAttackTarget called for {target?.name}");

            if (!CanAttack || target == null)
            {
                Debug.Log($"[CombatComponent] Cannot attack: CanAttack={CanAttack}, target={(target == null ? "null" : "not null")}");
                return false;
            }

            if (!this.IsValidTarget(target))
            {
                Debug.Log($"[CombatComponent] {target.name} is not a valid target (no health or dead)");
                return false;
            }

            if (!this.CanAttackByTeam(target))
            {
                Debug.Log($"[CombatComponent] {target.name} cannot be attacked (same team)");
                return false;
            }

            Debug.Log($"[CombatComponent] Checking position for {target.name}...");
            if (!gridManager.TryGetPositionToAttackTarget(target, out Vector2Int targetPosition))
            {
                Debug.LogError($"[CombatComponent] TryGetPositionToAttackTarget FAILED for {target.name} (ID: {target.GetInstanceID()})");
                return false;
            }

            Debug.Log($"[CombatComponent] Target {target.name} position: {targetPosition}");
            return CanAttackPosition(targetPosition);
        }

        public bool CanAttackPosition(Vector2Int position)
        {
            if (!CanAttack) return false;
            if (gridManager == null) return false;

            var myPosition = gridManager.GetUnitPosition(gameObject);
            var attackablePositions = GetAttackRange(myPosition);
            if(!attackablePositions.Contains(position))
            {
                Debug.Log($"[CombatComponent] targetPosition is not in AttackRange");

                return false;
            }

            return true;
        }

        public CombatResult Attack(GameObject target)
        {
            if (!CanAttackTarget(target))
            {
                return CombatResult.Failed("Cannot attack target");
            }

            if (isAttacking)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} is already attacking!");
                return CombatResult.Failed("Already attacking");
            }

            // 단일 타겟을 List로 변환
            List<GameObject> targets = new List<GameObject> { target };

            // 공격 상태 시작
            isAttacking = true;
            currentAttackTargets = targets;
            isSpecialAttackActive = false;
            isForceCritical = false;
            lastAttackTime = Time.time;
            EnterCombat();

            // BlendTree 애니메이션 재생 (데미지는 OnAnimationAttackHit에서 적용)
            if (animationController != null)
            {
                Debug.Log($"[CombatComponent] Attack animation started");
                animationController.PlayAttackAnimation();

                // 임시 결과 반환 (실제 결과는 OnAnimationAttackHit 이벤트로 전달)
                return CombatResult.Hit(0, target, attackType, false, "Attack animation started");
            }
            else
            {
                // 애니메이션 없으면 즉시 데미지 적용 (fallback)
                isAttacking = false;
                int affectedCount = ApplyDamageToTargets(targets, false, false);
                currentAttackTargets = null;
                return CombatResult.Hit(0, target, attackType, false, $"Attack completed ({affectedCount} targets hit)");
            }
        }

        public CombatResult AttackPosition(Vector2Int position)
        {
            if (!CanAttackPosition(position))
            {
                return CombatResult.Failed("Cannot attack position");
            }

            var target = gridManager?.GetUnitAtPosition(position);
            if (target == null)
            {
                return CombatResult.Failed("No target at position");
            }

            return Attack(target);
        }

        public List<Vector2Int> GetAttackRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            switch (rangeType)
            {
                case AttackRangeType.Single:
                    positions.AddRange(GetSingleTargetRange(fromPosition));
                    break;
                case AttackRangeType.Line:
                    positions.AddRange(GetLineRange(fromPosition));
                    break;
                case AttackRangeType.Cross:
                    positions.AddRange(GetCrossRange(fromPosition));
                    break;
                case AttackRangeType.Square:
                    positions.AddRange(GetSquareRange(fromPosition));
                    break;
                case AttackRangeType.Circle:
                    positions.AddRange(GetCircleRange(fromPosition));
                    break;
                case AttackRangeType.Cone:
                    positions.AddRange(GetConeRange(fromPosition));
                    break;
                case AttackRangeType.All:
                    positions.AddRange(GetAllRange());
                    break;
            }

            return positions;
        }

        public List<GameObject> GetTargetsInRange(Vector2Int fromPosition)
        {
            var targets = new List<GameObject>();
            var positions = GetAttackRange(fromPosition);

            foreach (var pos in positions)
            {
                // GetAttackableTargetAtPosition: Unit 우선, 없으면 Base 반환
                var target = gridManager?.GetAttackableTargetAtPosition(pos);
                if (target != null && this.IsValidTarget(target) && this.CanAttackByTeam(target))
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        public bool IsTargetInRange(GameObject target, Vector2Int fromPosition)
        {
            if (target == null) return false;
            if (!gridManager.TryGetPositionToAttackTarget(target, out Vector2Int targetPosition))
                return false;

            var attackablePositions = GetAttackRange(fromPosition);
            return attackablePositions.Contains(targetPosition);
        }

        public void SetBaseAttackPower(int newAttackPower)
        {
            var oldPower = baseAttackPower;
            baseAttackPower = Mathf.Max(0, newAttackPower);
            if (oldPower != baseAttackPower)
            {
                OnAttackPowerChanged?.Invoke(CurrentAttackPower);
            }
        }

        public void ModifyAttackPower(int modifier)
        {
            SetBaseAttackPower(baseAttackPower + modifier);
        }

        public void SetAttackRange(int newRange)
        {
            attackRange = Mathf.Max(1, newRange);
        }

        public void SetAttackSpeed(float newSpeed)
        {
            attackSpeed = Mathf.Max(0.1f, newSpeed);
        }

        public void EnterCombat()
        {
            if (!isInCombat)
            {
                isInCombat = true;
                OnCombatStateChanged?.Invoke();
            }
        }

        public void ExitCombat()
        {
            if (isInCombat)
            {
                isInCombat = false;
                OnCombatStateChanged?.Invoke();
            }
        }

        public void ResetAttackCooldown()
        {
            lastAttackTime = Time.time - (1f / attackSpeed);
        }

        #endregion

        #region IAdvancedCombatSystem Implementation

        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
        public bool CanCritical => criticalChance > 0f;
        public DamageType AttackType => attackType;
        public List<DamageType> AvailableAttackTypes => new List<DamageType> { attackType };
        public bool HasSpecialAttack => hasSpecialAttack;
        public int SpecialAttackCooldown => specialAttackCooldown;
        public bool CanUseSpecialAttack => hasSpecialAttack && (Time.fixedTime - lastSpecialAttackTurn) >= specialAttackCooldown;
        public bool CanCounterAttack => canCounterAttack;
        public float CounterAttackChance => counterAttackChance;
        public int CounterAttackDamage => Mathf.RoundToInt(CurrentAttackPower * 0.7f);
        public bool CanPierceArmor => armorPenetration > 0f;
        public float ArmorPenetration => armorPenetration;

        public CombatResult PerformSpecialAttack(GameObject target)
        {
            if (!CanUseSpecialAttack)
            {
                return CombatResult.Failed("Special attack not available");
            }

            if (!CanAttackTarget(target))
            {
                return CombatResult.Failed("Cannot attack target with special attack");
            }

            if (isAttacking)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} is already attacking!");
                return CombatResult.Failed("Already attacking");
            }

            // 단일 타겟을 List로 변환
            List<GameObject> targets = new List<GameObject> { target };

            // 공격 상태 시작 (특수 공격)
            isAttacking = true;
            currentAttackTargets = targets;
            isSpecialAttackActive = true;
            isForceCritical = false;
            lastSpecialAttackTurn = (int)Time.fixedTime;
            lastAttackTime = Time.time;
            EnterCombat();

            // BlendTree 애니메이션 재생
            if (animationController != null)
            {
                animationController.PlayAttackAnimation();
                return CombatResult.Hit(0, target, attackType, false, "Special attack animation started");
            }
            else
            {
                // 애니메이션 없으면 즉시 데미지 적용
                isAttacking = false;
                int affectedCount = ApplyDamageToTargets(targets, true, false);
                currentAttackTargets = null;
                isSpecialAttackActive = false;

                return CombatResult.Hit(0, target, attackType, false, $"Special attack completed ({affectedCount} targets hit)");
            }
        }

        public CombatResult PerformCounterAttack(GameObject attacker)
        {
            if (!CanCounterAttack || !CanAttackTarget(attacker))
            {
                return CombatResult.Failed("Cannot counter attack");
            }

            if (UnityEngine.Random.Range(0f, 1f) > counterAttackChance)
            {
                return CombatResult.Failed("Counter attack chance failed");
            }

            var damage = CounterAttackDamage;
            var targetHealth = attacker.GetComponent<IHealthComponent>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
                
                var result = CombatResult.Counter(damage, attacker, attackType, "Counter attack successful");
                OnCounterAttack?.Invoke(attacker, result);
                return result;
            }

            return CombatResult.Failed("Target has no health component");
        }

        public CombatResult PerformCriticalAttack(GameObject target)
        {
            // 크리티컬 확정 공격 (강제 크리티컬)
            // 현재는 일반 Attack과 동일하지만, OnAnimationAttackHit에서 forceCritical 플래그 필요 시 확장 가능
            return Attack(target);
        }

        public void SetCriticalChance(float chance)
        {
            criticalChance = Mathf.Clamp01(chance);
        }

        public void SetCriticalMultiplier(float multiplier)
        {
            criticalMultiplier = Mathf.Max(1f, multiplier);
        }

        public void SetAttackType(DamageType type)
        {
            var oldType = attackType;
            attackType = type;
            if (oldType != attackType)
            {
                OnAttackTypeChanged?.Invoke(attackType);
            }
        }

        public void SetArmorPenetration(float penetration)
        {
            armorPenetration = Mathf.Clamp01(penetration);
        }

        #endregion

        #region Events

        public event Action<GameObject, CombatResult> OnAttackPerformed;
        public event Action<List<GameObject>> OnAttackStarted;
        public event Action OnCombatStateChanged;
        public event Action<int> OnAttackPowerChanged;
        public event Action<GameObject, CombatResult> OnCriticalAttack;
        public event Action<GameObject, CombatResult> OnSpecialAttack;
        public event Action<GameObject, CombatResult> OnCounterAttack;
        public event Action<DamageType> OnAttackTypeChanged;

        #endregion

        #region Private Methods

        /// <summary>
        /// 실제 데미지 적용 메서드 (애니메이션 없이 즉시 적용)
        /// </summary>
        private CombatResult ApplyDamageToTarget(GameObject target, bool isSpecialAttack, bool forceCritical = false)
        {
            if (target == null)
            {
                return CombatResult.Failed("Target is null");
            }

            var targetHealth = target.GetComponent<IHealthComponent>();
            if (targetHealth == null || !targetHealth.IsAlive)
            {
                return CombatResult.Failed("Target has no health or is dead");
            }

            // 크리티컬 판정
            bool isCritical = forceCritical || this.RollCritical();

            // 피해량 계산
            int baseDamage = isSpecialAttack ? CurrentAttackPower * specialAttackDamageMultiplier : CurrentAttackPower;
            int finalDamage = this.CalculateFinalDamage(baseDamage, isCritical);

            // 방어력 관통 적용
            if (CanPierceArmor && targetHealth is IAdvancedHealthComponent advancedHealth)
            {
                var damageInfo = new DamageInfo(finalDamage, attackType, gameObject, isCritical, armorPenetration > 0.5f);
                targetHealth.TakeDamage(finalDamage);
            }
            else
            {
                targetHealth.TakeDamage(finalDamage);
            }

            Debug.Log($"[CombatComponent] {gameObject.name} hit {target.name} for {finalDamage} damage" +
                      (isCritical ? " (CRITICAL!)" : "") + (isSpecialAttack ? " (SPECIAL!)" : ""));

            var result = CombatResult.Hit(finalDamage, target, attackType, isCritical,
                isSpecialAttack ? "Special attack hit!" : (isCritical ? "Critical hit!" : "Attack hit!"));

            // 사운드 출력
            if (soundEventChannel != null && attackSound != null)
            {
                soundEventChannel.RaiseSoundEvent(attackSound, this);
            }

            OnAttackPerformed?.Invoke(target, result);

            if (isCritical)
            {
                OnCriticalAttack?.Invoke(target, result);
            }

            if (isSpecialAttack)
            {
                OnSpecialAttack?.Invoke(target, result);
            }

            return result;
        }

        private int GetModifiedAttackPower()
        {
            float totalPower = baseAttackPower;
            
            foreach (var modifier in attackPowerModifiers)
            {
                totalPower = modifier.ApplyModifier(totalPower);
            }
            
            return Mathf.RoundToInt(totalPower);
        }

        private List<Vector2Int> GetSingleTargetRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            for (int range = 1; range <= attackRange; range++)
            {
                // x좌우 방향
                positions.Add(fromPosition + Vector2Int.up * range);
                positions.Add(fromPosition + Vector2Int.down * range);
                //positions.Add(fromPosition + Vector2Int.left * range);
                //positions.Add(fromPosition + Vector2Int.right * range);
            }
            
            return positions;
        }

        private List<Vector2Int> GetLineRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            // 직선 범위 (상하좌우)
            for (int i = 1; i <= attackRange; i++)
            {
                positions.Add(fromPosition + Vector2Int.up * i);
                positions.Add(fromPosition + Vector2Int.down * i);
                positions.Add(fromPosition + Vector2Int.left * i);
                positions.Add(fromPosition + Vector2Int.right * i);
            }
            
            return positions;
        }

        private List<Vector2Int> GetCrossRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            // 십자형 범위
            for (int i = 1; i <= attackRange; i++)
            {
                positions.Add(fromPosition + Vector2Int.up * i);
                positions.Add(fromPosition + Vector2Int.down * i);
                positions.Add(fromPosition + Vector2Int.left * i);
                positions.Add(fromPosition + Vector2Int.right * i);
            }
            
            return positions;
        }

        private List<Vector2Int> GetSquareRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            // 사각형 범위
            for (int x = -attackRange; x <= attackRange; x++)
            {
                for (int y = -attackRange; y <= attackRange; y++)
                {
                    if (x == 0 && y == 0) continue; // 자기 자신 제외
                    positions.Add(fromPosition + new Vector2Int(x, y));
                }
            }
            
            return positions;
        }

        private List<Vector2Int> GetCircleRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            // 원형 범위
            for (int x = -attackRange; x <= attackRange; x++)
            {
                for (int y = -attackRange; y <= attackRange; y++)
                {
                    if (x == 0 && y == 0) continue; // 자기 자신 제외
                    
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance <= attackRange)
                    {
                        positions.Add(fromPosition + new Vector2Int(x, y));
                    }
                }
            }
            
            return positions;
        }

        private List<Vector2Int> GetConeRange(Vector2Int fromPosition)
        {
            var positions = new List<Vector2Int>();
            
            // 원뿔형 범위 (임시로 앞쪽 3x3 영역)
            for (int x = -1; x <= 1; x++)
            {
                for (int y = 1; y <= attackRange; y++)
                {
                    positions.Add(fromPosition + new Vector2Int(x, y));
                }
            }
            
            return positions;
        }

        private List<Vector2Int> GetAllRange()
        {
            var positions = new List<Vector2Int>();
            
            if (gridManager != null)
            {
                var gridSize = gridManager.GridSize;
                for (int x = 0; x < gridSize.x; x++)
                {
                    for (int y = 0; y < gridSize.y; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (gridManager.GetUnitPosition(gameObject) != pos)
                        {
                            positions.Add(pos);
                        }
                    }
                }
            }
            
            return positions;
        }

        #endregion

        #region BlendTree Animation Event Handlers (MovementComponent 패턴)

        /// <summary>
        /// BlendTree 공격 시작 핸들러
        /// UnitAnimationController.OnAttackStart 이벤트 구독
        /// </summary>
        private void OnAnimationAttackStart()
        {
            if (currentAttackTargets == null || currentAttackTargets.Count == 0)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack start but no targets");
                return;
            }

            string targetNames = string.Join(", ", currentAttackTargets.ConvertAll(t => t ? t.name : "destroyed"));
            Debug.Log($"[CombatComponent] {gameObject.name}: Attack animation started on [{targetNames}]");

            // OnAttackStarted 이벤트 발생 (외부 시스템에 알림)
            OnAttackStarted?.Invoke(currentAttackTargets);
        }

        /// <summary>
        /// BlendTree 공격 타격 핸들러 (데미지 적용 시점)
        /// UnitAnimationController.OnAttackHit 이벤트 구독
        /// 공격 진행도 60% 지점에서 호출됨
        /// 새로운 Action System도 지원
        /// </summary>
        private void OnAnimationAttackHit()
        {
            // 새로운 Action System 사용 중인지 확인
            if (currentAttackResult != null)
            {
                ApplyCurrentAttackDamage();
                Debug.Log($"[CombatComponent] {gameObject.name}: Damage applied by new Action System");

                return;
            }

            // 기존 시스템: currentAttackTargets 검증
            if (currentAttackTargets == null || currentAttackTargets.Count == 0)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack hit but no current targets");
                return;
            }

            // 실제 데미지 적용 (여러 타겟 처리)
            int affectedCount = ApplyDamageToTargets(currentAttackTargets, isSpecialAttackActive, isForceCritical);

            Debug.Log($"[CombatComponent] {gameObject.name}: Damage applied to {affectedCount} targets " +
                      $"(isSpecial: {isSpecialAttackActive}, isCritical: {isForceCritical})");
        }

        /// <summary>
        /// BlendTree 공격 완료 핸들러
        /// UnitAnimationController.OnAttackEnd 이벤트 구독
        /// 새로운 Action System도 지원
        /// </summary>
        private void OnAnimationAttackEnd()
        {
            // 새로운 Action System 사용 중인지 확인
            if (currentAttackResult != null)
            {
                OnAttackCompleted();
                return;
            }

            // 기존 시스템
            if (currentAttackTargets == null || currentAttackTargets.Count == 0)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name}: Attack end but no current targets");
                return;
            }

            string targetNames = string.Join(", ", currentAttackTargets.ConvertAll(t => t ? t.name : "destroyed"));
            Debug.Log($"[CombatComponent] {gameObject.name}: Attack animation ended on [{targetNames}]");

            // 공격 상태 초기화
            isAttacking = false;
            currentAttackTargets = null;
            isSpecialAttackActive = false;
            isForceCritical = false;
        }

        #endregion

        #region Stat Modifier Support

        public void AddAttackPowerModifier(StatModifier modifier)
        {
            if (modifier != null)
            {
                attackPowerModifiers.Add(modifier);
                OnAttackPowerChanged?.Invoke(CurrentAttackPower);
            }
        }

        public void RemoveAttackPowerModifier(StatModifier modifier)
        {
            if (attackPowerModifiers.Remove(modifier))
            {
                OnAttackPowerChanged?.Invoke(CurrentAttackPower);
            }
        }

        public void ClearAttackPowerModifiers()
        {
            if (attackPowerModifiers.Count > 0)
            {
                attackPowerModifiers.Clear();
                OnAttackPowerChanged?.Invoke(CurrentAttackPower);
            }
        }

        #endregion

        #region Tile-Based Attack System

        /// <summary>
        /// 여러 타겟에게 데미지 적용 (AttackTiles 로직 추출)
        /// 단일/다중 타겟 공격 모두 사용하는 공통 데미지 적용 로직
        /// </summary>
        /// <param name="targets">공격할 타겟 GameObject 리스트</param>
        /// <param name="isSpecialAttack">특수 공격 여부</param>
        /// <param name="forceCritical">강제 크리티컬 여부</param>
        /// <returns>피해를 받은 타겟 수</returns>
        private int ApplyDamageToTargets(List<GameObject> targets, bool isSpecialAttack, bool forceCritical = false)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            int affectedCount = 0;

            // 크리티컬 판정 (전체 공격에 동일 적용)
            bool isCritical = forceCritical || this.RollCritical();

            // 피해량 계산
            int baseDamage = isSpecialAttack ? CurrentAttackPower * specialAttackDamageMultiplier : CurrentAttackPower;
            int finalDamage = this.CalculateFinalDamage(baseDamage, isCritical);

            foreach (var targetObject in targets)
            {
                if (targetObject == null) continue;

                var targetHealth = targetObject.GetComponent<IHealthComponent>();
                if (targetHealth == null || !targetHealth.IsAlive) continue;

                // 방어력 관통 적용
                if (CanPierceArmor && targetHealth is IAdvancedHealthComponent)
                {
                    var damageInfo = new DamageInfo(finalDamage, attackType, gameObject, isCritical, armorPenetration > 0.5f);
                    targetHealth.TakeDamage(finalDamage);
                }
                else
                {
                    targetHealth.TakeDamage(finalDamage);
                }

                affectedCount++;

                Debug.Log($"[CombatComponent] {gameObject.name} hit {targetObject.name} for {finalDamage} damage" +
                          (isCritical ? " (CRITICAL!)" : "") + (isSpecialAttack ? " (SPECIAL!)" : ""));

                // 이벤트 발생
                var result = CombatResult.Hit(finalDamage, targetObject, attackType, isCritical,
                    isSpecialAttack ? "Special attack hit!" : (isCritical ? "Critical hit!" : "Attack hit!"));

                OnAttackPerformed?.Invoke(targetObject, result);

                if (isCritical)
                    OnCriticalAttack?.Invoke(targetObject, result);

                if (isSpecialAttack)
                    OnSpecialAttack?.Invoke(targetObject, result);
            }

            // 사운드 출력 (한 번만)
            if (affectedCount > 0 && soundEventChannel != null && attackSound != null)
            {
                soundEventChannel.RaiseSoundEvent(attackSound, this);
            }

            return affectedCount;
        }

        /// <summary>
        /// 타일 기반 범위 공격 (애니메이션 시스템 통합)
        /// HashSet으로 중복 제거하여 다중 타일 점유 엔티티(Base)가 중복 피해를 받지 않도록 방지
        /// 애니메이션 이벤트를 통해 실제 데미지가 적용됨
        /// </summary>
        /// <param name="targetTiles">공격할 타일 목록</param>
        /// <param name="isSpecialAttack">특수 공격 여부</param>
        /// <param name="forceCritical">강제 크리티컬 여부</param>
        /// <returns>공격 대상 타겟 수</returns>
        public int AttackTiles(List<Tile> targetTiles, bool isSpecialAttack = false, bool forceCritical = false)
        {
            // 1. 검증
            if (targetTiles == null || targetTiles.Count == 0)
            {
                Debug.LogWarning("[CombatComponent] No target tiles provided");
                return 0;
            }

            if (!CanAttack)
            {
                Debug.LogWarning("[CombatComponent] Cannot attack - cooldown or dead");
                return 0;
            }

            if (isAttacking)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} is already attacking!");
                return 0;
            }

            // 2. 타겟 추출 및 검증
            var myPosition = gridManager.GetUnitPosition(gameObject);
            var attackablePositions = GetAttackRange(myPosition);

            // 중복 제거용 HashSet (HealthComponent 인스턴스 기준)
            HashSet<HealthComponent> uniqueTargets = new HashSet<HealthComponent>();
            List<GameObject> validTargets = new List<GameObject>();

            foreach (var tile in targetTiles)
            {
                if (tile == null) continue;
                if (!attackablePositions.Contains(new Vector2Int(tile.X, tile.Y))) continue;

                // 타일에서 공격 가능한 타겟 가져오기 (유닛 우선, 없으면 Base)
                HealthComponent targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive && this.CanAttackByTeam(targetHealth.gameObject))
                {
                    if (uniqueTargets.Add(targetHealth)) // 중복 제거
                    {
                        validTargets.Add(targetHealth.gameObject);
                    }
                }
            }

            if (validTargets.Count == 0)
            {
                Debug.LogWarning("[CombatComponent] No valid targets in range");
                return 0;
            }

            // 3. 공격 상태 설정
            isAttacking = true;
            currentAttackTargets = validTargets;
            isSpecialAttackActive = isSpecialAttack;
            isForceCritical = forceCritical;
            lastAttackTime = Time.time;
            EnterCombat();

            // 4. 애니메이션 재생 (실제 데미지는 OnAnimationAttackHit에서)
            if (animationController != null)
            {
                Debug.Log($"[CombatComponent] AttackTiles animation started with {validTargets.Count} targets");
                animationController.PlayAttackAnimation();
                return validTargets.Count;
            }
            else
            {
                // Fallback: 즉시 데미지 적용
                isAttacking = false;
                int affectedCount = ApplyDamageToTargets(validTargets, isSpecialAttack, forceCritical);
                currentAttackTargets = null;
                isForceCritical = false;

                Debug.Log($"[CombatComponent] AttackTiles: {targetTiles.Count}개 타일 중 {affectedCount}개 고유 타겟에게 피해 적용");
                return affectedCount;
            }
        }

        #endregion

        #region New Action System Integration

        /// <summary>
        /// ActionResult를 사용한 공격 실행 (새로운 Action System용)
        /// </summary>
        public void ExecuteAttackWithResult(ActionResult result, ActionContext context)
        {
            if (isAttacking || result == null) return;

            isAttacking = true;
            currentAttackResult = result;
            currentAttackContext = context;

            if (animationController != null)
                animationController.PlayAttackAnimation();
            else
            {
                ApplyCurrentAttackDamage();
                OnAttackCompleted();
            }
        }

        /// <summary>
        /// 현재 ActionResult 기반 데미지 적용
        /// </summary>
        private void ApplyCurrentAttackDamage()
        {
            if (currentAttackResult == null || currentAttackResult.SelectedModifier == null)
                return;

            var attackModifier = currentAttackResult.SelectedModifier as IAttackModifier;
            if (attackModifier == null) return;

            int modifierDamage = attackModifier.CalculateDamage(currentAttackContext);

            // 동일 HealthComponent(특히 Base)가 여러 타일에 걸쳐 있어도 한 번만 피해 적용
            var uniqueTargets = new HashSet<HealthComponent>();

            foreach (var tile in currentAttackResult.ValidTiles)
            {
                if (tile == null) continue;

                var targetHealth = tile.GetDamageableTarget();
                if (targetHealth != null && targetHealth.IsAlive && uniqueTargets.Add(targetHealth))
                {
                    ApplyDamageToTarget(targetHealth.gameObject, modifierDamage);
                }
            }

            TriggerAttackEffects(currentAttackResult.ValidTiles, currentAttackContext);
        }

        /// <summary>
        /// 단일 타겟에 데미지 적용 (새 시스템용)
        /// </summary>
        private void ApplyDamageToTarget(GameObject target, int baseDamage)
        {
            var targetHealth = target.GetComponent<IHealthComponent>();
            if (targetHealth == null) return;

            bool isCritical = this.RollCritical();
            int finalDamage = isCritical ? Mathf.RoundToInt(baseDamage * criticalMultiplier) : baseDamage;

            targetHealth.TakeDamage(finalDamage);

            var result = CombatResult.Hit(finalDamage, target, attackType, isCritical, "Ability");
            OnAttackPerformed?.Invoke(target, result);

            if (isCritical)
                OnCriticalAttack?.Invoke(target, result);
        }

        /// <summary>
        /// 공격 효과 트리거 (EffectManager 연동)
        /// </summary>
        private void TriggerAttackEffects(System.Collections.Generic.List<Tile> tiles, ActionContext context)
        {
            var unit = GetComponent<Unit>();
            if (unit == null || tiles == null || tiles.Count == 0) return;

            var effectContext = new Game.Core.Effects.EffectContext(tiles);
            unit.TriggerEffects(Game.Core.Effects.EffectTrigger.OnAttack, effectContext);
        }

        /// <summary>
        /// 공격 완료 처리 (새 시스템용)
        /// </summary>
        private void OnAttackCompleted()
        {
            isAttacking = false;

            // 먼저 현재 공격 데이터 클리어 (다음 행동 시작 전)
            currentAttackResult = null;
            currentAttackContext = null;

            // 그 다음 다음 행동 트리거 (새 currentAttackResult 설정 가능)
            var unit = GetComponent<Unit>();
            if (unit != null)
                unit.OnActionCompleted();
        }

        #endregion
    }
}
