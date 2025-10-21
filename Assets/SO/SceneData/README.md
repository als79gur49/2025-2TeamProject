# ScriptableObject 기반 씬 관리 시스템

Unity의 ScriptableObject 패턴을 활용한 타입 안전 씬 관리 시스템입니다.

## 📚 개요

### 문제점: 문자열 기반 씬 참조의 한계
```csharp
// ❌ 기존 방식: 오타에 취약하고 리팩토링 시 깨짐
SceneManager.LoadScene("MainMenu"); // "MainMenu"라는 씬이 있는지 컴파일 타임에 알 수 없음
```

### 해결책: ScriptableObject 기반 씬 데이터
```csharp
// ✅ 새로운 방식: GUID 기반으로 안전하고 메타데이터 지원
sceneLoader.LoadScene(mainMenuSceneData); // Inspector에서 드래그 앤 드롭, 자동 검증
```

## 🎯 주요 장점

1. **타입 안정성**: GUID 기반 참조로 씬 파일명 변경에도 참조 유지
2. **에디터 검증**: 씬 이름 오타 방지, Build Settings 자동 체크
3. **메타데이터 지원**: 로딩 팁, 배경음악, 카테고리 등 추가 정보
4. **확장성**: 새 메타데이터 필드를 쉽게 추가 가능
5. **디자이너 친화적**: 코드 수정 없이 Inspector에서 설정 변경

## 📁 파일 구조

```
Assets/SO/SceneData/
├── SceneCategory.cs              # 씬 카테고리 enum
├── SceneData.cs                  # 핵심 ScriptableObject
├── Editor/
│   └── SceneDataEditor.cs        # 커스텀 인스펙터
└── [생성된 에셋들]
    ├── TitleTestScene.asset
    ├── StageTestScene.asset
    └── ...

Assets/Script/Game/Services/
├── SceneLoaderService.cs         # 씬 로딩 서비스
└── Interfaces/
    └── ISceneLoaderService.cs    # 인터페이스
```

## 🚀 빠른 시작

### 1. SceneData 에셋 생성

1. **프로젝트 창에서 우클릭**
2. **`Create > Game > Scene Management > Scene Data`** 선택
3. **에셋 이름 설정** (예: `TitleScene.asset`)

### 2. SceneData 설정

Inspector에서 다음을 설정합니다:

#### 기본 정보
- **씬 이름**: 드롭다운에서 선택 (Build Settings 기반)
- **카테고리**: Menu, Gameplay, Prototype, FeatureTest, Tool 중 선택

#### 로딩 화면 설정 (옵션)
- **배경 이미지**: 로딩 화면에 표시할 Sprite
- **로딩 팁**: 로딩 중 표시할 팁 배열

#### 오디오 설정 (옵션)
- **배경음악**: AudioData ScriptableObject 참조

#### 메타데이터
- **설명**: 에디터용 설명 (런타임에서는 사용되지 않음)

### 3. 씬 로딩 사용

#### 방법 1: SceneLoaderService 사용 (권장)

```csharp
using Game.Services;
using Game.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private SceneData titleSceneData;
    [SerializeField] private SceneData gameplaySceneData;

    private ISceneLoaderService sceneLoader;

    void Start()
    {
        // ServiceLocator에서 가져오기
        sceneLoader = ServiceLocator.Get<ISceneLoaderService>();

        // 또는 직접 참조
        sceneLoader = FindObjectOfType<SceneLoaderService>();
    }

    // 동기 로딩 (즉시 전환)
    public void LoadTitle()
    {
        sceneLoader.LoadScene(titleSceneData);
    }

    // 비동기 로딩 (진행률 표시)
    public void LoadGameplay()
    {
        sceneLoader.LoadSceneAsync(gameplaySceneData, OnLoadProgress);
    }

    private void OnLoadProgress(float progress)
    {
        loadingBar.fillAmount = progress;
        loadingText.text = $"Loading... {progress * 100:F0}%";
    }
}
```

#### 방법 2: 정적 헬퍼 메서드 (간단한 경우)

```csharp
using Game.Services;
using Game.SceneManagement;

public class QuickSceneChanger : MonoBehaviour
{
    [SerializeField] private SceneData targetScene;

    public void QuickLoad()
    {
        SceneLoaderService.QuickLoadScene(targetScene);
    }
}
```

#### 방법 3: 이벤트 구독

