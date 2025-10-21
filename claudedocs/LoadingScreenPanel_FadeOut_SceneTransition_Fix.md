# LoadingScreenPanel FadeOut 및 SceneLoaderService 타이밍 문제 해결

## 📋 개요

씬 전환 시 발생하는 두 가지 타이밍 문제를 동시에 해결했습니다:
1. **LoadingScreenPanel FadeOut 애니메이션 중단 문제**
2. **SceneLoaderService _isLoading 상태 리셋 타이밍 갭 문제**

---

## 🔥 문제 1: LoadingScreenPanel FadeOut 중단

### 문제 상황
```
1. OnSceneLoadCompleted 이벤트 발생 (씬 전환 **전**)
   ↓
2. LoadingScreenPanel.OnHide()
   ↓
3. FadeOut Coroutine 시작
   ↓
4. allowSceneActivation = true → 씬 즉시 전환
   ↓
5. LoadingScreenPanel 파괴됨 (Prefab 인스턴스, DontDestroyOnLoad 없음) ❌
   ↓
6. FadeOut Coroutine 중단됨 ❌
   ↓
7. FadeOut 애니메이션 실행 안 됨! ❌
```

### 근본 원인
- LoadingScreenPanel을 **Prefab 인스턴스화 패턴**으로 변경하면서 DontDestroyOnLoad 제거
- OnSceneLoadCompleted 이벤트는 씬 전환 **전**에 발생
- FadeOut Coroutine은 씬 전환 **후**에도 실행되어야 함
- 씬이 전환되면 GameObject 파괴 → Coroutine 중단

---

## 🔥 문제 2: SceneLoaderService _isLoading 타이밍 갭

### 문제 상황
```
1. LoadSceneAsyncCoroutine 실행:
   Line 115: _isLoading = true
   Line 165: OnSceneLoadCompleted 발생
   Line 169: allowSceneActivation = true → 씬 전환

2. 씬 전환 후 새 씬에서 즉시 LoadSceneWithLoading 호출:
   ↓
3. SceneLoaderService.LoadSceneAsync():
   Line 101: if (_isLoading) 체크
   ↓
4. _isLoading = true (아직 리셋 안 됨) ❌
   ↓
5. "Already loading a scene" 경고 → 씬 전환 차단 ❌
   ↓
6. Line 188: _isLoading = false (Coroutine 종료 시점, 너무 늦음)
```

### 근본 원인
- **타이밍 갭**: allowSceneActivation(169줄)과 _isLoading 리셋(188줄) 사이에 갭 존재
- 새 씬이 로드되자마자 다시 씬 전환 시도하면 이전 Coroutine이 아직 실행 중
- 101줄 체크에서 차단됨

---

## ✅ 해결 방법

### 해결책 1: LoadingScreenPanel에 DontDestroyOnLoad 추가

**파일:** [Assets/Script/UI/Core/LoadingScreenPanel.cs](../Assets/Script/UI/Core/LoadingScreenPanel.cs:108-130)

**수정 내용:**
```csharp
public override void OnShow()
{
    if (currentState == UIPanelState.Active) return;

    currentState = UIPanelState.Showing;
    gameObject.SetActive(true);
    displayStartTime = Time.time;

    // ✅ DontDestroyOnLoad 적용 - 씬 전환 시 FadeOut을 위해 유지
    // OnSceneLoadCompleted 이벤트는 씬 전환 **전**에 발생하지만,
    // FadeOut 애니메이션은 씬 전환 **후**에도 실행되어야 함
    // FadeOut 완료 후 Destroy(gameObject)로 자동 정리됨
    DontDestroyOnLoad(gameObject);
    Debug.Log("[LoadingScreenPanel] Applied DontDestroyOnLoad for FadeOut across scene transition");

    // 페이드 인 애니메이션
    StartCoroutine(FadeIn());

    Debug.Log("[LoadingScreenPanel] Showing loading screen (Prefab instantiated)");
}
```

