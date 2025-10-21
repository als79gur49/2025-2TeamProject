# 씬 로드 및 Fade In/Out 전체 흐름 분석

## 개요

Unity 씬 전환 시스템에서 LoadingScreenPanel의 Fade In/Out 애니메이션이 작동하는 전체 프로세스를 단계별로 분석합니다.

---

## 아키텍처 구성 요소

### 1. SceneTransitionController (Controller Layer)
- **역할**: 씬 전환 흐름 조율, Service와 UI 연결
- **특징**: `DontDestroyOnLoad` + `Singleton` 패턴으로 씬 전환 시 유지
- **위치**: [SceneTransitionController.cs](../Assets/Script/Game/Controllers/SceneTransitionController.cs)

### 2. SceneLoaderService (Service Layer)
- **역할**: Unity AsyncOperation을 사용한 실제 씬 로딩
- **특징**: 진행률 추적, 타이밍 제어
- **위치**: [SceneLoaderService.cs](../Assets/Script/Game/Services/SceneLoaderService.cs)

### 3. LoadingScreenPanel (View Layer)
- **역할**: 로딩 화면 UI 표시 및 Fade 애니메이션
- **특징**: `DontDestroyOnLoad`로 씬 전환 시 유지, 독립 Canvas 보유
- **위치**: [LoadingScreenPanel.cs](../Assets/Script/UI/Core/LoadingScreenPanel.cs)

---

## 전체 실행 흐름

### Phase 1: 씬 전환 시작 (Old Scene)

```
Time: 0.0s
Location: SceneTransitionController.LoadSceneWithLoading()
```

#### Step 1.1: 초기 설정
```csharp
// SceneTransitionController.cs:147-157
isTransitioning = true;

// 로딩 화면 표시
ShowLoadingScreen(sceneData);

// 파라미터 가져오기
float minimumDisplayTime = 3.0f;  // LoadingScreenPanel 설정값
float fadeOutDuration = 1.0f;     // LoadingScreenPanel 설정값

// SceneLoaderService에 씬 로딩 요청
sceneLoaderService.LoadSceneAsync(sceneData, OnLoadingProgressUpdated,
                                  minimumDisplayTime, fadeOutDuration);
```

**핵심 파라미터:**
- `minimumDisplayTime`: 로딩 화면 최소 표시 시간 (예: 3초)
- `fadeOutDuration`: FadeOut 애니메이션 지속 시간 (예: 1초)

#### Step 1.2: LoadingScreenPanel 표시 및 Fade In
```csharp
// LoadingScreenPanel.cs:97-132
public override void OnShow()
{
    gameObject.SetActive(true);

    // 🎯 부모로부터 분리 (독립적인 루트 GameObject)
    transform.SetParent(null);

    // 🎯 독립적인 Canvas 추가
    Canvas canvas = gameObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 9999;  // 최상위 렌더링

    // 🎯 DontDestroyOnLoad 적용
    DontDestroyOnLoad(gameObject);

    // Fade In 시작
    StartCoroutine(FadeIn());
}
```

**Fade In 애니메이션:**
```csharp
// LoadingScreenPanel.cs:196-220
private IEnumerator FadeIn()
{
    isFading = true;
    float elapsedTime = 0f;
    canvasGroup.alpha = 0f;  // 투명한 상태에서 시작

    while (elapsedTime < fadeInDuration)  // fadeInDuration = 0.5초
    {
        elapsedTime += Time.deltaTime;
        canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
        yield return null;
    }

    canvasGroup.alpha = 1f;  // 완전 불투명
    currentState = UIPanelState.Active;
}
```

**결과:**
- 로딩 화면이 0.5초 동안 서서히 나타남 (α: 0 → 1)
- LoadingScreenPanel이 DontDestroyOnLoad로 등록됨
- 독립 Canvas로 씬 전환 후에도 렌더링 가능

---

### Phase 2: 씬 로딩 진행 (Old Scene)

