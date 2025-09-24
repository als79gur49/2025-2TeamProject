# 클래스별 단일 책임 원칙(SRP) 분석 결과

## 📊 전체 분석 요약

| 클래스 | SRP 준수도 | 주요 문제점 | 우선순위 |
|--------|-----------|------------|----------|
| **Unit.cs** | ❌ 낮음 | 다중 책임, 레거시 혼재 | 🔴 최우선 |
| **HealthComponent.cs** | ⚠️ 보통 | 상태 이상/방어력 관리 포함 | 🟡 중간 |
| **MovementComponent.cs** | ⚠️ 보통 | 지형/제약 조건 관리 포함 | 🟡 중간 |
| **CombatComponent.cs** | ✅ 양호 | 경미한 의존성 문제 | 🟢 낮음 |
| **TeamComponent.cs** | ✅ 양호 | 경미한 설정 관리 문제 | 🟢 낮음 |

---

## 🔴 Unit.cs - 심각한 SRP 위반

### 현재 책임들:
1. **컴포넌트 조정** (주 책임)
2. **레거시 시스템 호환성** (기술 부채)
3. **그리드 위치 관리** (GridManager 책임)
4. **AI 행동 로직** (AIController 책임)
5. **서비스 등록** (ServiceLocator 책임)
6. **이벤트 관리** (EventManager 책임)

### 주요 문제점:
```csharp
// ❌ 그리드 관리 로직이 Unit에 포함됨
private void UpdateCurrentTile(Vector2Int gridPosition) { }
private Tile FindTileAtPosition(Vector3 worldPosition, Vector2Int gridPosition) { }
private void CreateVirtualTile(Vector2Int gridPosition) { }

// ❌ AI 행동 로직이 Unit에 포함됨  
private void Act() { }
private Unit SearchForNearbyEnemies() { }
private void AttackEnemy(Unit enemy) { }
private void MoveForward() { }

// ❌ 컴포넌트 초기화 로직이 Unit에 포함됨
private void InitializeHealthComponent() { }
private void InitializeCombatComponent() { }
```

---

## ⚠️ HealthComponent.cs - 중간 수준 SRP 위반

### 현재 책임들:
1. **체력 관리** (주 책임)
2. **상태 이상 관리** (StatusEffectComponent 책임)
3. **방어력/피해 감소** (ArmorComponent 책임)
4. **무적 상태 관리** (별도 시스템 가능)

### 문제점:
```csharp
// ❌ 상태 이상 관리가 체력 컴포넌트에 포함됨
public void ApplyPoison(int damagePerTick, float duration, float interval = 1f)
public void ApplyBleeding(int damagePerTick, float duration, float interval = 1f)
private void UpdateStatusEffects(float deltaTime)

// ❌ 방어력 계산이 체력 컴포넌트에 포함됨
public int CalculateDamageAfterArmor(int rawDamage)
private float CalculateDamageReduction(int armor)
```

---

## ⚠️ MovementComponent.cs - 중간 수준 SRP 위반

### 현재 책임들:
1. **이동 관리** (주 책임)
2. **지형 비용 관리** (TerrainManager 책임)
3. **경로 탐색** (PathfindingService 책임)
4. **특수 능력 관리** (AbilityComponent 책임)

### 문제점:
```csharp
// ❌ 지형 관리 로직이 이동 컴포넌트에 포함됨
public bool IsTerrainPassable(TerrainType terrain)
public void SetTerrainMovementCost(TerrainType terrain, int cost)

// ❌ 경로 탐색이 직접 구현됨
public List<Vector2Int> GetOptimalPath(Vector2Int targetPosition)
public int CalculatePathCost(List<Vector2Int> path)
```

---

## ✅ CombatComponent.cs - 양호한 SRP 준수

### 장점:
- 전투 관련 기능이 잘 집중됨
- 명확한 인터페이스 구현
- 이벤트 기반 통신 사용

### 경미한 개선점:
```csharp
// 🔧 StatModifier를 별도 서비스로 분리 고려
private List<StatModifier> attackPowerModifiers = new List<StatModifier>();
```

---

## ✅ TeamComponent.cs - 양호한 SRP 준수

### 장점:
- 팀 관련 기능이 명확하게 분리됨
- 관계 계산 로직이 체계적임
- 이벤트 기반 팀 변경 알림

### 경미한 개선점:
```csharp
// 🔧 TeamManager 캐싱을 별도 서비스로 분리 고려
private static TeamManager cachedTeamManager;
```

---

## 🛠️ 개선 권장사항

### 1. Unit.cs 리팩토링 (최우선)

#### A. AI 행동 로직 분리
```csharp
// ✅ 새로운 컴포넌트 생성
public class UnitAIController : MonoBehaviour
{
    private Unit unit;
    private IGridManager gridManager;
    
    public void ExecuteTurn()
    {
        var action = DecideAction();
        ExecuteAction(action);
    }
    
    private AIAction DecideAction() { /* AI 로직 */ }
}
```

#### B. 그리드 위치 관리 분리
```csharp
// ✅ 위치 추적을 별도 컴포넌트로
public class UnitPositionTracker : MonoBehaviour
{
    private IGridManager gridManager;
    private Tile currentTile;
    
    public void UpdatePosition(Vector2Int newPosition) { }
    public Tile GetCurrentTile() => currentTile;
}
```

#### C. 컴포넌트 초기화 분리
```csharp
// ✅ 팩토리 패턴 활용
public class UnitComponentFactory
{
    public static void InitializeComponents(GameObject unit)
    {
        InitializeHealthComponent(unit);
        InitializeCombatComponent(unit);
        // ...
    }
}
```

