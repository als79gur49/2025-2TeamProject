# Base 다중 타일 점유 및 범위 공격 중복 제거 시스템 설계

## 📋 요구사항

1. **TeamComponent 사용**: `int teamId` 대신 `TeamComponent`를 사용하여 `TeamType` enum으로 팀 구분
2. **다중 타일 점유**: Base는 여러 개의 타일을 점유 (예: 1x3, 2x2 크기)
3. **범위 공격 중복 제거**: `HashSet<HealthComponent>`를 사용하여 동일한 Base가 한 번의 범위 공격에 여러 번 피해받지 않도록 방지
4. **Unit의 Base 공격 지원**: CombatComponent를 통한 Base 타겟팅 가능

---

## ⚠️ CombatComponent 고려사항

### Base는 CombatComponent 불필요

**설계 원칙:**
- Base는 **방어 전용 고정 구조물**
- 능동적 공격 불가 → **CombatComponent 필요 없음**
- HealthComponent만으로 피해 처리 가능

```csharp
// ✅ Base 구성
Base
├── TeamComponent  (팀 구분)
├── HealthComponent (피해 처리)
└── [NO CombatComponent]  ← 공격 불가

// ✅ Unit 구성
Unit
├── TeamComponent  (팀 구분)
├── HealthComponent (피해 처리)
└── CombatComponent (공격 가능)
```

### Unit이 Base를 공격하기 위한 요구사항

**문제점:**
기존에는 GameObject 기반으로 타겟팅하여 Base가 여러 타일을 점유할 때 중복 피해 발생

**해결책: 타일 기반 공격 시스템**
- Unit과 Base를 GameObject로 직접 타겟팅하지 않음
- 대신 공격 범위 내 **타일(Tile)**들을 수집하여 `AttackTiles()` 메서드로 공격
- 각 타일의 `GetDamageableTarget()`이 Unit 또는 Base의 HealthComponent 반환
- HashSet으로 중복 제거하여 동일한 HealthComponent는 한 번만 피해

### CombatComponent 타일 기반 공격 메서드

```csharp
/// <summary>
/// 타일 기반 범위 공격 (DamageEffect 패턴)
/// HashSet으로 중복 제거하여 다중 타일 점유 엔티티(Base)가 중복 피해를 받지 않도록 방지
/// </summary>
/// <param name="targetTiles">공격할 타일 목록</param>
/// <param name="isSpecialAttack">특수 공격 여부</param>
/// <param name="forceCritical">강제 크리티컬 여부</param>
/// <returns>피해를 받은 고유 타겟 수</returns>
public int AttackTiles(List<Tile> targetTiles, bool isSpecialAttack = false, bool forceCritical = false)
{
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

    // ✅ 중복 제거용 HashSet (HealthComponent 인스턴스 기준)
    HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();
    int affectedCount = 0;

    // 크리티컬 판정 (범위 공격 전체에 동일 적용)
    bool isCritical = forceCritical || this.RollCritical();

    // 피해량 계산
    int baseDamage = isSpecialAttack ? CurrentAttackPower * specialAttackDamageMultiplier : CurrentAttackPower;
    int finalDamage = this.CalculateFinalDamage(baseDamage, isCritical);

    foreach (var tile in targetTiles)
    {
        if (tile == null) continue;

        // 타일에서 공격 가능한 타겟 가져오기 (유닛 우선, 없으면 Base)
        HealthComponent targetHealth = tile.GetDamageableTarget();

        // ✅ 이미 피해받은 타겟인지 확인 (Add는 새로 추가되면 true 반환)
        if (targetHealth != null && targetHealth.IsAlive && damagedTargets.Add(targetHealth))
        {
            // 팀 체크 (아군은 공격 불가)
            GameObject targetObject = targetHealth.gameObject;
            if (this.CanAttackByTeam(targetObject))
            {
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
                {
                    OnCriticalAttack?.Invoke(targetObject, result);
                }

                if (isSpecialAttack)
                {
                    OnSpecialAttack?.Invoke(targetObject, result);
                }
            }
        }
    }

    // 공격 쿨다운 적용
    lastAttackTime = Time.time;
    EnterCombat();

    Debug.Log($"[CombatComponent] AttackTiles: {targetTiles.Count}개 타일 중 {affectedCount}개 고유 타겟에게 {finalDamage} 피해 적용");

    return affectedCount;
}
```

### 타일 기반 타겟팅 시나리오

#### 시나리오: 타일 기반 범위 공격

```csharp
// Unit 주변 3x3 범위의 타일들을 공격
List<Vector2Int> attackRange = unit.CombatComponent.GetAttackRange(unitPos);

// 타일 객체로 변환
List<Tile> targetTiles = new List<Tile>();
foreach (var pos in attackRange)
{
    Tile tile = gridController.GetTileAtPosition(pos);
    if (tile != null)
    {
        targetTiles.Add(tile);
    }
}

// ✅ 타일 기반 범위 공격 실행 (Base와 Unit 모두 자동 처리)
int hitCount = unit.CombatComponent.AttackTiles(targetTiles, isSpecialAttack: false);
// → Tile.GetDamageableTarget()이 Unit 우선, Base 후순위로 타겟 반환
// → HashSet으로 중복 제거되어 Base가 여러 타일 점유해도 1번만 피해
```

---

## 🎯 핵심 설계 개념

### 다중 타일 점유 시스템

```
플레이어 좌측열 기지 예시 (1x3 크기):
┌───┬───┬───┬───┐
│ B │   │   │   │  ← Y=2
├───┼───┼───┼───┤
│ B │   │   │   │  ← Y=1
├───┼───┼───┼───┤
│ B │   │   │   │  ← Y=0
└───┴───┴───┴───┘
  X=0

Base 객체 하나가 3개의 타일 (0,0), (0,1), (0,2)를 점유
각 타일의 occupyingBase는 동일한 Base 인스턴스 참조
```