```csharp
void OnEnable()
{
    sceneLoader.OnSceneLoadStarted += HandleSceneLoadStarted;
    sceneLoader.OnSceneLoadProgress += HandleSceneLoadProgress;
    sceneLoader.OnSceneLoadCompleted += HandleSceneLoadCompleted;
    sceneLoader.OnSceneLoadFailed += HandleSceneLoadFailed;
}

void OnDisable()
{
    sceneLoader.OnSceneLoadStarted -= HandleSceneLoadStarted;
    sceneLoader.OnSceneLoadProgress -= HandleSceneLoadProgress;
    sceneLoader.OnSceneLoadCompleted -= HandleSceneLoadCompleted;
    sceneLoader.OnSceneLoadFailed -= HandleSceneLoadFailed;
}

private void HandleSceneLoadStarted(SceneData sceneData)
{
    Debug.Log($"Loading started: {sceneData.SceneName}");
}

private void HandleSceneLoadProgress(SceneData sceneData, float progress)
{
    loadingBar.fillAmount = progress;
}

private void HandleSceneLoadCompleted(SceneData sceneData)
{
    Debug.Log($"Loading completed: {sceneData.SceneName}");
}

private void HandleSceneLoadFailed(SceneData sceneData, string error)
{
    Debug.LogError($"Loading failed: {sceneData.SceneName} - {error}");
}
```

## 🔧 커스텀 인스펙터 기능

SceneData 에셋을 선택하면 강력한 커스텀 인스펙터가 표시됩니다:

### 씬 이름 드롭다운
- Build Settings에 있는 모든 씬을 드롭다운으로 표시
- 오타 방지 및 빠른 선택
- "새로고침" 버튼으로 씬 목록 업데이트

### 실시간 유효성 검사
- ✅ **성공**: 씬이 Build Settings에 있고 파일이 존재함
- ⚠️ **경고**: Build Settings에 없거나 비활성화됨
- ❌ **오류**: 씬 파일이 존재하지 않음

### 유틸리티 버튼
- **씬 열기**: 해당 씬을 Unity 에디터에서 바로 열기
- **Build Settings에 추가**: 씬을 Build Settings에 자동으로 추가
- **Build Settings 열기**: Build Settings 창 열기

## 📊 SceneData 프로퍼티

### 읽기 전용 프로퍼티

```csharp
public string SceneName { get; }           // 씬 파일 이름
public SceneCategory Category { get; }     // 씬 카테고리
public Sprite LoadingBackground { get; }   // 로딩 배경 이미지
public string[] LoadingTips { get; }       // 로딩 팁 배열
public ScriptableObject BgMusic { get; }   // 배경음악 SO
public string Description { get; }         // 에디터용 설명
```

### 유틸리티 메서드

```csharp
// 랜덤 로딩 팁 가져오기
string tip = sceneData.GetRandomLoadingTip();

// Build Settings 확인
bool inBuildSettings = sceneData.IsSceneInBuildSettings();

// 씬 파일 존재 확인
bool exists = sceneData.DoesSceneExist();
```

## 🎨 SceneCategory 종류

```csharp
public enum SceneCategory
{
    Menu,         // 메뉴 씬 (타이틀, 설정 등)
    Gameplay,     // 게임플레이 씬 (실제 게임 진행)
    Prototype,    // 프로토타입 테스트 씬
    FeatureTest,  // 개별 기능 테스트 씬
    Tool          // 도구/유틸리티 씬
}
```

## 🔌 ServiceLocator 통합 (옵션)

프로젝트의 ServiceLocator 패턴과 통합하려면:

```csharp
// GameInitializer.cs 또는 초기화 스크립트에서
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // SceneLoaderService 인스턴스 생성
        var sceneLoaderService = gameObject.AddComponent<SceneLoaderService>();

        // ServiceLocator에 등록
        ServiceLocator.Register<ISceneLoaderService>(sceneLoaderService);
    }
}

// 사용처에서
var sceneLoader = ServiceLocator.Get<ISceneLoaderService>();
sceneLoader.LoadScene(targetSceneData);
```

## 💡 고급 사용 예시

### 로딩 화면 통합

```csharp
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text tipText;
    [SerializeField] private Slider progressBar;

    public void ShowLoadingScreen(SceneData sceneData)
    {
        gameObject.SetActive(true);

        // 배경 이미지 설정
        if (sceneData.LoadingBackground != null)
        {
            backgroundImage.sprite = sceneData.LoadingBackground;
        }

        // 랜덤 로딩 팁 표시
        tipText.text = sceneData.GetRandomLoadingTip();

        // 진행률 초기화
        progressBar.value = 0f;
    }

    public void UpdateProgress(float progress)
    {
        progressBar.value = progress;
    }
}
```

### 배경음악 자동 재생

