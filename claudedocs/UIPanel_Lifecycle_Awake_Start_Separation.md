# UIPanel 생명주기 개선: Awake/Start 역할 분리

## 📋 개요

Unity 생명주기 원칙에 따라 **서비스 등록(Registration)과 소비(Consumption)**를 명확히 분리하여 Awake() 실행 순서 종속성 문제를 근본적으로 해결했습니다.

## 🎯 문제 상황

### 발생한 문제
```
MainMenuPanel.Awake() (순서 불확실)
├─ UIPanel.Awake()
│  └─ Initialize()
│     └─ OnInitialize()
│        └─ ServiceLocator.Get<ISceneTransitionController>()
│           └─ ❌ SceneTransitionController가 아직 등록 안 됐을 수 있음
└─ 실행 순서 보장 안 됨

SceneTransitionController.Awake() (순서 불확실)
└─ RegisterSingleton<ISceneTransitionController>()
```

**근본 원인:**
- Unity는 **같은 페이즈 내의 실행 순서를 보장하지 않음**
- `MainMenuPanel.Awake()`와 `SceneTransitionController.Awake()`의 순서가 불확실
- `MainMenuPanel`이 먼저 실행되면 `ServiceLocator.Get()`이 null 반환

---

## ✅ 해결 원칙

### Unity 생명주기 보장 활용

Unity는 **모든 GameObject의 Awake()가 완료된 후** 첫 번째 Start()를 호출하는 것을 보장합니다.

```
Phase 1: Awake() - 모든 GameObject
├─ SceneTransitionController.Awake() → RegisterSingleton ✅
├─ MainMenuPanel.Awake() (Initialize 호출 안 함) ✅
├─ 기타 모든 스크립트의 Awake() ✅
└─ 모든 서비스 등록 완료 보장

Phase 2: Start() - 모든 GameObject
└─ MainMenuPanel.Start()
   └─ Initialize()
      └─ OnInitialize()
         └─ ServiceLocator.Get<ISceneTransitionController>() ✅
            └─ 항상 안전 (이미 등록됨)
```

### 역할 분리 원칙

| 생명주기 | 역할 | 사용 목적 |
|----------|------|-----------|
| **Awake()** | 서비스 **등록(Registration)** | ServiceLocator.RegisterSingleton() |
| **Start()** | 서비스 **소비(Consumption)** | ServiceLocator.Get() |

---

## 🔧 구현 내용

### 수정 파일
**파일:** [Assets/Script/UI/Core/UIPanel.cs](../Assets/Script/UI/Core/UIPanel.cs)

### Before (문제 코드)
```csharp
protected virtual void Awake()
{
    if (initializeOnAwake)
    {
        Initialize(); // ❌ 외부 서비스 의존성이 있는 초기화
                     // 다른 스크립트의 Awake()가 아직 실행 안 됐을 수 있음
    }
}

protected virtual void Start()
{
    if (hideOnStart)
    {
        gameObject.SetActive(false);
    }
}
```

### After (해결 코드)
```csharp
protected virtual void Awake()
{
    // ✅ Awake()에서는 자기 자신에게만 종속적인 초기화만 수행
    // ServiceLocator.Get() 등 외부 의존성이 필요한 초기화는 Start()에서 수행
    // Unity는 모든 Awake() 완료 후 Start()를 호출하므로,
    // Start()에서 ServiceLocator를 사용하면 항상 안전함
}

protected virtual void Start()
{
    // ✅ Start()는 모든 Awake()가 완료된 후 호출됨 (Unity 보장)
    // 이 시점에서 ServiceLocator.Get()을 호출하면
    // 모든 서비스가 이미 RegisterSingleton()으로 등록된 상태이므로 항상 안전
    if (initializeOnAwake && !isInitialized)
    {
        Initialize();
    }

    if (hideOnStart)
    {
        gameObject.SetActive(false);
    }
}
```

---

## 💡 장점 분석