### 범위 공격 중복 제거

```csharp
// 문제: Base가 3x3 범위 공격에 2개 타일이 포함되면 2번 피해
// 해결: HashSet으로 이미 피해받은 HealthComponent 추적

HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();

foreach (var tile in targetTiles)
{
    var health = tile.GetDamageableTarget();
    if (health != null && damagedTargets.Add(health))  // Add는 새로 추가되면 true 반환
    {
        health.TakeDamage(damage);
    }
}
```

---

## 📦 컴포넌트별 설계

### 1️⃣ Base 클래스

```csharp
using UnityEngine;
using System.Collections.Generic;
using Game.Components;
using Game.Interfaces;

namespace Game
{
    /// <summary>
    /// 플레이어 기지 - 여러 타일을 점유하는 고정 구조물
    /// TeamComponent 기반 팀 시스템 사용
    /// 범위 공격 시 중복 피해 방지를 위해 HealthComponent 인스턴스 단위로 관리
    /// </summary>
    public class Base : MonoBehaviour
    {
        [Header("Base Configuration")]
        [SerializeField] private Vector2Int baseSize = new Vector2Int(1, 3);  // 가로x세로 크기
        [SerializeField] private int maxHealth = 500;

        [Header("Visual Settings")]
        [SerializeField] private GameObject basePrefab;  // 기지 시각적 프리팹

        // ✅ 컴포넌트 참조
        private TeamComponent teamComponent;
        private HealthComponent healthComponent;

        // ✅ 타일 점유 정보
        private Vector2Int startPosition;  // 좌하단 시작 위치
        private readonly List<Tile> occupiedTiles = new List<Tile>();

        // ✅ 속성
        public TeamType Team => teamComponent?.Team ?? TeamType.None;
        public Vector2Int BaseSize => baseSize;
        public Vector2Int StartPosition => startPosition;
        public bool IsAlive => healthComponent != null && healthComponent.IsAlive;
        public HealthComponent HealthComponent => healthComponent;
        public IReadOnlyList<Tile> OccupiedTiles => occupiedTiles;

        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // TeamComponent 가져오기 또는 추가
            teamComponent = GetComponent<TeamComponent>();
            if (teamComponent == null)
            {
                teamComponent = gameObject.AddComponent<TeamComponent>();
                Debug.LogWarning($"[Base] TeamComponent가 없어 자동 추가됨: {gameObject.name}");
            }

            // HealthComponent 가져오기 또는 추가
            healthComponent = GetComponent<HealthComponent>();
            if (healthComponent == null)
            {
                healthComponent = gameObject.AddComponent<HealthComponent>();
            }

            // 기지 체력 설정
            healthComponent.SetMaxHealth(maxHealth);
            healthComponent.RestoreToFullHealth();

            // 사망 이벤트 구독
            healthComponent.OnDeath += OnBaseDestroyed;
        }

        /// <summary>
        /// Base 초기화 - 시작 위치와 팀 설정
        /// GridState.PlaceBase()에서 호출
        /// </summary>
        public void Initialize(Vector2Int startPos, TeamType team)
        {
            startPosition = startPos;

            if (teamComponent != null)
            {
                teamComponent.Team = team;
            }

            Debug.Log($"[Base] Initialized at {startPos} for team {team}, size {baseSize}");
        }

        /// <summary>
        /// 타일 참조 추가
        /// </summary>
        public void AddOccupiedTile(Tile tile)
        {
            if (tile != null && !occupiedTiles.Contains(tile))
            {
                occupiedTiles.Add(tile);
            }
        }

        /// <summary>
        /// 타일 참조 제거
        /// </summary>
        public void RemoveOccupiedTile(Tile tile)
        {
            occupiedTiles.Remove(tile);
        }

        /// <summary>
        /// 특정 타일이 이 Base에 속하는지 확인
        /// </summary>
        public bool ContainsTile(Vector2Int tilePos)
        {
            return tilePos.x >= startPosition.x && tilePos.x < startPosition.x + baseSize.x &&
                   tilePos.y >= startPosition.y && tilePos.y < startPosition.y + baseSize.y;
        }

        /// <summary>
        /// Base가 점유하는 모든 타일 위치 반환
        /// </summary>
        public List<Vector2Int> GetOccupiedPositions()
        {
            var positions = new List<Vector2Int>();

            for (int x = 0; x < baseSize.x; x++)
            {
                for (int y = 0; y < baseSize.y; y++)
                {
                    positions.Add(new Vector2Int(startPosition.x + x, startPosition.y + y));
                }
            }

            return positions;
        }

        /// <summary>
        /// Base 파괴 시 처리
        /// </summary>
        private void OnBaseDestroyed()
        {
            Debug.Log($"[Base] Team {Team} base destroyed at {startPosition}!");

            // TODO: GameManager에 게임 오버 알림
            // GameManager.Instance?.OnBaseDestroyed(Team);

            // 점유 타일 정리
            foreach (var tile in occupiedTiles)
            {
                if (tile != null)
                {
                    tile.RemoveBase();
                }
            }
            occupiedTiles.Clear();

            // 오브젝트 파괴
            Destroy(gameObject, 1f);
        }

        private void OnDestroy()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= OnBaseDestroyed;
            }
        }

        /// <summary>
        /// 디버깅용 Gizmo
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (teamComponent != null)
            {
                Gizmos.color = teamComponent.TeamColor;
            }
            else
            {
                Gizmos.color = Color.cyan;
            }

            // Base 점유 영역 표시
            Vector3 center = transform.position;
            Vector3 size = new Vector3(baseSize.x, 1f, baseSize.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
```

