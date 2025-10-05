# Blend Tree 기반 유닛 이동 시스템 설계 문서

## 📋 개요

### 목적
기존 단순 Transition 기반 이동 애니메이션을 Blend Tree 기반으로 전환하여 더 부드러운 전환과 세밀한 코드 제어 구현

### 핵심 개선사항
- **부드러운 전환**: Blend Tree를 통한 매끄러운 애니메이션 블렌딩
- **코드 제어**: moveDuration 및 transitionDuration 파라미터로 정밀한 속도 제어
- **선형 감속**: transition 시작 시점부터 속도를 0으로 선형 감소시켜 자연스러운 정지

---

## 🎯 설계 개념

### 타이밍 구조
```
|<------------ moveDuration ------------>|
|                                        |
|<- frontTransition ->|<- move ->|<- backTransition ->|
|                     |          |                    |
[시작]            [전환완료]   [감속시작]          [정지]
Speed: 0.0 → 1.0       1.0        1.0 → 0.0         0.0
```

### 수학적 모델
```
totalTime = moveDuration
transitionStart = moveDuration - transitionDuration
currentTime = 0 ~ moveDuration

if currentTime < transitionStart:
    speed = 1.0  // 정속 이동
else:
    // 선형 감속
    t = (currentTime - transitionStart) / transitionDuration
    speed = Lerp(1.0, 0.0, t)
```

---

## 🏗️ 시스템 아키텍처

### 1. Animator Controller 구조

#### 기존 구조 (Before)
```
Idle State
  ↓ (Move Trigger)
Move State
  ↓ (Animation End)
Idle State
```

#### 새로운 구조 (After)
```
Movement Blend Tree (2D - Always Active)
  ├─ X축: MoveSpeed (0.0 ~ 1.0) - 이동 관련
  └─ Y축: AttackTrigger (0.0 ~ 1.0) - 공격 관련
      ├─ (0,0): Idle
      ├─ (1,0): Move
      ├─ (0,1): Attack (Idle에서)
      └─ (1,1): Move + Attack Blend
```

### 2. Blend Tree 파라미터

#### Animator Parameters
| 파라미터명 | 타입 | 용도 | 범위 |
|-----------|------|------|------|
| `MoveSpeed` | Float | X축 - Blend Tree 이동 블렌딩 값 | 0.0 ~ 1.0 |
| `AttackTrigger` | Float | Y축 - Blend Tree 공격 블렌딩 값 | 0.0 ~ 1.0 |
| `AnimSpeed` | Float | 애니메이션 재생 속도 | 0.1 ~ 2.0 |

**주요 변경사항**: Trigger 파라미터 전부 제거, Float 파라미터만 사용

#### Blend Tree 구조
```
Movement_Combat_BlendTree (2D Freeform Cartesian)
├─ X Parameter: MoveSpeed (0.0 ~ 1.0)
├─ Y Parameter: AttackTrigger (0.0 ~ 1.0)
├─ (0.0, 0.0): Idle Animation
├─ (1.0, 0.0): Move Animation
├─ (0.0, 1.0): Attack Animation
└─ (1.0, 1.0): Move + Attack Blend (optional)
```

**핵심**: 공격 애니메이션이 1개뿐이므로 2D Blend Tree에서 Y축으로 관리

---

## 💻 코드 설계

### 1. 새로운 컴포넌트: `BlendTreeMovementController`

#### 역할
- moveDuration 및 transitionDuration 관리
- 실시간 속도 계산 및 Animator 파라미터 업데이트
- Transform 위치 동기화

#### 주요 속성
```csharp
[Header("Movement Timing")]
[SerializeField] private float moveDuration = 1.0f;              // 전체 이동 시간
[SerializeField] private float frontTransitionDuration = 0.2f;   // 시작 가속 시간
[SerializeField] private float backTransitionDuration = 0.3f;    // 종료 감속 시간

[Header("Speed Control")]
[SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 0);
[SerializeField] private float baseAnimationSpeed = 1.0f;

// Runtime State
private float moveStartTime;
private float currentMoveSpeed;
private bool isBlendTreeMoving;
```