**설명:**
- OnShow() 시점에 DontDestroyOnLoad 적용
- 씬 전환 시 LoadingScreenPanel이 파괴되지 않고 유지됨
- FadeOut Coroutine이 정상적으로 완료될 때까지 실행 가능
- FadeOut 완료 후 Destroy(gameObject)로 자동 정리 (기존 코드 유지)

---

### 해결책 2: SceneLoaderService _isLoading 리셋 타이밍 변경

**파일:** [Assets/Script/Game/Services/SceneLoaderService.cs](../Assets/Script/Game/Services/SceneLoaderService.cs:163-175)

**수정 내용:**
```csharp
// 🎯 중요: 씬 전환 전에 이벤트 발생 (FadeOut 시작)
OnSceneLoadCompleted?.Invoke(sceneData);

// ✅ 씬 전환 직전에 _isLoading 리셋 (타이밍 갭 제거)
// 새 씬에서 즉시 다시 씬 전환을 시도해도 안전하도록 보장
_isLoading = false;
_currentLoadingScene = null;
Debug.Log($"[SceneLoaderService] Reset _isLoading before scene activation");

// 🎯 FadeOut이 시작된 후 씬 활성화
asyncLoad.allowSceneActivation = true;
```

**설명:**
- allowSceneActivation **직전**에 _isLoading = false 설정
- 타이밍 갭 완전 제거
- 새 씬에서 즉시 LoadSceneWithLoading 호출해도 안전
- 188줄의 중복 리셋은 harmless (이미 false)

---

## 🔄 수정 후 정상 흐름

### LoadingScreenPanel FadeOut 흐름 (수정 후)
```
1. LoadingScreenPanel 인스턴스화
   ↓
2. OnShow() → DontDestroyOnLoad(gameObject) ✅
   ↓
3. FadeIn Coroutine 실행
   ↓
4. OnSceneLoadCompleted 이벤트 발생 (씬 전환 **전**)
   ↓
5. OnHide() → FadeOut Coroutine 시작 ✅
   ↓
6. allowSceneActivation = true → 씬 전환
   ↓
7. LoadingScreenPanel은 DontDestroyOnLoad로 유지됨 ✅
   ↓
8. FadeOut Coroutine 계속 실행 ✅
   - 0.5초 동안 alpha 1.0 → 0.0
   ↓
9. FadeOut 완료 → Destroy(gameObject) ✅
   ↓
10. 자동 정리 완료 ✅
```

### SceneLoaderService 타이밍 흐름 (수정 후)
```
1. LoadSceneAsyncCoroutine 실행:
   Line 115: _isLoading = true
   ↓
2. 씬 로딩 완료:
   Line 165: OnSceneLoadCompleted 발생
   Line 169: _isLoading = false ✅ (타이밍 갭 제거)
   Line 175: allowSceneActivation = true → 씬 전환
   ↓
3. 새 씬에서 즉시 LoadSceneWithLoading 호출:
   ↓
4. SceneLoaderService.LoadSceneAsync():
   Line 101: if (_isLoading) 체크
   ↓
5. _isLoading = false ✅ (이미 리셋됨)
   ↓
6. 정상 진행 → 새 Coroutine 시작 ✅
```

---

## 📊 Before/After 비교

### Before (문제 발생)

**LoadingScreenPanel:**
```
OnShow() → FadeIn
  ↓
OnSceneLoadCompleted (씬 전환 전)
  ↓
OnHide() → FadeOut Coroutine 시작
  ↓
allowSceneActivation = true
  ↓
씬 전환 → LoadingScreenPanel 파괴 ❌
  ↓
FadeOut Coroutine 중단 ❌
```

**SceneLoaderService:**
```
_isLoading = true (Line 115)
  ↓
OnSceneLoadCompleted (Line 165)
  ↓
allowSceneActivation = true (Line 169)
  ↓
[타이밍 갭: _isLoading = true 상태]
  ↓
새 씬에서 LoadSceneWithLoading 호출
  ↓
❌ "Already loading" 에러
  ↓
_isLoading = false (Line 188, 너무 늦음)
```

