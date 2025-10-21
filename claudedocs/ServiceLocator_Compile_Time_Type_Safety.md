# ServiceLocator 컴파일 타임 타입 안전성 가이드

## 📋 개요

ServiceLocator의 `RegisterSingleton` 메서드가 **컴파일 타임 타입 검증** 방식으로 개선되어, 인터페이스 기반 싱글톤 등록을 지원하면서도 타입 안전성을 보장합니다.

---

## 🎯 핵심 개선 사항

### Before (문제점)
```csharp
// ❌ 기존 방식: 구체 타입만 등록 가능
public static void RegisterSingleton<T>(T instance) where T : MonoBehaviour

// 사용 시도
ServiceLocator.RegisterSingleton<ISceneTransitionController>(this);
// ❌ 컴파일 에러: ISceneTransitionController는 MonoBehaviour가 아님
```

**문제:**
- 인터페이스로 등록 불가능
- 의존성 역전 원칙(DIP) 위배
- 소비자가 구체 타입에 의존 (`Get<SceneTransitionController>()`)

### After (해결)
```csharp
// ✅ 새로운 방식: 인터페이스 기반 등록 지원
public static void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
    where TImplementation : MonoBehaviour, TInterface
    where TInterface : class

// 사용
ServiceLocator.RegisterSingleton<ISceneTransitionController>(this);
// ✅ 컴파일 성공 + 타입 안전성 보장
```

**해결:**
- ✅ 인터페이스로 등록 가능
- ✅ 컴파일 타임에 타입 관계 검증
- ✅ 소비자가 추상에 의존 (`Get<ISceneTransitionController>()`)

---

## 🔍 컴파일 타임 검증 vs 런타임 체크 비교

### 런타임 체크 방식 (사용 안 함)
```csharp
public static void RegisterSingleton<TInterface>(MonoBehaviour instance)
    where TInterface : class
{
    // ⚠️ 런타임에 타입 검증
    if (!(instance is TInterface))
        throw new ArgumentException(...);
}

// 사용
var audioService = GetComponent<AudioService>(); // ISceneTransitionController 구현 안 함
ServiceLocator.RegisterSingleton<ISceneTransitionController>(audioService);
// ✅ 컴파일 성공
// 💥 런타임 예외: ArgumentException
```

**단점:**
- 컴파일은 성공하지만 실행 시 예외 발생
- 에러 발견 시점이 늦음 (테스트 또는 프로덕션)
- 타입 안전성을 컴파일러가 보장하지 못함

### 컴파일 타임 검증 방식 (현재 구현)
```csharp
public static void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
    where TImplementation : MonoBehaviour, TInterface
    where TInterface : class
{
    // ✅ 컴파일러가 이미 타입 검증 완료 - 런타임 체크 불필요
}

// 사용
var audioService = GetComponent<AudioService>();
ServiceLocator.RegisterSingleton<ISceneTransitionController, AudioService>(audioService);
// ❌ 컴파일 에러: CS0311 - AudioService는 ISceneTransitionController를 구현하지 않음
```

**장점:**
- 코드 작성 시점에 에러 발견 (IDE 경고)
- 타입 안전성을 컴파일러가 보장
- 런타임 예외 발생 불가능

---

## 📚 사용 방법

### 1. 인터페이스 기반 등록 (권장)

```csharp
// 인터페이스 정의
public interface ISceneTransitionController
{
    void LoadSceneWithLoading(SceneData sceneData);
    bool IsTransitioning { get; }
}

// 구현 클래스
public class SceneTransitionController : MonoBehaviour, ISceneTransitionController
{
    private void Awake()
    {
        // ✅ 인터페이스로 등록 (타입 추론 활용)
        ServiceLocator.RegisterSingleton<ISceneTransitionController>(this);

        // 또는 명시적 타입 지정
        // ServiceLocator.RegisterSingleton<ISceneTransitionController, SceneTransitionController>(this);
    }
}

// 소비자 코드
public class MainMenuPanel : MonoBehaviour
{
    private void Start()
    {
        // ✅ 인터페이스로 조회 (추상에 의존)
        var controller = ServiceLocator.Get<ISceneTransitionController>();
        controller.LoadSceneWithLoading(sceneData);
    }
}
```

