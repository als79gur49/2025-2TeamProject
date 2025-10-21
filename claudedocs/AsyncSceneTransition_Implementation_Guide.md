# 비동기 씬 전환 시스템 구현 가이드

## 📊 구현 개요

메인 메뉴(TitleTestScene)에서 스테이지 씬(StageTestScene)으로 버튼을 통한 비동기 씬 전환 시스템을 구현했습니다.

**구현 날짜**: 2025-10-19
**프로젝트**: 2025-2TeamProject
**기능**: SceneLoaderService 기반 비동기 씬 로딩 with 로딩 화면

---

## 🎯 구현된 컴포넌트

### ✅ 1. SceneTransitionController
**파일**: [Assets/Script/Game/Controllers/SceneTransitionController.cs](../Assets/Script/Game/Controllers/SceneTransitionController.cs)

**역할**:
- SceneLoaderService와 LoadingScreenPanel 연동
- 씬 전환 흐름 제어 및 상태 관리
- 로딩 진행률 추적 및 이벤트 전파

**주요 메서드**:
```csharp
public void LoadSceneWithLoading(SceneData sceneData)  // 로딩 화면과 함께 씬 로드
public void LoadSceneImmediate(SceneData sceneData)    // 즉시 씬 로드
public bool IsTransitioning { get; }                   // 전환 중 여부 확인
```

**이벤트**:
- `OnSceneTransitionStarted` - 씬 전환 시작
- `OnSceneTransitionCompleted` - 씬 전환 완료
- `OnSceneTransitionFailed` - 씬 전환 실패

---

### ✅ 2. LoadingScreenPanel
**파일**: [Assets/Script/UI/Core/LoadingScreenPanel.cs](../Assets/Script/UI/Core/LoadingScreenPanel.cs)

**역할**:
- 씬 로딩 중 진행률 표시
- 로딩 팁 텍스트 표시
- 페이드 인/아웃 애니메이션
- 최소 표시 시간 보장 (너무 빠른 전환 방지)

**주요 메서드**:
```csharp
public void UpdateProgress(float progress)  // 진행률 업데이트 (0.0 ~ 1.0)
public void SetLoadingTip(string tip)       // 로딩 팁 설정
public override void OnShow()               // 로딩 화면 표시 (페이드 인)
public override void OnHide()               // 로딩 화면 숨김 (페이드 아웃)
```

**설정 가능한 옵션**:
- `fadeInDuration` - 페이드 인 시간 (기본: 0.3초)
- `fadeOutDuration` - 페이드 아웃 시간 (기본: 0.5초)
- `minimumDisplayTime` - 최소 표시 시간 (기본: 1.0초)
- `showPercentage` - 퍼센트 표시 여부
- `loadingTextFormat` - 로딩 텍스트 포맷

**UI 컴포넌트** (Inspector에서 할당 또는 자동 생성):
- `Slider progressBar` - 진행률 바
- `TextMeshProUGUI progressText` - 퍼센트 텍스트
- `TextMeshProUGUI loadingTipText` - 로딩 팁
- `CanvasGroup canvasGroup` - 페이드 효과용
- `Image backgroundImage` - 배경 이미지

---

### ✅ 3. MainMenuPanel
**파일**: [Assets/Script/UI/Menu/MainMenuPanel.cs](../Assets/Script/UI/Menu/MainMenuPanel.cs)

**역할**:
- 메인 메뉴 UI 관리
- 게임 시작 버튼을 통한 씬 전환 트리거
- 설정, 종료 버튼 제공

**주요 메서드**:
```csharp
private void OnStartGameButtonClicked()    // 게임 시작 버튼 핸들러
private void OnSettingsButtonClicked()     // 설정 버튼 핸들러
private void OnQuitButtonClicked()         // 종료 버튼 핸들러
```

**Inspector 할당 필요**:
- `Button startGameButton` - 게임 시작 버튼
- `Button settingsButton` - 설정 버튼 (옵션)
- `Button quitButton` - 종료 버튼 (옵션)
- `SceneData stageSceneData` - 스테이지 씬 데이터 (SO_StageTestScene)
- `SceneTransitionController sceneTransitionController` - 씬 전환 컨트롤러

---

### ✅ 4. SceneData 에셋

