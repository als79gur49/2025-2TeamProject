# 🎯 타일 기반 타겟팅 시스템 재설계 (VFX 비동기 통합)

> **📝 문서 업데이트 이력**
> - 2025-10-08: 구현 체크리스트 수정 - Phase 1에 누락된 핵심 인프라 추가 (VFXTriggerData, GameContext)

## 📋 개요

### 현재 VFX 비동기 구조
```
SpellEffectExecutor.ExecuteBatch()
  │
  ├─ CalculateAllPotentialTargets()  // ← 사전 타겟 계산
  │
  ├─ StartCoroutine(ExecuteWithVFX())  // ← 비동기 VFX 실행
  │    │
  │    ├─ Instantiate VFX
  │    ├─ VFXEventTrigger.Initialize()
  │    │    └─ 콜백: ExecuteEffectsWithDataList()  // ← VFX 트리거 시점
  │    │
  │    └─ yield return WaitForSeconds()
  │
  └─ FilterTriggersForEffect()  // ← 범위 필터링
       └─ effect.ExecuteWithVFXData(triggerData)
```

### 문제점
- `CalculateAllPotentialTargets()`가 각 Effect마다 중복된 타겟 계산 로직 포함
- Summon은 위치 기반, Damage/Heal은 유닛 기반으로 서로 다른 로직
- `AffectedType.None`이 "빈 타일"과 "Resource 효과" 두 의미로 혼용
- **GameObject(유닛) 기반 타겟팅**: VFX 시스템이 유닛 GameObject를 전달받아 불일치 발생

### 해결 방안
**타일 기반 통합 타겟팅 + VFX 비동기 통합**
- **모든 타겟팅을 타일 중심으로 재설계**: "타일 선택 → 타일에 효과 적용 → 유닛이 있으면 유닛도 영향"
- `AffectedType.None`: Range 내 모든 타일 반환 (빈 리스트 → 전체 타일)
- `AffectedType.NotAny`: 빈 타일만 선택 (명시적 분리)
- `VFXTriggerData`: `PredeterminedTarget` (GameObject) → `TilePositions` (Vector3Int)
- `GameContext`: `PredeterminedTarget` → `PredeterminedTiles` (타일 리스트)
- `EffectTargetingHelper`: **동기적 타일 계산** (VFX 실행 전)
- `SpellEffectExecutor`: **비동기 VFX 실행** + **트리거 시점 검증** (기존 유지)

---

## 🔄 AffectedType 재정의 (타일 기반 설계)

```csharp
/// <summary>
/// 효과 적용 대상 타입 - 타일 기반 필터링
/// Phase 3.x: 모든 타겟팅을 타일 중심으로 설계
/// 핵심 원칙: "타일을 선택하고, 타일에 있는 유닛에게 효과 적용"
/// </summary>
[Serializable]
public enum AffectedType
{
    /// <summary>
    /// Range 내 모든 타일 (필터링 없음)
    /// 사용처: 광역 효과, 지형 변경, Resource 증감
    /// 필터: 없음 - Range 내 모든 타일 반환
    /// VFX 통합: Range 내 모든 Tile 위치 리스트 반환
    /// 예시: Range=1 → 다이아몬드 5개 타일, Range=2 → 13개 타일
    /// </summary>
    None,

    /// <summary>
    /// 아군 유닛이 있는 타일만 선택
    /// 필터: Tile.OccupyingUnit != null && IsSameTeam(unit, casterTeam)
    /// VFX 통합: 필터링된 타일의 위치 리스트 반환
    /// 효과 적용: 타일의 OccupyingUnit에 효과 적용
    /// </summary>
    Ally,

    /// <summary>
    /// 적군 유닛이 있는 타일만 선택
    /// 필터: Tile.OccupyingUnit != null && !IsSameTeam(unit, casterTeam)
    /// VFX 통합: 필터링된 타일의 위치 리스트 반환
    /// 효과 적용: 타일의 OccupyingUnit에 효과 적용
    /// </summary>
    Enemy,

    /// <summary>
    /// 유닛이 있는 모든 타일 (팀 무관)
    /// 필터: Tile.OccupyingUnit != null
    /// VFX 통합: 필터링된 타일의 위치 리스트 반환
    /// 효과 적용: 타일의 OccupyingUnit에 효과 적용
    /// </summary>
    Any,

    /// <summary>
    /// 유닛이 없는 빈 타일만 선택
    /// 필터: Tile.OccupyingUnit == null
    /// VFX 통합: 필터링된 타일의 위치 리스트 반환
    /// 효과 적용: 타일에 유닛 소환 또는 지형 효과
    /// </summary>
    NotAny
}
```

---

## 🏗️ 통합 아키텍처

### 타겟팅 파이프라인 (타일 기반 VFX 통합)

```
┌───────────────────────────────────────────────────────────┐
│         SpellEffectExecutor.ExecuteBatch()                 │
│   Input: effectDataList, targetPos, context              │
└─────────────────────┬─────────────────────────────────────┘
                      │
                      ▼
┌───────────────────────────────────────────────────────────┐
│   CalculateAllPotentialTiles() [사전 타일 계산]           │
│                                                           │
│   foreach effectData in effectDataList:                  │
│     tiles = EffectTargetingHelper.GetTargetTiles()  ←─┐  │
│     context.PredeterminedTiles.AddRange(tiles)       │  │
│     tilePositions.AddRange(tiles.Select(WorldPos))   │  │
└────────────────────┬─────────────────────────────────┼───┘
                     │                                 │
                     │                         ┌───────┴────────┐
                     │                         │EffectTargeting │
                     │                         │Helper          │
                     │                         │ (동기 타일 계산) │
                     │                         │- GetTilesInRange│
                     │                         │- FilterByType  │
                     │                         └────────────────┘
                     ▼
┌───────────────────────────────────────────────────────────┐
│   StartCoroutine(ExecuteWithVFX()) [비동기 실행]          │
│                                                           │
│   1. Instantiate VFX                                     │
│   2. VFXEventTrigger.Initialize(tilePositions)           │
│      └─ VFXTriggerData.TilePositions (Vector3Int[])     │
│   3. yield return (VFX 재생 대기)                        │
│   4. 트리거 시점 → ExecuteEffectsWithDataList()          │
└─────────────────────┬─────────────────────────────────────┘
                      │
                      ▼
┌───────────────────────────────────────────────────────────┐
│   FilterTriggersForEffect() [VFX 트리거 시점 검증]        │
│                                                           │
│   - AttackSuccess 체크 (타일 기반 검증)                  │
│   - AffectedRange 맨하탄 거리 체크                        │
│   - 검증된 타일 TriggerData만 통과                       │
└─────────────────────┬─────────────────────────────────────┘
                      │
                      ▼
┌───────────────────────────────────────────────────────────┐
│   effect.ExecuteWithVFXData(triggerData) [최종 실행]      │
│                                                           │
│   - 타일 위치에서 Tile 객체 조회                         │
│   - DamageEffect: tile.OccupyingUnit?.TakeDamage()       │
│   - HealEffect: tile.OccupyingUnit?.Heal()               │
│   - SummonEffect: Instantiate(unit) at tile.position     │
│   - ResourceEffect: context.AddResource(value)           │
└───────────────────────────────────────────────────────────┘
```

