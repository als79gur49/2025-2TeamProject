# Vector2Int 좌표계 통일 계획

## 📊 현황 분석

### 좌표계 정의 (수정됨 - Phase 2)
- **기준점**: 좌하단 (0, 0) ← 카르테시안 좌표계 채택
- **축 방향**: +x = 우측 (가로, Column), +y = 상단 (세로, Row) ← Y축 방향 변경
- **올바른 벡터 형식**: `Vector2Int(x, y)` = `Vector2Int(col, row)`
- **변경 이유**: Unity 표준 2D 좌표계와 일치, Y축 상향 증가로 직관성 향상

### 🔍 발견된 문제

#### 1. CardData.cs:148 - 맨하탄 거리 계산 버그
```csharp
// ❌ 현재: y축만 계산 (주석과 불일치)
public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
{
    return Mathf.Abs(to.y - from.y); //오직 x축만 검증
}

// ✅ 수정 필요: 실제 맨하탄 거리 계산
public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
{
    // 좌표계: (row,col) = (y,x)
    return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
}
```

#### 2. CardData.cs:247 - 범위 계산 순서 문제
```csharp
// ❌ 현재: (x, y) 순서로 생성
for (int x = -effectData.AffectedRange; x <= effectData.AffectedRange; x++)
{
    for (int y = -effectData.AffectedRange; y <= effectData.AffectedRange; y++)
    {
        var pos = new Vector2Int(targetPosition.x + x, targetPosition.y + y);
        positions.Add(pos);
    }
}

// ✅ 수정 필요: 변수명을 의미에 맞게 변경
for (int rowOffset = -effectData.AffectedRange; rowOffset <= effectData.AffectedRange; rowOffset++)
{
    for (int colOffset = -effectData.AffectedRange; colOffset <= effectData.AffectedRange; colOffset++)
    {
        var pos = new Vector2Int(
            targetPosition.x + rowOffset,  // row (y축)
            targetPosition.y + colOffset   // col (x축)
        );
        positions.Add(pos);
    }
}
```

#### 3. 전체 Vector2Int 사용 현황
- **총 59개** Vector2Int 생성 위치
- **17개 파일**에 분산
- **순서 검증 필요**

---

## 🏗️ 실행 계획

### Phase 1: 진단 및 분석 (1-2시간)

#### 1.1 전체 코드 스캔
- 59개 Vector2Int 생성 위치 상세 분석
- 각 위치의 의미론적 컨텍스트 파악
- 버그 vs 올바른 사용 분류

#### 1.2 카테고리 분류

**분류 기준:**
- **Category A (명확한 버그)**: 로직 오류로 즉시 수정 필요
- **Category B (검증 필요)**: 변수명 혼동 가능성 있음, 리팩토링 권장
- **Category C (올바른 사용)**: 의미론적으로 명확하거나 검증된 코드

---

### Category A - 명확한 버그 (1개)

#### 1. CardData.cs:148 - CalculateManhattanDistance
```csharp
// ❌ 현재: y축만 계산 (주석과 불일치)
return Mathf.Abs(to.y - from.y); // 오직 x축만 검증

// ✅ 수정 필요: 실제 맨하탄 거리
return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
```
**우선순위**: 🔴 CRITICAL
**영향도**: 카드 범위 검증 시스템 전체
**수정 필요성**: 즉시 수정 필수

---

### Category B - 검증 필요 (18개 위치)

#### 루프 변수명으로 인한 혼동 가능성

**1. CardData.cs:243-250 - GetAffectedPositions**
```csharp
// ⚠️ 변수명 x, y 사용 → 혼동 가능
for (int x = -effectData.AffectedRange; x <= effectData.AffectedRange; x++)
{
    for (int y = -effectData.AffectedRange; y <= effectData.AffectedRange; y++)
    {
        var pos = new Vector2Int(targetPosition.x + x, targetPosition.y + y);
        positions.Add(pos);
    }
}

// ✅ 권장: row, col 변수명 사용
for (int rowOffset = -range; rowOffset <= range; rowOffset++)
{
    for (int colOffset = -range; colOffset <= range; colOffset++)
    {
        var pos = new Vector2Int(targetPosition.x + rowOffset, targetPosition.y + colOffset);
    }
}
```

