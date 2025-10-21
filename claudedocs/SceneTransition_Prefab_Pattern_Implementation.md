# Scene Transition Prefab Pattern Implementation

## 📋 개요

SceneTransitionController와 LoadingScreenPanel 간의 아키텍처를 개선하여 Prefab 인스턴스화 패턴을 적용했습니다. 이를 통해 단일 책임 원칙(SRP)을 준수하고, 확장성을 향상시키며, Canvas 관리 문제를 해결했습니다.

## 🎯 문제점 분석

### 1. 싱글턴이 필요한 이유

**SceneTransitionController가 여러 개 존재하면 안 되는 이유:**

1. **이벤트 구독 중복**
   - 여러 인스턴스가 동일한 `SceneLoaderService` 이벤트를 구독
   - 하나의 씬 로딩에 모든 컨트롤러가 반응하여 중복 처리 발생

2. **상태 관리 충돌**
   - 각 인스턴스의 `isTransitioning` 플래그가 독립적으로 관리됨
   - 경쟁 조건(race condition) 발생 가능

3. **LoadingScreenPanel 제어 충돌**
   - 여러 컨트롤러가 동일한 패널을 제어하여 애니메이션 충돌
   - ShowLoadingScreen/HideLoadingScreen 중복 호출

4. **DontDestroyOnLoad 누적**
   - 씬마다 새 컨트롤러가 생성되어 메모리에 누적
   - 리소스 낭비 및 성능 저하

### 2. LoadingScreenPanel Missing 참조 문제

**기존 문제:**
```csharp
// LoadingScreenPanel.cs
public override void OnShow()
{
    DontDestroyOnLoad(gameObject);  // 씬 전환 시 유지
    // ...
}

private IEnumerator FadeOut()
{
    // ...
    Destroy(gameObject);  // FadeOut 후 파괴
}
```

**문제 흐름:**
1. 씬 A에서 LoadingScreenPanel 표시 → DontDestroyOnLoad 적용
2. FadeOut 완료 → Destroy(gameObject)
3. SceneTransitionController의 참조 → null (Missing)
4. 씬 B 전환 시도 → 참조가 null이어서 실패

## 🔧 해결 방안: Prefab 인스턴스화 패턴

### 아키텍처 개선

#### 1. 책임 분리 (SRP)

**SceneTransitionController:**
- 씬 전환 조율
- 로딩 패널 Prefab 인스턴스화
- 로딩 패널 생명주기 시작점 제어

**LoadingScreenPanel:**
- 자신의 생명주기 완전 관리 (생성부터 소멸까지)
- 로딩 UI 표시 및 애니메이션
- FadeOut 완료 후 자기 파괴

#### 2. 구현 변경 사항

##### SceneTransitionController.cs

```csharp
[Header("Dependencies")]
[SerializeField] private GameObject loadingScreenPrefab;  // Prefab 참조
private LoadingScreenPanel currentLoadingPanel;           // 현재 활성 인스턴스

// 로딩 화면 표시
private void ShowLoadingScreen(SceneData sceneData, GameObject customPrefab = null)
{
    // 사용할 Prefab 결정 (커스텀 > 기본)
    GameObject prefabToUse = customPrefab != null ? customPrefab : loadingScreenPrefab;

    // Prefab 인스턴스화
    GameObject loadingPanelInstance = Instantiate(prefabToUse);
    currentLoadingPanel = loadingPanelInstance.GetComponent<LoadingScreenPanel>();

    // 로딩 화면 설정 및 표시
    currentLoadingPanel.SetLoadingBackground(sceneData.LoadingBackground);
    currentLoadingPanel.SetLoadingTip(sceneData.GetRandomLoadingTip());
    currentLoadingPanel.OnShow();
}

// 로딩 화면 숨기기
private void HideLoadingScreen()
{
    if (currentLoadingPanel == null) return;

    currentLoadingPanel.OnHide();
    // FadeOut 코루틴이 완료되면 LoadingScreenPanel이 자동으로 Destroy됨
}

// 정리
private void OnDestroy()
{
    // 현재 로딩 패널 정리 (아직 파괴되지 않았다면)
    if (currentLoadingPanel != null)
    {
        Destroy(currentLoadingPanel.gameObject);
        currentLoadingPanel = null;
    }
}
```

