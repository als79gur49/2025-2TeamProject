# CardData 리팩토링 Phase 1.3: 마이그레이션 전략 구현 완료

## 📋 개요

CardData 리팩토링 계획서 v2의 **Phase 1.3: 마이그레이션 전략 수립**에 따라 구현된 완전한 마이그레이션 시스템입니다.

### 🎯 구현 목표
- ✅ 기존 CardData 시스템에서 새로운 효과 기반 시스템으로의 안전한 마이그레이션
- ✅ Unit 카드 → Summon 효과 변환
- ✅ Spell 카드 → Damage/Heal 효과 변환
- ✅ 완전한 백업 및 롤백 시스템
- ✅ 포괄적인 테스팅 및 검증 프레임워크

## 🏗️ 아키텍처 구조

### 핵심 컴포넌트

#### 1. CardDataMigrator.cs
**역할**: 핵심 마이그레이션 로직 담당
- Unit 카드 → Summon 효과 변환
- Spell 카드 → Damage/Heal 효과 변환
- TargetType/AffectedType 매핑
- 단일 카드 및 프로젝트 전체 분석

**주요 메서드**:
```csharp
// 단일 카드 마이그레이션
MigrationResult MigrateCardData(CardData originalCard, MigrationOptions options = null)

// 전체 프로젝트 분석
List<MigrationResult> AnalyzeAllCards(MigrationOptions options = null)

// 마이그레이션 보고서 생성
string GenerateMigrationReport(List<MigrationResult> results)
```

#### 2. CardMigrationTester.cs
**역할**: 마이그레이션 품질 보증 및 테스팅
- 사전 정의된 테스트 케이스 실행
- 실제 프로젝트 카드 데이터 검증
- 성능 테스트 및 벤치마킹
- Unity 에디터 메뉴 통합

**주요 기능**:
```csharp
// 모든 테스트 실행
List<TestResult> RunAllTests()

// 성능 테스트
void RunPerformanceTest(int iterations = 1000)

// 테스트 보고서 생성
string GenerateTestReport(List<TestResult> results)
```

#### 3. CardMigrationBackup.cs
**역할**: 데이터 안전성 보장 백업 시스템
- 전체/선택적 백업 생성
- 백업 메타데이터 관리
- 완전한 롤백 기능
- 자동 백업 정리 (최대 10개 유지)

**주요 기능**:
```csharp
// 전체 백업 생성
BackupInfo CreateFullBackup(string description = "자동 백업")

// 백업에서 복원
bool RestoreFromBackup(string backupId)

// 백업 목록 조회
List<BackupInfo> GetAllBackups()
```

#### 4. CardMigrationCoordinator.cs
**역할**: 전체 마이그레이션 프로세스 통합 관리
- 5단계 마이그레이션 파이프라인 실행
- 실시간 진행 상황 추적
- 자동 롤백 및 오류 복구
- 드라이런 모드 지원

**마이그레이션 단계**:
1. **Preparation**: 대상 분석 및 사전 검증
2. **Backup**: 안전성을 위한 백업 생성
3. **Testing**: 마이그레이션 로직 테스트
4. **Execution**: 실제 마이그레이션 실행
5. **Validation**: 마이그레이션 후 검증

## 🔄 데이터 변환 로직

### Unit 카드 → Summon 효과
```csharp
// 기존 구조
CardType.Unit + UnitData unitToSummon

// 새로운 구조
EffectType.Summon + EffectData{
    Type: Summon,
    Value: 1,
    UnitToSummon: unitToSummon,
    AffectedType: None
}
```

### Spell 카드 → Damage/Heal 효과
```csharp
// 기존 구조
CardType.Spell + SpellType.Damage + spellEffectValue

// 새로운 구조
EffectType.Damage + EffectData{
    Type: Damage,
    Value: spellEffectValue,
    AffectedType: Enemy,
    AffectedRange: areaOfEffect
}
```

### 타겟팅 시스템 변환
```csharp
// 배치 가능 위치 검증 (TargetType)
TargetType.AllEnemies → TargetType.Any (배치는 어디든 가능)

// 효과 적용 대상 (AffectedType)
TargetType.AllEnemies → AffectedType.Enemy (적군에게만 영향)
```

## 🛡️ 안전성 보장 메커니즘

### 1. 다중 검증 시스템
- **사전 분석**: 마이그레이션 가능성 미리 검증
- **테스트 실행**: 실제 변경 전 로직 검증
- **사후 검증**: 마이그레이션 후 데이터 무결성 확인

### 2. 자동 백업 및 롤백
- 마이그레이션 전 자동 백업 생성
- 실패 시 자동 롤백 옵션
- 백업 메타데이터를 통한 추적 관리

### 3. 배치 처리 및 재시도
- 대량 데이터 처리를 위한 배치 시스템
- 개별 카드 실패에 대한 재시도 메커니즘
- 성능 제한을 위한 배치 간 지연

### 4. 드라이런 모드
- 실제 변경 없이 마이그레이션 로직 테스트
- 문제점 사전 발견 및 해결

## 📊 품질 보증