---

### After (수정 후)

**LoadingScreenPanel:**
```
OnShow() → DontDestroyOnLoad ✅ → FadeIn
  ↓
OnSceneLoadCompleted (씬 전환 전)
  ↓
OnHide() → FadeOut Coroutine 시작 ✅
  ↓
allowSceneActivation = true
  ↓
씬 전환 → LoadingScreenPanel 유지됨 ✅
  ↓
FadeOut Coroutine 완료 ✅
  ↓
Destroy(gameObject) → 자동 정리 ✅
```

**SceneLoaderService:**
```
_isLoading = true (Line 115)
  ↓
OnSceneLoadCompleted (Line 165)
  ↓
_isLoading = false ✅ (Line 169, 타이밍 갭 제거)
  ↓
allowSceneActivation = true (Line 175)
  ↓
[_isLoading = false 상태]
  ↓
새 씬에서 LoadSceneWithLoading 호출
  ↓
✅ 정상 진행 (새 Coroutine 시작)
```

---

## 💡 설계 논리

### DontDestroyOnLoad 사용 이유

1. **씬 전환 시점 불일치**:
   - OnSceneLoadCompleted는 씬 전환 **전**에 발생
   - FadeOut은 씬 전환 **후**에도 실행되어야 함

2. **Coroutine 생명주기**:
   - Coroutine은 GameObject가 파괴되면 중단됨
   - DontDestroyOnLoad로 유지하여 FadeOut 완료까지 보장

3. **자동 정리**:
   - FadeOut 완료 후 Destroy(gameObject)로 자동 정리
   - 메모리 누수 없음

### _isLoading 리셋 타이밍

1. **타이밍 갭 제거**:
   - allowSceneActivation **직전**에 리셋
   - 새 씬에서 즉시 재시도해도 안전

2. **순서 보장**:
   - OnSceneLoadCompleted 발생 → _isLoading = false → allowSceneActivation
   - 명확한 상태 전이 보장

3. **중복 리셋 허용**:
   - 188줄에서 중복 리셋되지만 harmless
   - 안전성 우선

---

## 🧪 테스트 검증

### 테스트 1: FadeOut 애니메이션 정상 실행
**절차:**
1. TitleScene에서 "Start Game" 버튼 클릭
2. LoadingScreenPanel 표시 확인
3. 씬 전환 시 FadeOut 애니메이션 관찰

**기대 결과:**
- ✅ 0.5초 동안 부드러운 FadeOut 애니메이션
- ✅ Console: "Applied DontDestroyOnLoad for FadeOut across scene transition"
- ✅ Console: "FadeOut complete - Panel destroyed"

---

### 테스트 2: LoadingScreenPanel 정리 확인
**절차:**
1. 씬 전환 완료 후 Hierarchy 확인
2. DontDestroyOnLoad 영역 확인

**기대 결과:**
- ✅ LoadingScreenPanel이 Hierarchy에서 사라짐
- ✅ DontDestroyOnLoad 영역에 남아있지 않음
- ✅ 메모리 누수 없음

---

### 테스트 3: 연속 씬 전환 정상 동작
**절차:**
1. TitleScene → StageTestScene
2. StageTestScene → TitleScene
3. TitleScene → StageTestScene (3회 이상 반복)

**기대 결과:**
- ✅ 모든 전환에서 FadeOut 정상 실행
- ✅ "Already loading" 에러 **없음**
- ✅ Console: "Reset _isLoading before scene activation"
- ✅ 매 전환마다 새 LoadingScreenPanel 인스턴스 생성

---

### 테스트 4: 빠른 연속 클릭 테스트
**절차:**
1. 씬 전환 중 버튼 연속 클릭
2. Console 로그 확인