### 1. 근본적 해결
- ✅ Unity 생명주기 보장을 활용한 **아키텍처 수준 해결**
- ✅ **Script Execution Order 수동 설정 불필요**
- ✅ 순서 종속성 완전 제거

### 2. 모든 패널에 자동 적용
- ✅ **UIPanel 베이스 클래스** 수정으로 모든 파생 클래스 자동 해결
- ✅ MainMenuPanel 외 **다른 UI 패널도 동일 문제 방지**
- ✅ 향후 추가될 패널도 자동으로 안전

**영향 받는 패널:**
- MainMenuPanel
- LoadingScreenPanel
- 기타 모든 UIPanel 파생 클래스

### 3. 코드 품질
- ✅ 재시도 로직 불필요 (MainMenuPanel의 OnStartGameButtonClicked 재시도 로직은 방어 코드로 남김)
- ✅ null 체크 안전장치 자동 해결
- ✅ 명확한 생명주기 관리

### 4. Unity 관례 준수
- ✅ **Unity 공식 권장사항**: Awake=등록, Start=소비
- ✅ 다른 Unity 개발자들에게 친숙한 패턴
- ✅ 유지보수성 향상

---

## 📊 실행 순서 비교

### Before (문제 상황)

**시나리오 1: MainMenuPanel이 먼저 실행**
```
1. MainMenuPanel.Awake()
   └─ Initialize()
      └─ OnInitialize()
         └─ ServiceLocator.Get<ISceneTransitionController>()
            └─ ❌ null 반환 (아직 등록 안 됨)

2. SceneTransitionController.Awake()
   └─ RegisterSingleton<ISceneTransitionController>()
      └─ 이제 등록됨 (하지만 너무 늦음)
```

**시나리오 2: SceneTransitionController가 먼저 실행**
```
1. SceneTransitionController.Awake()
   └─ RegisterSingleton<ISceneTransitionController>()
      └─ ✅ 등록 완료

2. MainMenuPanel.Awake()
   └─ Initialize()
      └─ OnInitialize()
         └─ ServiceLocator.Get<ISceneTransitionController>()
            └─ ✅ 정상 반환
```

**문제:** 실행 순서에 따라 결과가 달라짐

---

### After (해결 후)

**모든 시나리오에서 동일한 결과:**
```
Phase 1: Awake() (순서 무관)
├─ MainMenuPanel.Awake() (아무것도 안 함) ✅
└─ SceneTransitionController.Awake()
   └─ RegisterSingleton<ISceneTransitionController>() ✅

Phase 2: Start() (모든 Awake 완료 후)
└─ MainMenuPanel.Start()
   └─ Initialize()
      └─ OnInitialize()
         └─ ServiceLocator.Get<ISceneTransitionController>()
            └─ ✅ 항상 정상 반환 (이미 등록됨)
```

**결과:** 실행 순서와 무관하게 항상 안전

---

## 🔍 initializeOnAwake 필드의 의미 변화

### 필드 선언
```csharp
[SerializeField] protected bool initializeOnAwake = true;
```

### 의미 변경

| 구분 | 이전 | 이후 |
|------|------|------|
| **실제 동작** | Awake()에서 Initialize() | Start()에서 Initialize() |
| **의미** | "Awake 시점 초기화" | "자동 초기화 여부" |
| **타이밍** | Awake 페이즈 | Start 페이즈 |

**참고:** 필드 이름은 `initializeOnAwake`로 유지하지만, 실제 초기화는 Start()에서 수행됩니다. 이름은 "자동 초기화 활성화 여부"로 해석하는 것이 적절합니다.

---

## 📝 Unity Editor 설정 가이드

### Inspector 설정
- `initializeOnAwake = true` (기본값): Start()에서 자동 초기화
- `initializeOnAwake = false`: 수동 Initialize() 호출 필요

### 테스트 검증 체크리스트

