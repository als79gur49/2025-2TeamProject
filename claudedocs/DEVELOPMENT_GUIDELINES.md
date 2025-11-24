# 개발 주의사항 (Development Guidelines)

프로젝트 개발 시 반드시 지켜야 할 핵심 규칙과 주의사항을 정리한 문서입니다.

---

## 📦 1. CardData (ScriptableObject) 추가 규칙

### ⚠️ 중요: 경로 지정
새로운 `CardData` (ScriptableObject)를 생성할 때 반드시 올바른 경로에 저장해야 합니다.

#### ❌ 잘못된 경로
```
Assets/ScriptableobjectAssets/CardData~/
```

#### ✅ 올바른 경로
```
Assets/Resources/Cards/
```

### 📝 이유
프로젝트에서 `Resources.LoadAll<CardData>("Cards")` 메서드를 사용하여 카드 데이터를 동적으로 로드합니다.
`Resources.LoadAll`은 `Assets/Resources/` 폴더 내의 리소스만 로드할 수 있으므로,
반드시 **Assets/Resources/Cards/** 경로에 CardData를 배치해야 합니다.

### 💡 참고 코드
```csharp
// CardData 로딩 예시
CardData[] allCards = Resources.LoadAll<CardData>("Cards");
```

---

## 💀 2. Unit 사망 애니메이션 설정 규칙

### ⚠️ 중요: Trigger 이름 규칙
Unit의 사망 애니메이션을 추가할 때 반드시 **Trigger 방식**으로 연결하고, Trigger 이름은 정확히 **"Die"**로 설정해야 합니다.

#### ✅ 올바른 설정
- **애니메이션 연결 방식**: Trigger
- **Trigger 이름**: `Die` (대소문자 정확히 일치)

#### ❌ 잘못된 예시
```
- "Death" (X)
- "die" (X - 소문자)
- "DIE" (X - 대문자)
- Bool 타입 사용 (X - Trigger만 가능)
```

### 📝 이유
프로젝트의 `DeathAnimationManager` 클래스에서 사망 애니메이션을 중앙 관리하며,
다음과 같이 하드코딩된 상수를 사용합니다:

```csharp
private const string DEATH_ANIMATION_TRIGGER = "Die";
```

모든 Unit의 사망 애니메이션은 이 통합 시스템을 통해 재생되므로,
Trigger 이름이 정확히 일치하지 않으면 애니메이션이 재생되지 않습니다.

### 💡 Animator 설정 체크리스트
1. Animator Controller 열기
2. 사망 애니메이션 상태(State) 추가
3. Parameters 탭에서 새 Trigger 생성
4. Trigger 이름을 정확히 **"Die"**로 설정
5. Any State → Death State로 Transition 생성
6. Transition 조건: Trigger = "Die"

### 🔍 관련 파일
- `Assets/Script/Manager/DeathAnimationManager.cs`

---

## 📏 3. 위치 Offset 시스템

### ⚠️ 중요: Offset 계층 구조 이해
프로젝트에서 객체의 Y축 위치는 **전역 Offset**과 **개별 Offset**의 조합으로 결정됩니다.

### 🌍 전역 Offset (Global Offset)
모든 객체의 위치 계산에 영향을 미치는 기본 높이 값입니다.

| 변수명 | 타입 | 선언 위치 | 설명 |
|--------|------|----------|------|
| `baseHeight` | `float` | `GridController` | Base 타일 위치의 기본 높이 (기본값: 1.85f) |
| `groundHeight` | `float` | `GridController` | Ground 타일 위치의 기본 높이 (기본값: 0.65f) |
| `highlightPlaneYOffset` | `float` | `GridRenderer` | 그리드 하이라이트 평면의 높이 조정 |

### 🎯 개별 Offset (Individual Offset)
각 객체 타입별로 독립적으로 적용되는 위치 보정 값입니다.

| 변수명 | 타입 | 선언 위치 | 설명 |
|--------|------|----------|------|
| `positionOffset` | `Vector3` | `VFXData` | VFX 이펙트의 시각적 위치 미세 조정 |
| `unitOffset` | `Vector3` | `GridController` | 유닛 배치 위치의 보정 값 |

### 📐 실제 스폰 위치 계산 공식

```csharp
// 최종 Y축 위치 계산
float finalYPosition = (BaseHeight OR GroundHeight) + (positionOffset.y OR unitOffset.y);
```

#### 예시 1: Unit 스폰 위치
```csharp
// Base 타일에 유닛 배치
Vector3 spawnPosition = new Vector3(
    gridX,
    baseHeight + unitOffset.y,  // 1.85f + unitOffset.y
    gridZ
);
```

#### 예시 2: VFX 스폰 위치
```csharp
// Ground 타일에 VFX 배치
Vector3 vfxPosition = new Vector3(
    worldX,
    groundHeight + positionOffset.y,  // 0.65f + positionOffset.y
    worldZ
);
```

### 💡 Offset 조정 가이드

1. **전체적인 높이 변경이 필요할 때**
   - `baseHeight` 또는 `groundHeight` 값 조정
   - 모든 유닛/VFX가 동시에 영향받음

2. **특정 객체만 조정이 필요할 때**
   - `positionOffset` 또는 `unitOffset` 값 조정
   - 해당 객체 타입만 독립적으로 조정됨

3. **그리드 하이라이트가 맞지 않을 때**
   - `highlightPlaneYOffset` 값 조정

### 🔍 관련 파일
- `Assets/Script/Grid/GridController.cs` (line 1238-1300)
- `Assets/Script/VFX/VFXData.cs`
- `Assets/Script/Grid/GridRenderer.cs`

---

## 🎯 4. TeamRelation Result vs Filter 사용 규칙

### ⚠️ 중요: 레이어별 TeamRelation 사용 구분
프로젝트에서 `TeamRelation` enum은 **Result 레이어**와 **Filter 레이어** 두 곳에서 사용되며,
각 레이어에서의 의미와 사용 규칙이 다릅니다. 이를 혼용하면 설계 버그가 발생할 수 있습니다.

### 📊 Result 레이어 (관계 계산)

**용도**: 실제 팀 간의 관계를 계산하고 반환하는 계층

**사용 예시:**
- `TeamRelationMatrix.GetRelation()`
- `TeamComponent.GetRelationTo()`
- `IsEnemy()`, `IsAlly()`, `IsSelf()`

**사용 가능한 값:**
- ✅ `Self` - 자기 자신
- ✅ `Ally` - 아군
- ✅ `Enemy` - 적군
- ✅ `Neutral` - 중립
- ❌ `Any` - **절대 반환하면 안 됨**

**핵심 규칙:**
```csharp
// ✅ 올바른 사용
TeamRelation relation = matrix.GetRelation(TeamType.Player, TeamType.Enemy);
// 반환값: Self, Ally, Enemy, Neutral 중 하나

// ❌ 잘못된 사용
if (matrix.GetRelation(...) == TeamRelation.Any)  // Any는 Result에서 절대 나올 수 없음
{
    // 이 코드가 실행되면 설계 버그!
}
```

**장점:**
- 기존 로직(Combat, AI, GridController 판단 등) 유지
- 새로운 값 추가로 인한 사이드 이펙트 최소화
- 명확한 관계 상태 표현

### 🎯 Filter 레이어 (타겟팅/조건)

**용도**: 타겟을 필터링하거나 조건을 검사하는 계층

**사용 예시:**
- `GridController.HasUnitWithRelation(position, relativeTo, relation)`
- `ModifierConfig.TargetingParams.TargetRelation`
- `TargetFilterDefinition.TeamFilterDefinition`

**사용 가능한 값:**
- ✅ `Self` - 자신만 선택
- ✅ `Ally` - 아군만 선택
- ✅ `Enemy` - 적군만 선택
- ✅ `Neutral` - 중립만 선택
- ✅ `Any` - **"관계 무관, 유닛 존재만 확인"**

**핵심 규칙:**
```csharp
// ✅ 올바른 사용 - Any는 "모든 관계 허용"
bool hasAnyUnit = gridController.HasUnitWithRelation(
    position,
    myUnit,
    TeamRelation.Any  // 아군/적군 구분 없이 유닛이 있는지만 확인
);

// ✅ 복잡한 조건은 TargetFilterDefinition 조합으로 해결
var filter = new TargetFilterDefinition()
{
    TeamFilter = TeamRelation.Ally,
    HealthFilter = new HealthCondition { MinHP = 50 },
    AndFilter = new AnotherFilter()
};
```

**복잡한 조건 처리:**
- ❌ `TeamRelation` enum에 `AllyOrSelf`, `EnemyWithLowHP` 같은 복합 값 추가하지 말 것
- ✅ `TargetFilterDefinition`의 And/Or 조합 + 다른 필터들로 해결
- ✅ `TeamRelation` enum은 최소한의 축만 유지

### 🔍 왜 이렇게 나누는가

#### Result 레이어 특성
```
Result 레이어는 "현재 상태"를 나타내는 순수 데이터
→ 값이 많아질수록 모든 switch/로직에서 케이스 증가
→ 버그 위험 증가
→ 유지보수 어려움
```

#### Filter 레이어 특성
```
Filter 레이어는 "무엇을 허용할 것인가"를 표현하는 정책/조건
→ 와일드카드(Any) 같은 유연한 표현 필요
→ 복잡한 조건은 enum이 아닌 필터 조합으로 해결
→ 확장성과 유지보수성 향상
```

### 💡 설계 원칙

1. **TeamRelation enum의 책임**
   - 공통으로 이해 가능한 최소 축만 포함: `Self`, `Ally`, `Enemy`, `Neutral`, `Any`
   - 복합 조건이나 특수 케이스는 enum에 추가하지 않음

2. **복잡한 조건 처리**
   - `TargetFilterDefinition`에 책임 위임
   - And/Or 조합, 스탯 조건 등으로 유연하게 표현

3. **Any의 의미**
   - **Result 레이어**: 절대 사용 금지 (버그 신호)
   - **Filter 레이어**: "피아 구분 없는 와일드카드" (정상 사용)

### 📝 요약

```yaml
TeamRelation.Any:
  Result 레이어: ❌ 절대 반환하면 안 됨 (버그)
  Filter 레이어: ✅ "관계 무관" 와일드카드로 사용

관계 계산 결과: Self, Ally, Enemy, Neutral만 사용
타겟팅 조건: Self, Ally, Enemy, Enemy, Neutral, Any 모두 사용 가능

복잡한 조건: TargetFilterDefinition 조합으로 해결
TeamRelation enum: 최소한의 공통 축만 유지
```

### 🔍 관련 파일
- `Assets/Script/Game/Data/TeamRelationMatrix.cs`
- `Assets/Script/Game/Components/TeamComponent.cs`
- `Assets/Script/Game/Components/GridController.cs`
- `Assets/Script/Game/Card/Effects/TargetFilterDefinition.cs`
- `Assets/Script/Game/Services/Modifiers/ModifierConfig.cs`

### ⚠️ 주의사항

#### 코드 리뷰 체크리스트
```csharp
// ❌ 위험: Result 레이어에서 Any 반환
public TeamRelation GetRelation(TeamType a, TeamType b)
{
    // ...
    return TeamRelation.Any;  // 이건 버그!
}

// ❌ 위험: switch에서 Any를 Result처럼 다룸
switch(matrix.GetRelation(teamA, teamB))
{
    case TeamRelation.Any:  // Result에서는 나올 수 없음!
        break;
}

// ✅ 올바름: Filter에서만 Any 사용
bool hasUnit = HasUnitWithRelation(pos, unit, TeamRelation.Any);

// ✅ 올바름: Result는 4가지 값만 처리
switch(matrix.GetRelation(teamA, teamB))
{
    case TeamRelation.Self:
    case TeamRelation.Ally:
    case TeamRelation.Enemy:
    case TeamRelation.Neutral:
        break;
    // Any 케이스는 없어야 함!
}
```

---

## 🔊 5. AudioPlayRequest 사용 가이드 (Effect 사운드 전용 런타임 Modifier)

### 5.1 목적

`AudioPlayRequest`는 **AudioData ScriptableObject의 기본 설정을 변경하지 않고**,  
재생 호출 시점에 **볼륨/피치 배율을 곱해** 개별 사운드를 튜닝하기 위한 런타임 요청 객체입니다.

- AudioData: 설계자가 정한 **기본값 (volume/pitch/loop 등)** 유지
- AudioPlayRequest: 프로그래머가 **호출마다 추가 보정 (×multiplier)** 를 적용

현재는 **EffectAudioService (효과음)** 에만 multiplier가 적용됩니다.  
BGM은 기존 AudioData 기반 재생을 유지합니다.

### 5.2 기본 사용법 (Fluent API)

```csharp
// 1) 기본 재생 (기존 코드와 동일, multiplier = 1)
soundEventChannel.RaiseSoundEvent(effectSoundData, this);

// 2) 볼륨만 조정 (절반 볼륨)
var request = AudioPlayRequest
    .Create(effectSoundData, this)
    .WithVolume(0.5f);

soundEventChannel.RaiseSoundEvent(request);

// 3) 볼륨/피치 모두 조정
var spellRequest = AudioPlayRequest
    .Create(spellSoundData, this)
    .WithVolume(1.2f)   // 20% 더 큼
    .WithPitch(0.8f);   // 피치 ↓ → 느리게/굵게

soundEventChannel.RaiseSoundEvent(spellRequest);
```

- `Create(AudioData, owner)` 가 **시작점**입니다.
- `WithVolume`, `WithPitch` 는 항상 **새 인스턴스**를 반환합니다 (값 타입).
- 최종적으로 `SoundEventChannelSO.RaiseSoundEvent(AudioPlayRequest request)` 를 사용하면,
  Event Channel → AudioServiceContainer → EffectAudioService 경로에서 multiplier가 적용됩니다.

### 5.3 내부 동작 요약

1. 호출부에서 `AudioPlayRequest` 생성 후 `RaiseSoundEvent(request)` 호출
2. `SoundEventChannelSO`:
   - 쿨다운(`AudioData.CanPlay`) 체크
   - `OnSoundRequestedWithModifiers(request)` 이벤트 발행
3. `AudioServiceContainer`:
   - `OnSoundRequestedWithModifiers` 에만 구독
   - `AudioType.Effect` 인 경우 `IEffectAudioService.PlayEffect(request)` 호출
4. `EffectAudioService`:
   - `baseVolume = audioData.GetRandomVolume()`
   - `basePitch  = audioData.GetRandomPitch()`
   - `finalVolume = Mathf.Clamp01(baseVolume * request.volumeMultiplier)`
   - `finalPitch  = Mathf.Clamp(basePitch * request.pitchMultiplier, 0.1f, 3.0f)`
   - 최종 값을 `AudioSource` 에 적용 후 재생

### 5.4 ⚠️ 주의사항 (반드시 읽기)

1. **반환값을 무시하면 multiplier가 적용되지 않습니다.**

```csharp
// ❌ 잘못된 사용: request는 여전히 기본 배율(1, 1)
var request = AudioPlayRequest.Create(effectSoundData, this);
request.WithVolume(0.5f); // 반환값을 변수에 담지 않으면 의미 없음

// ✅ 올바른 사용
var request = AudioPlayRequest.Create(effectSoundData, this)
    .WithVolume(0.5f);
```

- `AudioPlayRequest` 는 `readonly struct` 이고 `WithXXX` 는 **새 값**을 반환합니다.
- 항상 **체이닝하거나, 반환값을 다시 변수에 담는 패턴**을 사용하십시오.

2. **AudioData는 절대 수정하지 않습니다.**

- multiplier는 **재생 시점에만** 적용됩니다.
- `AudioData.volumeMin/max`, `pitchMin/max` 등의 필드는 항상 설계 기본값으로 남겨두고,  
  코드에서 이 값을 직접 변경하지 않습니다.

3. **multiplier 범위는 재생 단계에서 클램프됩니다.**

- 볼륨:
  - `finalVolume = Mathf.Clamp01(baseVolume * volumeMultiplier);`
  - baseVolume와 multiplier가 1을 넘어도 최종 볼륨은 0~1로 제한됩니다.
- 피치:
  - `finalPitch = Mathf.Clamp(basePitch * pitchMultiplier, 0.1f, 3.0f);`
  - 너무 낮거나 높은 피치로 인한 오디오 깨짐을 방지합니다.

4. **현재는 Effect 전용입니다.**

- `AudioType.Effect` 경로에서만 `AudioPlayRequest` multiplier가 적용됩니다.
- `AudioType.BGM` 은 기존 `IBGMAudioService.PlayBGM(AudioData)` 경로를 사용하며,  
  개별 곡의 볼륨/피치는 AudioData + AudioMixer/VolumeController로 관리합니다.

5. **Event Channel 구독은 새 이벤트만 사용합니다.**

- `AudioServiceContainer` 는 `SoundEventChannelSO.OnSoundRequestedWithModifiers` 만 구독합니다.
- 레거시 `OnSoundRequested(AudioData, object)` 는 다른 시스템/디버그용으로만 사용하고,  
  컨테이너에서 동시에 구독하면 **같은 사운드가 두 번 재생**될 수 있으므로 금지됩니다.

### 5.5 추천 사용처 예시

- VFX 기반 스펠:
  - VFX 재생 속도(`PlaybackSpeed`)에 비례하여 피치 조절
  - 카메라 거리/줌 레벨에 따라 효과음 볼륨 조정
- UI/피드백:
  - 드롭 성공/실패, 크리티컬 등 상황에 따라 같은 AudioData에 다른 multiplier 적용

Fluent API (`Create().WithVolume().WithPitch()`) 를 기본 패턴으로 사용하면,  
AudioData 설계와 runtime 제어를 깔끔하게 분리할 수 있습니다.

---

## ✅ 규칙 준수 확인

### CardData 추가 시
- [ ] Assets/Resources/Cards/ 경로에 생성했는가?
- [ ] 다른 경로(ScriptableobjectAssets 등)에 실수로 저장하지 않았는가?

### Unit 애니메이션 추가 시
- [ ] 사망 애니메이션을 Trigger 방식으로 설정했는가?
- [ ] Trigger 이름이 정확히 "Die"인가? (대소문자 확인)
- [ ] Animator Controller에서 Transition이 올바르게 연결되었는가?

### 위치 Offset 조정 시
- [ ] 전역 변경이 필요한지, 개별 변경이 필요한지 판단했는가?
- [ ] 전역 Offset (baseHeight/groundHeight) 변경 시 모든 객체에 영향을 미친다는 것을 인지했는가?
- [ ] 개별 Offset (positionOffset/unitOffset) 조정 시 해당 객체만 영향받는다는 것을 확인했는가?
- [ ] 실제 스폰 위치 = (전역 Offset) + (개별 Offset) 공식을 이해했는가?

### TeamRelation 사용 시
- [ ] Result 레이어(관계 계산)에서 TeamRelation.Any를 반환하지 않는가?
- [ ] Filter 레이어(타겟팅/조건)에서만 TeamRelation.Any를 사용하는가?
- [ ] switch 문에서 Result 값으로 TeamRelation.Any 케이스를 처리하지 않는가?
- [ ] 복잡한 조건은 TeamRelation enum이 아닌 TargetFilterDefinition 조합으로 해결하는가?
- [ ] TeamRelationMatrix.GetRelation()의 반환값은 Self/Ally/Enemy/Neutral만 사용하는가?

---

## 📚 추가 참고 자료

프로젝트의 다른 주의사항과 설계 문서는 다음 파일들을 참고하세요:
- `README.md` - 프로젝트 전반적인 구조
- `claudedocs/` - 기타 설계 문서 및 분석 보고서
- 각종 `*_Design.md`, `*_Plan.md` - 시스템별 설계 명세

---

**마지막 업데이트**: 2025-11-23