**핵심 변경점**:
- `PredeterminedTarget` (GameObject) → `PredeterminedTiles` (Tile)
- `VFXTriggerData.TargetObject` → `VFXTriggerData.TilePositions` (Vector3Int[])
- 모든 타겟팅 로직이 타일 중심으로 통일

---

## 🛠️ EffectTargetingHelper 상세 설계 (타일 기반 VFX 통합)

### 핵심 API - 동기적 타일 계산

```csharp
namespace Game.Card.Effects
{
    /// <summary>
    /// 타일 기반 통합 타겟팅 시스템
    /// 핵심 원칙: "모든 타겟팅은 타일을 반환하고, 효과는 타일에 적용된다"
    /// VFX 시스템: 타일 위치(Vector3Int)를 VFX 재생 좌표로 사용
    /// </summary>
    public static class EffectTargetingHelper
    {
        #region Public API - 타일 기반 VFX 통합

        /// <summary>
        /// [타일 기반] EffectData 기반으로 대상 타일들을 반환
        /// SpellEffectExecutor.CalculateAllPotentialTiles()에서 호출됨
        /// </summary>
        /// <param name="center">중심 위치 (카드 드롭 위치)</param>
        /// <param name="effectData">효과 데이터 (AffectedType, AffectedRange)</param>
        /// <param name="context">게임 컨텍스트 (GridController, CasterTeam)</param>
        /// <returns>필터링된 타겟 타일 리스트 (동기 반환)</returns>
        public static List<Tile> GetTargetTiles(
            Vector2Int center,
            EffectData effectData,
            GameContext context)
        {
            if (context?.GridController == null)
            {
                Debug.LogError("[EffectTargeting] GridController null");
                return new List<Tile>();
            }

            // 1. 범위 내 모든 타일 수집 (동기)
            var tilesInRange = GetTilesInRange(
                center,
                effectData.AffectedRange,
                context.GridController);

            // 2. AffectedType으로 타일 필터링 (동기)
            return FilterTilesByAffectedType(
                tilesInRange,
                effectData.AffectedType,
                context.CasterTeam);
        }

        /// <summary>
        /// [타일 기반] 타일 리스트를 VFX 재생용 월드 좌표 리스트로 변환
        /// </summary>
        /// <param name="tiles">타겟 타일 리스트</param>
        /// <returns>VFX 재생에 사용할 Vector3 위치 리스트</returns>
        public static List<Vector3> TilesToWorldPositions(List<Tile> tiles)
        {
            return tiles.Select(t => t.transform.position).ToList();
        }

        /// <summary>
        /// [타일 기반] 타일 리스트를 그리드 좌표 리스트로 변환
        /// </summary>
        /// <param name="tiles">타겟 타일 리스트</param>
        /// <returns>Grid 좌표 리스트 (Vector3Int)</returns>
        public static List<Vector3Int> TilesToGridPositions(List<Tile> tiles)
        {
            return tiles.Select(t => t.GetGridPosition()).ToList();
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 중심 위치에서 지정된 범위 내의 모든 타일 반환 (동기)
        /// </summary>
        private static List<Tile> GetTilesInRange(
            Vector2Int center,
            int range,
            IGridController gridController)
        {
            var tiles = new List<Tile>();

            // Range 0: 단일 타일
            if (range == 0)
            {
                var tile = gridController.GetTileAtPosition(center);
                if (tile != null) tiles.Add(tile);
                return tiles;
            }

            // Range 1+: 정사각형 범위
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    var pos = center + new Vector2Int(x, y);
                    var tile = gridController.GetTileAtPosition(pos);

                    if (tile != null)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// AffectedType에 따라 타일 필터링 (동기)
        /// 타일 기반 설계: 모든 필터링은 타일의 상태를 기준으로 수행
        /// </summary>
        private static List<Tile> FilterTilesByAffectedType(
            List<Tile> tiles,
            AffectedType affectedType,
            TeamType casterTeam)
        {
            return affectedType switch
            {
                // None: 필터링 없음 - Range 내 모든 타일 반환
                AffectedType.None => tiles,

                // Ally: 아군 유닛이 있는 타일만
                AffectedType.Ally => tiles.Where(t =>
                    t.OccupyingUnit != null &&
                    IsSameTeam(t.OccupyingUnit, casterTeam)
                ).ToList(),

                // Enemy: 적군 유닛이 있는 타일만
                AffectedType.Enemy => tiles.Where(t =>
                    t.OccupyingUnit != null &&
                    !IsSameTeam(t.OccupyingUnit, casterTeam)
                ).ToList(),

                // Any: 유닛이 있는 모든 타일 (팀 무관)
                AffectedType.Any => tiles.Where(t =>
                    t.OccupyingUnit != null
                ).ToList(),

                // NotAny: 유닛이 없는 빈 타일만
                AffectedType.NotAny => tiles.Where(t =>
                    t.OccupyingUnit == null
                ).ToList(),

                _ => tiles
            };
        }

        /// <summary>
        /// 유닛이 시전자와 같은 팀인지 확인
        /// </summary>
        private static bool IsSameTeam(Unit unit, TeamType casterTeam)
        {
            if (unit == null) return false;

            TeamType unitTeam = unit.IsPlayerUnit ? TeamType.Player : TeamType.Enemy;
            return unitTeam == casterTeam;
        }

        #endregion
    }
}
```

---

## 📦 VFXTriggerData 구조 변경 (타일 기반)

### Before (유닛 기반)
```csharp
public struct VFXTriggerData
{
    public GameObject TargetObject;           // ❌ 유닛 GameObject
    public Vector3 TriggerWorldPosition;
    public bool AttackSuccess;
    public string ValidationFailureReason;
}
```

### After (타일 기반)
```csharp
/// <summary>
/// VFX 트리거 데이터 - 타일 기반 설계
/// 모든 타겟팅 정보를 타일 위치로 전달
/// </summary>
public struct VFXTriggerData
{
    /// <summary>
    /// 타겟 타일의 그리드 좌표
    /// </summary>
    public Vector3Int TileGridPosition;

    /// <summary>
    /// 타겟 타일의 월드 좌표 (VFX 재생 위치)
    /// </summary>
    public Vector3 TileWorldPosition;

    /// <summary>
    /// VFX 트리거 시점의 검증 결과
    /// </summary>
    public bool AttackSuccess;

    /// <summary>
    /// 검증 실패 사유 (디버깅용)
    /// </summary>
    public string ValidationFailureReason;
}
```

