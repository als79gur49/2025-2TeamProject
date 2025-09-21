# GameServiceManager 리팩토링 계획서

## 📋 개요

본 문서는 GameServiceManager.cs의 리팩토링 계획을 상세히 기술합니다. 현재 시스템의 문제점을 분석하고, 개선된 DI(Dependency Injection) 패턴과 이벤트 시스템을 통한 해결책을 제시합니다.

---

## 🔍 현재 상태 분석

### 현재 GameServiceManager 구조
```csharp
public class GameServiceManager : MonoBehaviour
{
    [SerializeField] private TurnService turnService;
    [SerializeField] private UnitService unitService;
    [SerializeField] private UIService uiService;
    [SerializeField] private GameService gameService;
    
    // 단순한 컴포넌트 생성만 담당
    private void EnsureServiceComponents() { ... }
}
```

### 🚨 현재 시스템의 문제점

#### 1. **수동적인 서비스 관리**
- GameServiceManager가 단순히 컴포넌트 존재만 확인
- 초기화 순서나 의존성 관리 부재
- 서비스 간 통신 조정 역할 없음

#### 2. **ServiceLocator 의존성**
- 모든 서비스가 ServiceLocator.Get<>() 사용
- 런타임 의존성 해결로 인한 불안정성
- 테스트하기 어려운 구조

#### 3. **분산된 이벤트 시스템**
- 서비스들이 개별적으로 이벤트 발행
- 중앙화된 이벤트 조정 없음
- 크로스 서비스 통신 패턴 부재

#### 4. **역할 분리 문제**
- UIService에서 UI 생성과 이벤트 처리 혼재
- GameService에서 테스트 입력 처리 포함
- 단일 책임 원칙 위반

---

## 🎯 개선 목표

### 1. **능동적인 서비스 코디네이터**
- 서비스 생명주기 관리
- 의존성 주입 조정
- 초기화 순서 보장

### 2. **명시적 의존성 주입**
- ServiceLocator 의존성 제거
- 생성자/메서드 주입 패턴
- 테스트 가능한 구조

### 3. **중앙화된 이벤트 시스템**
- 모든 서비스 이벤트 집계
- 공개 API를 통한 외부 접근
- 이벤트 로깅 및 디버깅

### 4. **명확한 역할 분리**
- 각 서비스의 단일 책임 보장
- 관심사 분리 강화
- 유지보수성 향상

---

## 🏗️ 새로운 아키텍처 설계

### 초기화 파이프라인
```
1. Create Components     → 서비스 컴포넌트 생성
2. Register Services     → ServiceLocator 등록
3. Inject Dependencies   → 의존성 주입
4. Initialize Services   → 개별 서비스 초기화
5. Connect Events        → 이벤트 시스템 연결
6. Validate Health       → 서비스 상태 검증
```

### 의존성 주입 패턴
```csharp
// 기존: ServiceLocator 의존
turnService = ServiceLocator.Get<ITurnService>();

// 개선: 명시적 주입
gameService.InjectDependencies(turnService, unitService, uiService);
```

### 이벤트 집계 시스템
```csharp
public class GameServiceManager : MonoBehaviour
{
    // 공개 이벤트 API
    public event Action<bool> OnTurnChanged;
    public event Action<Unit> OnUnitRegistered;
    public event Action OnGameStarted;
    
    // 서비스 이벤트 구독 및 전달
    private void ConnectServiceEvents()
    {
        turnService.OnTurnChanged += HandleTurnChanged;
        unitService.OnUnitRegistered += HandleUnitRegistered;
    }
    
    private void HandleTurnChanged(bool isPlayerTurn)
    {
        OnTurnChanged?.Invoke(isPlayerTurn);
    }
}
```

---

## 📝 서비스별 역할 재정의

### 🎮 GameService
**역할**: 게임 상태 관리 및 조정
```csharp
✅ 유지:
- 게임 상태 관리 (IsGameActive)
- 게임 시작/종료 이벤트
- 전체 게임 로직 조정

❌ 제거:
- 테스트 입력 처리 (별도 컴포넌트로 분리)
- 직접적인 ServiceLocator 호출

🔄 개선:
- 의존성 주입 방식으로 변경
- 순수한 게임 상태 코디네이터 역할
```

### 🔄 TurnService  
**역할**: 턴 관리 (변경 없음)
```csharp
✅ 현재 상태 유지:
- 턴 상태 관리
- 턴 변경 이벤트 발행
- 명확한 단일 책임
```