#### SO_TitleTestScene.asset
**파일**: [Assets/SO/SceneData/SO_TitleTestScene.asset](../Assets/SO/SceneData/SO_TitleTestScene.asset)

**설정**:
- Scene Name: `TitleTestScene`
- Category: `Menu` (0)
- Loading Tips:
  - "메인 메뉴에 오신 것을 환영합니다!"
  - "게임 시작 버튼을 눌러 시작하세요"
  - "설정에서 사운드와 그래픽을 조정할 수 있습니다"
- Description: "게임의 타이틀 화면 (Main Menu Scene)"

#### SO_StageTestScene.asset
**파일**: [Assets/SO/SceneData/SO_StageTestScene.asset](../Assets/SO/SceneData/SO_StageTestScene.asset)

**설정**:
- Scene Name: `StageTestScene`
- Category: `Gameplay` (1)
- Loading Tips:
  - "스테이지를 로딩하고 있습니다..."
  - "카드를 준비하세요!"
  - "적과의 전투가 곧 시작됩니다"
  - "전략을 세우고 승리를 쟁취하세요"
- Description: "게임 스테이지 씬 (Gameplay Scene)"

---

## 🔄 실행 흐름

```
1. TitleTestScene 로드
   ↓
2. MainMenuPanel 초기화 및 표시
   ↓
3. 사용자 "게임 시작" 버튼 클릭
   ↓
4. MainMenuPanel.OnStartGameButtonClicked()
   ↓
5. SceneTransitionController.LoadSceneWithLoading(stageSceneData)
   ↓
6. LoadingScreenPanel.OnShow() - 페이드 인
   ↓
7. SceneLoaderService.LoadSceneAsync(sceneData, OnProgressUpdated)
   ↓
8. 진행률 업데이트 콜백
   - LoadingScreenPanel.UpdateProgress(0.0 ~ 1.0)
   - Progress Bar 업데이트
   - Loading Tip 랜덤 표시
   ↓
9. StageTestScene 로드 완료
   ↓
10. LoadingScreenPanel.OnHide() - 페이드 아웃
   ↓
11. StageTestScene 활성화
```

---

## 🏗️ Unity 씬 설정 가이드

### TitleTestScene 설정

#### 1. Canvas 구조
```
TitleTestScene
├── Canvas (Canvas, CanvasScaler, GraphicRaycaster)
│   └── MainMenuPanel (MainMenuPanel.cs)
│       ├── Background (Image) - 배경
│       ├── Title (TextMeshProUGUI) - 타이틀 텍스트
│       ├── StartGameButton (Button)
│       │   └── Text (TextMeshProUGUI) - "게임 시작"
│       ├── SettingsButton (Button) [옵션]
│       │   └── Text (TextMeshProUGUI) - "설정"
│       └── QuitButton (Button) [옵션]
│           └── Text (TextMeshProUGUI) - "종료"
│
├── LoadingScreen (Canvas) [DontDestroyOnLoad]
│   └── LoadingScreenPanel (LoadingScreenPanel.cs)
│       ├── Background (Image) - 전체 화면 검은 배경
│       ├── ProgressBar (Slider)
│       │   ├── Background (Image)
│       │   └── Fill Area
│       │       └── Fill (Image) - 진행률 표시
│       ├── ProgressText (TextMeshProUGUI) - "Loading... 75%"
│       └── LoadingTipText (TextMeshProUGUI) - 로딩 팁
│
└── SceneTransitionController (GameObject)
    └── SceneTransitionController.cs
```

#### 2. MainMenuPanel Inspector 설정
1. MainMenuPanel GameObject 선택
2. MainMenuPanel 컴포넌트 Inspector:
   - **UI References**:
     - Start Game Button: Hierarchy에서 StartGameButton 드래그
     - Settings Button: Hierarchy에서 SettingsButton 드래그
     - Quit Button: Hierarchy에서 QuitButton 드래그
   - **Scene References**:
     - Stage Scene Data: `SO_StageTestScene` 에셋 할당
   - **Dependencies**:
     - Scene Transition Controller: Hierarchy에서 SceneTransitionController 드래그

#### 3. SceneTransitionController Inspector 설정
1. SceneTransitionController GameObject 선택
2. SceneTransitionController 컴포넌트 Inspector:
   - **Dependencies**:
     - Loading Screen Panel: Hierarchy에서 LoadingScreenPanel 드래그