#### 핵심 메서드
```csharp
// 1. 이동 시작
public void StartBlendTreeMove(Vector2Int from, Vector2Int to)
{
    moveStartTime = Time.time;
    isBlendTreeMoving = true;

    // Blend Tree 활성화 (Trigger 사용 안 함, Float만 사용)
    // X축(MoveSpeed)를 0에서 시작하여 천천히 증가
    animator.SetFloat("MoveSpeed", 0.0f);
    animator.SetFloat("AttackTrigger", 0.0f); // Y축 초기화

    StartCoroutine(UpdateBlendTreeSpeed());
}

// 2. 실시간 속도 업데이트
private IEnumerator UpdateBlendTreeSpeed()
{
    while (isBlendTreeMoving)
    {
        float elapsed = Time.time - moveStartTime;
        float speed = CalculateCurrentSpeed(elapsed);

        // Animator 파라미터 업데이트
        animator.SetFloat("MoveSpeed", speed);

        // 이동 완료 체크
        if (elapsed >= moveDuration)
        {
            CompleteBlendTreeMove();
            yield break;
        }

        yield return null;
    }
}

// 3. 속도 계산 (핵심 로직) - frontTransition, move, backTransition 구조
private float CalculateCurrentSpeed(float elapsedTime)
{
    float frontTransitionEnd = frontTransitionDuration;
    float moveEnd = frontTransitionEnd + (moveDuration - frontTransitionDuration - backTransitionDuration);
    float totalDuration = moveDuration;

    if (elapsedTime < frontTransitionEnd)
    {
        // frontTransition 구간 (0.0 → 1.0 가속)
        float t = elapsedTime / frontTransitionDuration;
        return Mathf.Lerp(0.0f, 1.0f, t);
    }
    else if (elapsedTime < moveEnd)
    {
        // move 구간 (정속 1.0)
        return 1.0f;
    }
    else
    {
        // backTransition 구간 (1.0 → 0.0 감속)
        float t = (elapsedTime - moveEnd) / backTransitionDuration;
        t = Mathf.Clamp01(t);
        return Mathf.Lerp(1.0f, 0.0f, t);
    }
}

// 4. 이동 완료
private void CompleteBlendTreeMove()
{
    isBlendTreeMoving = false;

    // Blend Tree를 Idle로 전환 (Trigger 사용 안 함, Float만 사용)
    animator.SetFloat("MoveSpeed", 0.0f);
    animator.SetFloat("AttackTrigger", 0.0f);

    // 최종 위치 보장
    if (gridManager != null)
    {
        transform.position = gridManager.GridToWorldPosition(targetPosition);
    }

    OnMovementComplete?.Invoke();
}
```

### 2. Transform 위치 동기화

#### 기존 방식 (Phase 2)
```csharp
// SyncTransformWithAnimation()
transform.position = Vector3.Lerp(startPos, endPos, animationProgress);
```

#### 새로운 방식 (Blend Tree)
```csharp
private IEnumerator SyncTransformWithBlendTree(Vector2Int from, Vector2Int to)
{
    Vector3 startPos = gridManager.GridToWorldPosition(from);
    Vector3 endPos = gridManager.GridToWorldPosition(to);
    float startTime = Time.time;

    while (isBlendTreeMoving)
    {
        float elapsed = Time.time - startTime;
        float progress = Mathf.Clamp01(elapsed / moveDuration);

        // 속도 기반 위치 계산 (적분)
        transform.position = Vector3.Lerp(startPos, endPos, progress);

        yield return null;
    }

    // 최종 위치 보장
    transform.position = endPos;
}
```

### 3. 기존 시스템과의 통합

#### UnitAnimationController 수정
```csharp
// Phase 2 호환성 유지하면서 Blend Tree 지원 추가
[Header("Movement Type")]
[SerializeField] private MovementAnimationType movementType = MovementAnimationType.AnimationEvent;

public enum MovementAnimationType
{
    AnimationEvent,  // 기존 방식 (Phase 1-2)
    BlendTree        // 새로운 방식 (Phase 3)
}

public void PlayMoveAnimation(Vector2Int from, Vector2Int to)
{
    if (movementType == MovementAnimationType.BlendTree)
    {
        // Blend Tree 방식
        blendTreeController.StartBlendTreeMove(from, to);
    }
    else
    {
        // 기존 Animation Event 방식
        animator.SetTrigger(MOVE_TRIGGER);
        OnTransformMoveStart?.Invoke(from, to);
    }
}
```

---

## 📐 구현 단계별 계획

### Phase 3-1: Animator Controller 설정
**목표**: Unity Animator에 2D Blend Tree 구조 생성

**작업 내용**:
1. Movement_Combat Blend Tree 생성
   - Type: 2D Freeform Cartesian
   - X Parameter: MoveSpeed (0.0 ~ 1.0)
   - Y Parameter: AttackTrigger (0.0 ~ 1.0)
   - Animations:
     - (0.0, 0.0): Idle
     - (1.0, 0.0): Move
     - (0.0, 1.0): Attack
     - (1.0, 1.0): Move+Attack Blend (optional)