### 👥 UnitService
**역할**: 유닛 생명주기 관리
```csharp
✅ 유지:
- 유닛 등록/해제
- 유닛 처리 로직
- 유닛 관련 이벤트

🔄 개선:
- 이벤트 기반 정리 시스템
- 더 나은 생명주기 관리
```

### 🖼️ UIService
**역할**: UI 관리 (분리 필요)
```csharp
❌ 현재 문제:
- UI 생성과 이벤트 처리 혼재
- 하드코딩된 UI 생성 로직

🔄 개선 방향:
- UIFactory와 UIController 분리 고려
- 프리팹 기반 UI 생성
- 의존성 주입으로 서비스 접근
```

---

## 🔧 구현 세부사항

### 1. 향상된 GameServiceManager

#### 주요 기능
```csharp
public class GameServiceManager : MonoBehaviour
{
    // 🎯 공개 이벤트 API
    public event Action<bool> OnTurnChanged;
    public event Action<int> OnTurnCountChanged;
    public event Action<Unit> OnUnitRegistered;
    public event Action<Unit> OnUnitUnregistered;
    public event Action OnUnitsProcessed;
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action OnEndTurnRequested;
    public event Action OnRestartRequested;
    public event Action OnServicesInitialized;
    public event Action<string> OnServiceError;
    
    // 🏗️ 초기화 파이프라인
    public void InitializeServices()
    {
        CreateServiceComponents();
        RegisterServicesWithLocator();
        InjectServiceDependencies();
        InitializeIndividualServices();
        ConnectServiceEvents();
        ValidateServiceHealth();
    }
    
    // 💉 의존성 주입
    private void InjectServiceDependencies()
    {
        gameService.InjectDependencies(turnService, unitService, uiService);
        uiService.InjectDependencies(turnService, unitService);
    }
    
    // 🔗 이벤트 연결
    private void ConnectServiceEvents()
    {
        turnService.OnTurnChanged += HandleTurnChanged;
        unitService.OnUnitRegistered += HandleUnitRegistered;
        // ... 모든 서비스 이벤트 구독
    }
}
```

### 2. 서비스 인터페이스 개선

#### 의존성 주입 지원
```csharp
public interface IGameService
{
    bool IsGameActive { get; }
    void InjectDependencies(ITurnService turn, IUnitService unit, IUIService ui);
    void Initialize();
    void StartGame();
    void RestartGame();
    
    event Action OnGameStarted;
    event Action OnGameEnded;
}
```

### 3. 이벤트 시스템 설계

#### 이벤트 명명 규칙
- `On[Service][Action]` 패턴 사용
- 예: `OnTurnChanged`, `OnUnitRegistered`, `OnGameStarted`

#### 이벤트 흐름
1. **서비스 → GameServiceManager**: 내부 이벤트 발행
2. **GameServiceManager → 외부**: 공개 이벤트로 집계
3. **외부 코드**: 공개 이벤트 구독

---

## 📊 개선 효과

### 🎯 아키텍처 개선
- **느슨한 결합**: 서비스 간 직접 의존성 제거
- **명확한 책임**: 각 컴포넌트의 역할 명확화
- **테스트 용이성**: 의존성 주입으로 모킹 가능

### 🔍 디버깅 & 모니터링
- **중앙화된 로깅**: 모든 이벤트 중앙 집중
- **서비스 상태 추적**: 초기화 단계별 상태 확인
- **에러 처리**: 체계적인 예외 처리

### 🚀 성능 개선
- **초기화 최적화**: 명확한 초기화 순서
- **이벤트 최적화**: 불필요한 이벤트 구독 방지
- **메모리 관리**: 적절한 이벤트 해제

### 🔧 유지보수성
- **확장성**: 새로운 서비스 추가 용이
- **수정 용이성**: 변경 영향 범위 최소화
- **문서화**: 명확한 의존성 관계

---

## 🛣️ 마이그레이션 전략

### 1단계: 인터페이스 확장
- 기존 인터페이스에 의존성 주입 메서드 추가
- 하위 호환성 유지

### 2단계: GameServiceManager 리팩토링
- 새로운 초기화 파이프라인 구현
- 이벤트 집계 시스템 추가

### 3단계: 서비스 구현체 수정
- ServiceLocator 의존성 제거
- 의존성 주입 메서드 구현