#### 1. Play 모드 실행
```
Unity Editor → Play 버튼 클릭
```

#### 2. Console 로그 확인
다음 순서대로 로그가 출력되어야 합니다:
```
[SceneTransitionController] Initialized as Global Singleton Service with DontDestroyOnLoad
[SceneTransitionController] ✅ Registered to ServiceLocator as Singleton (ISceneTransitionController)
[SceneTransitionController] ServiceCleanup component auto-attached for automatic cleanup on destroy

[UIPanel] UI Panel Initialized: MainMenuPanel
[MainMenuPanel] Initialized successfully
```

#### 3. MainMenuPanel 초기화 확인
- ✅ `ISceneTransitionController not found in ServiceLocator` 에러 **없어야 함**
- ✅ MainMenuPanel 정상 초기화 로그 확인

#### 4. 씬 전환 테스트
1. TitleScene에서 "Start Game" 버튼 클릭
2. GameScene 로드 확인
3. Console에서 Missing 참조 경고 **없음** 확인 ✅
4. GameScene에서 다시 TitleScene 전환
5. 정상 동작 확인 ✅

#### 5. 다른 UI 패널 확인
- LoadingScreenPanel 정상 동작 확인
- 기타 UIPanel 파생 클래스 정상 동작 확인

---

## 🎓 Unity 생명주기 Best Practices

### Awake()에서 할 것
```csharp
protected virtual void Awake()
{
    // ✅ GetComponent (자기 자신의 컴포넌트)
    myRenderer = GetComponent<SpriteRenderer>();

    // ✅ 내부 상태 초기화
    currentState = State.Idle;

    // ✅ ServiceLocator 등록 (Provider인 경우)
    ServiceLocator.RegisterSingleton<IMyService>(this);
}
```

### Start()에서 할 것
```csharp
protected virtual void Start()
{
    // ✅ ServiceLocator 조회 (Consumer인 경우)
    myService = ServiceLocator.Get<IMyService>();

    // ✅ 다른 GameObject 참조 (Find 계열)
    otherObject = GameObject.Find("OtherObject");

    // ✅ 외부 의존성이 필요한 초기화
    Initialize();
}
```

### 하지 말아야 할 것
```csharp
protected virtual void Awake()
{
    // ❌ ServiceLocator.Get() (다른 스크립트가 아직 등록 안 했을 수 있음)
    myService = ServiceLocator.Get<IMyService>();

    // ❌ 다른 GameObject 참조 (순서 불확실)
    otherObject = GameObject.Find("OtherObject");
}
```

---

## 📚 관련 문서

- [UIPanel.cs](../Assets/Script/UI/Core/UIPanel.cs) - 수정된 베이스 클래스
- [MainMenuPanel.cs](../Assets/Script/UI/Menu/MainMenuPanel.cs) - Consumer 예시
- [SceneTransitionController.cs](../Assets/Script/Game/Controllers/SceneTransitionController.cs) - Provider 예시
- [SceneTransitionController_ServiceLocator_Integration.md](./SceneTransitionController_ServiceLocator_Integration.md) - ServiceLocator 통합 문서

---

## 🎉 결론

### 수정 요약
- **수정 파일**: UIPanel.cs (Awake, Start 메서드)
- **변경 내용**: Initialize() 호출을 Awake()에서 Start()로 이동

### 기대 효과
1. ✅ **Awake 순서 문제 근본적 해결** - Unity 생명주기 보장 활용
2. ✅ **모든 UI 패널에 자동 적용** - 베이스 클래스 수정으로 전체 적용
3. ✅ **Script Execution Order 불필요** - 아키텍처 수준 해결
4. ✅ **Unity 생명주기 원칙 준수** - Awake=등록, Start=소비
5. ✅ **확장성 및 유지보수성 향상** - 향후 추가 패널도 자동 안전

이 방법이 **Unity 생명주기 원칙에 따른 근본적이고 확장 가능한 해결책**입니다.