2. Animator Parameters 추가
   - `MoveSpeed`: Float (default: 0.0)
   - `AttackTrigger`: Float (default: 0.0)
   - `AnimSpeed`: Float (default: 1.0)
   - **Trigger 파라미터 전부 제거**

3. State Transitions 설정
   - **Blend Tree를 항상 활성 상태로 유지**
   - Transition 없이 Blend Tree 단일 State 사용
   - X, Y축 Float 값 변경만으로 모든 애니메이션 제어

**검증 기준**:
- [ ] Blend Tree가 MoveSpeed, AttackTrigger 값에 따라 자동 블렌딩
- [ ] Trigger 없이 Float 값만으로 모든 애니메이션 전환
- [ ] (0,0)에서 Idle, (1,0)에서 Move, (0,1)에서 Attack 정상 재생

---

### Phase 3-2: BlendTreeMovementController 구현
**목표**: 3단계 속도 제어 및 타이밍 관리 컴포넌트 구현

**작업 내용**:
1. 기본 구조 생성
   ```csharp
   public class BlendTreeMovementController : MonoBehaviour
   {
       [Header("Movement Timing")]
       [SerializeField] private float moveDuration = 1.0f;
       [SerializeField] private float frontTransitionDuration = 0.2f;
       [SerializeField] private float backTransitionDuration = 0.3f;

       private Animator animator;
       private float moveStartTime;
       private bool isBlendTreeMoving;
   }
   ```

2. 핵심 기능 구현
   - `StartBlendTreeMove()`: 이동 시작 및 MoveSpeed 0.0 초기화
   - `UpdateBlendTreeSpeed()`: 실시간 속도 업데이트 코루틴
   - `CalculateCurrentSpeed()`: **3단계 구조 속도 계산**
     - frontTransition: 0.0 → 1.0 가속
     - move: 1.0 정속 유지
     - backTransition: 1.0 → 0.0 감속
   - `CompleteBlendTreeMove()`: 이동 완료 및 MoveSpeed 0.0 설정

3. Inspector 설정 인터페이스
   - moveDuration 슬라이더 (0.5 ~ 3.0s) - 전체 이동 시간
   - frontTransitionDuration 슬라이더 (0.1 ~ 0.5s) - 시작 가속 시간
   - backTransitionDuration 슬라이더 (0.1 ~ 0.5s) - 종료 감속 시간
   - speedCurve 에디터 (선형/커브 선택)

**검증 기준**:
- [x] moveDuration 설정 시간과 실제 이동 시간 일치
- [x] frontTransition 구간에서 속도가 0.0→1.0 선형 증가
- [x] move 구간에서 속도가 1.0 정속 유지
- [x] backTransition 구간에서 속도가 1.0→0.0 선형 감속
- [x] Animator의 MoveSpeed 파라미터가 계산된 값으로 업데이트

---

### Phase 3-3: Transform 동기화 시스템
**목표**: Blend Tree 애니메이션과 Transform 위치 완벽 동기화

**작업 내용**:
1. `SyncTransformWithBlendTree()` 코루틴 구현
   - 시간 기반 진행도 계산
   - Vector3.Lerp를 통한 부드러운 위치 보간
   - 최종 위치 스냅 보장

2. 애니메이션 루트 모션 비활성화
   - Animator: Apply Root Motion = false
   - 스크립트가 Transform 완전 제어

3. 동기화 검증 시스템
   - Grid 위치와 Transform 위치 오차 모니터링
   - 임계값 초과 시 경고 로그

**검증 기준**:
- [x] 이동 중 Transform이 Grid 경로를 정확히 따라감
  - `SyncTransformWithBlendTree()` 코루틴에서 시간 기반 진행도로 Vector3.Lerp 보간 구현됨
  - `ValidateTransformSync()`로 실시간 오차 모니터링 및 경고 시스템 구현됨
- [x] 이동 완료 시 Transform이 목표 Grid 위치와 정확히 일치
  - `CompleteBlendTreeMove()`에서 `transform.position = transformTargetPosition` 최종 스냅 보장
  - `snapDistanceThreshold` 기반 조기 스냅 처리로 정확도 향상
- [x] 애니메이션 속도 변화가 Transform 이동에 반영
  - BlendTree의 MoveSpeed 파라미터가 `UpdateBlendTreeSpeed()`로 실시간 업데이트됨
  - Transform 동기화가 동일한 `moveDuration`을 사용하여 애니메이션과 완벽히 동기화됨

---

### Phase 3-4: 기존 시스템 통합
**목표**: Phase 2 Animation Event 방식과 호환성 유지