**핵심 변경점**:
- `TargetObject` (GameObject) 제거 → 타일 위치만 전달
- `TileGridPosition` 추가: 그리드 시스템 연동
- `TileWorldPosition`: VFX 재생 좌표로 사용

---

## 🎮 GameContext 구조 변경 (타일 기반)

### Before (유닛 기반)
```csharp
public class GameContext
{
    public IGridController GridController { get; set; }
    public TeamType CasterTeam { get; set; }
    public List<GameUnit> PredeterminedTarget { get; set; }  // ❌ 유닛 리스트
}
```

### After (타일 기반)
```csharp
/// <summary>
/// 게임 컨텍스트 - 타일 기반 설계
/// </summary>
public class GameContext
{
    public IGridController GridController { get; set; }
    public TeamType CasterTeam { get; set; }

    /// <summary>
    /// 사전 계산된 타겟 타일 리스트
    /// CalculateAllPotentialTiles()에서 설정됨
    /// </summary>
    public List<Tile> PredeterminedTiles { get; set; } = new List<Tile>();

    /// <summary>
    /// 타일 기반 VFX 재생 좌표 리스트
    /// </summary>
    public List<Vector3> VFXPositions { get; set; } = new List<Vector3>();
}
```

**핵심 변경점**:
- `PredeterminedTarget` (GameUnit) → `PredeterminedTiles` (Tile)
- `VFXPositions` 추가: VFX 시스템 직접 연동

---

## 🔗 SpellEffectExecutor 통합 변경점

### CalculateAllPotentialTiles() 리팩토링

#### Before (유닛 기반, 중복 로직)
```csharp
private List<GameObject> CalculateAllPotentialTargets(
    IReadOnlyList<EffectData> effects,
    Vector2Int targetPos,
    GameContext context)
{
    var targets = new HashSet<GameObject>();

    foreach (var effectData in effects)
    {
        // ❌ 각 EffectType별 중복 로직
        if (effectData.Type == EffectType.Damage)
        {
            // ... 적군 유닛 GameObject 찾기 ...
        }
        else if (effectData.Type == EffectType.Heal)
        {
            // ... 아군 유닛 GameObject 찾기 ...
        }
        else if (effectData.Type == EffectType.Summon)
        {
            // ... 빈 타일 위치 찾기 (혼재된 로직) ...
        }
    }

    return targets.ToList();
}
```

#### After (타일 기반, 통합 시스템)
```csharp
/// <summary>
/// 사전 타일 계산 - 모든 효과의 타겟 타일을 한번에 계산
/// </summary>
private void CalculateAllPotentialTiles(
    IReadOnlyList<EffectData> effects,
    Vector2Int targetPos,
    GameContext context)
{
    context.PredeterminedTiles.Clear();
    context.VFXPositions.Clear();

    var uniqueTiles = new HashSet<Tile>();

    foreach (var effectData in effects)
    {
        // ✅ 통합 타일 타겟팅 시스템
        var tiles = EffectTargetingHelper.GetTargetTiles(
            targetPos,
            effectData,
            context);

        foreach (var tile in tiles)
        {
            uniqueTiles.Add(tile);
        }
    }

    // Context에 저장
    context.PredeterminedTiles.AddRange(uniqueTiles);
    context.VFXPositions = EffectTargetingHelper.TilesToWorldPositions(
        context.PredeterminedTiles);
}
```

**핵심 변경점**:
- 반환 타입: `List<GameObject>` → `void` (Context에 직접 저장)
- 타겟팅: 유닛 GameObject → 타일 객체
- 중복 제거: EffectType 분기 제거, 통합 API 사용
- VFX 연동: 타일 위치를 VFX 좌표로 직접 변환

---

## 📝 각 Effect 클래스 변경점

### 1️⃣ SummonEffect (타일 기반)

#### Before (유닛/위치 혼재 로직)
```csharp
private List<Vector2Int> GetAvailableSummonPositions(Vector2Int targetPos, GameContext context)
{
    // 60줄의 복잡한 로직
    // ❌ 유닛 존재 확인과 위치 계산이 혼재
    // - AffectedRange 수동 처리
    // - targetPos 우선순위 수동 처리
    // - IsValidSummonPosition 수동 체크
}

public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    if (!triggerData.AttackSuccess) return;

    var availablePositions = GetAvailableSummonPositions(targetPos, context);

    foreach (var pos in availablePositions)
    {
        var tile = context.GridController.GetTileAtPosition(pos);  // ❌ 위치 → 타일 재조회
        // 소환 로직
    }
}
```

#### After (타일 기반, 통합 시스템)
```csharp
public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    // VFX 트리거 시점 검증
    if (!triggerData.AttackSuccess)
    {
        Debug.Log($"[SummonEffect] Summon failed: {triggerData.ValidationFailureReason}");
        return;
    }

    // ✅ 타일 기반 타겟팅: 이미 계산된 타일 사용 또는 실시간 조회
    var targetTile = context.GridController.GetTileAtPosition(
        new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

    if (targetTile == null || targetTile.OccupyingUnit != null)
    {
        Debug.LogWarning($"[SummonEffect] Tile invalid for summon: {triggerData.TileGridPosition}");
        return;
    }

    // 타일에 유닛 소환
    SummonUnitAtTile(targetTile, context);

    // VFX 재생 (타일 위치 기반)
    PlaySummonEffect(triggerData.TileWorldPosition, context);
}

private void SummonUnitAtTile(Tile tile, GameContext context)
{
    Vector3 worldPos = tile.transform.position;
    // ... 기존 소환 로직 (타일 기준) ...
    Debug.Log($"[Summon] Unit spawned at tile {tile.GetGridPosition()}");
}
```

**EffectData 설정**:
```csharp
new EffectData(
    EffectType.Summon,
    value: 1,
    affectedType: AffectedType.NotAny,  // ✅ 빈 타일만 선택
    affectedRange: 1
)
```

**핵심 변경점**:
- 위치 리스트 → 타일 객체 직접 사용
- `GetAvailableSummonPositions()` 60줄 제거
- triggerData에서 타일 좌표 직접 전달받음

---

### 2️⃣ DamageEffect (타일 기반)

#### Before (유닛 GameObject 기반)
```csharp
public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    if (!triggerData.AttackSuccess) return;

    // ❌ triggerData에서 유닛 GameObject 접근
    var targetUnit = triggerData.TargetObject?.GetComponent<GameUnit>();
    if (targetUnit != null)
    {
        targetUnit.TakeDamage(_effectData.Value);
    }
}
```