```
Time: 0.5s ~ 2.0s (예시)
Location: SceneLoaderService.LoadSceneAsyncCoroutine()
```

#### Step 2.1: Unity AsyncOperation 시작
```csharp
// SceneLoaderService.cs:111-139
private IEnumerator LoadSceneAsyncCoroutine(...)
{
    float loadStartTime = Time.time;  // 시작 시간 기록

    // Unity 비동기 씬 로딩 시작
    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneData.SceneName);

    // 자동 활성화 방지 (수동 제어)
    asyncLoad.allowSceneActivation = false;

    // 🎯 타이밍 계산
    float waitTimeBeforeSceneSwitch = minimumDisplayTime - fadeOutDuration;
    // 예: 3.0s - 1.0s = 2.0s

    Debug.Log($"Wait time before scene switch: 2.0s");
}
```

**타이밍 계산 의미:**
- **minimumDisplayTime (3.0s)**: 로딩 화면을 최소 3초 동안 보여줌
- **fadeOutDuration (1.0s)**: FadeOut 애니메이션에 1초 소요
- **waitTimeBeforeSceneSwitch (2.0s)**: 2초 대기 후 FadeOut 시작
- **구조**: `[2초 대기] → [1초 FadeOut] = 총 3초 표시`

#### Step 2.2: 로딩 진행률 추적
```csharp
// SceneLoaderService.cs:143-175
while (!asyncLoad.isDone)
{
    // Unity 진행률: 0.0 ~ 0.9 (0.9에서 멈춤)
    _loadingProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

    // 진행률 이벤트 발생
    OnSceneLoadProgress?.Invoke(sceneData, _loadingProgress);

    // SceneTransitionController가 이벤트 수신
    // → LoadingScreenPanel.UpdateProgress() 호출
    // → 프로그레스 바 업데이트

    yield return null;
}
```

**진행률 업데이트 흐름:**
```
SceneLoaderService.OnSceneLoadProgress 이벤트
    ↓
SceneTransitionController.OnLoadingProgressUpdated()
    ↓
LoadingScreenPanel.UpdateProgress()
    ↓
UI 프로그레스 바 업데이트 (0% → 90% → 100%)
```

#### Step 2.3: 씬 준비 완료 대기
```csharp
// SceneLoaderService.cs:148-174
if (asyncLoad.progress >= 0.9f)  // 씬 로딩 완료
{
    sceneReady = true;
    _loadingProgress = 1.0f;

    float elapsedTime = Time.time - loadStartTime;

    if (elapsedTime >= waitTimeBeforeSceneSwitch)  // 2.0초 경과?
    {
        // 조건 충족: 씬 준비 완료 + 대기 시간 경과
        // → Phase 3로 이동
    }
    else
    {
        // 아직 대기 시간 미달 → 계속 대기
        float remainingTime = waitTimeBeforeSceneSwitch - elapsedTime;
        Debug.Log($"Scene ready but waiting ({remainingTime:F2}s remaining)");
    }
}
```

**대기 조건:**
1. ✅ **씬 로딩 완료** (`asyncLoad.progress >= 0.9f`)
2. ✅ **대기 시간 충족** (`elapsedTime >= 2.0s`)

**두 조건 모두 충족되어야 다음 단계 진행**

---

### Phase 3: FadeOut 시작 및 씬 전환 (Old Scene → Scene Transition)

```
Time: 2.0s
Location: SceneLoaderService.LoadSceneAsyncCoroutine()
```

#### Step 3.1: OnSceneLoadCompleted 이벤트 발생 (FadeOut 트리거)

```csharp
// SceneLoaderService.cs:156-168
if (elapsedTime >= waitTimeBeforeSceneSwitch)
{
    Debug.Log($"Scene ready and wait time met. Triggering FadeOut and activating scene...");

    // 🎯 핵심: 씬 전환 **전에** 이벤트 발생!
    OnSceneLoadCompleted?.Invoke(sceneData);

    // 🎯 그 다음 씬 활성화
    asyncLoad.allowSceneActivation = true;
}
```