**2. CardDataAffectedRangeExtensions.cs:151-158, 169-176**
- `AddPositionsInRange` 메서드 (2개 오버로드)
- 동일한 x, y 루프 변수 사용 패턴

**3. GridState.cs:53-60 - InitializeGrid**
```csharp
for (int x = 0; x < gridSize.x; x++)
{
    for (int y = 0; y < gridSize.y; y++)
    {
        var position = new Vector2Int(x, y);
        tileGrid[x, y] = new GridTileData(position);
    }
}
```
**의미론**: x가 가로(col), y가 세로(row)를 의미하는지 명확하지 않음

**4. GridRenderer.cs**
- Line 87, 331: 렌더링 루프에서 `new Vector2Int(x, y)` 사용
- Line 422-423, 429-430: 그리드 라인 그리기

**5. CombatComponent.cs**
- Line 470, 491, 508: 전투 범위 계산 루프
- Line 526: `new Vector2Int(x, y)` 위치 생성

**6. MovementComponent.cs:175**
- 이동 범위 계산 시 `currentPosition + new Vector2Int(x, y)`

**7. SummonEffect.cs:111**
- `var checkPos = targetPos + new Vector2Int(x, y)`

**8. GridController.cs:572**
- `Vector2Int pos = new Vector2Int(x, y)` in FindNearestAvailablePosition

**9. GridDataBridge.cs:27**
- `GetTile(int x, int y) => GetTile(new Vector2Int(x, y))`

**파라미터명 검증 필요:**

**10. GridCoordinateExtensions.cs:11**
```csharp
public static Vector2Int ToGridPosition(int x, int y) => new Vector2Int(x, y);
```
**문제**: x, y가 row, col 중 무엇을 의미하는지 불명확

**11. Unit.cs 여러 위치**
- Line 458, 506, 523, 728, 733, 746
- 대부분 x, y 파라미터로 Vector2Int 생성

**우선순위**: 🟡 MEDIUM
**영향도**: 코드 가독성 및 유지보수성
**수정 필요성**: 리팩토링 권장 (변수명을 row, col로 변경)

---

### Category C - 올바른 사용 (42개 위치)

#### 의미론적으로 명확한 사용

**1. 무효 위치 표시 (-1, -1)**
- GridController.cs:108, 118: `new Vector2Int(-1, -1)`
- GridManager.cs:385, 389: `new Vector2Int(-1, -1)`
- GridState.cs:103, 122: `new Vector2Int(-1, -1)`
- 에러/무효 위치 표시 관례로 올바름

**2. Tile 클래스 필드 사용**
```csharp
// Tile.cs:20
public Vector2Int GetGridPosition()
{
    return new Vector2Int(x, y); // Tile의 x, y 필드는 의미가 명확
}

// GridCoordinateExtensions.cs:20
public static Vector2Int GetGridPosition(this Tile tile)
    => new Vector2Int(tile.X, tile.Y);
```
**이유**: Tile.X, Tile.Y는 클래스 설계상 의미가 명확함

**3. 리터럴 값 사용**
```csharp
// GridState.cs:14
private Vector2Int gridSize = new Vector2Int(10, 10);

// GridManager.cs:328-329
var start = new Vector2Int(0, 0);
var end = new Vector2Int(gridSize.x - 1, gridSize.y - 1);
```

**4. 기준점 계산 (의미 명확)**
```csharp
// GridController.cs:464, 481
Vector2Int basePosition = new Vector2Int(gridSize.x / 2, 0);
Vector2Int basePosition = new Vector2Int(rightmostColumn, gridSize.y / 2);
```