### 2. 구체 타입 등록 (하위 호환성)

```csharp
// 인터페이스가 없는 경우
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // ✅ 구체 타입으로 등록 (기존 코드와 동일)
        ServiceLocator.RegisterSingleton<GameManager>(this);
    }
}

// 소비자 코드
public class SomeSystem : MonoBehaviour
{
    private void Start()
    {
        var manager = ServiceLocator.Get<GameManager>();
    }
}
```

---

## 🎓 C# 제네릭 타입 추론 원리

### 컴파일러가 타입을 추론하는 과정

```csharp
// 메서드 시그니처
public static void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
    where TImplementation : MonoBehaviour, TInterface
    where TInterface : class
```

**사용 코드:**
```csharp
ServiceLocator.RegisterSingleton<ISceneTransitionController>(this);
```

**컴파일러 처리 과정:**

1. **명시적 타입 파라미터:** `TInterface = ISceneTransitionController`
2. **파라미터 타입 추론:** `this`의 타입 = `SceneTransitionController` → `TImplementation = SceneTransitionController`
3. **제약 검증:**
   - `SceneTransitionController : MonoBehaviour` ✅
   - `SceneTransitionController : ISceneTransitionController` ✅
4. **결과:** 컴파일 성공 + 타입 안전성 보장

**만약 타입이 맞지 않으면:**
```csharp
// AudioService는 ISceneTransitionController를 구현하지 않음
ServiceLocator.RegisterSingleton<ISceneTransitionController>(audioServiceInstance);

// 컴파일러 에러:
// CS0311: The type 'AudioService' cannot be used as type parameter 'TImplementation'
// There is no implicit reference conversion from 'AudioService' to 'ISceneTransitionController'
```

---

## ✅ 타입 안전성 보장 예시

### 올바른 사용 (컴파일 성공)

```csharp
// ✅ SceneTransitionController가 ISceneTransitionController를 구현함
ServiceLocator.RegisterSingleton<ISceneTransitionController>(sceneTransitionController);

// ✅ 명시적 타입 지정도 가능
ServiceLocator.RegisterSingleton<ISceneTransitionController, SceneTransitionController>(sceneTransitionController);

// ✅ 구체 타입 등록
ServiceLocator.RegisterSingleton<GameManager>(gameManager);
```

### 잘못된 사용 (컴파일 에러)

```csharp
// ❌ AudioService가 ISceneTransitionController를 구현하지 않음
ServiceLocator.RegisterSingleton<ISceneTransitionController>(audioService);
// Compiler Error: CS0311

// ❌ ISceneTransitionController는 MonoBehaviour가 아님 (기존 API 사용 시)
// ServiceLocator.RegisterSingleton<ISceneTransitionController, ISceneTransitionController>(interfaceInstance);
// Compiler Error: CS0311
```

---

## 🏗️ 아키텍처 장점

### 1. 의존성 역전 원칙 (DIP) 준수
```csharp
// 등록: 구현에 의존
ServiceLocator.RegisterSingleton<ISceneTransitionController>(concreteController);

// 조회: 추상에 의존 ✅
var controller = ServiceLocator.Get<ISceneTransitionController>();
```

### 2. 테스트 가능성 향상
```csharp
// 프로덕션 환경
ServiceLocator.RegisterSingleton<ISceneTransitionController>(realController);

// 테스트 환경
public class MockSceneTransitionController : MonoBehaviour, ISceneTransitionController
{
    // Mock 구현
}

ServiceLocator.RegisterSingleton<ISceneTransitionController>(mockController);
// ✅ 테스트 더블 주입 가능
```

### 3. 유연성 증가
```csharp
// 구현체 교체 가능 (소비자 코드 수정 불필요)
// V1
ServiceLocator.RegisterSingleton<ISceneTransitionController>(basicController);

// V2 (성능 개선 버전)
ServiceLocator.RegisterSingleton<ISceneTransitionController>(optimizedController);

// 소비자 코드는 변경 없음
var controller = ServiceLocator.Get<ISceneTransitionController>();
```