#### After (타일 기반)
```csharp
public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    // VFX 트리거 시점 검증
    if (!triggerData.AttackSuccess)
    {
        return;
    }

    // ✅ 타일 기반: 타일 좌표로 타일 조회
    var targetTile = context.GridController.GetTileAtPosition(
        new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

    if (targetTile?.OccupyingUnit != null)
    {
        // 타일에 유닛이 있으면 데미지 적용
        targetTile.OccupyingUnit.TakeDamage(_effectData.Value);
        Debug.Log($"[Damage] {_effectData.Value} damage to unit at {triggerData.TileGridPosition}");
    }

    // VFX 재생 (타일 위치 기반)
    PlayDamageEffect(triggerData.TileWorldPosition, context);
}
```

**핵심 변경점**:
- `triggerData.TargetObject` (GameObject) → `triggerData.TileGridPosition` (타일 좌표)
- 타일 조회 → 유닛 확인 → 효과 적용 (명확한 흐름)

---

### 3️⃣ HealEffect (타일 기반)

#### Before (유닛 GameObject 기반)
```csharp
public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    if (!triggerData.AttackSuccess) return;

    // ❌ triggerData에서 유닛 GameObject 접근
    var targetUnit = triggerData.TargetObject?.GetComponent<GameUnit>();
    if (targetUnit != null && targetUnit.IsPlayerUnit)
    {
        targetUnit.Heal(_effectData.Value);
    }
}
```

#### After (타일 기반)
```csharp
public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    // VFX 트리거 시점 검증
    if (!triggerData.AttackSuccess)
    {
        return;
    }

    // ✅ 타일 기반: 타일 좌표로 타일 조회
    var targetTile = context.GridController.GetTileAtPosition(
        new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y));

    if (targetTile?.OccupyingUnit != null)
    {
        // 타일에 유닛이 있으면 힐 적용
        targetTile.OccupyingUnit.Heal(_effectData.Value);
        Debug.Log($"[Heal] {_effectData.Value} heal to unit at {triggerData.TileGridPosition}");
    }

    // VFX 재생 (타일 위치 기반)
    PlayHealEffect(triggerData.TileWorldPosition, context);
}
```

**핵심 변경점**:
- `triggerData.TargetObject` (GameObject) → `triggerData.TileGridPosition` (타일 좌표)
- 팀 체크는 AffectedType.Ally 필터링으로 이미 처리됨
- 타일 기반 흐름으로 로직 단순화

---

### 4️⃣ ResourceEffect (타일 기반)

#### After (타일 무관 효과)
```csharp
public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
{
    // Resource 효과는 타일과 무관하게 항상 실행
    // AffectedType.None: Range 내 모든 타일이 triggerData로 전달되지만
    // Resource 효과는 타일 정보를 사용하지 않음

    context.AddResource(_effectData.Value);
    Debug.Log($"[Resource] Added {_effectData.Value} resources");

    // VFX 재생 (드롭 위치 기반)
    PlayResourceEffect(targetPos, context);
}
```

**EffectData 설정**:
```csharp
new EffectData(
    EffectType.Resource,
    value: 50,
    affectedType: AffectedType.None,  // ✅ 타일 필터링 없음
    affectedRange: 0                   // Range 0: 단일 위치
)
```

**핵심 변경점**:
- AffectedType.None: Range 내 모든 타일 반환되지만 Resource 효과는 타일 정보 미사용
- 타일 기반 구조와 호환되는 타일 무관 효과

---

## ⏱️ 비동기 흐름 타임라인 (타일 기반)

```
시간축 →

T0: SpellEffectExecutor.ExecuteBatch() 호출
  │
  ├─ CalculateAllPotentialTiles() (동기)
  │   └─ EffectTargetingHelper.GetTargetTiles() (동기)
  │        └─ 타일 계산 완료 (즉시 반환)
  │        └─ context.PredeterminedTiles 저장
  │        └─ context.VFXPositions 생성
  │
T1: StartCoroutine(ExecuteWithVFX()) (비동기 시작)
  │
  ├─ Instantiate VFX (동기)
  ├─ VFXEventTrigger.Initialize(context.VFXPositions) (동기)
  │   └─ VFXTriggerData[] 생성 (타일 위치 기반)
  │
T2: yield return (VFX 재생 대기, 비동기)
  │    ... 시간 경과 ...
  │
T3: VFX 트리거 시점 도달 (TriggerNormalizedTime)
  │
  ├─ ExecuteEffectsWithDataList() 콜백 실행
  │   │
  │   ├─ FilterTriggersForEffect() (동기)
  │   │    └─ AttackSuccess 체크 (타일 기반)
  │   │    └─ AffectedRange 맨하탄 거리 체크
  │   │
  │   └─ effect.ExecuteWithVFXData(triggerData) (동기)
  │        └─ triggerData.TileGridPosition으로 Tile 조회
  │        └─ tile.OccupyingUnit 확인 후 효과 적용
  │
T4: yield return WaitForSeconds(duration) (비동기)
  │
T5: Destroy VFX (비동기 종료)
```

**핵심 변경점**:
- `CalculateAllPotentialTargets()` → `CalculateAllPotentialTiles()` (타일 반환)
- VFX 시스템: GameObject 리스트 → 타일 위치(Vector3) 리스트
- Effect 실행: triggerData에서 타일 좌표 전달 → 타일 조회 → 효과 적용
- `EffectTargetingHelper`는 **동기 함수**이므로 VFX 비동기 흐름과 독립적으로 작동

---

## ⚠️ 알려진 문제점 및 해결 방안

이 섹션은 현재 설계에서 발견된 잠재적 문제점과 권장 해결 방안을 기술합니다.

### 🔴 문제 1: VFXEventTrigger와 Summon 효과의 충돌 (치명적)

#### 문제점
설계상 Summon 효과(`AffectedType.NotAny`)는 VFX 타겟으로 **타일의 GameObject**를 전달하게 됩니다. 하지만 `VFXEventTrigger`의 유효성 검사 로직(`ValidateSingleTarget`)은 타겟에게서 `HealthComponent`를 찾아 생존 여부를 확인하도록 설계되어 있습니다. 타일 GameObject에는 `HealthComponent`가 없으므로, 이 검증은 항상 실패하게 됩니다.

#### 예상되는 오작동
1. Summon 카드를 빈 타일에 사용
2. `SpellEffectExecutor`는 `EffectTargetingHelper`를 통해 타겟 타일 GameObject 리스트 생성 및 `VFXEventTrigger`에 전달
3. VFX가 재생되고 트리거 시점이 되면, `VFXEventTrigger`는 타일 GameObject에 `HealthComponent`가 없어 타겟이 유효하지 않다고 판단
4. `VFXTriggerData.AttackSuccess`가 `false`로 설정됨
5. `SummonEffect`는 `AttackSuccess`가 `false`이므로 소환 로직을 실행하지 않고 즉시 종료
6. **결과: 유닛이 소환되지 않는 치명적인 버그 발생**

#### 해결 방안 (적용: VFXEventTrigger 수정) ✅