### 4단계: 테스트 및 검증
- 유닛 테스트 작성
- 통합 테스트 실행
- 성능 검증

---

## 🧪 테스트 전략

### 유닛 테스트
```csharp
[Test]
public void GameServiceManager_InitializeServices_ShouldInjectDependencies()
{
    // Given
    var manager = CreateGameServiceManager();
    
    // When
    manager.InitializeServices();
    
    // Then
    Assert.IsTrue(manager.IsInitialized);
    Assert.IsTrue(manager.AreServicesHealthy());
}
```

### 통합 테스트
- 전체 초기화 파이프라인 테스트
- 이벤트 시스템 검증
- 서비스 간 통신 테스트

---

## 📚 추가 고려사항

### 성능 최적화
- 이벤트 풀링 시스템 고려
- 조건부 이벤트 발행
- 메모리 할당 최소화

### 확장성
- 플러그인 시스템 지원
- 런타임 서비스 추가/제거
- 모듈화된 서비스 아키텍처

### 에러 핸들링
- 서비스 초기화 실패 복구
- 런타임 서비스 오류 처리
- 우아한 성능 저하 대응

---

## ✅ 구현 TodoList

### 📋 Phase 1: 인터페이스 및 기반 준비
- [ ] **IGameService 인터페이스 수정**
  - [ ] `InjectDependencies` 메서드 추가
  - [ ] 기존 메서드 유지 (하위 호환성)
  - [ ] 문서화 업데이트

- [ ] **IUIService 인터페이스 수정**
  - [ ] `InjectDependencies` 메서드 추가
  - [ ] UI 생성 관련 메서드 분리 검토

- [ ] **ITurnService, IUnitService 검토**
  - [ ] 현재 인터페이스 적합성 확인
  - [ ] 필요시 이벤트 시그니처 조정

### 🏗️ Phase 2: GameServiceManager 핵심 리팩토링 ✅ **완료**
- [x] **새로운 GameServiceManager 구조 구현**
  - [x] 공개 이벤트 API 정의 (11개 이벤트)
  - [x] 초기화 파이프라인 구현 (6단계)
  - [x] 서비스 상태 추적 변수 추가
  - [x] 이벤트 로깅 시스템 구현

- [x] **서비스 생성 및 관리**
  - [x] `CreateServiceComponents()` 메서드 구현
  - [x] `RegisterServicesWithLocator()` 메서드 구현
  - [x] `ValidateServiceHealth()` 메서드 구현
  - [x] 서비스 팩토리 패턴 적용

- [x] **의존성 주입 시스템**
  - [x] `InjectServiceDependencies()` 메서드 구현
  - [x] GameService 의존성 주입 (3개 서비스)
  - [x] UIService 의존성 주입 (2개 서비스)
  - [x] 의존성 검증 로직 추가

### 🔗 Phase 3: 이벤트 시스템 구현
- [ ] **이벤트 연결 시스템**
  - [ ] `ConnectServiceEvents()` 메서드 구현
  - [ ] 모든 서비스 이벤트 구독 (11개)
  - [ ] 이벤트 핸들러 메서드 구현
  - [ ] 이벤트 전달 로직 구현

- [ ] **이벤트 정리 시스템**
  - [ ] `OnDestroy()` 이벤트 해제 구현
  - [ ] 메모리 누수 방지 로직
  - [ ] 이벤트 구독 상태 추적

### 🔧 Phase 4: 서비스 구현체 수정 ✅ **완료**
- [x] **GameService 리팩토링**
  - [x] `InjectDependencies()` 메서드 구현
  - [x] ServiceLocator 의존성 제거
  - [x] 테스트 입력 처리 분리 (TestInputHandler로 분리)
  - [x] 순수한 게임 상태 관리로 집중
  - [x] 의존성 검증 로직 추가
  - [x] 에러 처리 및 안전성 강화

- [x] **UIService 리팩토링**
  - [x] `InjectDependencies()` 메서드 구현
  - [x] ServiceLocator 의존성 제거
  - [x] 의존성 주입 상태 추적 추가
  - [x] 이벤트 정리 시스템 구현
  - [x] 테스트 입력 지원 메서드 추가

- [x] **UnitService 개선**
  - [x] 이벤트 기반 정리 시스템 확인 (이미 구현됨)
  - [x] 생명주기 관리 확인 (이미 적절함)
  - [x] ServiceLocator 독립성 확인 (독립적임)

