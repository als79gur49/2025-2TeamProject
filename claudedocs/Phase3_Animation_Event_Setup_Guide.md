# Phase 3: Animation Event Setup Guide

**작성일**: 2025-10-03
**버전**: 1.0
**상태**: Implementation Complete

---

## 📋 Overview

Phase 3에서는 Unity Animation Clip에 Animation Event를 설정하기 위한 **에디터 도구**를 구현했습니다.

### 구현된 컴포넌트

1. **AnimationEventSetupTool** - Animation Event 자동 설정 에디터 창
2. **UnitAnimationControllerInspector** - UnitAnimationController 커스텀 인스펙터
3. **AnimationClipValidator** - Animation Clip 검증 유틸리티

---

## 🛠️ 1. AnimationEventSetupTool

### 경로
`Assets/Script/Game/Editor/AnimationEventSetupTool.cs`

### 기능

**자동 Animation Event 설정**:
- Move Animation에 Start/End 이벤트 자동 추가
- Attack Animation에 Start/Impact/End 이벤트 자동 추가
- Impact 타이밍 조절 가능 (0.0 ~ 1.0)
- 배치 작업 지원 (Move + Attack 동시 설정)

### 사용 방법

#### Unity 메뉴에서 열기
```
Tools → Animation → Setup Animation Events
```

#### 설정 단계

**1. Move Animation 설정**
```
1. Move Animation Clip을 "Move Animation Clip" 필드에 드래그
2. (선택적) "Include Move Complete Event" 체크박스 선택
3. "Setup Move Animation Events" 버튼 클릭
4. 완료 다이얼로그 확인
```

**2. Attack Animation 설정**
```
1. Attack Animation Clip을 "Attack Animation Clip" 필드에 드래그
2. "Impact Timing" 슬라이더로 타격 타이밍 조절 (기본: 0.6 = 60%)
3. "Setup Attack Animation Events" 버튼 클릭
4. 완료 다이얼로그 확인
```

**3. 배치 설정**
```
1. Move와 Attack Animation Clip 모두 설정
2. "Setup Both Animations" 버튼 클릭
3. 두 애니메이션에 모두 이벤트 추가됨
```

### 추가된 Animation Events

**Move Animation**:
```yaml
Events:
  - Time: 0.0s
    Function: AnimEvent_OnAnimationStart

  - Time: [마지막 프레임]
    Function: AnimEvent_OnAnimationEnd

  - Time: [마지막 프레임] (선택적)
    Function: AnimEvent_OnMoveComplete
```

**Attack Animation**:
```yaml
Events:
  - Time: 0.0s
    Function: AnimEvent_OnAnimationStart

  - Time: [클립 길이 × Impact Timing]
    Function: AnimEvent_OnAttackImpact

  - Time: [마지막 프레임]
    Function: AnimEvent_OnAnimationEnd
```

### 예시

**Move Animation (0.5초)**:
```
Time: 0.0s  → AnimEvent_OnAnimationStart
Time: 0.5s  → AnimEvent_OnAnimationEnd
Time: 0.5s  → AnimEvent_OnMoveComplete (선택적)
```

**Attack Animation (0.6초, Impact 60%)**:
```
Time: 0.0s  → AnimEvent_OnAnimationStart
Time: 0.36s → AnimEvent_OnAttackImpact (60% 지점)
Time: 0.6s  → AnimEvent_OnAnimationEnd
```

---

## 🔍 2. UnitAnimationControllerInspector

### 경로
`Assets/Script/Game/Editor/UnitAnimationControllerInspector.cs`

### 기능

**커스텀 인스펙터**:
- UnitAnimationController의 설정을 보기 쉽게 표시
- Animation Clip의 Animation Event 설정 상태 확인
- Animation Event Setup Tool 빠른 열기
- 플레이 모드에서 애니메이션 테스트

### UI 구성

**1. Animator Settings**
- Animator 컴포넌트 참조
- Use Animator 활성화 여부

**2. Advanced Settings**
- Animation Speed Multiplier (속도 배율)
- Skip Animations (애니메이션 스킵)

**3. Debug**
- Log Animation Events (이벤트 로그 출력)

**4. Animation Clip Status**
- Move Animation 상태 표시
  - ✓ READY: 필수 이벤트 모두 설정됨
  - ⚠ MISSING EVENTS: 필수 이벤트 누락
  - NOT FOUND: Animation Clip 없음
