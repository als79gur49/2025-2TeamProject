# Team-Based Card System Refactoring

## 📋 개요

`bool isPlayerCard` → `TeamType casterTeam`으로 리팩토링하여 타입 안정성과 확장성을 개선했습니다.

## 🔄 변경 사항

### 1. GameContext.cs
**변경 전:**
```csharp
public int PlayerId { get; private set; }

public GameContext(
    IUnitService unitService,
    IGridController gridController,
    ICardSpawnService cardSpawnService,
    ISpawnValidator spawnValidator,
    int playerId,
    Vector2Int originPosition)
{
    PlayerId = playerId;
    // ...
}
```

**변경 후:**
```csharp
public TeamType CasterTeam { get; private set; }

public GameContext(
    IUnitService unitService,
    IGridController gridController,
    ICardSpawnService cardSpawnService,
    ISpawnValidator spawnValidator,
    TeamType casterTeam,
    Vector2Int originPosition)
{
    CasterTeam = casterTeam;
    // ...
}
```

### 2. ICardSpawnService.cs
**변경 전:**
```csharp
bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, bool isPlayerCard);
```

**변경 후:**
```csharp
using static Game.Interfaces.ITeamComponent;

bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, TeamType casterTeam);
```

### 3. CardSpawnService.cs
**변경 전:**
```csharp
public bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, bool isPlayerCard)
{
    Log($"🎯 Executing card: {cardData.CardName} at position {targetPosition} (Player: {isPlayerCard})");
    UpdateGameContext(isPlayerCard ? 0 : 1, targetPosition);
    // ...
}

private void UpdateGameContext(int playerId, Vector2Int originPosition)
{
    gameContext = new GameContext(unitService, gridController, this, spawnValidator, playerId, originPosition);
}
```

**변경 후:**
```csharp
public bool TryExecuteCard(CardData cardData, Vector2Int targetPosition, TeamType casterTeam)
{
    Log($"🎯 Executing card: {cardData.CardName} at position {targetPosition} (Team: {casterTeam})");
    UpdateGameContext(casterTeam, targetPosition);

    // ResourceManager는 아직 bool을 사용하므로 변환
    bool isPlayerCard = (casterTeam == TeamType.Player);
    if (resourceManager != null && !resourceManager.SpendResources(isPlayerCard, cardData.ManaCost))
    {
        // ...
    }
    // ...
}

private void UpdateGameContext(TeamType casterTeam, Vector2Int originPosition)
{
    gameContext = new GameContext(unitService, gridController, this, spawnValidator, casterTeam, originPosition);
}
```

### 4. TileDropHandler.cs
**변경 전:**
```csharp
bool spawnSuccess = cardSpawnService.TryExecuteCard(cardData, gridPosition, true);
bool spellSuccess = cardSpawnService.TryExecuteCard(cardData, gridPosition, true);
```

**변경 후:**
```csharp
using static Game.Interfaces.ITeamComponent;

bool spawnSuccess = cardSpawnService.TryExecuteCard(cardData, gridPosition, TeamType.Player);
bool spellSuccess = cardSpawnService.TryExecuteCard(cardData, gridPosition, TeamType.Player);
```

## ✅ 개선 효과

### 1. 타입 안정성 향상
```csharp
// 이전: bool로는 의도가 불명확
TryExecuteCard(cardData, pos, true);  // true가 뭐지?

// 현재: enum으로 명확한 의도 표현
TryExecuteCard(cardData, pos, TeamType.Player);  // 플레이어 팀!
```

### 2. 확장성 개선
```csharp
// TeamType enum 지원
public enum TeamType
{
    None = 0,
    Player = 1,
    Enemy = 2,
    Neutral = 3,    // ✅ 중립 팀 지원
    Ally = 4        // ✅ 동맹 팀 지원
}
```

### 3. SpellEffect에서 팀 비교 가능
```csharp
public class DamageEffect : IVFXAwareEffect
{
    public void Execute(Vector2Int targetPos, GameContext context)
    {
        var affectedUnits = GetAffectedUnits(targetPos, context);

        foreach (var unit in affectedUnits)
        {
            // ✅ 유닛의 팀 정보 가져오기
            var teamComponent = unit.GetComponent<ITeamComponent>();
            if (teamComponent == null) continue;

            // ✅ 시전자 팀과 대상 팀 비교
            if (teamComponent.Team == context.CasterTeam)
            {
                // 아군에게는 데미지 주지 않음
                Debug.Log($"Skipping damage to ally: {unit.name}");
                continue;
            }

            // 적에게만 데미지 적용
            ApplyDamageToUnit(unit, _effectData.Value, null);
        }
    }
}
```