---

### 2️⃣ Tile 클래스 수정

**현재 Tile 클래스는 Unit만 추적하므로, Base 지원을 위해 다음 수정이 필요합니다:**

```csharp
public class Tile : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    // ✅ 2-Layer 시스템 (향후 구현)
    [SerializeField] private Unit occupyingUnit;    // 상위 레이어 (현재 구현됨)
    [SerializeField] private Base occupyingBase;    // 하위 레이어 (향후 추가 필요)

    private bool isOccupied = false;  // 유닛 점유
    private bool hasBase = false;     // 기지 존재 (향후 추가)

    public Unit OccupyingUnit => occupyingUnit;
    public Base OccupyingBase => occupyingBase;  // 향후 추가
    public bool HasBase => hasBase;              // 향후 추가

    /// <summary>
    /// 공격 우선순위에 따라 HealthComponent 반환
    /// 유닛 우선 → 기지 후순위
    /// ⚠️ 중요: 범위 공격 시 동일한 HealthComponent가 반환될 수 있음
    /// 호출하는 쪽에서 HashSet으로 중복 제거 필요
    ///
    /// ✅ CombatComponent.AttackTiles()와 DamageEffect.Execute()에서 사용
    /// </summary>
    public HealthComponent GetDamageableTarget()
    {
        // 유닛이 있으면 유닛의 HealthComponent (각 유닛은 고유 인스턴스)
        if (occupyingUnit != null)
        {
            var unitHealth = occupyingUnit.GetComponent<HealthComponent>();
            if (unitHealth != null && unitHealth.IsAlive)
                return unitHealth;
        }

        // 유닛이 없으면 기지의 HealthComponent
        // ⚠️ 여러 타일이 같은 Base를 참조하므로 동일한 HealthComponent 반환 가능
        if (occupyingBase != null)
        {
            var baseHealth = occupyingBase.HealthComponent;
            if (baseHealth != null && baseHealth.IsAlive)
                return baseHealth;
        }

        return null;
    }

    /// <summary>
    /// 기지 배치 (유닛과 독립적)
    /// Base는 여러 타일에서 동일한 인스턴스를 참조 가능
    /// </summary>
    public bool PlaceBase(Base baseUnit)
    {
        if (hasBase)
        {
            Debug.LogWarning($"[Tile] ({x}, {y}) already has a base");
            return false;
        }

        occupyingBase = baseUnit;
        hasBase = true;

        if (baseUnit != null)
        {
            // Base에 타일 추가 (양방향 참조)
            baseUnit.AddOccupiedTile(this);
        }

        UpdateVisuals();
        return true;
    }

    /// <summary>
    /// 기지 제거
    /// </summary>
    public void RemoveBase()
    {
        if (occupyingBase != null)
        {
            occupyingBase.RemoveOccupiedTile(this);
        }

        occupyingBase = null;
        hasBase = false;
        UpdateVisuals();
    }

    /// <summary>
    /// 유닛 배치 가능 여부 (기지 존재와 무관)
    /// </summary>
    public bool CanPlaceUnit()
    {
        return !isOccupied;  // 기지가 있어도 유닛은 배치 가능
    }

    // 기존 PlaceUnit, SetOccupyingUnitLogic, RemoveUnit 메서드는 그대로 유지

    private void UpdateVisuals()
    {
        if (tileRenderer == null) return;

        // 시각화 우선순위: 유닛 > 기지 > 빈 타일
        if (isOccupied)
        {
            tileRenderer.material.color = Color.yellow;  // 유닛 있음
        }
        else if (hasBase)
        {
            tileRenderer.material.color = Color.cyan;    // 기지만 있음
        }
        else
        {
            tileRenderer.material.color = originalColor;  // 비어있음
        }
    }
}
```

---

### 3️⃣ GridState 클래스 수정