**이벤트 타이밍의 중요성:**
```
❌ 잘못된 순서 (이전 코드):
allowSceneActivation = true
    ↓ 씬 전환 발생
    ↓ SceneLoaderService 파괴
    ↓ 코루틴 중단
    ✗ OnSceneLoadCompleted 실행 안 됨

✅ 올바른 순서 (수정된 코드):
OnSceneLoadCompleted?.Invoke()  ← SceneLoaderService 살아있음
    ↓ FadeOut 시작 (LoadingScreenPanel은 DontDestroyOnLoad)
    ↓
allowSceneActivation = true
    ↓ 씬 전환 발생
    ↓ SceneLoaderService 파괴 (문제 없음)
    ↓
FadeOut 계속 실행 (LoadingScreenPanel 유지됨)
```

#### Step 3.2: SceneTransitionController 이벤트 수신

```csharp
// SceneTransitionController.cs:251-260
private void HandleSceneLoadCompleted(SceneData sceneData)
{
    Debug.Log($"Scene load completed: {sceneData.SceneName}");

    // 🎯 LoadingScreenPanel.OnHide() 호출
    HideLoadingScreen();

    isTransitioning = false;
    OnSceneTransitionCompleted?.Invoke(sceneData);
}

private void HideLoadingScreen()
{
    if (loadingScreenPanel != null)
    {
        loadingScreenPanel.OnHide();  // FadeOut 시작
    }
}
```

#### Step 3.3: LoadingScreenPanel FadeOut 시작

```csharp
// LoadingScreenPanel.cs:134-143
public override void OnHide()
{
    if (currentState == UIPanelState.Inactive) return;

    // 🎯 FadeOut 코루틴 시작
    StartCoroutine(FadeOut());

    Debug.Log("[LoadingScreenPanel] Hiding loading screen");
}
```

**FadeOut 코루틴:**
```csharp
// LoadingScreenPanel.cs:225-258
private IEnumerator FadeOut()
{
    currentState = UIPanelState.Hiding;
    isFading = true;
    float elapsedTime = 0f;
    canvasGroup.alpha = 1f;  // 불투명한 상태에서 시작

    while (elapsedTime < fadeOutDuration)  // fadeOutDuration = 1.0초
    {
        elapsedTime += Time.deltaTime;
        canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
        yield return null;  // 매 프레임 대기
    }

    canvasGroup.alpha = 0f;  // 완전 투명
    currentState = UIPanelState.Inactive;

    // FadeOut 완료 후 패널 파괴
    Destroy(gameObject);
}
```

**중요:** FadeOut 코루틴이 **이미 시작된 상태**에서 씬 전환 발생!

#### Step 3.4: 씬 전환 발생

```
Time: 2.0s + 1 frame
```

```csharp
// SceneLoaderService.cs:167
asyncLoad.allowSceneActivation = true;
```

**씬 전환 순간의 상태:**
```
Old Scene:
  ├─ SceneLoaderService → 파괴됨 ✗
  ├─ GameInitializer → 파괴됨 ✗
  ├─ ServiceLocator → 파괴됨 ✗
  └─ 기타 씬 GameObject들 → 파괴됨 ✗

DontDestroyOnLoad Zone:
  ├─ SceneTransitionController → 유지됨 ✅ (Singleton)
  └─ LoadingScreenPanel → 유지됨 ✅ (FadeOut 코루틴 실행 중)
```

**핵심 원리:**
- DontDestroyOnLoad된 GameObject의 코루틴은 씬 전환 후에도 계속 실행됨
- LoadingScreenPanel의 FadeOut 코루틴은 새 씬에서도 정상 작동

---

### Phase 4: 새 씬에서 FadeOut 완료 (New Scene)

```
Time: 2.0s ~ 3.0s
Location: New Scene (LoadingScreenPanel 코루틴 계속 실행 중)
```