**5. 대각선 방향 오프셋**
```csharp
// GridController.cs:893-896
position + new Vector2Int(1, 1),
position + new Vector2Int(1, -1),
position + new Vector2Int(-1, 1),
position + new Vector2Int(-1, -1)

// IMovementSystem.cs:339-342
position + new Vector2Int(-1, -1),
position + new Vector2Int(-1, 1),
position + new Vector2Int(1, -1),
position + new Vector2Int(1, 1)
```
**이유**: 대각선 방향은 (±1, ±1) 패턴으로 명확

**6. 위치 변환 (WorldToGrid)**
```csharp
// IMovementSystem.cs:357
return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));

// GridState.cs:259
return new Vector2Int(
    Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / tileSize),
    Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / tileSize)
);
```

**7. 그리드 라인 그리기**
```csharp
// GridState.cs:515-516, 522-523
GridToWorldPosition(new Vector2Int(x, 0));
GridToWorldPosition(new Vector2Int(x, gridSize.y));
GridToWorldPosition(new Vector2Int(0, y));
GridToWorldPosition(new Vector2Int(gridSize.x, y));
```

**8. 테스트 데이터**
```csharp
// TileOccupancyIntegrationTest.cs:20-24
private Vector2Int startPosition = new Vector2Int(2, 2);
new Vector2Int(3, 2),
new Vector2Int(3, 3),
new Vector2Int(2, 3)
```

**9. UI/드롭 핸들러**
```csharp
// TileDropHandler.cs:144
gridPosition = new Vector2Int(x, y);
```

**10. 기타 명시적 사용**
- GridUnitExtensions.cs:43: `new Vector2Int(unit.X, unit.Y)`
- CombatComponent.cs: 위치 오프셋 계산들
- GridState.cs:276: 초기화 루프

**우선순위**: 🟢 LOW
**영향도**: 없음
**수정 필요성**: 수정 불필요 (현재 상태 유지)

---

### 📊 통계 요약

| Category | 개수 | 우선순위 | 수정 필요성 |
|----------|------|----------|------------|
| **A - 명확한 버그** | 1 | 🔴 CRITICAL | 즉시 수정 필수 |
| **B - 검증 필요** | 18 | 🟡 MEDIUM | 리팩토링 권장 |
| **C - 올바른 사용** | 42+ | 🟢 LOW | 수정 불필요 |
| **총계** | 61+ | - | - |

**비율:**
- 명확한 버그: 1.6%
- 검증/개선 필요: 29.5%
- 올바른 사용: 68.9%

---

### 🎯 Phase 1.2 완료 결과

✅ **완료 항목:**
1. 전체 코드베이스 Vector2Int 사용 스캔 (61개 위치)
2. 17개 파일 분석 완료
3. 3단계 카테고리 분류 완료
4. 우선순위 및 영향도 평가 완료

✅ **주요 발견:**
- **1개 치명적 버그**: CalculateManhattanDistance 오류
- **18개 개선 대상**: 변수명 혼동 가능성
- **42개 정상 코드**: 의미론적으로 명확한 사용

✅ **다음 단계 (Phase 2):**
- GridPositionHelper 유틸리티 생성
- Category A 버그 즉시 수정
- Category B 리팩토링 계획 수립

---

## ✅ Phase 2 완료: 표준화 도구 생성

### Phase 2: 표준화 도구 생성 (완료)

#### 2.1 GridPosition 헬퍼 유틸리티 생성

**파일 위치**: `Assets/Script/Game/Utilities/GridPositionHelper.cs`

