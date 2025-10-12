# BaseManager & GridState 리팩토링 - PlaceBase() 책임 분리

## 📋 개요

**목적**: `GridState.PlaceBase()`를 `IGridState` 인터페이스에 추가하기 위한 구조적 리팩토링
**날짜**: 2025-10-12
**상태**: ✅ 완료

## 🔍 문제 분석

### 초기 문제점

1. **인터페이스 누락**
   - `BaseManager.cs:208`에서 `gridState.PlaceBase()` 호출
   - `PlaceBase()`가 `GridState` 구현체에만 존재
   - `IGridState` 인터페이스에 정의되지 않음
   - 타입 안전성 위반 (인터페이스가 아닌 구체 타입 의존)

2. **Single Responsibility Principle (SRP) 위반**
   ```csharp
   // GridState.cs:448 (리팩토링 전)
   public bool PlaceBase(...)
   {
       // ❌ 문제: GridState가 Base 초기화까지 담당
       baseComponent.Initialize(startPosition, team);

       // ✅ 올바른 책임: 그리드 상태 관리
       positionToBase[position] = baseObject;
   }
   ```

3. **중복 초기화**
   - `BaseManager.cs:202`: `baseComponent.Initialize(gridPosition, baseSize, team)` (3-parameter)
   - `GridState.cs:448`: `baseComponent.Initialize(startPosition, team)` (2-parameter)
   - Base.Initialize()가 두 번 호출되는 불필요한 중복

### 책임 분석

#### GridState의 올바른 책임
✅ **해야 하는 것:**
- 그리드 상태 데이터 관리 (unitPositions, positionUnits, basePositions)
- 물리적 Tile 컴포넌트 동기화
- 상태 변경 이벤트 발생

❌ **하면 안 되는 것:**
- 외부 컴포넌트 초기화 (Base, Unit 등)
- 비즈니스 로직 (Manager 계층의 책임)

#### BaseManager의 책임
✅ **해야 하는 것:**
- Base GameObject 생성 및 초기화
- Base 컴포넌트 완전 설정
- 팀 비주얼 적용
- 월드 포지션 설정
- GridState를 통한 그리드 상태 업데이트

## 🎯 해결 방안

### Phase 1: GridState.PlaceBase() 리팩토링

**변경 사항:**
```csharp
// 📁 GridState.cs
public bool PlaceBase(GameObject baseObject, Vector2Int startPosition, Vector2Int baseSize, TeamType team)
{
    // ✅ Base 초기화 제거 - BaseManager의 책임으로 이전
    // 호출자(BaseManager)가 이미 baseComponent.Initialize()를 완료했다고 가정

    // Base 컴포넌트 확인 (초기화 여부는 호출자 책임)
    Base baseComponent = baseObject.GetComponent<Base>();
    if (baseComponent == null) return false;

    // 순수 그리드 상태 관리만 수행
    foreach (var position in positions)
    {
        positionToBase[position] = baseObject;
        UpdatePhysicalTileBase(position, baseObject);
    }
    basePositions[baseObject] = positions;

    return true;
}
```

**근거:**
- `UpdateGridDataLayer()`와 동일한 패턴 적용
- Unit이 이미 초기화되었다고 가정하는 것처럼, Base도 동일하게 처리
- 단일 책임 원칙 준수

### Phase 2: IGridState 인터페이스 확장

**추가된 메서드:**
```csharp
// 📁 IGridManager.cs - IGridState 인터페이스
public interface IGridState : IReadOnlyGridState
{
    // 유닛 상태 변경 연산
    bool UpdateGridDataLayer(GameObject unit, Vector2Int newPosition);
    bool RemoveUnit(GameObject unit);

    // ✅ Base 상태 관리 연산 추가
    bool PlaceBase(GameObject baseObject, Vector2Int startPosition, Vector2Int baseSize, TeamType team);
    bool RemoveBase(GameObject baseObject);
    GameObject GetBaseAtPosition(Vector2Int position);
    List<Vector2Int> GetBaseOccupiedPositions(GameObject baseObject);
    IEnumerable<GameObject> GetAllBases();

    // 타일 상태 관리
    void SetTileBlocked(Vector2Int position, bool blocked);
    void ResizeGrid(Vector2Int newSize);
    void ClearAllState();
}
```

**설계 원칙:**
- **Interface Segregation Principle (ISP)**: Base 관련 전체 API를 하나의 논리적 그룹으로 제공
- **Dependency Inversion Principle (DIP)**: 추상화(IGridState)에 의존
- **일관성**: Unit 관련 메서드와 동일한 패턴

### Phase 3: Base.cs 정리

**변경 사항:**
```csharp
// 📁 Base.cs
/// <summary>
/// Base 초기화 - 시작 위치와 팀 설정
/// [Deprecated] BaseManager에서 3-parameter 버전 사용 권장
/// </summary>
[System.Obsolete("Use Initialize(Vector2Int, Vector2Int, TeamType) instead", false)]
public void Initialize(Vector2Int startPos, TeamType team)
{
    // 2-parameter 버전을 deprecated로 표시
}

/// <summary>
/// Base 초기화 - 시작 위치, 크기, 팀 설정
/// BaseManager에서 호출
/// </summary>
public void Initialize(Vector2Int startPos, Vector2Int size, TeamType team)
{
    // 3-parameter 버전 사용 권장
}
```

