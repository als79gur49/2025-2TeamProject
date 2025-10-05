# Animation Event 기반 공격 시스템 설정 가이드

## 개요

공격 애니메이션을 **BlendTree**에서 **Hybrid 방식**(Animation Event + StateMachineBehaviour)으로 변경했습니다.

### 변경 이유
- BlendTree는 Progress가 0부터 시작하지 않아 공격과 같은 정확한 타이밍이 필요한 애니메이션과 맞지 않음
- Animation Event는 애니메이션 제작자가 의도한 정확한 프레임에서 이벤트 발생 보장
- **AttackEnd는 StateMachineBehaviour로 신뢰성 향상** (Unity Animation Event 마지막 프레임 버그 회피)
- 애니메이션 속도 변경 시 자동으로 타이밍 조정
- 외부 개입 없이 애니메이션 시스템이 자동으로 처리

### 시스템 구조

```
┌─────────────────────────────────────────┐
│   UnitAnimationController               │
│                                         │
│  - PlayAttackAnimation()                │
│  - AttackHit() ← Animation Event 호출   │
│  - AttackEnd() ← StateMachineBehaviour  │
└─────────────────────────────────────────┘
           ↑                    ↑
           │                    │
  ┌────────┴─────────┐  ┌──────┴──────────┐
  │ Animation Event  │  │ AttackStateBehaviour │
  │  (60% 프레임)     │  │  OnStateExit()  │
  └──────────────────┘  └─────────────────┘
```

## 코드 변경 사항

### UnitAnimationController.cs

#### 1. 새로운 Animation Parameter 추가
```csharp
private const string ATTACK_TRIGGER = "Attack";
```

#### 2. PlayAttackAnimation() 메서드 변경
**이전 (BlendTree 방식):**
```csharp
blendTreeController.StartBlendTreeAttack(target);
StartCoroutine(MonitorBlendTreeAttack(target));
```

**현재 (Animation Event 방식):**
```csharp
animator.SetTrigger(ATTACK_TRIGGER);
// 타격과 종료는 Animation Event에서 자동 호출
```

#### 3. Animation Event 메서드 추가

**AttackHit()** - 공격 타격 순간
```csharp
public void AttackHit()
{
    OnAttackHit?.Invoke(currentTarget);
    // VFX/SFX 처리
}
```

**AttackEnd()** - 공격 종료 (StateMachineBehaviour에서 호출)
```csharp
public void AttackEnd()
{
    OnAttackEnd?.Invoke(currentTarget);
    // 상태 초기화
}
```

**주의**: AttackEnd는 Animation Event가 아닌 AttackStateBehaviour에서 호출됨

## Unity Editor 설정 가이드

### 1. Animator Controller 설정

#### 1.1 Parameter 추가
1. Animator Controller 열기
2. Parameters 탭에서 새로운 Parameter 추가:
   - **Name**: `Attack`
   - **Type**: `Trigger`

#### 1.2 Attack State 생성
1. Animator 창에서 새로운 State 생성
2. State 이름: `Attack` (또는 `Attack_Front`, `Attack_Back` 등 방향별)
3. 공격 애니메이션 클립 할당

#### 1.3 Transition 설정

**Idle → Attack:**
- Conditions: `Attack` (Trigger)
- Has Exit Time: ❌ False
- Transition Duration: 0.1 ~ 0.15초
- Interruption Source: None

**Attack → Idle:**
- Conditions: 없음 (자동 복귀)
- Has Exit Time: ✅ True
- Exit Time: 0.95 ~ 1.0
- Transition Duration: 0.1 ~ 0.15초

#### 1.4 Attack State Tag 설정 (필수)
1. Attack State 선택
2. Inspector에서 Tag 추가: `Attack`
3. 진행도 추적 및 AttackStateBehaviour 식별에 사용됨

#### 1.5 AttackStateBehaviour 추가 ⭐ 중요

**방법 1: Editor 도구 사용 (권장)**
1. Unity 메뉴: `Tools > Animation > Setup Attack State Behaviours`
2. Animator Controller 선택
3. Attack State 자동 검색 및 Behaviour 추가

**방법 2: 수동 추가**
1. Attack State 선택
2. Inspector에서 "Add Behaviour" 클릭
3. `AttackStateBehaviour` 선택

**콤보 공격 주의사항:**
- 중간 콤보 State: AttackStateBehaviour 제거 ❌
- 마지막 콤보 State: AttackStateBehaviour 추가 ✅