```csharp
using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// 그리드 좌표 생성을 위한 명시적 헬퍼
    ///
    /// 좌표계 규칙:
    /// - 기준: 좌상단(0,0), +x=우측, +y=하단
    /// - Vector2Int 구조: (row, col) = (y축, x축)
    /// - Vector2Int.x = row (세로 위치, y축)
    /// - Vector2Int.y = col (가로 위치, x축)
    /// </summary>
    public static class GridPositionHelper
    {
        /// <summary>
        /// 행(row), 열(col) 기반 위치 생성
        /// row = 세로 위치 (y축), col = 가로 위치 (x축)
        /// </summary>
        /// <param name="row">행 번호 (y축, 0부터 시작)</param>
        /// <param name="col">열 번호 (x축, 0부터 시작)</param>
        /// <returns>그리드 위치 Vector2Int(row, col)</returns>
        public static Vector2Int CreateRowCol(int row, int col)
            => new Vector2Int(row, col);

        /// <summary>
        /// Y, X 축 기반 위치 생성 (명시적)
        /// </summary>
        /// <param name="y">Y축 위치 (하단 방향 증가)</param>
        /// <param name="x">X축 위치 (우측 방향 증가)</param>
        /// <returns>그리드 위치 Vector2Int(y, x)</returns>
        public static Vector2Int CreateYX(int y, int x)
            => new Vector2Int(y, x);

        /// <summary>
        /// 디버깅용: 좌표를 명시적으로 출력
        /// </summary>
        public static string ToDebugString(this Vector2Int pos)
            => $"(row:{pos.x}, col:{pos.y}) = (y:{pos.x}, x:{pos.y})";

        /// <summary>
        /// Row (행) 값 추출
        /// </summary>
        public static int GetRow(this Vector2Int pos) => pos.x;

        /// <summary>
        /// Col (열) 값 추출
        /// </summary>
        public static int GetCol(this Vector2Int pos) => pos.y;
    }
}
```

### 🎯 Phase 2 완료 결과

**✅ 생성된 파일:**
1. **GridPositionHelper.cs** (Utilities 폴더)
   - 카르테시안 좌표계 기반 (Y축 하→상)
   - X축 거리, Y축 거리 개별 계산 메서드
   - 맨하탄 거리, 체비셰프 거리 계산
   - 4방향/8방향 벡터 상수 제공
   - 명시적 좌표 생성 메서드 (CreateXY, CreateColRow, CreateRowCol)

2. **GridCoordinateValidator.cs** (Editor 폴더)
   - Vector2Int 사용 패턴 자동 검증
   - 의심스러운 패턴 감지 (역순, 변수명 혼동)
   - 심각도별 분류 (Critical, Warning, Info)
   - 상세 보고서 생성 기능
   - Unity 메뉴: Tools/Grid/Validate Coordinates

**✅ 좌표계 변경 사항:**
- GridState.cs 변환 메서드 수정:
  - `GridToWorldPosition`: Y축 반전 적용 (하→상)
  - `WorldToGridPosition`: 역변환 반전 적용

**✅ 사용 방법:**
```csharp
// 위치 생성
var pos = GridPositionHelper.CreateXY(3, 5);        // X=3(가로), Y=5(세로)
var pos2 = GridPositionHelper.CreateColRow(3, 5);   // 동일

// 거리 계산
int manhattan = GridPositionHelper.CalculateManhattanDistance(from, to);
int xDist = GridPositionHelper.CalculateXDistance(from, to);  // 가로 거리
int yDist = GridPositionHelper.CalculateYDistance(from, to);  // 세로 거리

// 디버깅
Debug.Log(pos.ToDebugString());  // "(x:3, y:5) = (col:3, row:5) [좌하단 기준]"
```

---

#### 2.2 검증 도구 추가 (완료)