#### 4. LoadingScreenPanel Inspector 설정
1. LoadingScreenPanel GameObject 선택
2. LoadingScreenPanel 컴포넌트 Inspector:
   - **UI References**:
     - Progress Bar: Hierarchy에서 ProgressBar (Slider) 드래그
     - Progress Text: Hierarchy에서 ProgressText 드래그
     - Loading Tip Text: Hierarchy에서 LoadingTipText 드래그
     - Canvas Group: 자동 생성 또는 수동 할당
     - Background Image: Hierarchy에서 Background 드래그
   - **Animation Settings**:
     - Fade In Duration: `0.3`
     - Fade Out Duration: `0.5`
     - Minimum Display Time: `1.0`
   - **Progress Settings**:
     - Show Percentage: ✅ 체크
     - Loading Text Format: `"Loading... {0:0}%"`
     - Default Loading Tip: `"로딩 중..."`
   - **UI Panel Settings**:
     - Priority: `High`
     - Initialize On Awake: ✅ 체크
     - Hide On Start: ✅ 체크

---

## 📦 Build Settings 설정

Unity Editor에서 다음 씬들을 Build Settings에 추가해야 합니다:

### Build Settings 열기
1. `File > Build Settings` (Ctrl+Shift+B)
2. "Add Open Scenes" 또는 "Scenes in Build" 섹션에서 씬 추가

### 필수 씬 목록
```
Scenes In Build:
✅ 0. TitleTestScene
✅ 1. StageTestScene
```

### 씬 추가 방법
1. Project 창에서 씬 파일 선택:
   - `Assets/Scenes/TestScenes/Prototypes/TitleTestScene.unity`
   - `Assets/Scenes/TestScenes/Prototypes/StageTestScene.unity`
2. Build Settings 창으로 드래그 앤 드롭
3. 체크박스가 활성화되어 있는지 확인

---

## 🔧 GameInitializer 통합

SceneLoaderService를 ServiceLocator에 등록해야 합니다.

### GameInitializer.cs 수정

```csharp
using UnityEngine;
using Game.Services;

namespace Game.Core
{
    public class GameInitializer : MonoBehaviour
    {
        private void Awake()
        {
            InitializeServices();
        }

        private void InitializeServices()
        {
            // SceneLoaderService 등록
            InitializeSceneLoader();

            // 기타 서비스 초기화...
        }

        private void InitializeSceneLoader()
        {
            // SceneLoaderService GameObject 생성
            GameObject sceneLoaderObject = new GameObject("SceneLoaderService");
            DontDestroyOnLoad(sceneLoaderObject);

            // SceneLoaderService 컴포넌트 추가
            var sceneLoaderService = sceneLoaderObject.AddComponent<SceneLoaderService>();

            // ServiceLocator에 등록
            ServiceLocator.Register<ISceneLoaderService>(sceneLoaderService);

            Debug.Log("[GameInitializer] SceneLoaderService registered successfully");
        }
    }
}
```

---

## 🎨 UI 스타일 가이드 (권장)

### 버튼 스타일
```
StartGameButton:
- Size: 200x50
- Color: rgb(51, 153, 204) - 파란색
- Text: "게임 시작" (white, 24px)
- Position: Center (0, 50)

SettingsButton:
- Size: 200x50
- Color: rgb(153, 153, 153) - 회색
- Text: "설정" (white, 24px)
- Position: Center (0, -20)

QuitButton:
- Size: 200x50
- Color: rgb(204, 51, 51) - 빨간색
- Text: "종료" (white, 24px)
- Position: Center (0, -90)
```

### 로딩 화면 스타일
```
Background:
- Color: rgba(0, 0, 0, 0.95) - 거의 불투명 검정

ProgressBar:
- Size: 40% 화면 너비, 20px 높이
- Position: Center (0, -50)
- Background Color: rgba(51, 51, 51, 0.8)
- Fill Color: rgb(51, 204, 51) - 초록색

ProgressText:
- Font Size: 24px
- Color: white
- Position: Center, ProgressBar 위쪽 30px

LoadingTipText:
- Font Size: 18px
- Color: rgba(204, 204, 204, 1.0)
- Position: Center, ProgressBar 아래쪽 50px
```