```csharp
public class GridState : MonoBehaviour, IGridState
{
    // 기존 딕셔너리
    private readonly Dictionary<GameObject, Vector2Int> unitPositions = new Dictionary<GameObject, Vector2Int>();
    private readonly Dictionary<Vector2Int, GameObject> positionUnits = new Dictionary<Vector2Int, GameObject>();

    // ✅ Base 추적: Base 객체 → 점유 타일 목록
    private readonly Dictionary<GameObject, List<Vector2Int>> basePositions =
        new Dictionary<GameObject, List<Vector2Int>>();

    // ✅ 역방향 조회: 타일 위치 → Base 객체 (빠른 조회용)
    private readonly Dictionary<Vector2Int, GameObject> positionToBase =
        new Dictionary<Vector2Int, GameObject>();

    /// <summary>
    /// Base 배치 - 시작 위치와 크기 기반으로 여러 타일 점유
    /// </summary>
    public bool PlaceBase(GameObject baseObject, Vector2Int startPosition, Vector2Int baseSize, TeamType team)
    {
        if (baseObject == null)
        {
            Debug.LogWarning("[GridState] Cannot place null base object");
            return false;
        }

        // Base 컴포넌트 확인
        Base baseComponent = baseObject.GetComponent<Base>();
        if (baseComponent == null)
        {
            Debug.LogError($"[GridState] Object {baseObject.name} does not have Base component");
            return false;
        }

        // 모든 타일이 유효하고 비어있는지 확인
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = 0; x < baseSize.x; x++)
        {
            for (int y = 0; y < baseSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(startPosition.x + x, startPosition.y + y);

                if (!IsValidPosition(pos))
                {
                    Debug.LogWarning($"[GridState] Invalid position for base: {pos}");
                    return false;
                }

                if (positionToBase.ContainsKey(pos))
                {
                    Debug.LogWarning($"[GridState] Position {pos} already has a base");
                    return false;
                }

                positions.Add(pos);
            }
        }

        // Base 초기화
        baseComponent.Initialize(startPosition, team);

        // 모든 타일에 Base 배치
        foreach (var position in positions)
        {
            positionToBase[position] = baseObject;
            UpdatePhysicalTileBase(position, baseObject);
        }

        // Base → 타일 목록 매핑 저장
        basePositions[baseObject] = positions;

        Debug.Log($"[GridState] Base placed at {startPosition} with size {baseSize}, occupying {positions.Count} tiles");
        return true;
    }

    /// <summary>
    /// Base 제거 - 점유한 모든 타일에서 제거
    /// </summary>
    public bool RemoveBase(GameObject baseObject)
    {
        if (!basePositions.TryGetValue(baseObject, out var positions))
        {
            Debug.LogWarning("[GridState] Base not found in tracking");
            return false;
        }

        // 모든 타일에서 Base 제거
        foreach (var position in positions)
        {
            positionToBase.Remove(position);
            UpdatePhysicalTileBase(position, null);
        }

        basePositions.Remove(baseObject);

        Debug.Log($"[GridState] Base removed from {positions.Count} tiles");
        return true;
    }

    /// <summary>
    /// 특정 위치의 Base 반환
    /// </summary>
    public GameObject GetBaseAtPosition(Vector2Int position)
    {
        return positionToBase.GetValueOrDefault(position);
    }

    /// <summary>
    /// Base가 점유한 모든 타일 위치 반환
    /// </summary>
    public List<Vector2Int> GetBaseOccupiedPositions(GameObject baseObject)
    {
        return basePositions.GetValueOrDefault(baseObject, new List<Vector2Int>());
    }

    /// <summary>
    /// 모든 Base 객체 반환
    /// </summary>
    public IEnumerable<GameObject> GetAllBases()
    {
        return basePositions.Keys;
    }

    /// <summary>
    /// 물리적 Tile의 기지 상태 업데이트
    /// </summary>
    private void UpdatePhysicalTileBase(Vector2Int position, GameObject baseObject)
    {
        if (!IsValidPosition(position))
            return;

        GameObject tileObject = GameObject.Find($"Tile_{position.x}_{position.y}");
        if (tileObject != null)
        {
            Tile tile = tileObject.GetComponent<Tile>();
            if (tile != null)
            {
                if (baseObject != null)
                {
                    Base baseComponent = baseObject.GetComponent<Base>();
                    if (baseComponent != null)
                    {
                        tile.PlaceBase(baseComponent);
                    }
                }
                else
                {
                    tile.RemoveBase();
                }
            }
        }
    }

    /// <summary>
    /// 모든 상태 초기화 (Base 포함)
    /// </summary>
    public void ClearAllState()
    {
        unitPositions.Clear();
        positionUnits.Clear();
        blockedPositions.Clear();

        // ✅ Base 정리
        basePositions.Clear();
        positionToBase.Clear();

        ClearAllHighlights();

        Debug.Log("[GridState] All state cleared (including bases)");
    }
}
```

---

### 4️⃣ DamageEffect 클래스 수정

```csharp
using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.VFX;
using Game.Components;

namespace Game.Card.Effects
{
    /// <summary>
    /// 데미지 효과 - 범위 공격 시 중복 제거 적용
    /// HashSet을 사용하여 동일한 HealthComponent에 중복 피해 방지
    /// </summary>
    public class DamageEffect : IVFXAwareEffect
    {
        private readonly EffectData _effectData;

        public EffectType EffectType => EffectType.Damage;
        public int Priority => _effectData?.Priority ?? 0;

        public DamageEffect(EffectData effectData)
        {
            _effectData = effectData ?? throw new System.ArgumentNullException(nameof(effectData));

            if (_effectData.Type != EffectType.Damage)
            {
                throw new System.ArgumentException($"EffectData의 타입이 Damage가 아닙니다: {_effectData.Type}");
            }
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_effectData == null || !_effectData.IsValid())
            {
                Debug.LogWarning("DamageEffect: 유효하지 않은 EffectData입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("DamageEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            return context.PredeterminedTiles != null && context.PredeterminedTiles.Count > 0;
        }

        /// <summary>
        /// ✅ 범위 공격 중복 제거 적용
        /// HashSet으로 동일한 HealthComponent에 중복 피해 방지
        /// </summary>
        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("DamageEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            var damageAmount = _effectData.Value;

            // ✅ 중복 제거용 HashSet
            HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();
            int affectedCount = 0;

            foreach (var tile in context.PredeterminedTiles)
            {
                if (tile == null) continue;

                // 타일에서 공격 가능한 타겟 가져오기 (유닛 우선, 없으면 기지)
                HealthComponent targetHealth = tile.GetDamageableTarget();

                // ✅ 이미 피해받은 타겟인지 확인 (Add는 새로 추가되면 true 반환)
                if (targetHealth != null && targetHealth.IsAlive && damagedTargets.Add(targetHealth))
                {
                    targetHealth.TakeDamage(damageAmount);
                    affectedCount++;
                }
            }

            Debug.Log($"DamageEffect: {context.PredeterminedTiles.Count}개 타일 중 {affectedCount}개 고유 타겟에게 {damageAmount} 피해를 적용했습니다.");

            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// ✅ VFX 데이터 포함 실행 - 범위 공격 중복 제거 적용
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[DamageEffect] Attack failed: {triggerData.ValidationFailureReason}");
                return;
            }

            var targetTile = context.GridController.GetTileAtPosition(
                new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

            // ✅ 단일 타겟 공격 (VFX 트리거는 보통 단일 타일)
            HealthComponent targetHealth = targetTile?.GetDamageableTarget();

            if (targetHealth != null && targetHealth.IsAlive)
            {
                var damageAmount = _effectData.Value;
                targetHealth.TakeDamage(damageAmount);

                Debug.Log($"[DamageEffect] {damageAmount} damage to target at {triggerData.TileGridPosition}");
            }

            PlayDamageEffect(triggerData.TileWorldPosition, context);
        }

        private void PlayDamageEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[DamageEffect] Playing damage effect at world position {worldPos}");

            if (_effectData.EffectPrefab != null)
            {
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }
        }

        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            if (_effectData.EffectPrefab != null)
            {
                var worldPos = new Vector3(targetPos.x, 0, targetPos.y);
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }

            if (!string.IsNullOrEmpty(_effectData.EffectAnimation))
            {
                Debug.Log($"DamageEffect: 애니메이션 재생 - {_effectData.EffectAnimation}");
            }
        }

        public override string ToString()
        {
            return $"DamageEffect[Value: {_effectData?.Value}, AffectedType: {_effectData?.AffectedType}, Range: {_effectData?.AffectedRange}]";
        }
    }
}
```