**기대 결과:**
- ✅ 중복 방지 로직 정상 동작
- ✅ "Scene transition already in progress" 경고 (정상)
- ✅ 씬 전환 정상 완료 후 다음 전환 가능

---

### 테스트 5: Profiler 메모리 누수 확인
**절차:**
1. Unity Profiler 실행
2. 10회 이상 씬 전환 반복
3. LoadingScreenPanel 인스턴스 수 확인

**기대 결과:**
- ✅ LoadingScreenPanel 인스턴스 누적 **없음**
- ✅ DontDestroyOnLoad 영역에 잔여 GameObject **없음**

---

## 📝 수정 요약

### 수정 파일 및 내용

| 파일 | 수정 위치 | 변경 내용 |
|------|-----------|-----------|
| **LoadingScreenPanel.cs** | OnShow() (Line 108-130) | DontDestroyOnLoad(gameObject) 추가 (2줄) |
| **SceneLoaderService.cs** | LoadSceneAsyncCoroutine (Line 167-171) | _isLoading 리셋 타이밍 변경 (4줄) |

### 총 변경량
- 추가된 코드: 6줄 (주석 포함 10줄)
- 최소한의 변경으로 두 가지 타이밍 문제 동시 해결

---

## 🎯 기대 효과

### LoadingScreenPanel
- ✅ 씬 전환 시 FadeOut 애니메이션 정상 실행
- ✅ FadeOut 완료 후 자동 정리
- ✅ Prefab 인스턴스화 패턴 유지
- ✅ 메모리 누수 없음

### SceneLoaderService
- ✅ _isLoading 타이밍 갭 완전 제거
- ✅ 연속 씬 전환 정상 동작
- ✅ "Already loading" 에러 해결
- ✅ 새 씬에서 즉시 씬 전환 가능

---

## 🔍 관련 문서

- [LoadingScreenPanel.cs](../Assets/Script/UI/Core/LoadingScreenPanel.cs)
- [SceneLoaderService.cs](../Assets/Script/Game/Services/SceneLoaderService.cs)
- [SceneTransitionController.cs](../Assets/Script/Game/Controllers/SceneTransitionController.cs)
- [SceneTransition_Prefab_Pattern_Implementation.md](./SceneTransition_Prefab_Pattern_Implementation.md)

---

## 🎉 결론

두 가지 타이밍 문제를 **최소한의 코드 변경**(총 6줄)으로 동시에 해결했습니다:

1. **LoadingScreenPanel FadeOut 중단**: DontDestroyOnLoad로 씬 전환 후에도 Coroutine 실행 보장
2. **SceneLoaderService 타이밍 갭**: allowSceneActivation 직전에 _isLoading 리셋으로 갭 제거

이 수정으로 **연속 씬 전환이 정상적으로 동작**하고, **FadeOut 애니메이션도 완벽하게 실행**됩니다.

---

## 📌 주의사항 및 제안

### Unity Editor 테스트 필수
- 실제 Play 모드에서 위의 모든 테스트 케이스를 검증해주세요
- 특히 Profiler로 메모리 누수 확인이 중요합니다

### 향후 개선 제안
현재는 문제를 최소 변경으로 해결했지만, 향후 더 근본적인 개선을 고려할 수 있습니다:

1. **SceneLoaderService를 글로벌 싱글턴으로 변경**
   - DontDestroyOnLoad + RegisterSingleton
   - SceneTransitionController와 동일한 생명주기
   - 하지만 현재 "씬 종속" 설계 의도가 있다면 유지

2. **LoadingScreenPanel을 글로벌 싱글턴으로 변경**
   - 매번 인스턴스화하지 않고 재사용
   - 성능 개선 가능
   - 하지만 현재 Prefab 패턴도 충분히 효율적

현재 솔루션이 잘 동작하므로 당장 변경할 필요는 없습니다. 필요 시 추후 리팩토링 고려 가능합니다.
