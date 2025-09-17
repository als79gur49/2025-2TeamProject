using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Interfaces;
using Game.Data;

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

        // 런타임 상태
        private bool isInCombat = false;
        private float lastAttackTime = -999f;
        private int lastSpecialAttackTurn = -999;
        private List<StatModifier> attackPowerModifiers = new List<StatModifier>();

        // 캐시된 컴포넌트
        private IGridManager gridManager;
        private ITeamComponent teamComponent;
        private IHealthComponent healthComponent;

        #region Unity Lifecycle

        private void Awake()
        {
            // 캐시 컴포넌트 초기화
            teamComponent = GetComponent<ITeamComponent>();
            healthComponent = GetComponent<IHealthComponent>();
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
            if (!CanAttack || target == null) return false;
            if (!this.IsValidTarget(target)) return false;
            if (!this.CanAttackByTeam(target)) return false;

            var targetPosition = gridManager?.GetUnitPosition(target);
            if (!targetPosition.HasValue) return false;

            return CanAttackPosition(targetPosition.Value);
        }

        public bool CanAttackPosition(Vector2Int position)
        {
            if (!CanAttack) return false;
            if (gridManager == null) return false;

            var myPosition = gridManager.GetUnitPosition(gameObject);
            var attackablePositions = GetAttackRange(myPosition);
            return attackablePositions.Contains(position);
        }

        public CombatResult Attack(GameObject target)
        {
            if (!CanAttackTarget(target))
            {
                return CombatResult.Failed("Cannot attack target");
            }

            return PerformAttack(target, false);
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

            return PerformAttack(target, false);
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
                var unit = gridManager?.GetUnitAtPosition(pos);
                if (unit != null && this.IsValidTarget(unit) && this.CanAttackByTeam(unit))
                {
                    targets.Add(unit);
                }
            }

            return targets;
        }

        public bool IsTargetInRange(GameObject target, Vector2Int fromPosition)
        {
            if (target == null) return false;
            var targetPosition = gridManager?.GetUnitPosition(target);
            if (!targetPosition.HasValue) return false;

            var attackablePositions = GetAttackRange(fromPosition);
            return attackablePositions.Contains(targetPosition.Value);
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

            lastSpecialAttackTurn = (int)Time.fixedTime;
            var result = PerformAttack(target, true);
            
            if (result.Success)
            {
                OnSpecialAttack?.Invoke(target, result);
            }

            return result;
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
            return PerformAttack(target, false, true);
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
        public event Action<GameObject> OnAttackStarted;
        public event Action<GameObject> OnAttackMissed;
        public event Action OnCombatStateChanged;
        public event Action<int> OnAttackPowerChanged;
        public event Action<GameObject, CombatResult> OnCriticalAttack;
        public event Action<GameObject, CombatResult> OnSpecialAttack;
        public event Action<GameObject, CombatResult> OnCounterAttack;
        public event Action<DamageType> OnAttackTypeChanged;

        #endregion

        #region Private Methods

        private CombatResult PerformAttack(GameObject target, bool isSpecialAttack, bool forceCritical = false)
        {
            OnAttackStarted?.Invoke(target);
            lastAttackTime = Time.time;
            EnterCombat();

            var targetHealth = target.GetComponent<IHealthComponent>();
            if (targetHealth == null)
            {
                return CombatResult.Failed("Target has no health component");
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

            var result = CombatResult.Hit(finalDamage, target, attackType, isCritical, 
                isSpecialAttack ? "Special attack hit!" : (isCritical ? "Critical hit!" : "Attack hit!"));

            OnAttackPerformed?.Invoke(target, result);

            if (isCritical)
            {
                OnCriticalAttack?.Invoke(target, result);
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
                // 상하좌우 방향
                positions.Add(fromPosition + Vector2Int.up * range);
                positions.Add(fromPosition + Vector2Int.down * range);
                positions.Add(fromPosition + Vector2Int.left * range);
                positions.Add(fromPosition + Vector2Int.right * range);
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
    }
}