---

### 5️⃣ HealEffect 클래스 수정

```csharp
using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;
using Game.VFX;
using Game.Components;

namespace Game.Card.Effects
{
    /// <summary>
    /// 회복 효과 - 범위 회복 시 중복 제거 적용
    /// HashSet을 사용하여 동일한 HealthComponent에 중복 회복 방지
    /// </summary>
    public class HealEffect : IVFXAwareEffect
    {
        private readonly EffectData _effectData;

        public EffectType EffectType => EffectType.Heal;
        public int Priority => _effectData?.Priority ?? 0;

        public HealEffect(EffectData effectData)
        {
            _effectData = effectData ?? throw new System.ArgumentNullException(nameof(effectData));

            if (_effectData.Type != EffectType.Heal)
            {
                throw new System.ArgumentException($"EffectData의 타입이 Heal이 아닙니다: {_effectData.Type}");
            }
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_effectData == null || !_effectData.IsValid())
            {
                Debug.LogWarning("HealEffect: 유효하지 않은 EffectData입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("HealEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            return context.PredeterminedTiles != null && context.PredeterminedTiles.Count > 0;
        }

        /// <summary>
        /// ✅ 범위 회복 중복 제거 적용
        /// HashSet으로 동일한 HealthComponent에 중복 회복 방지
        /// </summary>
        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("HealEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            var healAmount = _effectData.Value;

            // ✅ 중복 제거용 HashSet
            HashSet<HealthComponent> healedTargets = new HashSet<HealthComponent>();
            int affectedCount = 0;

            foreach (var tile in context.PredeterminedTiles)
            {
                if (tile == null) continue;

                // 타일에서 회복 가능한 타겟 가져오기
                HealthComponent targetHealth = tile.GetDamageableTarget();

                // ✅ 이미 회복받은 타겟인지 확인
                if (targetHealth != null && targetHealth.IsAlive && healedTargets.Add(targetHealth))
                {
                    targetHealth.Heal(healAmount);
                    affectedCount++;
                }
            }

            Debug.Log($"HealEffect: {context.PredeterminedTiles.Count}개 타일 중 {affectedCount}개 고유 타겟을 {healAmount}만큼 회복시켰습니다.");

            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// ✅ VFX 데이터 포함 실행
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[HealEffect] Heal failed: {triggerData.ValidationFailureReason}");
                return;
            }

            var targetTile = context.GridController.GetTileAtPosition(
                new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

            HealthComponent targetHealth = targetTile?.GetDamageableTarget();

            if (targetHealth != null && targetHealth.IsAlive)
            {
                var healAmount = _effectData.Value;
                targetHealth.Heal(healAmount);

                Debug.Log($"[HealEffect] {healAmount} heal to target at {triggerData.TileGridPosition}");
            }

            PlayHealEffect(triggerData.TileWorldPosition, context);
        }

        private void PlayHealEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[HealEffect] Playing heal effect at world position {worldPos}");
        }

        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            Debug.Log($"[HealEffect] Visual effect requested at {targetPos}");
        }

        public override string ToString()
        {
            return $"HealEffect[Value: {_effectData?.Value}, AffectedType: {_effectData?.AffectedType}, Range: {_effectData?.AffectedRange}]";
        }
    }
}
```

---

## 🔄 동작 시나리오

### 시나리오 1: Base 배치

```csharp
// 플레이어 좌측열에 1x3 기지 배치
GameObject playerBase = Instantiate(basePrefab);
Vector2Int startPos = new Vector2Int(0, 0);  // 최하단
Vector2Int baseSize = new Vector2Int(1, 3);  // 가로1 x 세로3

gridState.PlaceBase(playerBase, startPos, baseSize, TeamType.Player);

// 결과: (0,0), (0,1), (0,2) 타일이 모두 같은 Base 인스턴스 참조
```

### 시나리오 2: 단일 타일 공격

```csharp
// (0, 1) 타일 공격 - Base의 중간 타일
Tile targetTile = gridController.GetTileAtPosition(new Vector2Int(0, 1));
HealthComponent target = targetTile.GetDamageableTarget();

// Base의 HealthComponent 반환
target.TakeDamage(50);  // Base가 50 피해
```

### 시나리오 3: 범위 공격 (중복 제거)

