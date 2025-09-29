# CardDataMigrator Usage Guide

## Phase 3.13: CardData 리팩토링 마이그레이션 도구

CardDataMigrator는 기존 CardData 시스템을 새로운 EffectData 기반 시스템으로 자동 변환하는 도구입니다.

## 📋 개요

### 마이그레이션 대상
- **Unit 카드** → **Summon 효과**로 변환
- **Spell 카드** → **Damage/Heal 효과**로 변환 (SpellType 기반)

### 변환 규칙
```
레거시 → 새로운 시스템
─────────────────────────────────────
Unit cards        → EffectType.Summon
Spell + Damage    → EffectType.Damage
Spell + Heal      → EffectType.Heal
Spell + Summon    → EffectType.Summon
Spell + 기타      → EffectType.Damage (경고 발생)
```

## 🚀 사용 방법

### 1. Unity 에디터에서 마이그레이션 창 열기
```
Unity 메뉴 → Tools → Card System → CardData Migrator
```

### 2. 마이그레이션 설정
- **백업 생성**: 원본 파일들을 자동 백업 (권장)
- **드라이런**: 실제 변경 없이 테스트 실행

### 3. 마이그레이션 실행
1. **드라이런** 먼저 실행하여 결과 확인
2. 문제없으면 **실제 마이그레이션** 실행
3. 결과 리포트 확인

## 🔧 코드 사용법

### 전체 마이그레이션 (에디터 전용)
```csharp
// 모든 CardData를 마이그레이션
var result = CardDataMigrator.MigrateAllCards(
    createBackup: true,  // 백업 생성
    dryRun: false        // 실제 실행
);

if (result.IsSuccessful)
{
    Debug.Log($"마이그레이션 완료: {result.SuccessfulMigrations}/{result.ProcessedCards}");
}
```

### 단일 카드 마이그레이션
```csharp
var cardData = // 마이그레이션할 CardData;
var result = CardDataMigrator.MigrateCard(cardData, dryRun: false);

if (result.success)
{
    if (result.wasAlreadyMigrated)
    {
        Debug.Log("이미 마이그레이션된 카드");
    }
    else
    {
        Debug.Log("마이그레이션 완료");
    }
}
```

### 런타임 마이그레이션 (제한적)
```csharp
// 런타임에서 기본적인 마이그레이션만 가능
bool success = CardDataMigrator.MigrateCardRuntime(cardData);
```

### 마이그레이션 검증
```csharp
bool isValid = CardDataMigrator.ValidateMigration(cardData);
```

## 📊 변환 예시

### Unit 카드 변환
```csharp
// 변환 전 (레거시)
CardType.Unit
unitToSummon = KnightUnitData

// 변환 후 (새로운 시스템)
effectDataList = [
    new EffectData(EffectType.Summon, 1, AffectedType.None, 0)
    {
        UnitToSummon = KnightUnitData
    }
]
```

### Spell 카드 변환
```csharp
// 변환 전 (레거시)
CardType.Spell
spellType = SpellType.Damage
spellEffectValue = 3
targetType = TargetType.Enemy

// 변환 후 (새로운 시스템)
effectDataList = [
    new EffectData(EffectType.Damage, 3, AffectedType.Enemy, 0)
]
```

## 🛡️ 안전성 기능

### 자동 백업
- 마이그레이션 실행 전 원본 파일 백업
- 백업 위치: `backup_carddata_migration_YYYYMMDD_HHMMSS/`
- 백업 정보: `backup_info.json`

### 검증 시스템
- 마이그레이션 전후 데이터 유효성 검사
- 실패 시 자동 롤백 가능
- 상세한 오류/경고 메시지

### 드라이런 모드
- 실제 변경 없이 마이그레이션 시뮬레이션
- 예상 결과 미리 확인
- 안전한 테스트 환경

## 📈 유틸리티 기능

### 마이그레이션 상태 확인
```csharp
// Unity 메뉴: Tools → Card System → Test Migration

// 프로젝트의 모든 CardData 상태 확인
var guids = AssetDatabase.FindAssets("t:CardData");
foreach (var guid in guids)
{
    var cardData = AssetDatabase.LoadAssetAtPath<CardData>(path);
    if (cardData.IsEffectBasedCard)
    {
        // 이미 마이그레이션됨
    }
    else
    {
        // 레거시 시스템 사용 중
    }
}
```

### 테스트 카드 생성
```
Unity 메뉴 → Tools → Card System → Create Test Cards
```

### 마이그레이션 리포트 생성
```
Unity 메뉴 → Tools → Card System → Generate Migration Report
```

## ⚠️ 주의사항

### 마이그레이션 전 체크리스트
- [ ] 프로젝트 전체 백업
- [ ] 드라이런으로 결과 확인
- [ ] 백업 생성 옵션 활성화
- [ ] 버전 관리 시스템에 커밋

### 지원되지 않는 SpellType
- `Buff`, `Debuff`, `Shield`, `Teleport`
- 이러한 타입들은 `Damage` 효과로 대체됨
- 수동으로 적절한 효과로 변경 필요

### 런타임 제한사항
- 런타임에서는 기본적인 변환만 가능
- Reflection을 사용한 레거시 필드 읽기 제한
- 에디터에서 미리 마이그레이션 권장

## 🔍 문제 해결

### 마이그레이션 실패 시
1. 콘솔에서 상세 오류 메시지 확인
2. 해당 카드 데이터의 설정 검토
3. 필요시 수동으로 EffectData 설정

### 유효성 검사 실패 시
```csharp
// 문제가 있는 카드 찾기
if (!cardData.IsValid())
{
    // 각 EffectData 확인
    foreach (var effect in cardData.EffectDataList)
    {
        if (!effect.IsValid())
        {
            Debug.LogError($"유효하지 않은 효과: {effect}");
        }
    }
}
```

### 백업에서 복원
1. `backup_carddata_migration_YYYYMMDD_HHMMSS/` 폴더 찾기
2. 백업된 파일을 원본 위치로 복사
3. Unity에서 `Refresh` (Ctrl+R)

## 📝 마이그레이션 결과 구조

```csharp
public class MigrationResult
{
    public int ProcessedCards         // 처리된 카드 수
    public int SuccessfulMigrations   // 성공한 마이그레이션 수
    public int FailedMigrations       // 실패한 마이그레이션 수
    public int SkippedCards           // 건너뛴 카드 수 (이미 마이그레이션됨)
    public List<string> ErrorMessages    // 오류 메시지
    public List<string> WarningMessages  // 경고 메시지
    public DateTime MigrationTimestamp   // 실행 시간

    public bool IsSuccessful => FailedMigrations == 0 && ProcessedCards > 0
}
```

## 🎯 모범 사례

### 마이그레이션 워크플로우
1. **계획** - 마이그레이션할 카드들 확인
2. **백업** - 프로젝트 전체 백업
3. **테스트** - 드라이런으로 시뮬레이션
4. **실행** - 실제 마이그레이션
5. **검증** - 결과 확인 및 테스트
6. **정리** - 불필요한 레거시 필드 제거

### 대규모 프로젝트
- 단계별로 나누어서 마이그레이션
- 카드 타입별로 분리하여 처리
- 마이그레이션 후 충분한 테스트

### 팀 개발 환경
- 마이그레이션 전 팀원들과 협의
- 버전 관리 시스템에 백업 커밋
- 마이그레이션 결과 공유

이 도구를 사용하여 CardData 리팩토링 Phase 3.13을 안전하고 효율적으로 완료할 수 있습니다.