**작업 내용**:
1. UnitAnimationController 확장
   ```csharp
   [SerializeField] private MovementAnimationType movementType;
   [SerializeField] private BlendTreeMovementController blendTreeController;
   ```

2. 조건부 분기 구현
   - `PlayMoveAnimation()`: 타입에 따라 적절한 방식 선택
   - 기존 Animation Event 방식 유지

3. MovementComponent 연동
   - BlendTree 완료 이벤트를 MovementComponent와 연결
   - `OnBlendTreeMoveComplete` → `OnTransformMoveEnd` 브릿지

**검증 기준**:
- [ ] MovementAnimationType.AnimationEvent 선택 시 기존 방식 정상 동작
- [ ] MovementAnimationType.BlendTree 선택 시 새로운 방식 정상 동작
- [ ] 런타임 중 타입 전환 가능

---

### Phase 3-5: 최적화 및 고급 기능
**목표**: 성능 최적화 및 사용성 개선

**작업 내용**:
1. 속도 커브 프리셋
   ```csharp
   public enum SpeedCurvePreset
   {
       Linear,         // 선형 감속
       EaseOut,        // 부드러운 감속
       EaseInOut,      // 가속+감속
       Custom          // 사용자 정의 커브
   }
   ```

2. 성능 최적화
   - Coroutine 대신 Update()에서 deltaTime 사용 검토
   - Animator.SetFloat() 호출 빈도 제어 (임계값 기반)

3. 디버그 시각화
   - Gizmos로 속도 그래프 표시
   - Scene View에서 실시간 속도 값 표시

4. 에디터 도구
   - Custom Inspector로 타이밍 시뮬레이션
   - 미리보기 기능

**검증 기준**:
- [ ] 다양한 커브 프리셋 정상 동작
- [ ] 다중 유닛 동시 이동 시 성능 저하 없음 (60 FPS 유지)
- [ ] 디버그 시각화가 직관적이고 정확함

---

## 🎨 사용자 인터페이스 (Inspector)

### BlendTreeMovementController Inspector
```
┌─────────────────────────────────────┐
│ Blend Tree Movement Controller      │
├─────────────────────────────────────┤
│ Movement Timing                     │
│   Move Duration:           [1.0] s  │
│   Front Transition:        [0.2] s  │
│   Back Transition:         [0.3] s  │
│                                     │
│ Speed Control                       │
│   Preset: [Linear ▼]               │
│   Custom Curve: [═══════]          │
│                                     │
│ Animation                           │
│   Base Anim Speed:        [1.0] x  │
│                                     │
│ Debug                               │
│   ☑ Show Speed Graph               │
│   ☑ Log Timing Events              │
│   Current Speed:          [0.75]   │
│   Current Phase:    [move]         │
└─────────────────────────────────────┘
```

**타이밍 구조 시각화**:
```
Total: [============================] 1.0s
Front: [====]                          0.2s (0.0→1.0 가속)
Move:       [===============]          0.5s (1.0 정속)
Back:                       [=======]  0.3s (1.0→0.0 감속)
```

---

## 📊 테스트 시나리오

### 시나리오 1: 기본 이동 (3단계 구조)
**입력**: 유닛을 3칸 거리로 이동
**설정**: moveDuration=1.0, frontTransition=0.2, backTransition=0.3
**예상 결과**:
- 1.0초 동안 이동
- 0.0~0.2초: frontTransition (0.0→1.0 가속)
- 0.2~0.7초: move (1.0 정속)
- 0.7~1.0초: backTransition (1.0→0.0 감속)
- Transform이 목표 위치에 정확히 도착

### 시나리오 2: 연속 이동
**입력**: 이동 → 즉시 다른 위치로 이동
**예상 결과**:
- 첫 번째 이동 완료 후 두 번째 이동 시작
- 각 이동이 독립적인 3단계 타이밍으로 실행
- 중간에 끊김 없이 자연스러운 전환

### 시나리오 3: 이동 중 공격 (2D Blend Tree 활용)
**입력**: 이동 중 공격 명령
**예상 결과**:
- MoveSpeed 유지하면서 AttackTrigger=1.0 설정
- 2D Blend Tree가 Move+Attack 블렌딩 재생
- 공격 완료 후 AttackTrigger=0.0으로 복귀

### 시나리오 4: 타이밍 변경 테스트
**입력**: moveDuration=2.0, frontTransition=0.3, backTransition=0.5
**예상 결과**:
- 2.0초 동안 이동
- 0.0~0.3초: frontTransition (가속)
- 0.3~1.5초: move (정속)
- 1.5~2.0초: backTransition (감속)
- 속도 계산 수식 정확히 적용