---

## 🔄 마이그레이션 가이드

### 기존 코드 영향 없음 (하위 호환성)

**기존 코드:**
```csharp
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        ServiceLocator.RegisterSingleton<GameManager>(this);
    }
}
```

**마이그레이션 필요 없음:**
- 기존 `RegisterSingleton<T>(T instance)` 메서드는 여전히 작동
- 내부적으로 새로운 `RegisterSingleton<T, T>(instance)` 호출
- 컴파일 에러 없음 ✅

### 새로운 코드 권장 패턴

```csharp
// 인터페이스 정의
public interface IMyService
{
    void DoSomething();
}

// 구현
public class MyService : MonoBehaviour, IMyService
{
    private void Awake()
    {
        // ✅ 인터페이스로 등록 (권장)
        ServiceLocator.RegisterSingleton<IMyService>(this);
    }

    public void DoSomething() { /* ... */ }
}

// 사용
public class Consumer : MonoBehaviour
{
    private void Start()
    {
        // ✅ 인터페이스로 조회 (추상에 의존)
        var service = ServiceLocator.Get<IMyService>();
    }
}
```

---

## 🧪 테스트 시나리오

### 1. 정상 등록 및 조회
```csharp
[Test]
public void RegisterSingleton_WithInterface_ShouldRegisterAndRetrieve()
{
    // Arrange
    var controller = CreateController<SceneTransitionController>();

    // Act
    ServiceLocator.RegisterSingleton<ISceneTransitionController>(controller);
    var retrieved = ServiceLocator.Get<ISceneTransitionController>();

    // Assert
    Assert.IsNotNull(retrieved);
    Assert.AreSame(controller, retrieved);
}
```

### 2. 타입 안전성 검증 (컴파일 에러)
```csharp
// ❌ 이 코드는 컴파일되지 않음 (의도적으로 에러 발생)
// ServiceLocator.RegisterSingleton<ISceneTransitionController>(wrongTypeInstance);
// Compiler Error: CS0311
```

### 3. 생명주기 관리
```csharp
[Test]
public void RegisterSingleton_WhenDestroyed_ShouldAutoUnregister()
{
    // Arrange
    var controller = CreateController<SceneTransitionController>();
    ServiceLocator.RegisterSingleton<ISceneTransitionController>(controller);

    // Act
    Destroy(controller.gameObject);
    yield return null; // Wait for OnDestroy

    // Assert
    var retrieved = ServiceLocator.Get<ISceneTransitionController>();
    Assert.IsNull(retrieved);
}
```

---

## 📌 주요 포인트 요약

| 항목 | 내용 |
|------|------|
| **타입 검증 시점** | 컴파일 타임 (코드 작성 시) |
| **런타임 체크** | 불필요 (컴파일러가 검증 완료) |
| **타입 안전성** | 제네릭 제약으로 보장 (`where TImplementation : TInterface`) |
| **하위 호환성** | 기존 코드 영향 없음 |
| **사용 편의성** | 타입 추론 지원 (`RegisterSingleton<IInterface>(this)`) |
| **DIP 준수** | 인터페이스 등록/조회 가능 |
| **테스트 가능성** | Mock 객체 주입 용이 |

---

## 🔗 관련 파일

- **ServiceLocator.cs** - `RegisterSingleton` 메서드 구현
- **SceneTransitionController.cs** - 인터페이스 등록 예시
- **ISceneTransitionController.cs** - 서비스 인터페이스 정의
- **MainMenuPanel.cs** - 인터페이스 조회 예시

---

## 📖 참고 자료

- [C# Generic Constraints](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters)
- [Dependency Inversion Principle (DIP)](https://en.wikipedia.org/wiki/Dependency_inversion_principle)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)

---

**작성일:** 2025-10-21
**작성자:** Claude (SuperClaude Framework)
**버전:** 1.0