---

## 🧪 테스트 시나리오

### 1. 기본 씬 전환 테스트
**절차**:
1. Unity Editor에서 TitleTestScene 실행
2. "게임 시작" 버튼 클릭
3. 로딩 화면이 페이드 인으로 나타나는지 확인
4. Progress Bar가 0% → 100%로 진행되는지 확인
5. 로딩 팁이 표시되는지 확인
6. StageTestScene이 로드되는지 확인
7. 로딩 화면이 페이드 아웃되는지 확인

**예상 결과**:
- ✅ 부드러운 페이드 인/아웃 애니메이션
- ✅ 실시간 진행률 업데이트
- ✅ 로딩 팁 표시
- ✅ StageTestScene 정상 로드

---

### 2. 중복 클릭 방지 테스트
**절차**:
1. TitleTestScene에서 "게임 시작" 버튼 빠르게 여러 번 클릭

**예상 결과**:
- ✅ 첫 클릭 후 버튼 비활성화
- ✅ 중복 씬 로드 발생하지 않음
- ⚠️ Console에 "Scene transition already in progress" 경고 (정상)

---

### 3. 최소 표시 시간 테스트
**절차**:
1. 작은 씬으로 빠른 로딩 시뮬레이션
2. 로딩이 1초 이내에 완료되더라도 최소 1초간 화면 표시되는지 확인

**예상 결과**:
- ✅ 로딩이 빨라도 최소 1초간 로딩 화면 유지
- ✅ 너무 빠른 전환으로 인한 깜빡임 없음

---

### 4. SceneData 검증 테스트
**절차**:
1. SO_StageTestScene 에셋 선택
2. Inspector에서 유효성 검사 확인
3. Scene Name이 Build Settings에 있는지 확인

**예상 결과**:
- ✅ 녹색 체크: "씬이 Build Settings에 있습니다"
- ✅ 녹색 체크: "씬 파일이 존재합니다"

---

## 🐛 문제 해결 가이드

### 문제 1: "SceneLoaderService not found in ServiceLocator!"
**원인**: SceneLoaderService가 ServiceLocator에 등록되지 않음

**해결책**:
1. GameInitializer에 SceneLoaderService 등록 코드 추가
2. TitleTestScene에 GameInitializer GameObject 배치
3. 프로젝트 시작 씬에서 SceneLoaderService 초기화 확인

---

### 문제 2: "Scene validation failed: Scene 'StageTestScene' is not in Build Settings"
**원인**: StageTestScene이 Build Settings에 추가되지 않음

**해결책**:
1. `File > Build Settings` 열기
2. `Assets/Scenes/TestScenes/Prototypes/StageTestScene.unity` 드래그
3. 체크박스 활성화 확인

---

### 문제 3: 로딩 화면이 표시되지 않음
**원인**: LoadingScreenPanel이 제대로 할당되지 않음

**해결책**:
1. SceneTransitionController Inspector 확인
2. Loading Screen Panel 필드에 LoadingScreenPanel GameObject 할당
3. LoadingScreenPanel이 Canvas 하위에 있는지 확인
4. Canvas에 DontDestroyOnLoad 태그 또는 별도 Canvas 사용

---

### 문제 4: Progress Bar가 업데이트되지 않음
**원인**: LoadingScreenPanel의 UI 컴포넌트가 제대로 할당되지 않음

**해결책**:
1. LoadingScreenPanel Inspector 확인
2. Progress Bar (Slider) 할당 확인
3. Progress Text (TextMeshProUGUI) 할당 확인
4. Slider의 Min Value = 0, Max Value = 1 확인

---

### 문제 5: "LoadingScreenPanel not available" 경고
**원인**: SceneTransitionController가 LoadingScreenPanel을 찾지 못함

**해결책**:
1. LoadingScreenPanel GameObject가 씬에 존재하는지 확인
2. LoadingScreenPanel.cs 컴포넌트가 추가되어 있는지 확인
3. SceneTransitionController Inspector에서 수동 할당
4. 또는 `FindObjectOfType<LoadingScreenPanel>()`이 작동하도록 Active 상태 확인

---

## 📚 추가 기능 확장 아이디어