```csharp
// 3x3 범위 공격이 (0,0), (0,1), (0,2) 모두 포함
List<Tile> targetTiles = new List<Tile> {
    gridController.GetTileAtPosition(new Vector2Int(0, 0)),
    gridController.GetTileAtPosition(new Vector2Int(0, 1)),
    gridController.GetTileAtPosition(new Vector2Int(0, 2))
};

HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();

foreach (var tile in targetTiles)
{
    HealthComponent target = tile.GetDamageableTarget();

    // ✅ 3개 타일이 모두 같은 Base.HealthComponent를 반환
    // HashSet.Add()는 이미 있으면 false 반환
    if (target != null && damagedTargets.Add(target))
    {
        target.TakeDamage(100);  // Base가 100 피해 (1번만!)
    }
}

// 결과: Base는 300이 아닌 100 피해만 받음
```

### 시나리오 4: 유닛 + Base 혼합 범위 공격

```csharp
// (0, 1) 타일에 유닛 배치 (Base는 그대로 유지)
Unit unit = Instantiate(unitPrefab);
gridController.PlaceUnit(unit, new Vector2Int(0, 1));

// 3개 타일 범위 공격
HashSet<HealthComponent> damagedTargets = new HashSet<HealthComponent>();

// (0, 0) - Base만 있음 → Base.HealthComponent
// (0, 1) - Unit + Base → Unit.HealthComponent (우선순위)
// (0, 2) - Base만 있음 → Base.HealthComponent

foreach (var tile in targetTiles)
{
    HealthComponent target = tile.GetDamageableTarget();
    if (target != null && damagedTargets.Add(target))
    {
        target.TakeDamage(100);
    }
}

// 결과:
// - Unit: 100 피해
// - Base: 100 피해 (2개 타일이지만 중복 제거로 1번만)
```

### 시나리오 5: CombatComponent.AttackTiles() 사용

```csharp
// Unit이 자신의 공격 범위 타일들을 공격
Unit attackingUnit = ...;
CombatComponent combat = attackingUnit.GetComponent<CombatComponent>();

// 공격 범위 타일 가져오기
Vector2Int unitPos = gridManager.GetUnitPosition(attackingUnit.gameObject);
List<Vector2Int> attackRange = combat.GetAttackRange(unitPos);

// 타일 객체로 변환
List<Tile> targetTiles = new List<Tile>();
foreach (var pos in attackRange)
{
    Tile tile = gridController.GetTileAtPosition(pos);
    if (tile != null)
    {
        targetTiles.Add(tile);
    }
}

// ✅ 타일 기반 범위 공격 실행 (중복 제거 자동 적용)
int hitCount = combat.AttackTiles(targetTiles, isSpecialAttack: false, forceCritical: false);

Debug.Log($"공격 결과: {targetTiles.Count}개 타일 중 {hitCount}개 고유 타겟 피해");
// 결과: Base가 여러 타일 점유해도 1번만 피해
```

---

## ✅ 변경 사항 요약

| 컴포넌트 | 변경 종류 | 주요 변경 내용 |
|---------|---------|-------------|
| **Base** | 신규 생성 | TeamComponent 사용, 다중 타일 점유, List<Tile> 관리, CombatComponent 없음 |
| **Tile** | 중규모 수정 | GetDamageableTarget() 메서드 추가 (Unit 우선, Base 후순위), PlaceBase/RemoveBase 양방향 참조 |
| **GridState** | 중규모 수정 | basePositions, positionToBase 딕셔너리 추가, Base 배치/제거/조회 메서드 구현 |
| **CombatComponent** | 중규모 수정 | **AttackTiles() 메서드 추가** (타일 기반 범위 공격 + HashSet 중복 제거) |
| **DamageEffect** | 중규모 수정 | HashSet<HealthComponent> 중복 제거 로직 추가 |
| **HealEffect** | 중규모 수정 | HashSet<HealthComponent> 중복 제거 로직 추가 |

---

## 🚀 구현 순서 (Phase별 TodoList)

### **Phase 1: Base 클래스 및 GridState 핵심 인프라 구현**
**목표**: Base와 GridState의 핵심 기능 구현하여 다중 타일 점유 시스템 기반 마련

#### 📦 작업 목록
- [x] **Base 클래스 생성** (TeamComponent 통합, 다중 타일 점유, CombatComponent 제외)
   - [x] TeamComponent, HealthComponent 통합
   - [x] 다중 타일 점유 필드 및 속성 추가 (occupiedTiles, baseSize, startPosition)
   - [x] Initialize(), AddOccupiedTile(), RemoveOccupiedTile() 메서드 구현
   - [x] Base 파괴 시 타일 정리 로직 구현

- [x] **GridState에 Base 추적 시스템 구현** (basePositions, positionToBase 딕셔너리 추가)
   - [x] basePositions: Base 객체 → 점유 타일 목록 매핑
   - [x] positionToBase: 타일 위치 → Base 객체 역방향 조회

- [x] **GridState PlaceBase/RemoveBase/GetBaseAtPosition 메서드 구현**
   - [x] PlaceBase(): 시작 위치와 크기 기반 다중 타일 배치
   - [x] RemoveBase(): 점유한 모든 타일에서 Base 제거
   - [x] GetBaseAtPosition(): 특정 위치의 Base 반환
   - [x] GetBaseOccupiedPositions(): Base가 점유한 타일 목록 반환

**✅ Phase 1 완료 기준**: Base 배치/제거가 정상 작동하고 GridState가 다중 타일 점유를 올바르게 추적

---

### **Phase 2: Tile 클래스 타일 기반 타겟팅 시스템 구현**
**목표**: Tile이 Unit과 Base를 모두 추적하고, 공격 우선순위에 따라 타겟 반환