**파일 위치**: `Assets/Script/Game/Editor/GridCoordinateValidator.cs`

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Game.Editor
{
    public static class GridCoordinateValidator
    {
        [MenuItem("Tools/Grid/Validate Coordinates")]
        public static void ValidateAllCoordinates()
        {
            Debug.Log("=== Grid Coordinate Validation Started ===");

            var scriptFiles = Directory.GetFiles(
                "Assets/Script/Game",
                "*.cs",
                SearchOption.AllDirectories
            );

            var pattern = new Regex(@"new\s+Vector2Int\s*\(\s*(\w+)\s*,\s*(\w+)\s*\)");
            var suspiciousUsages = new List<string>();

            foreach (var file in scriptFiles)
            {
                var content = File.ReadAllText(file);
                var matches = pattern.Matches(content);

                foreach (Match match in matches)
                {
                    var param1 = match.Groups[1].Value;
                    var param2 = match.Groups[2].Value;

                    // x, y 순서로 사용하는 경우 의심
                    if (param1.ToLower().Contains("x") && param2.ToLower().Contains("y"))
                    {
                        suspiciousUsages.Add($"{Path.GetFileName(file)}: {match.Value}");
                    }
                }
            }

            if (suspiciousUsages.Count > 0)
            {
                Debug.LogWarning($"Found {suspiciousUsages.Count} suspicious Vector2Int usages:");
                foreach (var usage in suspiciousUsages)
                {
                    Debug.LogWarning($"  - {usage}");
                }
            }
            else
            {
                Debug.Log("✅ No suspicious coordinate usages found!");
            }

            Debug.Log("=== Grid Coordinate Validation Completed ===");
        }
    }
}
#endif
```

---

### Phase 3: 코드 수정 (3-4시간)

#### 3.1 명확한 버그 수정

##### 수정 1: CardData.cs - CalculateManhattanDistance

**위치**: `Assets/Script/Game/Data/CardData.cs:146-149`

```csharp
/// <summary>
/// Phase 2.11: 맨하탄 거리 계산 (TargetRange 검증용)
/// 좌표계: Vector2Int(row, col) = (y, x)
/// </summary>
/// <param name="from">시작 위치</param>
/// <param name="to">목표 위치</param>
/// <returns>맨하탄 거리</returns>
public static int CalculateManhattanDistance(Vector2Int from, Vector2Int to)
{
    // Vector2Int.x = row(y축), Vector2Int.y = col(x축)
    return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
}
```

##### 수정 2: CardData.cs - GetAffectedPositions

**위치**: `Assets/Script/Game/Data/CardData.cs:243-250`

```csharp
// AffectedRange가 1+이면 범위 내 모든 위치 포함
for (int rowOffset = -effectData.AffectedRange; rowOffset <= effectData.AffectedRange; rowOffset++)
{
    for (int colOffset = -effectData.AffectedRange; colOffset <= effectData.AffectedRange; colOffset++)
    {
        // Vector2Int(row, col) 형식으로 생성
        var pos = new Vector2Int(
            targetPosition.x + rowOffset,  // row (세로, y축)
            targetPosition.y + colOffset   // col (가로, x축)
        );
        positions.Add(pos);
    }
}
```

##### 수정 3: CardData.cs - IsPositionInAffectedRange

**위치**: `Assets/Script/Game/Data/CardData.cs:269`

```csharp
// 맨하탄 거리로 범위 체크
// Vector2Int.x = row, Vector2Int.y = col
int rowDistance = Mathf.Abs(targetPosition.x - checkPosition.x);
int colDistance = Mathf.Abs(targetPosition.y - checkPosition.y);
int distance = rowDistance + colDistance;
return distance <= effectData.AffectedRange;
```

#### 3.2 GridCoordinateExtensions 개선

**위치**: `Assets/Script/Game/Utilities/GridCoordinateExtensions.cs`

```csharp
using UnityEngine;

namespace Game.Utilities
{
    /// <summary>
    /// 좌표계 통합을 위한 확장 메서드
    ///
    /// 좌표계 규칙:
    /// - 기준: 좌상단(0,0), +x=우측, +y=하단
    /// - Vector2Int: (row, col) = (y축, x축)
    /// </summary>
    public static class GridCoordinateExtensions
    {
        /// <summary>
        /// 레거시 지원: Row, Col 기반 위치 생성
        /// </summary>
        public static Vector2Int ToGridPosition(int row, int col)
            => new Vector2Int(row, col);