## 📊 변경 후 흐름도

### 리팩토링 전 (문제 있음)
```
BaseManager.CreateBase()
  ↓
1. Instantiate(basePrefab)
2. baseComponent.Initialize(gridPosition, baseSize, team)  ← BaseManager 초기화
  ↓
GridState.PlaceBase()
  ↓
3. baseComponent.Initialize(startPosition, team)  ← GridState 중복 초기화 ❌
4. 그리드 상태 업데이트
```

### 리팩토링 후 (올바름)
```
BaseManager.CreateBase()
  ↓
1. Instantiate(basePrefab)
2. baseComponent.Initialize(gridPosition, baseSize, team)  ← BaseManager 완전 초기화 ✅
  ↓
GridState.PlaceBase()  (via IGridState interface)
  ↓
3. 그리드 상태 업데이트만 수행 ✅
   - positionToBase 업데이트
   - basePositions 업데이트
   - UpdatePhysicalTileBase() 호출
```

## ✅ 달성한 목표

### 1. 타입 안전성 확보
- ✅ `IGridState` 인터페이스에 `PlaceBase()` 추가
- ✅ BaseManager가 인터페이스를 통해 안전하게 호출

### 2. 단일 책임 원칙 (SRP) 준수
- ✅ BaseManager: Base 초기화 전담
- ✅ GridState: 순수 그리드 상태 관리만 수행
- ✅ 중복 초기화 제거

### 3. 일관성 있는 아키텍처
- ✅ `UpdateGridDataLayer()` 패턴과 동일
- ✅ Manager → Initialize → State Update 흐름 통일

### 4. 확장성 향상
- ✅ Base 관련 전체 API를 인터페이스로 제공
- ✅ 미래의 Base 기능 확장 시 명확한 계층 구조

## 🔧 영향받는 파일

### 수정된 파일
1. **GridState.cs**
   - `PlaceBase()` 메서드에서 Base 초기화 로직 제거
   - 순수 상태 관리 책임만 유지

2. **IGridManager.cs**
   - `IGridState` 인터페이스에 Base 관련 메서드 5개 추가
   - 명확한 책임 구분 및 문서화

3. **Base.cs**
   - 2-parameter `Initialize()` deprecated 표시
   - 3-parameter 버전 사용 권장

4. **BaseManager.cs**
   - 변경 없음 (이미 올바른 구조)
   - Base 초기화를 완전히 담당

## 📚 설계 원칙 준수

### SOLID 원칙
- **S (Single Responsibility)**: ✅ GridState는 상태 관리만, BaseManager는 초기화만
- **O (Open/Closed)**: ✅ 인터페이스를 통한 확장 가능
- **L (Liskov Substitution)**: ✅ IGridState 구현체 교체 가능
- **I (Interface Segregation)**: ✅ Base 관련 메서드를 논리적 그룹으로 제공
- **D (Dependency Inversion)**: ✅ BaseManager가 추상화(IGridState)에 의존

### Unity 아키텍처 패턴
- **Manager 계층**: Component 생성 및 초기화
- **State 계층**: 데이터 관리
- **Controller 계층**: 비즈니스 로직

## 🧪 검증 체크리스트

- [x] GridState.PlaceBase()에서 Base 초기화 코드 제거
- [x] IGridState에 PlaceBase() 및 관련 메서드 추가
- [x] BaseManager.CreateBase()가 올바른 순서로 호출 확인
- [x] Base.cs의 2-parameter Initialize() deprecated 표시
- [x] 문서화 및 주석 업데이트

## 📖 참고 자료

**관련 파일:**
- [GridState.cs](../Assets/Script/Game/Components/GridState.cs) - Line 404-466
- [IGridManager.cs](../Assets/Script/Game/Interfaces/IGridManager.cs) - Line 47-94
- [BaseManager.cs](../Assets/Script/Game/Services/BaseManager.cs) - Line 183-226
- [Base.cs](../Assets/Script/Game/Base.cs) - Line 75-106

**설계 패턴:**
- Repository Pattern (GridState as state repository)
- Dependency Injection (BaseManager dependencies)
- Interface Segregation (IGridState, IReadOnlyGridState)

## 🎓 교훈

1. **인터페이스 설계 시 책임 분석 필수**
   - 메서드가 정말 해당 계층의 책임인가?
   - 다른 계층의 책임을 침범하지 않는가?

2. **기존 패턴 일관성 유지**
   - UpdateGridDataLayer()처럼 이미 잘 설계된 패턴이 있다면 동일하게 적용
   - 일관된 패턴은 코드 이해도와 유지보수성 향상

3. **중복 로직 제거의 중요성**
   - 두 번 호출되는 Initialize()는 버그의 온상
   - 책임이 명확하면 중복 자연스럽게 제거

4. **리팩토링 전 구조 분석**
   - 왜 이 메서드가 인터페이스에 없었는지 이해
   - 단순 추가가 아닌 구조적 문제 해결
