# CardInfoPanel 리팩토링 완료 보고서

## 변경 사항 요약

CardInfoPanel을 단순화된 텍스트 기반 구조로 전면 리팩토링했습니다.

### 이전 구조 (제거됨)
- `cardNameText` (TextMeshProUGUI)
- `cardDescriptionText` (TextMeshProUGUI)
- `cardIconImage` (Image)
- `manaCostText` (TextMeshProUGUI)
- `cardBackgroundImage` (Image)

### 새로운 구조 (3개 필드만 사용)
1. **essentialInfoText** (TextMeshProUGUI)
   - 카드 이름
   - TargetType
   - TargetRange
   
2. **descriptionText** (TextMeshProUGUI)
   - 카드 설명

3. **effectInfoText** (TextMeshProUGUI)
   - EffectData 리스트의 모든 효과 정보
   - 각 효과별로 Type, Value, AffectedType, AffectedRange 표시
   - UnitToSummon이 있을 경우 소환 유닛 정보 추가 표시

## 수정된 파일
- `Assets/Script/UI/Game/CardInfoPanel.cs`

## 코드 변경 세부사항

### 1. UI Elements 필드 변경
```csharp
// Before (5개 필드)
[SerializeField] private TextMeshProUGUI cardNameText;
[SerializeField] private TextMeshProUGUI cardDescriptionText;
[SerializeField] private Image cardIconImage;
[SerializeField] private TextMeshProUGUI manaCostText;
[SerializeField] private Image cardBackgroundImage;

// After (3개 필드)
[SerializeField] private TextMeshProUGUI essentialInfoText;
[SerializeField] private TextMeshProUGUI descriptionText;
[SerializeField] private TextMeshProUGUI effectInfoText;
```

### 2. ShowCardInfo 메서드 재작성
StringBuilder를 사용한 효율적인 문자열 생성으로 변경:

```csharp
private void ShowCardInfo(CardData cardData)
{
    // 1. 필수 정보
    essentialInfoText.text = $"{cardData.CardName}\nTarget: {cardData.Target} / Range: {FormatTargetRange(cardData.TargetRange)}";
    
    // 2. 설명
    descriptionText.text = cardData.Description;
    
    // 3. 효과 정보 (StringBuilder 사용)
    StringBuilder sb = new StringBuilder();
    foreach (var effect in cardData.EffectDataList)
    {
        sb.AppendLine("[Effect]");
        sb.AppendLine($"Type: {effect.Type}");
        sb.AppendLine($"Value: {effect.Value}");
        sb.AppendLine($"AffectedType: {effect.AffectedType}");
        sb.AppendLine($"AffectedRange: {effect.AffectedRange}");
        
        if (effect.UnitToSummon != null)
        {
            sb.AppendLine("[Summoned Unit Info]");
            sb.AppendLine($"  UnitName: {unit.UnitName}");
            sb.AppendLine($"  MaxHealth: {unit.MaxHealth}");
            sb.AppendLine($"  AttackPower: {unit.AttackPower}");
            sb.AppendLine($"  MovementRange: {unit.MovementRange}");
        }
        sb.AppendLine("");
    }
    effectInfoText.text = sb.ToString();
}
```

### 3. 헬퍼 메서드 추가
```csharp
private string FormatTargetRange(int targetRange)
{
    return targetRange == -1 ? "무제한" : targetRange.ToString();
}
```

## Unity Inspector 설정 필요 사항

### CardInfoPanel GameObject 설정
1. **EssentialInfoText** 연결
   - TextMeshProUGUI 컴포넌트 할당
   - 상단 영역에 배치 권장
   
2. **DescriptionText** 연결
   - TextMeshProUGUI 컴포넌트 할당
   - 중단 영역에 배치 권장
   
3. **EffectInfoText** 연결
   - TextMeshProUGUI 컴포넌트 할당
   - 하단 영역에 배치 권장
   - 스크롤 가능한 영역 권장 (효과가 많을 경우 대비)

### 제거할 컴포넌트
- cardIconImage (Image)
- cardBackgroundImage (Image)
- manaCostText (TextMeshProUGUI)
- 기존 cardNameText, cardDescriptionText (새로운 필드로 교체)

## 출력 예시

### 필수 정보 (essentialInfoText)
```
화염구
Target: Enemy / Range: 2
```

### 설명 (descriptionText)
```
적에게 강력한 화염 피해를 입힙니다.
```

### 효과 정보 (effectInfoText)
```
[Effect]
Type: Damage
Value: 5
AffectedType: Enemy
AffectedRange: 1

[Effect]
Type: Summon
Value: 1
AffectedType: NotAny
AffectedRange: 0
[Summoned Unit Info]
  UnitName: Fire Elemental
  MaxHealth: 50
  AttackPower: 15
  MovementRange: 3

```

## 장점

### 단순성
- UI 요소가 10개 이상에서 3개로 대폭 감소
- 동적 생성, Object Pooling 불필요
- 코드 복잡도 감소

### 성능
- StringBuilder 사용으로 메모리 효율적
- GC 압력 감소

### 유지보수성
- 텍스트 기반으로 확장 용이
- Rich Text 태그로 향후 스타일링 가능
- 디버깅 간편

## 다음 단계
1. Unity Scene에서 CardInfoPanel GameObject 찾기
2. Inspector에서 3개의 TextMeshProUGUI 컴포넌트 할당
3. 기존 Image 컴포넌트 제거
4. 테스트: 카드 드래그 시 정보 표시 확인

## 테스트 체크리스트
- [ ] 카드 이름, TargetType, TargetRange가 essentialInfoText에 표시됨
- [ ] 카드 설명이 descriptionText에 표시됨
- [ ] 모든 EffectData가 effectInfoText에 표시됨
- [ ] Summon 효과의 UnitToSummon 정보가 올바르게 표시됨
- [ ] 효과가 없는 카드의 경우 "(No effects)" 메시지 표시됨
- [ ] 카드 드래그 종료 시 패널이 숨겨짐