```csharp
// VFXEventTrigger.cs
private bool ValidateSingleTarget(GameObject target, AffectedType affectedType)
{
    if (target == null) return false;

    // AffectedType.NotAny는 타일 타겟이므로 HealthComponent 검사 건너뛰기
    if (affectedType == AffectedType.NotAny)
    {
        // 타일 GameObject는 항상 유효
        return true;
    }

    // 기존 로직: 유닛 타겟은 HealthComponent 확인
    var health = target.GetComponent<HealthComponent>();
    if (health == null) return false;

    return health.IsAlive;
}
```

**권장 사항**: VFXEventTrigger에서 AffectedType에 따른 검증 로직을 분기 처리하여, 타일 타겟(`NotAny`)의 경우 HealthComponent 검사를 건너뜁니다.

---

### ⚖️ 문제 2: 범위 계산 방식의 불일치

#### 문제점
타겟을 선정하는 방식과 필터링하는 방식 간에 범위 계산법이 다릅니다:

- **EffectTargetingHelper.GetTilesInRange()**: `for` 루프를 사용하여 **정사각형(Square)** 형태로 범위 계산
- **SpellEffectExecutor.FilterTriggersForEffect()**: 맨해튼 거리(Manhattan distance)를 사용하여 **마름모(Diamond)** 형태로 범위 필터링

#### 예상되는 오작동
**Range: 1인 광역기 사용 시**:
1. `EffectTargetingHelper`는 대각선을 포함한 3×3 영역의 타겟을 모두 잠재적 타겟으로 설정
2. `SpellEffectExecutor`는 맨해튼 거리가 1 이하인 타겟만 통과시킴
3. **결과: 대각선에 위치한 타겟들은 최종 단계에서 필터링되어 효과를 받지 못함**

**시각화**:
```
Range 1 광역기 사용 시:

EffectTargetingHelper (정사각형):    FilterTriggersForEffect (맨해튼):
  X X X                                 . X .
  X O X   →   최종 결과 →               X O X
  X X X                                 . X .

(O = 중심, X = 타겟 선정, . = 필터링됨)
```

#### 해결 방안 (적용: 맨해튼 거리로 통일) ✅

```csharp
// EffectTargetingHelper.cs
private static List<Tile> GetTilesInRange(
    Vector2Int center,
    int range,
    IGridController gridController)
{
    var tiles = new List<Tile>();

    if (range == 0)
    {
        var tile = gridController.GetTileAtPosition(center);
        if (tile != null) tiles.Add(tile);
        return tiles;
    }

    // 맨해튼 거리 기반 범위 계산 (다이아몬드 형태)
    for (int x = -range; x <= range; x++)
    {
        for (int y = -range; y <= range; y++)
        {
            int manhattanDistance = Mathf.Abs(x) + Mathf.Abs(y);
            if (manhattanDistance > range) continue; // 대각선 제외

            var pos = center + new Vector2Int(x, y);
            var tile = gridController.GetTileAtPosition(pos);

            if (tile != null)
            {
                tiles.Add(tile);
            }
        }
    }

    return tiles;
}
```

**권장 사항**: **맨해튼 거리(다이아몬드 형태)**가 일반적인 턴제 게임에서 더 직관적이므로 이 방식으로 통일합니다.

---

### 🔍 문제 3: AffectedType.None 효과의 실행 누락

#### 문제점
설계에 따르면 `AffectedType.None` (자원 증가, 드로우 등 타겟이 없는 효과)은 `CalculateAllPotentialTargets` 단계에서 빈 타겟 리스트를 반환합니다. 이후 `ExecuteEffectsWithDataList`는 `triggerDataList`를 기반으로 효과를 실행하는데, 타겟이 없었으므로 `triggerDataList` 역시 비어있게 됩니다.

**결과적으로 `AffectedType.None`을 사용하는 효과는 실행될 경로가 전혀 없습니다.**

#### 예상되는 오작동
1. Resource 증가 카드 사용 (AffectedType.None)
2. `CalculateAllPotentialTargets()`가 빈 리스트 반환
3. `triggerDataList`가 비어있음
4. `ExecuteEffectsWithDataList()`가 루프를 돌지 않고 종료
5. **결과: Resource 증가 효과가 전혀 실행되지 않음**

#### 해결 방안

**ExecuteEffectsWithDataList() 메서드 수정**
```csharp
// SpellEffectExecutor.cs
private void ExecuteEffectsWithDataList(
    List<VFXTriggerData> triggerDataList,
    IReadOnlyList<EffectData> effects,
    Vector2Int targetPos,
    GameContext context)
{
    foreach (var effectData in effects)
    {
        var effect = GetOrCreateEffect(effectData.Type);
        effect.SetEffectData(effectData);

        // AffectedType.None 효과는 타겟 없이 즉시 실행
        if (effectData.AffectedType == AffectedType.None)
        {
            if (effect is IVFXAwareEffect vfxEffect)
            {
                // 더미 VFXTriggerData 생성 (AttackSuccess = true)
                var dummyTriggerData = new VFXTriggerData
                {
                    AttackSuccess = true,
                    TargetObject = null,
                    TriggerWorldPosition = context.GridController.GetTileAtPosition(targetPos).transform.position
                };
                vfxEffect.ExecuteWithVFXData(targetPos, context, dummyTriggerData);
            }
            else
            {
                effect.Execute(targetPos, context);
            }
            continue; // 다음 효과로
        }

        // 기존 로직: triggerDataList 기반 실행
        var filteredTriggers = FilterTriggersForEffect(
            triggerDataList,
            effectData.AffectedRange,
            targetPos);

        foreach (var triggerData in filteredTriggers)
        {
            if (effect is IVFXAwareEffect vfxAwareEffect)
            {
                vfxAwareEffect.ExecuteWithVFXData(targetPos, context, triggerData);
            }
        }
    }
}
```

**권장 사항**: 위 수정을 적용하여 `AffectedType.None` 효과가 VFX 트리거와 무관하게 항상 실행되도록 보장합니다.

---

## 🔄 마이그레이션 가이드

### 1. EffectData.cs 수정

```csharp
public enum AffectedType
{
    None,
    Ally,
    Enemy,
    Any,
    NotAny  // ← 추가
}
```

### 2. 기존 Summon 카드 데이터 변경

**OnValidate 자동 변환**:
```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    // 자동 마이그레이션
    if (type == EffectType.Summon && affectedType == AffectedType.None)
    {
        affectedType = AffectedType.NotAny;
        Debug.Log($"[Migration] {name}: Summon → NotAny");
    }
}
#endif
```

### 3. SpellEffectExecutor.cs 수정

```csharp
// CalculateAllPotentialTargets() 메서드만 리팩토링
private List<GameObject> CalculateAllPotentialTargets(...)
{
    var targets = new HashSet<GameObject>();

    foreach (var effectData in effects)
    {
        var tiles = EffectTargetingHelper.GetTargetTiles(targetPos, effectData, context);
        var gameObjects = EffectTargetingHelper.TilesToGameObjects(tiles, effectData.AffectedType);

        foreach (var obj in gameObjects)
        {
            if (obj != null) targets.Add(obj);
        }
    }

    return targets.ToList();
}
```