- Attack Animation 상태 표시
- 각 Clip의 Animation Event 목록

**5. Quick Actions**
- "Open Animation Event Setup Tool" 버튼
- "Test Move Animation" 버튼 (플레이 모드 전용)
- "Test Attack Animation" 버튼 (플레이 모드 전용)

### 사용 방법

**Animation Clip 상태 확인**:
```
1. Unit GameObject 선택
2. Inspector에서 UnitAnimationController 컴포넌트 확인
3. "Animation Clip Status" 섹션 펼치기
4. Move/Attack Animation 상태 확인
   - ✓ READY: 설정 완료
   - ⚠ MISSING EVENTS: Animation Event Setup Tool로 설정 필요
```

**애니메이션 테스트**:
```
1. 플레이 모드 진입
2. Unit GameObject 선택
3. Inspector에서 "Test Move Animation" 또는 "Test Attack Animation" 클릭
4. Console에서 Animation Event 로그 확인
```

---

## ✅ 3. AnimationClipValidator

### 경로
`Assets/Script/Game/Editor/AnimationClipValidator.cs`

### 기능

**Animation Clip 검증**:
- 필수 Animation Event 존재 여부 확인
- 이벤트 타이밍 검증
- Attack Impact 타이밍 적절성 검증
- 배치 검증 (프로젝트 내 모든 Clip)

### 사용 방법

#### 단일 Clip 검증
```
1. Project 창에서 Animation Clip 선택
2. 우클릭 → "Validate Animation Clip"
3. 검증 결과 다이얼로그 확인
```

#### 전체 Clip 배치 검증
```
Tools → Animation → Validate All Animation Clips
```

### 검증 항목

**필수 이벤트 체크**:
- Move: AnimEvent_OnAnimationStart, AnimEvent_OnAnimationEnd
- Attack: AnimEvent_OnAnimationStart, AnimEvent_OnAttackImpact, AnimEvent_OnAnimationEnd

**타이밍 검증**:
- Start 이벤트는 0.0s에 있어야 함
- End 이벤트는 마지막 프레임에 있어야 함
- 이벤트 시간이 Clip 길이 범위 내여야 함

**Attack 특수 검증**:
- Impact 타이밍은 30% ~ 80% 사이 권장
- 30% 미만 또는 80% 초과 시 경고

### 검증 결과

**통과 (Valid)**:
```
Clip: Attack_Sword (0.60s, 3 events)
✓ Valid
```

**실패 (Failed)**:
```
Clip: Attack_Sword (0.60s, 2 events)
❌ Errors:
  • Missing required event: AnimEvent_OnAttackImpact

⚠ Warnings:
  • Start event should be at 0.0s (current: 0.05s)
```

---

## 📊 완전한 워크플로우

### 새 유닛 애니메이션 설정

**1. Animation Clip 준비**
```
Assets/Animations/Units/Knight/
├── Knight_Move.anim       (0.5초)
└── Knight_Attack.anim     (0.6초)
```

**2. Animation Event 자동 설정**
```
1. Tools → Animation → Setup Animation Events
2. Knight_Move.anim 드래그 → Setup Move Animation Events
3. Knight_Attack.anim 드래그 → Impact Timing 0.6 설정 → Setup Attack Animation Events
```

**3. 검증**
```
1. Knight_Move.anim 우클릭 → Validate Animation Clip → ✓ Valid
2. Knight_Attack.anim 우클릭 → Validate Animation Clip → ✓ Valid
```

**4. Unit GameObject에 적용**
```
1. Knight GameObject 생성
2. UnitAnimationController 컴포넌트 추가
3. Animator 설정 (Knight Animator Controller)
4. Inspector에서 Animation Clip Status 확인 → 모두 ✓ READY
```

**5. 테스트**
```
1. 플레이 모드 진입
2. Knight 선택 → Inspector → Test Move Animation
3. Knight 이동 → Console에서 Animation Event 로그 확인:
   [AnimationController] Knight: Animation Started - Move
   [AnimationController] Knight: Move Complete to (1, 1)
   [AnimationController] Knight: Animation Ended - Move
```

---

## 🔧 트러블슈팅

### Animation Event가 호출되지 않음

**원인 1: Animation Clip에 Event가 없음**
```
해결: AnimationEventSetupTool로 Event 설정
```