### 2. Animation Event 설정

#### 2.1 Animation 창 열기
1. 공격 애니메이션 클립 선택
2. Window → Animation → Animation

#### 2.2 AttackHit 이벤트 추가

**타격 프레임 찾기:**
- 애니메이션을 재생하며 실제 타격이 발생해야 하는 프레임 확인
- 일반적으로 애니메이션 **50~70%** 지점 (예: 0.3초 / 0.5초 애니메이션)

**이벤트 추가:**
1. 타격 프레임 위치에 이벤트 마커 클릭 (타임라인 상단)
2. Inspector에서 설정:
   - **Function**: `AttackHit`
   - Parameters: 없음

#### 2.3 AttackEnd 이벤트 제거 ⚠️

**중요**: AttackEnd는 더 이상 Animation Event로 추가하지 않습니다!

- **이유**: Unity Animation Event는 마지막 프레임에서 신뢰성 문제 (Transition과 충돌)
- **대신 사용**: AttackStateBehaviour.OnStateExit에서 자동 호출
- **기존 이벤트 제거**: 이전에 추가한 AttackEnd Event가 있다면 제거하세요

**검증 도구:**
- Unity 메뉴: `Tools > Animation > Validate Animation Events`
- AttackEnd Event가 남아있는지 자동 검증

### 3. 예시 타임라인

```
공격 애니메이션 (0.5초 기준)

0.0초           0.3초              0.475초(Exit Time)  0.5초
|===============|==================|===================|
시작            [AttackHit]       [OnStateExit]      종료
                Animation Event   AttackStateBehaviour
                (타격 순간)        (AttackEnd 호출)

Progress:       60%               95%                100%
```

**주요 변경점:**
- AttackEnd는 Animation Event(0.5초)가 아닌 OnStateExit(0.475초)에서 호출
- Exit Time 도달 시 확실하게 호출되어 신뢰성 보장

## 다양한 공격 애니메이션 처리

### 방향별 공격 (4방향)

#### Animator Setup
```
Parameters:
- Attack (Trigger)
- DirectionX (Float)
- DirectionY (Float)

States:
- Attack_Front (DirectionY > 0.5)
- Attack_Back (DirectionY < -0.5)
- Attack_Right (DirectionX > 0.5)
- Attack_Left (DirectionX < -0.5)
```

각 State의 애니메이션 클립마다 `AttackHit` 이벤트 추가 (AttackEnd는 StateMachineBehaviour 사용)

### 다단 공격 (콤보)

**3단 공격 예시:**
```
Attack_Combo1 (0.4초):
  - Animation Event: 0.2초 AttackHit
  - AttackStateBehaviour: 제거 ❌ (중간 콤보)

Attack_Combo2 (0.5초):
  - Animation Event: 0.3초 AttackHit
  - AttackStateBehaviour: 제거 ❌ (중간 콤보)

Attack_Combo3 (0.7초):
  - Animation Event: 0.4초 AttackHit
  - AttackStateBehaviour: 추가 ✅ (마지막 콤보만)
```

**중요**: 마지막 콤보에서만 AttackStateBehaviour 추가하여 최종 상태 초기화

### 다중 타격 공격

하나의 애니메이션에서 여러 번 타격하는 경우:

```
Attack_Spin (1.0초):
  - Animation Event:
    * 0.3초: AttackHit (첫 번째 타격)
    * 0.6초: AttackHit (두 번째 타격)
    * 0.9초: AttackHit (세 번째 타격)
  - AttackStateBehaviour: 추가 ✅ (OnStateExit에서 AttackEnd)
```

`AttackHit`를 여러 프레임에 추가 가능, AttackEnd는 StateMachineBehaviour로 자동 처리

## 검증 및 테스트

### 1. Animation Event 동작 확인

**Debug 로그 활성화:**
```csharp
[SerializeField] private bool logAnimationEvents = true;
```

**예상 로그 출력:**
```
[UnitAnimationController] Unit_001: Attack animation started on Enemy_001
[UnitAnimationController] Unit_001: Attack hit on Enemy_001
[UnitAnimationController] Unit_001: Attack animation ended on Enemy_001
```

### 2. 타이밍 검증

**Scene View에서 확인:**
1. Play Mode 실행
2. 공격 애니메이션 재생
3. Animation 창에서 현재 프레임 확인
4. 타격 프레임과 실제 타격 시점 일치 여부 확인