- [x] **TurnService 검증**
  - [x] 현재 구현 적합성 확인 (적절함)
  - [x] ServiceLocator 독립성 확인 (독립적임)

- [x] **TestInputHandler 생성**
  - [x] GameService에서 테스트 입력 처리 분리
  - [x] 독립적인 테스트 입력 컴포넌트 구현
  - [x] 키보드 입력을 통한 게임 제어 지원
  - [x] GameServiceManager 통합

### 🧪 Phase 5: 테스트 구현 ✅ **완료**
- [x] **유닛 테스트 작성**
  - [x] GameServiceManager 초기화 테스트 (GameServiceManagerTests.cs)
  - [x] 의존성 주입 테스트 (DependencyInjectionTests.cs)
  - [x] 이벤트 시스템 테스트 (EventSystemTests.cs)
  - [x] 서비스 상태 검증 테스트 (ServiceHealthValidationTests.cs)

- [x] **통합 테스트 작성**
  - [x] 전체 초기화 파이프라인 테스트 (InitializationPipelineIntegrationTests.cs)
  - [x] 서비스 간 통신 테스트 (ServiceCommunicationIntegrationTests.cs)
  - [x] 이벤트 흐름 테스트 (EventFlowIntegrationTests.cs)
  - [x] 에러 처리 테스트 (ErrorHandlingIntegrationTests.cs)

- [x] **성능 테스트**
  - [x] 초기화 시간 측정 (통합 테스트에 포함)
  - [x] 메모리 사용량 확인 (성능 테스트에 포함)
  - [x] 이벤트 처리 성능 검증 (이벤트 플로우 테스트에 포함)

### 📚 Phase 6: 문서화 및 마무리
- [ ] **코드 문서화**
  - [ ] XML 문서 주석 추가
  - [ ] 인터페이스 계약 명시
  - [ ] 사용 예제 작성

- [ ] **마이그레이션 가이드**
  - [ ] 기존 코드 변경점 정리
  - [ ] 업그레이드 절차 문서화
  - [ ] 호환성 이슈 정리

- [ ] **최종 검증**
  - [ ] 모든 기능 동작 확인
  - [ ] 성능 기준 달성 확인
  - [ ] 코드 품질 검토

### 🚀 Phase 7: 최적화 및 확장
- [ ] **성능 최적화** (선택사항)
  - [ ] 이벤트 풀링 시스템 구현
  - [ ] 조건부 이벤트 발행
  - [ ] 메모리 할당 최소화

- [ ] **확장성 개선** (선택사항)
  - [ ] 플러그인 시스템 기반 마련
  - [ ] 런타임 서비스 추가/제거 지원
  - [ ] 모듈화 아키텍처 검토

---

## 📊 진행 상황 추적

### 📈 완료도 지표
- **Phase 1**: 0/4 완료 (0%)
- **Phase 2**: 8/8 완료 (100%) ✅ **완료**
- **Phase 3**: 0/4 완료 (0%)
- **Phase 4**: 8/8 완료 (100%) ✅ **완료**
- **Phase 5**: 9/9 완료 (100%) ✅ **완료**
- **Phase 6**: 0/6 완료 (0%)
- **Phase 7**: 0/6 완료 (0%)

**전체 진행률**: 25/45 (56%)

### ⏱️ 예상 작업 시간
- **Phase 1-2**: 핵심 구조 (4-6시간)
- **Phase 3-4**: 구현 및 리팩토링 (6-8시간)
- **Phase 5**: 테스트 (3-4시간)
- **Phase 6**: 문서화 (2-3시간)
- **Phase 7**: 최적화 (2-3시간)

**총 예상 시간**: 17-24시간

---

## 📋 결론

이 리팩토링 계획을 통해 GameServiceManager는 단순한 컴포넌트 관리자에서 **능동적인 서비스 코디네이터**로 발전합니다. 

### 핵심 개선사항
1. **명시적 의존성 주입**으로 안정성 향상
2. **중앙화된 이벤트 시스템**으로 통신 체계화  
3. **체계적인 초기화 파이프라인**으로 신뢰성 확보
4. **명확한 역할 분리**로 유지보수성 향상

위의 Todo List를 단계별로 진행하여 더욱 견고하고 확장 가능한 게임 서비스 아키텍처를 구축할 수 있습니다.