```csharp
public class SceneAudioManager : MonoBehaviour
{
    private ISceneLoaderService sceneLoader;

    void Start()
    {
        sceneLoader = ServiceLocator.Get<ISceneLoaderService>();
        sceneLoader.OnSceneLoadCompleted += HandleSceneLoaded;
    }

    private void HandleSceneLoaded(SceneData sceneData)
    {
        // 배경음악이 설정되어 있으면 재생
        if (sceneData.BgMusic != null)
        {
            // AudioData SO를 사용한 배경음악 재생
            var audioService = ServiceLocator.Get<IAudioService>();
            audioService.PlayBGM(sceneData.BgMusic);
        }
    }
}
```

### 씬 카테고리별 필터링

```csharp
public class SceneSelector : MonoBehaviour
{
    [SerializeField] private SceneData[] allScenes;

    // 특정 카테고리의 씬만 가져오기
    public SceneData[] GetScenesByCategory(SceneCategory category)
    {
        return allScenes.Where(s => s.Category == category).ToArray();
    }

    // 테스트 씬만 표시
    public void ShowTestScenes()
    {
        var testScenes = GetScenesByCategory(SceneCategory.FeatureTest);
        // UI에 표시...
    }
}
```

## ⚠️ 주의사항

### 1. Build Settings 동기화
- SceneData 에셋을 만든 후 반드시 **Build Settings에 씬을 추가**해야 합니다
- 커스텀 인스펙터의 "Build Settings에 추가" 버튼을 활용하세요

### 2. 씬 이름 일치
- SceneData의 `sceneName` 필드는 **확장자(.unity)를 제외한 파일명**과 일치해야 합니다
- 드롭다운을 사용하면 자동으로 올바른 이름이 설정됩니다

### 3. 에셋 참조 유지
- SceneData 에셋의 **파일명은 자유롭게 변경 가능** (GUID 기반)
- 하지만 에셋을 **삭제하면 참조가 깨짐** (Missing Reference)

### 4. 런타임 검증
- `OnValidate()`는 에디터 전용이므로 런타임에서는 실행되지 않습니다
- `SceneLoaderService`가 로딩 전 자동으로 검증을 수행합니다

## 🐛 문제 해결

### Q: "Scene '...' is not in Build Settings" 경고가 뜹니다
**A**:
1. SceneData 인스펙터에서 "Build Settings에 추가" 버튼 클릭
2. 또는 `File > Build Settings`에서 수동으로 씬을 추가

### Q: 드롭다운에 씬이 표시되지 않습니다
**A**:
1. "새로고침" 버튼 클릭
2. Build Settings에 씬이 추가되어 있고 활성화되어 있는지 확인

### Q: SceneData 에셋을 만들 수 없습니다
**A**:
1. Unity 에디터를 재시작하여 스크립트 컴파일
2. Console 창에서 컴파일 오류 확인

### Q: 씬 로딩이 실패합니다
**A**:
1. SceneData 인스펙터에서 유효성 검사 확인 (녹색 체크 표시)
2. `sceneLoader.OnSceneLoadFailed` 이벤트로 에러 메시지 확인
3. Build Settings에 씬이 활성화되어 있는지 확인

## 📚 추가 확장 아이디어

### 씬 전환 효과
```csharp
// SceneTransitionData.cs 추가
[CreateAssetMenu(fileName = "Transition", menuName = "Game/Scene Management/Transition Data")]
public class SceneTransitionData : ScriptableObject
{
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public Color fadeColor = Color.black;
}

// SceneData에 전환 효과 참조 추가
[SerializeField] private SceneTransitionData transition;
```

### 씬 프리로드
```csharp
// 다음 씬을 미리 로드 (백그라운드)
public void PreloadScene(SceneData sceneData)
{
    SceneManager.LoadSceneAsync(sceneData.SceneName, LoadSceneMode.Additive);
}
```

### 씬 히스토리 추적
```csharp
// 이전 씬으로 돌아가기
private Stack<SceneData> sceneHistory = new Stack<SceneData>();

public void LoadSceneWithHistory(SceneData sceneData)
{
    sceneHistory.Push(currentSceneData);
    sceneLoader.LoadScene(sceneData);
}

public void GoBack()
{
    if (sceneHistory.Count > 0)
    {
        sceneLoader.LoadScene(sceneHistory.Pop());
    }
}
```

## 📖 참고 자료

- [Unity ScriptableObject 공식 문서](https://docs.unity3d.com/Manual/class-ScriptableObject.html)
- [Unity SceneManager API](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html)
- [Unity Custom Editor](https://docs.unity3d.com/Manual/editor-CustomEditors.html)

## 🤝 기여

이 시스템을 개선하기 위한 아이디어나 버그 리포트를 환영합니다!

---

**Created with**: Unity ScriptableObject Pattern
**Version**: 1.0.0
**Author**: Game Development Team
**Date**: 2025-10-19