**Animator 창 활용:**
1. Animator 창에서 Attack State 재생
2. Event Marker가 정확한 위치에 있는지 확인

### 3. 네트워크 동기화 테스트

**서버/클라이언트 환경:**
- 서버: Animation Event 발생 → OnAttackHit → 데미지 적용
- 클라이언트: Animation Event 발생 → OnAttackHit → VFX/SFX만 재생
- 타이밍이 일치하는지 확인

## 트러블슈팅

### 문제: AttackHit()가 호출되지 않음

**확인 사항:**
1. Animation Event Function 이름이 정확한지 확인 (`AttackHit`)
2. UnitAnimationController 컴포넌트가 Animator와 같은 GameObject에 있는지 확인
3. Animation Event가 올바른 프레임에 설정되었는지 확인
4. Animator가 활성화 상태인지 확인 (`animator.enabled = true`)

### 문제: 타격 타이밍이 부정확함

**해결 방법:**
1. Animation Event 프레임 위치 조정
2. Transition Duration 확인 (너무 길면 애니메이션 시작이 지연됨)
3. Animation Speed Multiplier 확인 (`animationSpeedMultiplier`)

### 문제: currentTarget이 null

**원인:**
- `AttackHit()` 호출 시점에 `currentTarget`이 이미 초기화됨
- 공격 애니메이션이 시작되지 않은 상태에서 이벤트 발생

**해결:**
1. `PlayAttackAnimation()` 호출 확인
2. `AttackEnd()` 이벤트가 `AttackHit()` 이전에 호출되는지 확인
3. 애니메이션 길이와 이벤트 타이밍 재검토

### 문제: 애니메이션이 중단됨

**확인 사항:**
1. Attack State의 `Can Transition To Self` 설정 확인
2. 다른 Transition이 공격을 중단시키는지 확인
3. `StopCurrentAnimation()` 호출 여부 확인

## 성능 고려사항

### Animation Event vs Time-based 비교

| 기준 | Animation Event | Time-based |
|------|-----------------|------------|
| CPU 오버헤드 | 낮음 (이벤트 기반) | 중간 (매 프레임 체크) |
| 메모리 사용 | 낮음 | 동일 |
| 정확성 | 매우 높음 | 높음 (프레임 드랍 시 누락 가능) |
| 유지보수 | 쉬움 (Unity Editor) | 중간 (코드 수정 필요) |

### 최적화 팁

1. **Animation Event 최소화**: 필요한 이벤트만 추가
2. **로그 비활성화**: 프로덕션에서 `logAnimationEvents = false`
3. **VFX/SFX 풀링**: GameSettings 통해 효과 On/Off 제어

## 추가 참고 자료

- Unity Animation Events 공식 문서: https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html
- Animator Controller 가이드: https://docs.unity3d.com/Manual/class-AnimatorController.html
- BlendTree vs State Machine: https://docs.unity3d.com/Manual/BlendTree.html

## 마이그레이션 체크리스트

### 코드 작업
- [x] `AttackStateBehaviour.cs` 생성
- [x] `UnitAnimationController.cs` 주석 업데이트
- [x] `AnimatorSetupHelper.cs` Editor 도구 생성

### Unity Editor 설정
- [ ] Animator Controller에 `Attack` Trigger Parameter 추가
- [ ] Attack State 생성 및 공격 애니메이션 클립 할당
- [ ] Idle ↔ Attack Transition 설정
- [ ] Attack State에 `Attack` Tag 추가 (필수)
- [ ] **AttackStateBehaviour 추가** (Tools > Animation > Setup Attack State Behaviours)
  - [ ] 일반 공격 State에 추가
  - [ ] 콤보 공격 마지막 State에만 추가
- [ ] 공격 애니메이션 클립에 `AttackHit` Event 추가 (60% 지점)
- [ ] **기존 `AttackEnd` Event 제거** (있는 경우)
- [ ] Animation Event 검증 (Tools > Animation > Validate Animation Events)

### 테스트
- [ ] Play Mode에서 타이밍 검증
- [ ] Debug 로그로 AttackEnd 호출 확인 (StateMachineBehaviour에서 호출됨)
- [ ] VFX/SFX 효과 동작 확인
- [ ] 빠른 연속 공격 테스트 (AttackEnd 누락 없는지)
- [ ] 네트워크 동기화 테스트 (멀티플레이어인 경우)
