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

---

## 📚 추가 참고 자료

프로젝트의 다른 주의사항과 설계 문서는 다음 파일들을 참고하세요:
- `README.md` - 프로젝트 전반적인 구조
- `claudedocs/` - 기타 설계 문서 및 분석 보고서
- 각종 `*_Design.md`, `*_Plan.md` - 시스템별 설계 명세

---

**마지막 업데이트**: 2025-11-08