        /// <summary>
        /// Vector2Int를 row, col로 분해
        /// </summary>
        public static void Deconstruct(this Vector2Int pos, out int row, out int col)
        {
            row = pos.x;  // Vector2Int.x = row
            col = pos.y;  // Vector2Int.y = col
        }

        // Tile 통합
        public static Vector2Int GetGridPosition(this Tile tile)
            => new Vector2Int(tile.X, tile.Y);

        public static void SetGridPosition(this Tile tile, Vector2Int position)
        {
            // Tile.X = row, Tile.Y = col 가정
            tile.Initialize(position.x, position.y);
        }
    }
}
```

#### 3.3 문서화 및 주석 추가

모든 Vector2Int 사용 코드에 다음 주석 추가:
```csharp
// 좌표계: Vector2Int(row, col) = (y축, x축)
```

---

### Phase 4: 검증 및 테스트 (2-3시간)

#### 4.1 단위 테스트 작성

**파일 위치**: `Assets/Script/Game/Tests/GridCoordinateTests.cs`

```csharp
using NUnit.Framework;
using UnityEngine;
using Game.Data;
using Game.Utilities;

namespace Game.Tests
{
    public class GridCoordinateTests
    {
        [Test]
        public void ManhattanDistance_CalculatesCorrectly()
        {
            // Arrange: (0,0)에서 (3,4)까지
            var from = new Vector2Int(0, 0);
            var to = new Vector2Int(3, 4);

            // Act
            int distance = CardData.CalculateManhattanDistance(from, to);

            // Assert: 3 + 4 = 7
            Assert.AreEqual(7, distance, "맨하탄 거리가 올바르게 계산되어야 함");
        }

        [Test]
        public void GridPositionHelper_CreatesCorrectPosition()
        {
            // Arrange
            int row = 2, col = 3;

            // Act
            var pos = GridPositionHelper.CreateRowCol(row, col);

            // Assert
            Assert.AreEqual(row, pos.x, "Vector2Int.x는 row여야 함");
            Assert.AreEqual(col, pos.y, "Vector2Int.y는 col이어야 함");
        }