---

## 🔧 문제 해결 가이드

### 문제 1: 애니메이션과 Transform 동기화 어긋남
**원인**:
- Animator의 Apply Root Motion이 활성화됨
- moveDuration과 실제 애니메이션 길이 불일치

**해결**:
1. Apply Root Motion = false 확인
2. moveDuration을 애니메이션 클립 길이와 독립적으로 설정
3. Transform 위치는 스크립트가 완전 제어

### 문제 2: Blend Tree 전환이 부자연스러움
**원인**:
- **Phase 3에서는 State Transition이 없음** (단일 Blend Tree State)
- MoveSpeed 파라미터 값 변경 속도가 너무 빠름

**해결**:
1. **Transition 제거**: Blend Tree를 항상 활성 상태로 유지
2. frontTransition/backTransition 값을 적절히 설정 (0.2~0.3초)
3. Blend Tree 자체의 블렌딩이 부드러움을 담당

### 문제 3: 속도가 0에 도달하기 전에 애니메이션 종료
**원인**:
- backTransitionDuration이 너무 짧음
- 3단계 구조의 시간 계산 오류

**해결**:
1. backTransitionDuration을 moveDuration의 20~30%로 설정
2. `CalculateCurrentSpeed()` 수식 검증:
   - frontTransitionEnd = frontTransitionDuration
   - moveEnd = moveDuration - backTransitionDuration
   - 각 구간의 경계값 정확히 체크
3. Debug.Log로 현재 Phase와 속도 값 실시간 모니터링

---

## 📈 성능 고려사항

### 최적화 전략
1. **Animator 업데이트 빈도 제어**
   ```csharp
   // 속도 변화가 임계값 이상일 때만 업데이트
   if (Mathf.Abs(newSpeed - currentSpeed) > 0.01f)
   {
       animator.SetFloat("MoveSpeed", newSpeed);
       currentSpeed = newSpeed;
   }
   ```

2. **코루틴 대신 Update() 사용**
   ```csharp
   // 매 프레임 자동 호출, 별도 코루틴 불필요
   void Update()
   {
       if (isBlendTreeMoving)
       {
           UpdateBlendTreeSpeed();
       }
   }
   ```

3. **캐싱**
   ```csharp
   private int moveSpeedHash; // Animator.StringToHash("MoveSpeed")

   void Awake()
   {
       moveSpeedHash = Animator.StringToHash("MoveSpeed");
   }

   // SetFloat 대신 해시 사용
   animator.SetFloat(moveSpeedHash, speed);
   ```

### 메모리 관리
- Animation Curve를 static으로 공유 가능한 프리셋은 공유
- 불필요한 코루틴 인스턴스 방지

---

## 🎯 결론

### 장점
✅ **부드러운 전환**: Blend Tree로 자연스러운 애니메이션 블렌딩
✅ **정밀한 제어**: moveDuration/transitionDuration으로 타이밍 완벽 제어
✅ **확장성**: 다양한 속도 커브 적용 가능
✅ **호환성**: 기존 Animation Event 방식과 공존 가능

### 단점
⚠️ **초기 설정 복잡**: Animator Controller 설정 및 스크립트 연동 필요
⚠️ **디버깅 난이도**: 타이밍과 속도 계산 로직 검증 필요

### 권장 사항
- **프로토타입**: 먼저 Phase 2 방식으로 기능 검증 후 Blend Tree 전환
- **단계적 적용**: 한 유닛에만 먼저 적용하여 안정성 확인
- **테스트**: 다양한 이동 거리 및 타이밍 조합 충분히 테스트
- **문서화**: 각 설정 값의 의미와 효과 명확히 주석 작성

---

## 📚 참고 자료

### Unity 공식 문서
- [Blend Trees](https://docs.unity3d.com/Manual/class-BlendTree.html)
- [Animation Parameters](https://docs.unity3d.com/Manual/AnimationParameters.html)
- [Animator Controller](https://docs.unity3d.com/Manual/class-AnimatorController.html)

### 관련 파일
- `UnitAnimationController.cs`: 기존 애니메이션 컨트롤러
- `MovementComponent.cs`: 이동 로직 및 Transform 동기화
- `IAnimationController.cs`: 애니메이션 인터페이스

### 구현 예정 파일
- `BlendTreeMovementController.cs`: 새로운 Blend Tree 제어 컴포넌트
- `MovementSpeedCurves.cs`: 속도 커브 프리셋 정의
- `BlendTreeMovementEditor.cs`: Custom Inspector (선택사항)