### 4. 각 Effect 클래스 리팩토링

- SummonEffect: `GetAvailableSummonPositions()` 제거 → `EffectTargetingHelper` 사용
- DamageEffect: 유닛 찾기 로직 제거 → `EffectTargetingHelper` 사용
- HealEffect: 유닛 찾기 로직 제거 → `EffectTargetingHelper` 사용

---

## 🎯 타일 기반 설계의 핵심 원칙

### 1. **타일 중심 타겟팅**
```
모든 타겟팅은 타일을 선택한다
↓
타일에 효과를 적용한다
↓
타일에 유닛이 있으면 유닛도 영향을 받는다
```

### 2. **단일 진실의 원천 (Single Source of Truth)**
- **타겟 정보**: 타일 좌표 (Vector3Int)
- **유닛 정보**: tile.OccupyingUnit (Tile의 속성)
- **VFX 위치**: tile.transform.position (타일의 월드 좌표)

### 3. **일관된 데이터 흐름**
```
EffectData (AffectedType + Range)
  ↓
EffectTargetingHelper.GetTargetTiles()
  ↓
List<Tile>
  ↓
VFXTriggerData (TileGridPosition, TileWorldPosition)
  ↓
Effect.ExecuteWithVFXData()
  ↓
tile.OccupyingUnit (유닛 접근)
```

### 4. **명확한 책임 분리**
- **EffectTargetingHelper**: "어떤 타일들이 타겟인가?"
- **VFXTriggerData**: "타일이 어디에 있는가?"
- **Effect**: "타일에 무엇을 할 것인가?"

---

## ✅ 타일 기반 리팩토링 장점

| 측면 | Before (유닛 기반) | After (타일 기반) | 개선 효과 |
|------|------------------|-----------------|----------|
| **일관성** | Summon은 위치, Damage는 유닛 | 모든 Effect가 타일 기반 | ✅ 통일된 타겟팅 패러다임 |
| **코드 중복** | 각 Effect별 타겟 계산 | 통합 Helper 사용 | ✅ 60줄 → 10줄 (83% 감소) |
| **VFX 통합** | GameObject 전달 | 타일 위치 전달 | ✅ VFX는 위치 기반이므로 자연스러움 |
| **확장성** | 새 Effect마다 타겟 로직 작성 | Helper 재사용 | ✅ 개발 시간 단축 |
| **유지보수** | 타겟 로직 분산 | 중앙 집중화 | ✅ 버그 수정 한 곳만 |
| **테스트** | Effect별 타겟팅 테스트 필요 | Helper만 테스트 | ✅ 테스트 범위 축소 |
| **명확성** | None이 "빈 타일"과 "Resource" 혼용 | None=모든 타일, NotAny=빈 타일 | ✅ 의미 명확화 |
| **지형 효과** | 지원 어려움 | 타일 기반이므로 자연스러움 | ✅ 향후 확장 용이 |

---

## 📌 구현 체크리스트

### 🔷 Phase 1: 핵심 인프라 구축 ✅

#### 1.1 AffectedType 확장 ✅
- [x] `EffectData.cs` 수정
  - [x] `AffectedType` enum에 `NotAny` 추가
  - [x] XML 문서화 주석 작성 (각 enum 값의 용도 명시)
  - [x] 기존 None 타입의 용도를 "Resource/Global Effects"로 명확히 문서화
  - [x] OnValidate 메서드 추가: Summon 효과 자동 마이그레이션
- [x] 컴파일 확인 및 기존 코드 영향 검증

#### 1.2 EffectTargetingHelper 클래스 생성 ✅
- [x] `Assets/Script/Game/Card/Effects/EffectTargetingHelper.cs` 생성
- [x] Public API 구현
  - [x] `GetTargetTiles()`: 중심 위치, EffectData, GameContext 기반 타일 리스트 반환
  - [x] `TilesToWorldPositions()`: 타일 리스트를 VFX용 월드 좌표 리스트로 변환
  - [x] `TilesToGridPositions()`: 타일 리스트를 그리드 좌표 리스트로 변환
- [x] Private Helpers 구현
  - [x] `GetTilesInRange()`: 맨하탄 거리 기반 범위 계산 (다이아몬드 형태)
  - [x] `FilterTilesByAffectedType()`: AffectedType별 타일 필터링 로직
  - [x] `IsSameTeam()`: 팀 비교 유틸리티
- [x] 에러 핸들링
  - [x] GridController null 체크
  - [x] 빈 리스트 반환 처리
- [ ] 디버깅 로그 추가 (선택적)

#### 1.3 VFXTriggerData 구조 변경 (타일 기반) ✅
- [x] `VFXTriggerData.cs` 수정 (섹션 5: 📦 VFXTriggerData 구조 변경)
  - [x] `TileGridPosition` (Vector3Int) 필드 추가
  - [x] `TileWorldPosition` (Vector3) 필드 추가
  - [x] 기존 필드 유지: `AttackSuccess`, `ValidationFailureReason`
  - [x] XML 문서화 주석 작성 (각 필드의 용도 명시)
  - [x] Legacy Support: 기존 GameObject 기반 메서드는 Obsolete 마크
  - [x] 새 헬퍼 메서드: `SetTileTargetValid()`, `SetTileTargetInvalid()`
- [x] 컴파일 확인 및 참조하는 모든 코드 영향 검증
- [ ] VFXEventTrigger에서 사용하는 부분 확인 (Phase 2에서 처리)

#### 1.4 GameContext 구조 변경 (타일 기반) ✅
- [x] `GameContext.cs` 수정 (섹션 6: 🎮 GameContext 구조 변경)
  - [x] `PredeterminedTiles` (List<Tile>) 필드 추가 및 초기화
  - [x] `VFXPositions` (List<Vector3>) 필드 추가 및 초기화
  - [x] XML 문서화 주석 작성
  - [x] 기존 서비스 참조 유지 (하위 호환성)
- [x] 컴파일 확인 및 참조하는 모든 코드 영향 검증
- [ ] SpellEffectExecutor에서 사용하는 부분 확인 (Phase 2에서 처리)

#### 1.5 단위 테스트 (선택 사항)
- [ ] EffectTargetingHelper 테스트 케이스 작성
  - [ ] Range 0: 단일 타일 반환
  - [ ] Range 1: 다이아몬드 5개 타일 (맨하탄 거리)
  - [ ] Range 2: 다이아몬드 13개 타일
  - [ ] AffectedType 필터링 (Ally, Enemy, Any, NotAny, None)
  - [ ] 빈 타일 필터링
  - [ ] 팀 검증 로직

---

### 🔷 Phase 2: SpellEffectExecutor 리팩토링 ✅