#### 📦 작업 목록
- [x] **Tile 클래스에 occupyingBase 필드 및 hasBase 플래그 추가**
   - [x] occupyingBase: Base 타입 필드 추가
   - [x] hasBase: bool 플래그 추가
   - [x] OccupyingBase, HasBase 프로퍼티 노출

- [x] **Tile.GetDamageableTarget() 메서드 구현** (Unit 우선, Base 후순위)
   - [x] Unit이 있으면 Unit의 HealthComponent 반환
   - [x] Unit이 없으면 Base의 HealthComponent 반환
   - [x] null 체크 및 IsAlive 검증

- [x] **Tile.PlaceBase/RemoveBase 메서드 구현** (양방향 참조 관리)
   - [x] PlaceBase(): Base 배치 및 Base.AddOccupiedTile() 호출
   - [x] RemoveBase(): Base 제거 및 Base.RemoveOccupiedTile() 호출
   - [x] 양방향 참조 동기화 보장

- [x] **Tile.UpdateVisuals() 수정** (Base 시각화 지원)
   - [x] 시각화 우선순위: Unit > Base > 빈 타일
   - [x] Base만 있는 타일 색상 표시 추가

**✅ Phase 2 완료 기준**: Tile이 Unit과 Base를 올바르게 추적하고, GetDamageableTarget()이 우선순위에 따라 정확히 동작

---

### **Phase 3: CombatComponent 타일 기반 공격 시스템 구현**
**목표**: 타일 기반 범위 공격 시스템으로 중복 피해 문제 해결

#### 📦 작업 목록
- [x] **CombatComponent.AttackTiles() 메서드 구현** (타일 기반 범위 공격)
   - [x] List<Tile> targetTiles 파라미터 수용
   - [x] 공격 가능 여부 체크 (CanAttack)
   - [x] 타일 순회 및 GetDamageableTarget() 호출

- [x] **AttackTiles에 HashSet<HealthComponent> 중복 제거 로직 적용**
   - [x] HashSet<HealthComponent> damagedTargets 선언
   - [x] damagedTargets.Add()로 중복 체크 및 추가
   - [x] 동일한 HealthComponent는 1번만 피해 적용

- [x] **AttackTiles에 크리티컬, 특수 공격, 이벤트 발생 로직 통합**
   - [x] 크리티컬 판정 (RollCritical())
   - [x] 피해량 계산 (CalculateFinalDamage())
   - [x] 팀 체크 (CanAttackByTeam())
   - [x] OnAttackPerformed, OnCriticalAttack, OnSpecialAttack 이벤트 발생
   - [x] 공격 쿨다운 적용

**✅ Phase 3 완료 기준**: AttackTiles()가 타일 기반 범위 공격을 정상 실행하고, 중복 제거가 올바르게 동작

---

### **Phase 4: 카드 Effect 시스템 중복 제거 적용**
**목표**: DamageEffect와 HealEffect에 동일한 중복 제거 패턴 적용하여 일관성 확보

#### 📦 작업 목록
- [x] **DamageEffect.Execute()에 HashSet 중복 제거 로직 추가**
   - [x] HashSet<HealthComponent> damagedTargets 선언
   - [x] context.PredeterminedTiles 순회
   - [x] tile.GetDamageableTarget() 호출 및 중복 체크
   - [x] 고유 타겟에만 TakeDamage() 적용

- [x] **DamageEffect.ExecuteWithVFXData() 단일 타겟 처리 구현**
   - [x] VFXTriggerData에서 타일 위치 추출
   - [x] 단일 타겟에 대한 피해 적용
   - [x] VFX 효과 재생

- [x] **HealEffect.Execute()에 HashSet 중복 제거 로직 추가**
   - [x] HashSet<HealthComponent> healedTargets 선언
   - [x] context.PredeterminedTiles 순회
   - [x] tile.GetDamageableTarget() 호출 및 중복 체크
   - [x] 고유 타겟에만 Heal() 적용

- [x] **HealEffect.ExecuteWithVFXData() 단일 타겟 처리 구현**
   - [x] VFXTriggerData에서 타일 위치 추출
   - [x] 단일 타겟에 대한 회복 적용
   - [x] VFX 효과 재생

**✅ Phase 4 완료 기준**: DamageEffect와 HealEffect가 범위 효과 시 중복 제거를 올바르게 수행하고, AttackTiles()와 동일한 패턴 적용

---

### **Phase 5: 통합 테스트 및 검증**
**목표**: 전체 시스템의 통합 동작을 검증하고 엣지 케이스 처리 확인

#### 📦 작업 목록
- [ ] **1x3 Base 배치 테스트** (GridState.PlaceBase() 검증)
   - [ ] 플레이어/적 팀 Base 생성 및 배치
   - [ ] 다중 타일 점유 확인 (3개 타일 모두 동일 Base 참조)
   - [ ] GridState 딕셔너리 추적 확인

- [ ] **타일 기반 범위 공격 테스트** (AttackTiles() 메서드 검증)
   - [ ] Unit의 공격 범위 타일 수집
   - [ ] AttackTiles() 호출 및 피해 적용 확인
   - [ ] 공격 결과 로그 검증

- [ ] **범위 공격 중복 제거 검증** (Base 여러 타일 점유 시 1번만 피해)
   - [ ] 3x3 범위 공격이 Base의 3개 타일을 포함하는 시나리오
   - [ ] Base가 300 피해가 아닌 100 피해만 받는지 확인
   - [ ] HashSet 중복 제거 동작 검증