### 4. HealEffect에서 아군/적군 구분
```csharp
public class HealEffect : IVFXAwareEffect
{
    public void Execute(Vector2Int targetPos, GameContext context)
    {
        var affectedUnits = GetAffectedUnits(targetPos, context);

        foreach (var unit in affectedUnits)
        {
            var teamComponent = unit.GetComponent<ITeamComponent>();
            if (teamComponent == null) continue;

            // ✅ 아군에게만 힐 적용
            if (teamComponent.Team == context.CasterTeam)
            {
                ApplyHealToUnit(unit, _effectData.Value);
            }
            else
            {
                Debug.Log($"Cannot heal enemy: {unit.name}");
            }
        }
    }
}
```

## 🎯 사용 예시

### 플레이어 카드 사용
```csharp
cardSpawnService.TryExecuteCard(fireballCard, enemyPosition, TeamType.Player);
// → context.CasterTeam = TeamType.Player
// → 적(TeamType.Enemy)에게 데미지
```

### 적 카드 사용
```csharp
cardSpawnService.TryExecuteCard(enemyCard, playerPosition, TeamType.Enemy);
// → context.CasterTeam = TeamType.Enemy
// → 플레이어(TeamType.Player)에게 데미지
```

### 중립 카드 사용 (향후 확장)
```csharp
cardSpawnService.TryExecuteCard(trapCard, position, TeamType.Neutral);
// → context.CasterTeam = TeamType.Neutral
// → 모든 팀에게 영향
```

## 📊 영향받은 파일

1. ✅ `GameContext.cs` - PlayerId → CasterTeam
2. ✅ `ICardSpawnService.cs` - 인터페이스 시그니처 변경
3. ✅ `CardSpawnService.cs` - 구현체 로직 변경
4. ✅ `TileDropHandler.cs` - 호출 코드 변경

## 🔜 다음 단계

### SpellEffect 구현체에 팀 검증 로직 추가
- `DamageEffect.cs`: 적에게만 데미지
- `HealEffect.cs`: 아군에게만 힐
- `SummonEffect.cs`: 시전자 팀으로 소환

### 예시: DamageEffect 개선
```csharp
private List<GameObject> GetAffectedUnits(Vector2Int targetPos, GameContext context)
{
    var units = new List<GameObject>();

    // 범위 내 모든 유닛 탐색
    foreach (var offset in _effectData.AreaPattern.GetCellOffsets())
    {
        Vector2Int checkPos = targetPos + offset;
        var unit = context.UnitService.GetUnitAt(checkPos);

        if (unit != null)
        {
            var teamComponent = unit.GetComponent<ITeamComponent>();

            // ✅ 적 팀만 대상으로 추가
            if (teamComponent != null && teamComponent.Team != context.CasterTeam)
            {
                units.Add(unit);
            }
        }
    }

    return units;
}
```

## 📊 영향받은 파일 (총 10개)

1. ✅ **GameContext.cs** - `PlayerId` → `CasterTeam`
2. ✅ **ICardSpawnService.cs** - 인터페이스 시그니처 변경
3. ✅ **CardSpawnService.cs** - 구현체 로직 변경
4. ✅ **TileDropHandler.cs** - 호출 코드 변경
5. ✅ **GridController.cs** - `GetAffectedUnits` 시그니처 변경 (`int playerId` → `TeamType casterTeam`)
6. ✅ **IGridManager.cs** - 인터페이스 업데이트
7. ✅ **DamageEffect.cs** - `context.PlayerId` → `context.CasterTeam`
8. ✅ **HealEffect.cs** - `context.PlayerId` → `context.CasterTeam`
9. ✅ **SummonEffect.cs** - `context.PlayerId` → `context.CasterTeam`
10. ✅ **SpellEffectExecutor.cs** - `context.PlayerId` → `context.CasterTeam`

## 🎉 결과

- **타입 안정성**: bool → TeamType enum
- **명확성**: isPlayerCard → CasterTeam
- **확장성**: 중립, 동맹 팀 지원 가능
- **팀 비교**: SpellEffect에서 시전자/대상 팀 비교 가능
- **일관성**: 전체 시스템에서 TeamType 사용