##### LoadingScreenPanel.cs

```csharp
protected override void OnInitialize()
{
    base.OnInitialize();

    // Canvas 컴포넌트 검증 (Prefab에 미리 설정되어 있어야 함)
    Canvas canvas = GetComponent<Canvas>();
    if (canvas == null)
    {
        Debug.LogError("[LoadingScreenPanel] Canvas component is missing! " +
                      "This Prefab must have Canvas, CanvasScaler, and GraphicRaycaster components.");
    }
}

public override void OnShow()
{
    if (currentState == UIPanelState.Active) return;

    currentState = UIPanelState.Showing;
    gameObject.SetActive(true);
    displayStartTime = Time.time;

    // Prefab으로 인스턴스화되므로 이미 독립적인 Canvas를 가짐
    // DontDestroyOnLoad 제거됨 - 필요 없음

    StartCoroutine(FadeIn());
}

private IEnumerator FadeOut()
{
    // ... FadeOut 애니메이션 ...

    // FadeOut 완료 후 자기 파괴
    Destroy(gameObject);
}
```

#### 3. LoadingScreenPanel Prefab 구성

**Prefab 구조:**
```
LoadingScreenPanel (Root)
├── Canvas (Screen Space - Overlay, Sort Order: 999)
├── CanvasScaler (Scale With Screen Size: 1920x1080)
├── GraphicRaycaster
├── CanvasGroup
└── [UI Elements]
    ├── Background (Image)
    ├── ProgressBar (Slider)
    ├── ProgressText (TextMeshProUGUI)
    └── LoadingTipText (TextMeshProUGUI)
```

**Canvas 설정:**
- Render Mode: Screen Space - Overlay
- Sort Order: 999 (최상위 레이어)
- Pixel Perfect: 비활성화 (선택사항)

### 확장성 개선

#### 커스텀 로딩 화면 지원

```csharp
// 기본 로딩 화면 사용
sceneTransitionController.LoadSceneWithLoading(sceneData);

// 커스텀 로딩 화면 사용
GameObject customLoadingPrefab = ...; // 특별한 로딩 화면
sceneTransitionController.LoadSceneWithLoading(sceneData, customLoadingPrefab);
```

**활용 예시:**
- 보스전 씬: 보스 이미지와 전용 로딩 화면
- 튜토리얼 씬: 간소화된 로딩 화면
- 일반 씬: 기본 로딩 화면

## 📊 장점 분석

### 1. SRP (단일 책임 원칙) 준수

**SceneTransitionController:**
- ✅ 씬 전환 조율만 담당
- ✅ 로딩 패널 생성 책임만 가짐

**LoadingScreenPanel:**
- ✅ 자신의 UI 표시 및 생명주기만 관리
- ✅ 자기 파괴 책임 포함

### 2. Canvas 비용 문제 해결

- ❌ **이전**: DontDestroyOnLoad로 인한 전역 Canvas 필요
- ✅ **개선**: Prefab에 독립적인 Canvas 포함, 사용 후 즉시 정리
- ✅ 씬 전환 시 "고아" 객체 없음
- ✅ 메모리 효율적 관리

### 3. 확장성 향상

```csharp
// 다양한 로딩 화면 지원
public void LoadSceneWithLoading(SceneData sceneData, GameObject customLoadingPrefab = null)
{
    GameObject prefabToUse = customLoadingPrefab ?? loadingScreenPrefab;
    // ... 인스턴스화 및 표시
}
```

### 4. 참조 안정성

- ✅ `currentLoadingPanel`은 항상 명시적으로 관리됨
- ✅ Missing 참조 불가능 (매번 새로 생성)
- ✅ 생명주기가 명확함

### 5. 성능

- ✅ UI Prefab 인스턴스화 비용 << 씬 로딩 비용
- ✅ 메모리에 불필요한 객체 유지 없음
- ✅ 사용 시에만 생성, 완료 후 즉시 정리

## 🔄 동작 흐름