- [ ] **유닛 + Base 혼합 공격 시나리오 테스트**
   - [ ] Unit이 배치된 타일과 Base만 있는 타일 혼합
   - [ ] Unit 우선순위 타겟팅 확인
   - [ ] 각각 독립적으로 피해 적용 확인

- [ ] **DamageEffect/HealEffect와 AttackTiles 일관성 비교 검증**
   - 동일한 타일 범위에 대해 두 방식 결과 비교
   - 중복 제거 로직이 동일하게 동작하는지 확인
   - 피해량 및 타겟 수 일치 검증

**✅ Phase 5 완료 기준**: 모든 시나리오에서 중복 피해 없이 정상 동작하고, Unit과 Base가 올바르게 타겟팅됨

---

## 📊 Phase별 의존성 다이어그램

```
Phase 1 (Base + GridState)
    ↓
Phase 2 (Tile 타겟팅)
    ↓
Phase 3 (CombatComponent)
    ↘
Phase 4 (Effect 시스템)  ← 독립적
    ↓
Phase 5 (통합 테스트)
```

**의존성 규칙:**
- Phase 2는 Phase 1 완료 후 시작 가능
- Phase 3은 Phase 2 완료 후 시작 가능
- Phase 4는 Phase 2 완료 후 독립적으로 시작 가능 (Phase 3과 병렬 가능)
- Phase 5는 Phase 3, 4 모두 완료 후 시작

---

## 🎯 각 Phase별 예상 소요 시간

| Phase | 작업량 | 예상 시간 | 난이도 |
|-------|--------|----------|--------|
| Phase 1 | Base 클래스 + GridState | 2-3시간 | 중 |
| Phase 2 | Tile 수정 (4개 작업) | 1-2시간 | 하 |
| Phase 3 | CombatComponent (3개 작업) | 2-3시간 | 중 |
| Phase 4 | Effect 시스템 (4개 작업) | 1-2시간 | 하 |
| Phase 5 | 통합 테스트 (5개 작업) | 2-3시간 | 중 |
| **합계** | **24개 작업** | **8-13시간** | - |

---

## 🚀 구현 순서 (기존 요약)

1. **Base 클래스 생성** (TeamComponent 통합, CombatComponent 제외)
2. **GridState 구현** (Base 위치 추적, 다중 타일 관리)
3. **Tile 클래스 수정** (GetDamageableTarget() 메서드 추가, 양방향 참조)
4. **CombatComponent 수정**
   - **AttackTiles() 메서드 추가** (타일 기반 범위 공격 + HashSet 중복 제거)
5. **DamageEffect HashSet 중복 제거** 적용
6. **HealEffect HashSet 중복 제거** 적용
7. **통합 테스트**
   - 1x3 Base 배치
   - 타일 기반 범위 공격 (AttackTiles 메서드)
   - 범위 공격 중복 제거 검증
   - 유닛 + Base 혼합 공격
   - DamageEffect/HealEffect와 AttackTiles 비교

---

## 💡 주요 설계 포인트

### HashSet 선택 이유
- **O(1) 중복 체크**: Contains()와 Add() 모두 상수 시간
- **참조 기반**: HealthComponent 인스턴스 비교 (같은 Base는 같은 인스턴스)
- **간결한 코드**: `damagedTargets.Add(target)` 한 줄로 중복 체크 + 추가

### TeamComponent 통합 이점
- **표준 팀 시스템**: Unit과 동일한 팀 관리 방식
- **확장 가능**: TeamRelation, 팀 색상, 팀 배율 등 활용 가능
- **타입 안정성**: enum으로 팀 타입 명확화
- **CombatExtensions 호환**: IsValidTarget(), CanAttackByTeam() 자동 지원

### 다중 타일 점유 이점
- **현실적인 기지**: 실제 게임처럼 큰 구조물 표현
- **전략적 깊이**: 기지 배치와 보호가 게임 플레이의 일부
- **확장 가능**: 나중에 타워, 장애물 등도 동일 패턴 적용 가능

### CombatComponent 분리 이점
- **명확한 역할 구분**: Base는 방어만, Unit은 공격+방어
- **컴포넌트 경량화**: Base에 불필요한 공격 로직 제거
- **유지보수 용이성**: Base 특화 로직만 관리하면 됨
- **타입 안전성**: Base는 절대 공격하지 않음이 코드로 보장됨

### 타일 기반 공격의 이점
- **중복 제거 자동화**: GameObject 타겟팅 시 발생하는 중복 피해 문제 원천 차단
- **통합된 공격 인터페이스**: Unit/Base 구분 없이 타일 기반으로 통일
- **범위 효과 일관성**: DamageEffect, HealEffect와 동일한 패턴 사용
- **확장성**: 향후 타워, 장애물 등 다양한 타일 엔티티 추가 용이

---

## ⚠️ 주의사항

1. **양방향 참조 관리**: Base ↔ Tile 참조 동기화 필수
2. **메모리 누수 방지**: Base 파괴 시 타일 참조 정리
3. **경계 체크**: Base 크기가 그리드 범위를 벗어나지 않도록
4. **팀 초기화**: Base 생성 시 TeamComponent 초기화 필수
5. **중복 제거 일관성**: 모든 범위 효과에 HashSet 패턴 적용

---

## 📊 성능 고려사항

- **HashSet 오버헤드**: 범위 공격당 O(타겟 수) 추가 메모리, 일반적으로 10개 이하로 무시 가능
- **Dictionary 조회**: O(1) 타일 → Base 매핑 조회
- **참조 비교**: HealthComponent 인스턴스 비교는 포인터 비교 (매우 빠름)

이 설계는 **확장 가능성**, **타입 안정성**, **성능 효율성**을 모두 고려한 균형잡힌 솔루션입니다.