        [Test]
        public void GetAffectedPositions_ReturnsCorrectRange()
        {
            // Arrange
            var cardData = ScriptableObject.CreateInstance<CardData>();
            var effectData = new EffectData(EffectType.Damage, 1, AffectedType.Any, 1);
            var targetPos = new Vector2Int(5, 5);

            // Act
            var positions = cardData.GetAffectedPositions(targetPos, effectData);

            // Assert
            Assert.AreEqual(9, positions.Count, "범위 1은 3x3 = 9칸");
            Assert.Contains(targetPos, positions, "중심 위치 포함");
            Assert.Contains(new Vector2Int(4, 5), positions, "위쪽 포함");
            Assert.Contains(new Vector2Int(6, 5), positions, "아래쪽 포함");
            Assert.Contains(new Vector2Int(5, 4), positions, "왼쪽 포함");
            Assert.Contains(new Vector2Int(5, 6), positions, "오른쪽 포함");
        }
    }
}
```

#### 4.2 통합 테스트 시나리오

1. **그리드 이동 테스트**
   - 유닛을 (0,0)에서 (5,5)로 이동
   - 경로가 올바른 좌표로 생성되는지 검증

2. **카드 효과 범위 테스트**
   - 범위 2 데미지 카드 사용
   - 영향받는 타일이 정확한지 시각적 검증

3. **경로 탐색 테스트**
   - A* 알고리즘 경로가 올바른 좌표 사용하는지 확인

#### 4.3 체크리스트

- [ ] CardData.CalculateManhattanDistance 수정 완료
- [ ] CardData.GetAffectedPositions 수정 완료
- [ ] CardData.IsPositionInAffectedRange 수정 완료
- [ ] GridPositionHelper 유틸리티 추가
- [ ] GridCoordinateValidator 에디터 도구 추가
- [ ] 단위 테스트 작성 및 통과
- [ ] 통합 테스트 시나리오 실행
- [ ] 문서화 및 주석 추가
- [ ] 코드 리뷰 완료

---

## 📋 영향받는 파일 목록

### 즉시 수정 필요 (Priority: High)
1. `Assets/Script/Game/Data/CardData.cs` - 맨하탄 거리, 범위 계산
2. `Assets/Script/Game/Utilities/GridCoordinateExtensions.cs` - 주석 추가

### 검증 필요 (Priority: Medium)
3. `Assets/Script/Game/Components/GridController.cs`
4. `Assets/Script/Game/Components/GridState.cs`
5. `Assets/Script/Game/GridManager.cs`
6. `Assets/Script/Game/Components/MovementComponent.cs`

### 검토 권장 (Priority: Low)
7. `Assets/Script/Game/Unit.cs`
8. `Assets/Script/Game/Tile.cs`
9. 기타 Vector2Int 사용 파일들 (총 17개)

---

## 🎯 예상 결과

### 수정 전
- ❌ 맨하탄 거리 계산 오류 (y축만 계산)
- ❌ 범위 계산 변수명 혼동 (x, y)
- ❌ 59개 Vector2Int 생성 중 순서 불일치
- ❌ 문서화 부족

### 수정 후
- ✅ 올바른 맨하탄 거리 계산
- ✅ 명확한 변수명 (row, col)
- ✅ 모든 좌표 (row, col) = (y, x) 통일
- ✅ 명시적 헬퍼 메서드로 실수 방지
- ✅ 검증 도구로 향후 버그 예방
- ✅ 완전한 문서화

---

## ⏱️ 예상 소요 시간

| Phase | 작업 내용 | 예상 시간 |
|-------|----------|----------|
| Phase 1 | 진단 및 분석 | 1-2시간 |
| Phase 2 | 표준화 도구 생성 | 2-3시간 |
| Phase 3 | 코드 수정 및 리팩토링 | 3-4시간 |
| Phase 4 | 테스트 및 검증 | 2-3시간 |
| **총계** | - | **8-12시간** (1-2일) |

---

## 📌 참고 사항

### 좌표계 규칙 요약 (Phase 2 업데이트)
```
좌표계 (카르테시안 - Unity 표준):
- 기준점: 좌하단(0, 0)
- +x 방향: 우측 (가로, Column)
- +y 방향: 상단 (세로, Row) ← 변경됨!

Vector2Int 구조:
- Vector2Int.x = Column (열, 가로 위치, X축 값)
- Vector2Int.y = Row (행, 세로 위치, Y축 값)
- 생성: new Vector2Int(x, y) = new Vector2Int(col, row)

권장 사용법:
- GridPositionHelper.CreateXY(x, y)           // 표준 좌표
- GridPositionHelper.CreateColRow(col, row)   // 의미 명시
- GridPositionHelper.CreateRowCol(row, col)   // 2차원 배열 관습 (내부에서 순서 바꿈)
- 명시적 주석 추가
```

### 주의사항 (Phase 2 업데이트)
1. **좌표계 변경**: Y축이 이제 하→상으로 증가합니다 (카르테시안)
2. **Vector2Int.x = Column (가로)**, **Vector2Int.y = Row (세로)**
3. 변수명을 단순 x, y 대신 col, row 또는 xPos, yPos 사용 권장
4. GridToWorldPosition/WorldToGridPosition 변환 시 Y축 반전 자동 처리됨
5. 헬퍼 메서드 사용으로 혼동 방지
6. Unity 에디터 메뉴 "Tools/Grid/Validate Coordinates"로 검증 가능

---

## 🔗 관련 문서
- Unity Vector2Int API: https://docs.unity3d.com/ScriptReference/Vector2Int.html
- 그리드 시스템 설계 문서: (추가 예정)
- 좌표계 표준화 가이드: (이 문서)