### 1. 페이드 효과 커스터마이징
```csharp
// LoadingScreenPanel에 추가
[SerializeField] private AnimationCurve fadeCurve;

private IEnumerator FadeInWithCurve()
{
    float elapsedTime = 0f;
    while (elapsedTime < fadeInDuration)
    {
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / fadeInDuration;
        canvasGroup.alpha = fadeCurve.Evaluate(t);
        yield return null;
    }
}
```

---

### 2. 로딩 팁 애니메이션
```csharp
// LoadingScreenPanel에 추가
private IEnumerator AnimateLoadingTip()
{
    while (true)
    {
        loadingTipText.text = sceneData.GetRandomLoadingTip();
        yield return new WaitForSeconds(3f); // 3초마다 변경
    }
}
```

---

### 3. 씬 전환 히스토리
```csharp
// SceneTransitionController에 추가
private Stack<SceneData> sceneHistory = new Stack<SceneData>();

public void LoadSceneWithHistory(SceneData sceneData)
{
    SceneData currentScene = GetCurrentSceneData();
    if (currentScene != null)
        sceneHistory.Push(currentScene);

    LoadSceneWithLoading(sceneData);
}

public void GoBack()
{
    if (sceneHistory.Count > 0)
    {
        LoadSceneWithLoading(sceneHistory.Pop());
    }
}
```

---

### 4. 씬 프리로드
```csharp
// SceneTransitionController에 추가
public void PreloadScene(SceneData sceneData)
{
    StartCoroutine(PreloadSceneCoroutine(sceneData));
}

private IEnumerator PreloadSceneCoroutine(SceneData sceneData)
{
    var asyncLoad = SceneManager.LoadSceneAsync(sceneData.SceneName, LoadSceneMode.Additive);
    asyncLoad.allowSceneActivation = false;

    while (asyncLoad.progress < 0.9f)
        yield return null;

    Debug.Log($"Scene preloaded: {sceneData.SceneName}");
}
```

---

## ✅ 완료 체크리스트

### 코드 구현
- [x] SceneTransitionController.cs 작성
- [x] LoadingScreenPanel.cs 작성
- [x] MainMenuPanel.cs 작성
- [x] SO_TitleTestScene.asset 설정
- [x] SO_StageTestScene.asset 생성

### Unity 씬 설정 (사용자 작업)
- [ ] TitleTestScene에 MainMenuPanel 추가
- [ ] MainMenuPanel UI 요소 생성 및 할당
- [ ] LoadingScreenPanel GameObject 생성
- [ ] LoadingScreenPanel UI 요소 생성 및 할당
- [ ] SceneTransitionController GameObject 추가
- [ ] Inspector에서 모든 참조 할당

### 프로젝트 설정 (사용자 작업)
- [ ] Build Settings에 TitleTestScene 추가
- [ ] Build Settings에 StageTestScene 추가
- [ ] GameInitializer에 SceneLoaderService 등록
- [ ] TextMeshPro 설정 확인

### 테스트 (사용자 작업)
- [ ] 기본 씬 전환 테스트
- [ ] 중복 클릭 방지 테스트
- [ ] 최소 표시 시간 테스트
- [ ] SceneData 검증 테스트

---

## 🎉 결론

**구현 완료 항목**:
1. ✅ SceneTransitionController - 씬 전환 흐름 제어
2. ✅ LoadingScreenPanel - 로딩 화면 및 진행률 표시
3. ✅ MainMenuPanel - 메인 메뉴 UI 및 버튼 핸들러
4. ✅ SceneData 에셋 - TitleTestScene, StageTestScene
5. ✅ 완전한 비동기 씬 전환 시스템

**프로젝트 이점**:
- 🚀 비동기 씬 로딩으로 부드러운 사용자 경험
- 📊 실시간 로딩 진행률 표시
- 💬 로딩 팁을 통한 사용자 피드백
- 🎨 페이드 효과로 전문적인 전환
- 🔧 SceneData 기반 타입 안전 씬 관리

**다음 단계**:
1. Unity Editor에서 씬 설정 및 UI 배치
2. Inspector에서 모든 컴포넌트 참조 할당
3. Build Settings 설정
4. 테스트 및 검증

---

**구현자**: Claude Code
**작성일**: 2025-10-19
**프로젝트**: 2025-2TeamProject
**버전**: 1.0.0