### 2. HealthComponent.cs 개선

#### A. 상태 이상 시스템 분리
```csharp
// ✅ 별도 컴포넌트로 분리
public class StatusEffectComponent : MonoBehaviour
{
    private List<StatusEffect> activeEffects;
    
    public void ApplyStatusEffect(StatusEffect effect) { }
    public void UpdateEffects(float deltaTime) { }
}
```

#### B. 방어력 시스템 분리
```csharp
// ✅ 별도 컴포넌트로 분리
public class ArmorComponent : MonoBehaviour
{
    public int CalculateDamageReduction(int rawDamage) { }
    public float GetDamageReduction() { }
}
```

### 3. MovementComponent.cs 개선

#### A. 지형 관리 분리
```csharp
// ✅ 서비스로 분리
public class TerrainService
{
    public bool IsPassable(TerrainType terrain, MovementAbility abilities) { }
    public int GetMovementCost(TerrainType terrain) { }
}
```

#### B. 경로 탐색 분리
```csharp
// ✅ 서비스로 분리
public class PathfindingService
{
    public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to, 
                                   MovementAbility abilities) { }
}
```

---

## 📈 예상 개선 효과

### 유지보수성 향상
- 각 클래스의 책임이 명확해져 코드 이해 용이
- 수정 시 영향 범위 최소화
- 단위 테스트 작성 용이

### 확장성 향상
- 새로운 기능 추가 시 기존 코드 수정 최소화
- 플러그인 방식으로 기능 추가 가능
- 다양한 유닛 타입 지원 용이

### 성능 개선
- 불필요한 의존성 제거
- 컴포넌트별 최적화 가능
- 메모리 사용량 최적화

---

## 🎯 구현 우선순위

1. **1단계**: Unit.cs AI 로직 분리 (UnitAIController 생성)
2. **2단계**: Unit.cs 위치 관리 분리 (UnitPositionTracker 생성)
3. **3단계**: StatusEffectComponent 분리
4. **4단계**: ArmorComponent 분리
5. **5단계**: TerrainService, PathfindingService 분리

이러한 리팩토링을 통해 각 클래스가 단일 책임을 가지도록 개선하여 코드의 유지보수성과 확장성을 크게 향상시킬 수 있습니다.

---

## 📝 분석 세부사항

### Unit.cs 상세 분석

**파일 위치**: `/Assets/Script/Game/Unit.cs`
**총 라인 수**: 711줄
**주요 메서드 수**: 45개

#### 책임 분포:
- **컴포넌트 관리**: 25% (InitializeComponents, 각종 Initialize 메서드)
- **AI 행동**: 20% (Act, SearchForNearbyEnemies, AttackEnemy, MoveForward)
- **그리드 위치 관리**: 30% (UpdateCurrentTile, FindTileAtPosition, CreateVirtualTile)
- **서비스 통합**: 15% (Init, RegisterWithGameServiceManager)
- **이벤트 처리**: 10% (OnUnitMovedInGrid, SetupEventSubscriptions)

#### 가장 큰 문제:
1. **God Object 패턴**: 너무 많은 책임을 한 클래스에서 처리
2. **높은 결합도**: 다른 컴포넌트들과 강하게 결합됨
3. **테스트 어려움**: 단위 테스트 작성이 복잡함

### HealthComponent.cs 상세 분석

**파일 위치**: `/Assets/Script/Game/Components/HealthComponent.cs`
**총 라인 수**: 509줄
**주요 메서드 수**: 35개

#### 개선 가능 영역:
1. **StatusEffect 관리** (100줄): 별도 컴포넌트로 분리 가능
2. **Armor 계산** (50줄): ArmorComponent로 분리 가능
3. **Invulnerability 관리** (30줄): 별도 시스템으로 분리 가능

### MovementComponent.cs 상세 분석

**파일 위치**: `/Assets/Script/Game/Components/MovementComponent.cs`
**총 라인 수**: 574줄
**주요 메서드 수**: 40개

#### 개선 가능 영역:
1. **지형 관리** (80줄): TerrainService로 분리
2. **경로 탐색** (120줄): PathfindingService로 분리
3. **특수 능력** (60줄): AbilityComponent로 분리

---

## 🔍 추가 관찰사항

### 코드 품질 지표

| 메트릭 | Unit.cs | HealthComponent.cs | MovementComponent.cs | CombatComponent.cs | TeamComponent.cs |
|--------|---------|-------------------|---------------------|-------------------|------------------|
| **복잡도** | 매우 높음 | 높음 | 높음 | 보통 | 낮음 |
| **결합도** | 매우 높음 | 보통 | 보통 | 낮음 | 낮음 |
| **응집도** | 낮음 | 보통 | 보통 | 높음 | 높음 |
| **테스트 용이성** | 어려움 | 보통 | 보통 | 쉬움 | 쉬움 |

### 리팩토링 예상 작업량

| 단계 | 예상 시간 | 난이도 | 위험도 |
|------|-----------|--------|--------|
| Unit.cs AI 분리 | 2-3일 | 중간 | 보통 |
| Unit.cs 위치 관리 분리 | 1-2일 | 중간 | 낮음 |
| StatusEffect 분리 | 1일 | 낮음 | 낮음 |
| Armor 분리 | 1일 | 낮음 | 낮음 |
| TerrainService 분리 | 2일 | 중간 | 보통 |

---

**분석 일자**: 2025-09-23  
**분석자**: Claude Code Assistant  
**버전**: 1.0