#### Step 4.1: 새 씬 초기화

```
New Scene 로드됨:
  ├─ 새 GameInitializer 생성
  ├─ 새 ServiceLocator 생성
  ├─ 새 SceneLoaderService 생성
  └─ 기타 새 씬 GameObject들 생성

DontDestroyOnLoad Zone:
  ├─ SceneTransitionController (이전 씬에서 유지)
  └─ LoadingScreenPanel (FadeOut 진행 중: α 1.0 → 0.0)
```

#### Step 4.2: FadeOut 애니메이션 계속 진행

```
Time: 2.0s ~ 3.0s (1초 동안)

LoadingScreenPanel.FadeOut() 코루틴:
  ├─ elapsedTime: 0.0s → canvasGroup.alpha = 1.0 (불투명)
  ├─ elapsedTime: 0.25s → canvasGroup.alpha = 0.75
  ├─ elapsedTime: 0.5s → canvasGroup.alpha = 0.5
  ├─ elapsedTime: 0.75s → canvasGroup.alpha = 0.25
  └─ elapsedTime: 1.0s → canvasGroup.alpha = 0.0 (투명)
```

**사용자 경험:**
- 새 씬이 로드되었지만 로딩 화면이 아직 보임
- 로딩 화면이 1초 동안 서서히 사라짐 (Fade Out)
- 새 씬이 점진적으로 드러남

#### Step 4.3: FadeOut 완료 및 정리

```csharp
// LoadingScreenPanel.cs:248-258
canvasGroup.alpha = 0f;
isFading = false;
gameObject.SetActive(false);
currentState = UIPanelState.Inactive;

// 패널 리셋
ResetPanel();

// 🎯 DontDestroyOnLoad로 유지된 패널 파괴
Destroy(gameObject);

Debug.Log("[LoadingScreenPanel] FadeOut complete - Panel destroyed");
```

**최종 상태:**
```
New Scene:
  ├─ GameInitializer ✅
  ├─ ServiceLocator ✅
  └─ 기타 씬 GameObject들 ✅

DontDestroyOnLoad Zone:
  ├─ SceneTransitionController ✅ (다음 씬 전환을 위해 유지)
  └─ LoadingScreenPanel ✗ (파괴됨 - 역할 완료)
```

---

## 전체 타임라인 요약

```
Time: 0.0s
├─ SceneTransitionController.LoadSceneWithLoading() 호출
├─ LoadingScreenPanel.OnShow() → FadeIn 시작 (0.5초)
└─ SceneLoaderService.LoadSceneAsync() 시작

Time: 0.0s ~ 0.5s
└─ Fade In 애니메이션 진행 (α: 0.0 → 1.0)

Time: 0.5s ~ 2.0s
├─ 로딩 화면 완전히 표시됨
├─ Unity AsyncOperation으로 씬 로딩 진행
├─ 진행률 업데이트 (0% → 100%)
└─ 씬 준비 완료 (progress = 0.9)

Time: 2.0s (대기 시간 충족)
├─ OnSceneLoadCompleted 이벤트 발생
├─ LoadingScreenPanel.OnHide() → FadeOut 시작
├─ allowSceneActivation = true
└─ 씬 전환 발생

Time: 2.0s ~ 3.0s
├─ 새 씬 로드 완료
├─ FadeOut 애니메이션 계속 진행 (α: 1.0 → 0.0)
└─ 새 씬이 점진적으로 드러남

Time: 3.0s
├─ FadeOut 완료
├─ LoadingScreenPanel 파괴
└─ 씬 전환 완료
```

**총 소요 시간:**
- **Fade In**: 0.5초
- **로딩 대기**: 1.5초 (2.0 - 0.5)
- **Fade Out**: 1.0초
- **총합**: 3.0초 (`minimumDisplayTime`)

---

## 핵심 설계 원칙

### 1. DontDestroyOnLoad 패턴