### 테스트 커버리지
- **Unit Tests**: 개별 변환 로직 검증
- **Integration Tests**: 전체 시스템 통합 테스트
- **Performance Tests**: 대량 데이터 처리 성능 검증
- **Validation Tests**: 마이그레이션 후 데이터 무결성 검증

### 성공 기준
- 최소 성공률: 90% (설정 가능)
- 개별 카드 재시도: 최대 3회
- 테스트 통과율: 90% 이상
- 백업 생성 필수

## 🎮 Unity 에디터 통합

### 메뉴 구조
```
Tools/Card Migration/
├── Execute Full Migration      # 전체 마이그레이션 실행
├── Run Tests                  # 마이그레이션 테스트
├── Run Performance Test       # 성능 테스트
├── Analyze All Cards         # 카드 분석 보고서
├── Create Full Backup        # 수동 백업 생성
└── Show Backup List          # 백업 목록 조회
```

### 사용 순서
1. **`Analyze All Cards`**: 마이그레이션 가능성 사전 분석
2. **`Run Tests`**: 마이그레이션 로직 검증
3. **`Create Full Backup`**: 수동 백업 생성 (선택사항)
4. **`Execute Full Migration`**: 실제 마이그레이션 실행

## 📈 성능 특성

### 벤치마크 결과 (예상)
- **단일 카드 마이그레이션**: ~0.5ms
- **100개 카드 배치 처리**: ~50ms
- **전체 프로젝트 분석**: 카드 수에 선형 비례
- **백업 생성**: 카드 수 × 2ms (JSON 직렬화)

### 메모리 사용량
- **마이그레이션 중**: 원본 데이터의 ~150% (변환 데이터 포함)
- **백업 데이터**: JSON 텍스트 형태로 압축 효율적
- **배치 처리**: 메모리 사용량 제한

## 🔧 설정 옵션

### MigrationSettings
```csharp
public class MigrationSettings {
    // 안전성
    bool CreateBackupBeforeMigration = true;
    bool RunTestsBeforeMigration = true;
    bool AutoRollbackOnFailure = true;

    // 실행 모드
    bool DryRunMode = false;  // 테스트 전용 모드

    // 품질 기준
    float MinimumSuccessRate = 0.9f;  // 90%
    int MaxRetryAttempts = 3;

    // 성능 조정
    int BatchSize = 10;
    float DelayBetweenBatches = 0.1f;
}
```

## 📋 출력 보고서

### 1. 마이그레이션 분석 보고서
- 마이그레이션 가능한 카드 목록
- 변환될 효과 타입 및 설정
- 마이그레이션 불가능한 카드 및 사유
- 예상 성공률

### 2. 테스트 보고서
- 개별 테스트 케이스 결과
- 전체 테스트 통과율
- 실패한 테스트 상세 정보
- 성능 벤치마크 결과

### 3. 상세 마이그레이션 보고서
- 실행 시간 및 진행 상황
- 성공/실패 카드 통계
- 발생한 오류 목록
- 백업 정보

## 🚨 주의사항

### 사용 전 확인사항
1. **백업 필수**: 중요한 데이터는 반드시 수동 백업
2. **버전 관리**: Git 커밋 후 마이그레이션 실행
3. **테스트 환경**: 먼저 복사본에서 테스트
4. **의존성 확인**: UnitData 등 참조 데이터 무결성

### 제한사항
- 현재 Phase에서는 Damage, Heal, Summon만 지원
- Buff, Debuff, Shield 등은 추후 Phase에서 구현
- 복잡한 조건부 효과는 수동 변환 필요

## 🔮 다음 단계 (Phase 2)

1. **실제 CardData 구조 변경**: EffectData 리스트 적용
2. **팩토리 패턴 구현**: ICardEffect 인터페이스 및 구현체
3. **CardSpawnService 업데이트**: 새로운 효과 시스템 연동
4. **UI 시스템 업데이트**: 새로운 카드 설명 생성

## 📁 파일 구조

```
Assets/Script/Game/Card/Effects/
├── EffectType.cs               # 기존 (Phase 1.2)
├── EffectData.cs              # 기존 (Phase 1.2)
├── CardDataMigrator.cs        # 신규 - 핵심 마이그레이션 로직
├── CardMigrationTester.cs     # 신규 - 테스팅 프레임워크
├── CardMigrationBackup.cs     # 신규 - 백업 시스템
└── CardMigrationCoordinator.cs # 신규 - 통합 코디네이터
```

## 🎉 결론

Phase 1.3에서 구현된 마이그레이션 시스템은 다음을 달성했습니다:

- ✅ **완전한 자동화**: 수동 개입 최소화
- ✅ **안전성 보장**: 다중 백업 및 검증 시스템
- ✅ **확장성**: 추후 효과 타입 추가 대응
- ✅ **사용성**: Unity 에디터 완전 통합
- ✅ **투명성**: 상세한 보고서 및 로그

이제 Phase 2에서 실제 CardData 구조 변경과 팩토리 패턴 구현을 안전하게 진행할 수 있는 완전한 기반이 마련되었습니다.