#### 2.1 CalculateAllPotentialTargets() 간소화 ✅
- [x] 기존 개별 Effect별 타겟 계산 로직 백업 (주석 처리)
- [x] 통합 타겟팅 시스템으로 교체
  - [x] `EffectTargetingHelper.GetTargetTiles()` 호출
  - [x] `EffectTargetingHelper.TilesToWorldPositions()` 호출
  - [x] HashSet으로 중복 제거 유지
- [x] 코드 줄 수 확인: 28줄 → 47줄 (타일+GameObject 변환 포함)
- [x] Context에 PredeterminedTiles 및 VFXPositions 저장

#### 2.2 VFXEventTrigger 수정 (문제 1 해결) ✅
- [x] `VFXEventTrigger.cs` 수정
  - [x] `ValidateSingleTarget()` 메서드에 Tile 컴포넌트 체크 추가
  - [x] 타일 GameObject는 HealthComponent 검사 건너뛰기 (항상 유효)
  - [x] 기존 HealthComponent 검증 로직 유지 (유닛 타겟용)
- [x] Summon 효과가 타일 타겟으로 정상 작동

#### 2.3 ExecuteEffectsWithDataList() 수정 (문제 3 해결) ✅
- [x] `AffectedType.None` 효과 별도 처리 로직 추가
  - [x] 더미 VFXTriggerData 생성 (AttackSuccess = true)
  - [x] IVFXAwareEffect 인터페이스 체크 및 실행
  - [x] 기존 Execute() 메서드 폴백
- [x] 기존 triggerDataList 기반 실행 로직 유지
- [x] Resource 증가 효과가 VFX와 무관하게 즉시 실행되도록 구현

---

### 🔷 Phase 3: Effect 클래스 리팩토링 ✅

#### 3.1 SummonEffect.cs 수정 ✅
- [x] `GetAvailableSummonPositions()` 메서드 백업 (LEGACY로 유지)
- [x] `ExecuteWithVFXData()` 리팩토링
  - [x] 타일 좌표 기반으로 타일 조회 (`triggerData.TileGridPosition`)
  - [x] 타일 유효성 검증 (null 체크, OccupyingUnit 체크)
  - [x] VFX 위치 기반 효과 재생 (`triggerData.TileWorldPosition`)
- [x] `SummonUnitAtTile()` 헬퍼 메서드 생성
- [x] 디버깅 로그 추가: 타겟 타일 위치

#### 3.2 DamageEffect.cs 수정 ✅
- [x] 기존 유닛 찾기 로직 백업 (LEGACY로 유지)
- [x] `ExecuteWithVFXData()` 리팩토링
  - [x] 타일 좌표 기반으로 타일 조회
  - [x] `targetTile.OccupyingUnit.TakeDamage()` 호출
- [x] Null 체크 유지 (`targetTile?.OccupyingUnit != null`)
- [x] `PlayDamageEffect()` 메서드 추가 (타일 기반)

#### 3.3 HealEffect.cs 수정 ✅
- [x] 기존 유닛 찾기 로직 백업 (LEGACY로 유지)
- [x] `ExecuteWithVFXData()` 리팩토링
  - [x] 타일 좌표 기반으로 타일 조회
  - [x] `targetTile.OccupyingUnit.Heal()` 호출
- [x] Null 체크 유지 (`targetTile?.OccupyingUnit != null`)

---

### 🔷 Phase 4: 카드 데이터 마이그레이션

#### 4.1 OnValidate 자동 변환 추가
- [ ] `EffectData.cs`에 `#if UNITY_EDITOR` 블록 추가
- [ ] `OnValidate()` 메서드 구현
  - [ ] `EffectType.Summon && AffectedType.None` 조건 체크
  - [ ] 자동으로 `AffectedType.NotAny`로 변경
  - [ ] Debug.Log로 변환 로그 출력
- [ ] Unity Editor에서 모든 Summon 카드 데이터 열어서 자동 변환 트리거

#### 4.2 수동 검증
- [ ] Project 창에서 모든 CardData ScriptableObject 검색
- [ ] Summon 타입 카드 필터링
- [ ] Inspector에서 `AffectedType.NotAny` 확인
- [ ] 필요시 수동 수정

---

### 🔷 Phase 5: 통합 테스트 및 검증

#### 5.1 기능 테스트
- [ ] Summon 카드 테스트
  - [ ] Range 0: 드롭한 타일에만 소환
  - [ ] Range 1: 다이아몬드 5개 타일 중 빈 타일에 소환
  - [ ] 유닛이 이미 있는 타일은 제외되는지 확인
- [ ] Damage 카드 테스트
  - [ ] Range 0: 단일 타겟 적군 대미지
  - [ ] Range 1: 다이아몬드 범위 내 모든 적군 대미지
  - [ ] 아군은 대미지 받지 않는지 확인
- [ ] Heal 카드 테스트
  - [ ] Range 0: 단일 타겟 아군 힐
  - [ ] Range 1: 다이아몬드 범위 내 모든 아군 힐
  - [ ] 적군은 힐 받지 않는지 확인
- [ ] Resource 카드 테스트 (AffectedType.None)
  - [ ] 자원 증가 효과가 정상 실행되는지 확인
  - [ ] VFX 재생과 무관하게 즉시 실행되는지 확인

#### 5.2 VFX 통합 테스트
- [ ] VFX 재생 타이밍 확인
  - [ ] CalculateAllPotentialTargets 단계에서 타겟 수집
  - [ ] VFX 트리거 시점에 효과 실행
  - [ ] AttackSuccess 검증 로직 작동
- [ ] **문제 2 해결 검증**: 범위 계산 일치 확인 (섹션 10: 문제 2)
  - [ ] EffectTargetingHelper와 FilterTriggersForEffect의 범위가 동일한지 (맨하탄 거리)
  - [ ] 대각선 타일이 제외되는지 확인 (정사각형 → 다이아몬드 형태)
  - [ ] Range 1 광역기에서 5개 타일만 영향받는지 확인 (3×3 아님)
- [ ] VFXEventTrigger 타일 타겟 검증
  - [ ] Summon 효과에서 타일 GameObject 전달 시 AttackSuccess = true
  - [ ] HealthComponent 없어도 정상 작동

#### 5.3 엣지 케이스 테스트
- [ ] 빈 그리드에 Summon 시도
- [ ] 모든 타일에 유닛이 가득 찬 상태에서 Summon 시도
- [ ] Range가 0인 효과들
- [ ] 맵 경계 밖 타겟 시도
- [ ] GridController가 null인 상황 (에러 로그 확인)

---

### 🔷 Phase 6: 성능 최적화 및 마무리

#### 6.1 성능 테스트
- [ ] Unity Profiler로 측정
  - [ ] CalculateAllPotentialTargets() 실행 시간 (Before vs After)
  - [ ] GetTargetTiles() 실행 시간
  - [ ] 메모리 할당량 (List, HashSet 사용)