**원인 2: 함수 이름 오타**
```
해결: AnimationClipValidator로 검증
```

**원인 3: UnitAnimationController 컴포넌트 없음**
```
해결: Unit GameObject에 UnitAnimationController 추가
```

**원인 4: Animator가 비활성화됨**
```
해결: Animator 컴포넌트 활성화 확인
```

### Impact 타이밍이 부자연스러움

**해결책**:
```
1. AnimationEventSetupTool에서 Impact Timing 조절 (0.5 ~ 0.7 추천)
2. 또는 Animation 창에서 AnimEvent_OnAttackImpact를 드래그하여 미세 조정
```

### Animation Event Setup Tool 창이 안 열림

**해결책**:
```
1. Unity 메뉴: Tools → Animation → Setup Animation Events
2. 또는 UnitAnimationController Inspector → "Open Animation Event Setup Tool" 버튼
```

### 검증 경고: "Start event should be at 0.0s"

**원인**: Animation Event가 0.0s가 아닌 곳에 배치됨

**해결책**:
```
1. Animation 창 열기 (Window → Animation → Animation)
2. Animation Clip 선택
3. AnimEvent_OnAnimationStart를 0.0s로 드래그
```

---

## 📝 체크리스트

### Phase 3 구현 완료 확인

- [x] AnimationEventSetupTool 작성
  - [x] Move Animation 자동 설정
  - [x] Attack Animation 자동 설정
  - [x] Impact Timing 조절 가능
  - [x] 배치 작업 지원
  - [x] Clear 기능

- [x] UnitAnimationControllerInspector 작성
  - [x] 커스텀 인스펙터 UI
  - [x] Animation Clip 상태 표시
  - [x] Quick Actions
  - [x] 테스트 버튼

- [x] AnimationClipValidator 작성
  - [x] 단일 Clip 검증
  - [x] 배치 검증
  - [x] 검증 규칙 정의
  - [x] 검증 리포트

### 다음 단계: Unity Editor에서 실제 설정

- [ ] Move Animation Clip 준비
- [ ] Attack Animation Clip 준비
- [ ] AnimationEventSetupTool로 Event 설정
- [ ] AnimationClipValidator로 검증
- [ ] Unit GameObject에서 테스트
- [ ] Console 로그 확인

---

## 🎯 사용 시나리오

### 시나리오 1: 기존 프로젝트에 추가

**상황**: 이미 Animation Clip이 있지만 Animation Event가 없음

**단계**:
```
1. Tools → Animation → Setup Animation Events
2. 기존 Move/Attack Clip 드래그
3. Setup 버튼 클릭
4. 완료!
```

### 시나리오 2: 새 유닛 추가

**상황**: 새로운 유닛의 애니메이션 설정

**단계**:
```
1. 새 Move/Attack Animation Clip 생성
2. AnimationEventSetupTool에서 자동 설정
3. Animator Controller에 Clip 연결
4. Unit GameObject에 UnitAnimationController 추가
5. Inspector에서 상태 확인
```

### 시나리오 3: 타이밍 조정

**상황**: Attack Impact 타이밍이 맞지 않음

**단계**:
```
방법 A (에디터 도구):
1. AnimationEventSetupTool 열기
2. Impact Timing 슬라이더 조절 (예: 0.5 → 0.7)
3. Setup Attack Animation Events 재실행

방법 B (Animation 창):
1. Window → Animation → Animation
2. Attack Clip 선택
3. AnimEvent_OnAttackImpact를 드래그하여 이동
```

### 시나리오 4: 검증 및 QA

**상황**: 모든 Animation Clip이 올바르게 설정되었는지 확인

**단계**:
```
1. Tools → Animation → Validate All Animation Clips
2. 검증 리포트 확인
3. Failed Clip 수정
4. 재검증
```

---

## 🚀 다음 단계

Phase 3 완료 후:

1. **Unity Editor에서 Animation Clip 설정**
   - Move/Attack Animation Clip에 Event 추가
   - 검증 완료

2. **Phase 4: 통합 테스트**
   - Unit 이동 시 애니메이션 재생 확인
   - Unit 공격 시 타격 타이밍 확인
   - Animation Event 로그 확인

3. **Phase 5: 최적화 및 폴리시**
   - 애니메이션 속도 조절
   - 애니메이션 스킵 기능 테스트
   - 성능 프로파일링

---

**End of Guide**