**SceneTransitionController:**
```csharp
// Singleton + DontDestroyOnLoad
private void Awake()
{
    if (instance != null && instance != this)
    {
        Destroy(gameObject);
        return;
    }

    instance = this;
    DontDestroyOnLoad(gameObject);
}
```

**LoadingScreenPanel:**
```csharp
public override void OnShow()
{
    transform.SetParent(null);  // 부모로부터 독립
    DontDestroyOnLoad(gameObject);  // 씬 전환 시 유지
}
```

**효과:**
- 씬 전환 시 GameObject 유지
- 코루틴 계속 실행
- 새 씬에서도 렌더링 가능

### 2. 이벤트 기반 아키텍처

```
SceneLoaderService (Service)
    ↓ OnSceneLoadCompleted 이벤트
SceneTransitionController (Controller)
    ↓ HideLoadingScreen() 호출
LoadingScreenPanel (View)
    ↓ FadeOut 실행
```

**장점:**
- 느슨한 결합 (Loose Coupling)
- 단일 책임 원칙 준수 (SRP)
- 확장성 우수

### 3. 타이밍 제어 분리

**SceneLoaderService:**
- 타이밍 계산: `waitTimeBeforeSceneSwitch = minimumDisplayTime - fadeOutDuration`
- 씬 활성화 시점 제어: `allowSceneActivation = true`

**LoadingScreenPanel:**
- Fade In/Out 애니메이션 실행
- 애니메이션 지속 시간 제공

**SceneTransitionController:**
- 두 레이어 조율
- 이벤트 중계

### 4. 이벤트 발생 타이밍 최적화

**핵심:** OnSceneLoadCompleted 이벤트를 씬 전환 **직전**에 발생

```csharp
// 씬 전환 전에 이벤트 발생 (FadeOut 시작)
OnSceneLoadCompleted?.Invoke(sceneData);

// 그 다음 씬 활성화
asyncLoad.allowSceneActivation = true;
```

**이유:**
- SceneLoaderService가 파괴되기 전에 이벤트 발생
- LoadingScreenPanel의 FadeOut 코루틴 시작 보장
- 새 씬에서도 FadeOut 계속 실행 가능

---

## 시각적 플로우차트