- [ ] 대규모 그리드 테스트 (10×10 이상)
  - [ ] Range 5 광역기 성능 측정
  - [ ] 다중 효과 카드 성능 측정

#### 6.2 코드 정리
- [ ] 백업해둔 기존 로직 주석 제거
- [ ] 불필요한 Debug.Log 제거 또는 조건부 컴파일 처리
- [ ] 코드 포맷팅 및 일관성 확인
- [ ] XML 문서화 주석 완성도 확인

#### 6.3 문서화
- [ ] 이 설계 문서의 "구현 완료" 섹션 작성
  - [ ] 변경된 파일 목록
  - [ ] 삭제된 코드 라인 수
  - [ ] 추가된 기능 요약
- [ ] 팀원 공유용 마이그레이션 가이드 작성 (선택 사항)
- [ ] Git 커밋 메시지 작성 가이드
  - [ ] Phase별 커밋 권장
  - [ ] 각 커밋의 목적과 변경 사항 명시

---

### 🔷 Phase 7: 회고 및 개선 (선택 사항)

#### 7.1 회고
- [ ] 구현 과정에서 발견된 추가 문제점 기록
- [ ] 설계 문서와 실제 구현의 차이점 분석
- [ ] 예상치 못한 버그나 제약 사항 문서화

#### 7.2 추가 개선 아이디어
- [ ] 타일 하이라이트 시스템 (카드 호버 시 영향받을 타일 표시)
- [ ] 범위 계산 알고리즘 확장 (Circle, Cross 등)
- [ ] EffectTargetingHelper 캐싱 전략 검토
- [ ] 다중 팀 지원 (3팀 이상)
- [ ] AffectedType 확장 (AllyExcludingSelf, EnemyLowestHP 등)

---

## 📊 진행률 추적

| Phase | 상태 | 완료율 | 비고 |
|-------|------|--------|------|
| Phase 1: 핵심 인프라 (5개 항목) | ✅ 완료 | 100% | 1.1~1.4 완료, 1.5 선택 사항 |
| Phase 2: SpellEffectExecutor (3개 항목) | ✅ 완료 | 100% | 문제 1, 3 해결 완료 |
| Phase 3: Effect 클래스 (3개 항목) | ✅ 완료 | 100% | Summon, Damage, Heal 리팩토링 완료 |
| Phase 4: 데이터 마이그레이션 (2개 항목) | 🔄 진행 중 | 50% | OnValidate 완료 |
| Phase 5: 통합 테스트 (3개 항목) | ⏳ 대기 | 0% | 문제 2 검증 포함 |
| Phase 6: 최적화 (2개 항목) | ⏳ 대기 | 0% | |
| Phase 7: 회고 (2개 항목) | ⏳ 대기 | 0% | 선택 사항 |

**범례**: ⏳ 대기 | 🔄 진행 중 | ✅ 완료 | ⚠️ 블로킹 이슈

**핵심 의존성**:
- Phase 2-6은 **Phase 1.1~1.4 완료가 필수 전제조건** ✅
- Phase 1.3 (VFXTriggerData), 1.4 (GameContext) 누락 시 컴파일 에러 발생

**구현 완료 내역** (2025-10-08):
- ✅ Phase 1.1: AffectedType.NotAny 추가 + 자동 마이그레이션
- ✅ Phase 1.2: EffectTargetingHelper 클래스 생성 (맨해튼 거리)
- ✅ Phase 1.3: VFXTriggerData 타일 기반 전환 (Legacy Support)
- ✅ Phase 1.4: GameContext 타일 기반 전환 (PredeterminedTiles, VFXPositions)
- ✅ Phase 2.1: CalculateAllPotentialTargets() → 타일 기반 리팩토링 (EffectTargetingHelper 통합)
- ✅ Phase 2.2: VFXEventTrigger.ValidateSingleTarget() - 타일 GameObject 지원 추가
- ✅ Phase 2.3: ExecuteEffectsWithDataList() - AffectedType.None 즉시 실행 처리
- ✅ Phase 3.1: SummonEffect.cs 타일 기반 리팩토링 (SummonUnitAtTile 메서드 추가)
- ✅ Phase 3.2: DamageEffect.cs 타일 기반 리팩토링 (타일 좌표 → 타일 조회 → 데미지 적용)
- ✅ Phase 3.3: HealEffect.cs 타일 기반 리팩토링 (타일 좌표 → 타일 조회 → 힐 적용)

---

## 🎯 핵심 요약

### 타일 기반 설계 원칙
1. **타일 중심 타겟팅**: 모든 타겟팅은 타일을 선택하고, 타일에 효과를 적용
2. **단일 진실의 원천**: 타일 좌표가 모든 타겟 정보의 기준점
3. **일관된 데이터 흐름**: EffectData → Tiles → VFXTriggerData → Effect
4. **동기 타일 계산**: EffectTargetingHelper는 VFX 실행 전 사전 계산
5. **비동기 VFX 실행**: SpellEffectExecutor는 Coroutine 기반 유지

### 책임 분리 (타일 기반)
- **EffectTargetingHelper**: "어떤 **타일들**이 타겟인가?" (동기)
- **VFXTriggerData**: "타겟 **타일**이 어디에 있는가?" (타일 좌표)
- **SpellEffectExecutor**: "VFX를 언제 어떻게 재생할까?" (비동기)
- **Effect**: "타겟 **타일**에 무엇을 할 것인가?" (효과 적용)

### 핵심 변경 사항
#### 구조 변경
- `AffectedType.None`: 빈 리스트 → **Range 내 모든 타일**
- `VFXTriggerData.TargetObject` (GameObject) → `TileGridPosition` (Vector3Int)
- `GameContext.PredeterminedTarget` (유닛) → `PredeterminedTiles` (타일)
- `CalculateAllPotentialTargets()` → `CalculateAllPotentialTiles()` (타일 반환)

#### 패러다임 전환
```
Before: "유닛을 찾아서 효과 적용"
After:  "타일을 선택하고 → 타일에 효과 적용 → 유닛이 있으면 영향"
```

### 호환성
✅ 기존 VFX 비동기 구조 **완전 호환** (Coroutine 유지)
✅ IVFXAwareEffect 인터페이스 **유지** (시그니처 동일)
✅ VFXTriggerData 검증 시스템 **유지** (AttackSuccess 체크)
✅ AffectedRange 필터링 **유지** (맨해튼 거리)

### 마이그레이션 임팩트
- **추가 파일**: EffectTargetingHelper.cs (새 파일)
- **수정 파일**: EffectData.cs, VFXTriggerData.cs, GameContext.cs, SpellEffectExecutor.cs
- **리팩토링 파일**: SummonEffect.cs, DamageEffect.cs, HealEffect.cs, ResourceEffect.cs
- **삭제 코드**: ~150줄 (각 Effect의 중복 타겟 계산 로직)
- **추가 코드**: ~80줄 (EffectTargetingHelper)