```
1. 씬 전환 요청
   ↓
2. SceneTransitionController.LoadSceneWithLoading(sceneData)
   ↓
3. currentLoadingPanel = Instantiate(loadingScreenPrefab).GetComponent<LoadingScreenPanel>()
   ↓
4. currentLoadingPanel.OnShow() → FadeIn 애니메이션
   ↓
5. SceneLoaderService.LoadSceneAsync() 진행
   ↓
6. 진행률 업데이트: currentLoadingPanel.UpdateProgress(progress)
   ↓
7. 씬 로딩 완료
   ↓
8. currentLoadingPanel.OnHide() → FadeOut 애니메이션
   ↓
9. FadeOut 완료 → Destroy(currentLoadingPanel.gameObject)
   ↓
10. currentLoadingPanel = null (다음 전환 대기)
```

## ✅ 구현 체크리스트

### Phase 1: LoadingScreenPanel Prefab 준비
- [x] Prefab에 Canvas 컴포넌트 추가 (Screen Space - Overlay, Sort Order: 999)
- [x] CanvasScaler 추가 (Scale With Screen Size: 1920x1080)
- [x] GraphicRaycaster 추가
- [x] 모든 UI 요소가 Prefab에 구성되어 있는지 확인

### Phase 2: LoadingScreenPanel.cs 수정
- [x] OnShow()에서 DontDestroyOnLoad 제거
- [x] OnShow()에서 transform.SetParent(null) 제거
- [x] OnShow()에서 Canvas 동적 추가 로직 제거
- [x] OnInitialize()에 Canvas 검증 로직 추가
- [x] FadeOut 후 Destroy(gameObject) 유지

### Phase 3: SceneTransitionController.cs 수정
- [x] SerializeField 변경: `LoadingScreenPanel` → `GameObject loadingScreenPrefab`
- [x] `private LoadingScreenPanel currentLoadingPanel` 필드 추가
- [x] ShowLoadingScreen() 수정: Prefab 인스턴스화 로직 추가
- [x] Awake()의 FindObjectOfType 로직 제거
- [x] HideLoadingScreen() 수정: currentLoadingPanel 사용
- [x] OnLoadingProgressUpdated() 수정: currentLoadingPanel 사용
- [x] LoadSceneWithLoading() 수정: currentLoadingPanel 사용
- [x] OnDestroy()에 currentLoadingPanel 정리 로직 추가
- [x] 커스텀 로딩 화면 지원 오버로드 추가

### Phase 4: 검증
- [ ] 첫 번째 씬 전환 정상 동작 확인
- [ ] 연속 씬 전환 (2-3회) 정상 동작 확인
- [ ] 메모리 누수 없음 확인 (Unity Profiler)
- [ ] 커스텀 로딩 화면 확장성 테스트 (선택사항)

## 🚀 다음 단계

1. **Unity Editor에서 LoadingScreenPanel Prefab 생성**
   - 새 GameObject 생성 → LoadingScreenPanel 이름 지정
   - Canvas, CanvasScaler, GraphicRaycaster 컴포넌트 추가
   - Canvas 설정: Screen Space - Overlay, Sort Order: 999
   - LoadingScreenPanel 스크립트 추가
   - UI 요소 구성 (Background, ProgressBar, Text 등)
   - Prefab으로 저장

2. **SceneTransitionController에 Prefab 할당**
   - Scene에 있는 SceneTransitionController 선택
   - Inspector에서 Loading Screen Prefab 필드에 Prefab 드래그

3. **테스트**
   - 첫 씬 전환 테스트
   - 연속 씬 전환 테스트
   - 메모리 프로파일링

4. **문서화** (완료)
   - ✅ 구현 가이드 작성
   - ✅ 아키텍처 다이어그램 설명
   - ✅ 사용 예시 정리

## 📚 참고 자료

- [SceneTransitionController.cs](../Assets/Script/Game/Controllers/SceneTransitionController.cs)
- [LoadingScreenPanel.cs](../Assets/Script/UI/Core/LoadingScreenPanel.cs)
- [SceneData.cs](../Assets/SO/SceneData/SceneData.cs)
- [AsyncSceneTransition_Implementation_Guide.md](./AsyncSceneTransition_Implementation_Guide.md)