```
┌─────────────────────────────────────────────────────────────┐
│                    Old Scene (씬 A)                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────┐        │
│  │ SceneTransitionController.LoadSceneWithLoading() │        │
│  └────────────────┬────────────────────────────────┘        │
│                   │                                          │
│         ┌─────────┴─────────┐                               │
│         ▼                   ▼                                │
│  ┌──────────────┐   ┌────────────────┐                     │
│  │ShowLoading   │   │SceneLoader     │                      │
│  │Screen()      │   │Service.Load    │                      │
│  └──────┬───────┘   │SceneAsync()    │                     │
│         │           └────────┬───────┘                      │
│         │                    │                               │
│         ▼                    ▼                               │
│  ┌──────────────┐   ┌────────────────┐                     │
│  │LoadingScreen │   │AsyncOperation  │                      │
│  │Panel.OnShow()│   │씬 로딩 시작     │                     │
│  │              │   │progress:       │                      │
│  │- SetParent   │   │0.0 → 0.9       │                     │
│  │- Canvas 추가 │   └────────┬───────┘                      │
│  │- DontDestroy │            │                               │
│  │- FadeIn 시작 │   ┌────────▼───────┐                     │
│  └──────┬───────┘   │대기 조건 체크:  │                     │
│         │           │1. 씬 준비 완료  │                      │
│  [0.5초 동안]       │2. 경과 시간     │                      │
│  α: 0.0 → 1.0      │   >= 2.0s?     │                      │
│         │           └────────┬───────┘                      │
│         ▼                    │                               │
│  ┌──────────────┐            ▼                              │
│  │로딩 화면     │   ┌────────────────┐                     │
│  │완전 표시     │   │조건 충족!       │                     │
│  │(alpha = 1.0) │   │                │                      │
│  └──────────────┘   │OnSceneLoad     │                     │
│                     │Completed 발생   │                      │
│  [1.5초 대기]       └────────┬───────┘                      │
│  진행률 표시:                │                               │
│  0% → 100%          ┌────────▼───────┐                     │
│                     │SceneTransition  │                      │
│                     │Controller       │                      │
│                     │이벤트 수신      │                      │
│                     └────────┬───────┘                      │
│                              │                               │
│                     ┌────────▼───────┐                     │
│                     │HideLoadingScreen│                     │
│                     │() 호출          │                      │
│                     └────────┬───────┘                      │
│                              │                               │
│                     ┌────────▼───────┐                     │
│                     │LoadingScreen    │                      │
│                     │Panel.OnHide()   │                      │
│                     │                │                      │
│                     │FadeOut 코루틴   │                      │
│                     │시작! ✅         │                      │
│                     └────────┬───────┘                      │
│                              │                               │
│                              ▼                               │
│                     ┌────────────────┐                     │
│                     │allowSceneActiv- │                     │
│                     │ation = true    │                      │
│                     └────────┬───────┘                      │
│                              │                               │
└──────────────────────────────┼───────────────────────────────┘
                               │
                               │ 씬 전환 발생
                               │
┌──────────────────────────────▼───────────────────────────────┐
│                    New Scene (씬 B)                           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────┐        │
│  │ DontDestroyOnLoad Zone (씬 전환 시 유지)         │        │
│  ├─────────────────────────────────────────────────┤        │
│  │                                                  │        │
│  │ ┌────────────────────┐  ┌──────────────────┐   │        │
│  │ │SceneTransition     │  │LoadingScreenPanel │   │        │
│  │ │Controller (Singleton)  │(FadeOut 실행 중) │   │        │
│  │ │✅ 유지됨           │  │✅ 유지됨          │   │        │
│  │ └────────────────────┘  └──────┬───────────┘   │        │
│  │                                │                │        │
│  │                       [1.0초 동안]              │        │
│  │                       α: 1.0 → 0.0             │        │
│  │                                │                │        │
│  │                       ┌────────▼───────┐       │        │
│  │                       │FadeOut 완료     │       │        │
│  │                       │Destroy(panel)  │       │        │
│  │                       └────────────────┘       │        │
│  │                                                  │        │
│  └─────────────────────────────────────────────────┘        │
│                                                               │
│  ┌─────────────────────────────────────────────────┐        │
│  │ New Scene GameObject들                           │        │
│  ├─────────────────────────────────────────────────┤        │
│  │                                                  │        │
│  │ ✅ GameInitializer (새로 생성)                   │        │
│  │ ✅ ServiceLocator (새로 생성)                    │        │
│  │ ✅ SceneLoaderService (새로 생성)                │        │
│  │ ✅ 기타 씬 GameObject들                          │        │
│  │                                                  │        │
│  └─────────────────────────────────────────────────┘        │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 주요 문제점 및 해결 과정

### 문제 1: DontDestroyOnLoad가 작동하지 않음

**증상:**
- LoadingScreenPanel에 DontDestroyOnLoad를 적용했지만 새 씬에서 보이지 않음

**원인:**
- LoadingScreenPanel이 Canvas의 자식이었음
- DontDestroyOnLoad는 루트 GameObject에만 작동
- Canvas가 파괴되면서 LoadingScreenPanel도 함께 파괴됨

**해결:**
```csharp
// LoadingScreenPanel.OnShow()
transform.SetParent(null);  // 부모로부터 독립
gameObject.AddComponent<Canvas>();  // 독립 Canvas 추가
DontDestroyOnLoad(gameObject);  // 이제 정상 작동
```

### 문제 2: OnSceneLoadCompleted 이벤트가 발생하지 않음

**증상:**
- SceneTransitionController가 DontDestroyOnLoad로 유지되는 것은 확인했지만 FadeOut이 시작되지 않음

**원인:**
```csharp
// 이전 코드 (잘못된 타이밍)
asyncLoad.allowSceneActivation = true;  // 씬 전환 발생
yield return null;
// 이 시점에 SceneLoaderService 파괴됨 → 코루틴 중단
OnSceneLoadCompleted?.Invoke(sceneData);  // 실행 안 됨!
```

**해결:**
```csharp
// 수정된 코드 (올바른 타이밍)
OnSceneLoadCompleted?.Invoke(sceneData);  // 먼저 이벤트 발생!
asyncLoad.allowSceneActivation = true;    // 그 다음 씬 전환
```

### 문제 3: 타이밍 계산 오류

**요구사항:**
- 로딩 화면을 3초 동안 표시
- 마지막 1초는 FadeOut

**잘못된 접근:**
```csharp
// 3초 대기 후 씬 전환 → FadeOut 시작
// 문제: 총 4초 소요 (3초 대기 + 1초 FadeOut)
```

**올바른 접근:**
```csharp
// 2초 대기 → FadeOut 시작 (1초) → 씬 전환
// 결과: 총 3초 소요 (2초 대기 + 1초 FadeOut)
float waitTimeBeforeSceneSwitch = minimumDisplayTime - fadeOutDuration;
// = 3.0s - 1.0s = 2.0s
```

---

## 성능 최적화 고려사항

### 1. 메모리 관리

- LoadingScreenPanel은 FadeOut 완료 후 즉시 파괴
- SceneTransitionController는 Singleton으로 재사용
- 새 씬마다 불필요한 인스턴스 생성 방지

### 2. 렌더링 최적화

```csharp
// Canvas 설정
canvas.sortingOrder = 9999;  // 최상위 레이어
canvas.renderMode = RenderMode.ScreenSpaceOverlay;  // GPU 최적화
```

### 3. 코루틴 최적화

```csharp
// 매 프레임 yield return null로 부드러운 애니메이션
while (elapsedTime < fadeOutDuration)
{
    elapsedTime += Time.deltaTime;
    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
    yield return null;  // 60 FPS → 60단계 보간
}
```

---

## 확장 가능성

### 1. 다양한 로딩 화면 추가

```csharp
// SceneData에 LoadingScreenType 추가
public enum LoadingScreenType
{
    Default,
    Combat,
    Exploration
}

// SceneTransitionController에서 타입별 패널 선택
private void ShowLoadingScreen(SceneData sceneData)
{
    var panel = GetLoadingPanelByType(sceneData.LoadingScreenType);
    panel.OnShow();
}
```

### 2. 진행률 기반 씬 활성화

```csharp
// 실제 로딩 진행률이 100%가 되면 즉시 전환
if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumDisplayTime)
{
    // 빠른 로딩 시에도 최소 시간 보장
}
```

### 3. 취소 가능한 로딩

```csharp
// 사용자 입력으로 로딩 스킵
public void CancelLoading()
{
    if (isTransitioning && canCancelLoading)
    {
        // FadeOut 즉시 시작
        HideLoadingScreen();
    }
}
```

---

## 결론

현재 씬 전환 시스템은 다음과 같은 특징을 가집니다:

✅ **명확한 책임 분리**
- SceneLoaderService: 씬 로딩
- SceneTransitionController: 흐름 조율
- LoadingScreenPanel: UI 표시

✅ **안정적인 씬 전환**
- DontDestroyOnLoad로 컴포넌트 유지
- 이벤트 기반 느슨한 결합
- 타이밍 제어로 일관된 사용자 경험

✅ **확장 가능한 아키텍처**
- 새로운 로딩 화면 타입 추가 용이
- 이벤트 리스너 추가로 기능 확장 가능
- SOLID 원칙 준수

이 시스템은 Unity의 비동기 씬 로딩과 DontDestroyOnLoad 메커니즘을 효과적으로 활용하여, 부드러운 Fade In/Out 애니메이션과 함께 안정적인 씬 전환을 제공합니다.
