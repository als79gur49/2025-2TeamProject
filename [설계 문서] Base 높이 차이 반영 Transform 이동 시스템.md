<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# [설계 문서] Base 높이 차이 반영 Transform 이동 시스템


***

## 1. 설계 검증 요약

모든 이동 시나리오(상승, 하강, 평행)에 대해 **정상적으로 동작하는 것**을 최종적으로 검증하였다.

### ✔️ Base → Ground: 하강 (Y: 1.0 → 0.5, 자연스러운 대각선 하강)

### ✔️ Ground → Base: 상승 (Y: 0.5 → 1.0, 자연스러운 대각선 상승)

### ✔️ Base → Base: 평행 (Y 고정)

### ✔️ Ground → Ground: 평행 (Y 고정)

`GridController.CalculateWorldPositionWithHeight()` 메서드가 높이가 포함된 월드 좌표를 반환하고,
`SyncTransformWithAnimation()`에서 `Vector3.Lerp(startPos, endPos, progress)`를 통해 Y축까지 모든 축이 자연스럽게 보간되므로, **추가적인 Y축 전용 로직 없이도** 완벽하게 "위→아래" 혹은 "아래→위"의 이동을 구현할 수 있다.

***

## 2. 책임과 계층 구조

| 계층 | 컴포넌트 | 책임 (높이 관련) |
| :-- | :-- | :-- |
| Data Layer | GridState | 데이터 관리, 순수 좌표 변환(Y=0) |
| Business Logic Layer | GridController | **높이 계산 및 월드 좌표 반환** |
| Presentation Layer | GridRenderer | 시각화만 담당, 높이 계산 없음 |
| Unity Integration | GridManager | 전체 중재 및 래퍼 제공(선택) |
| 행동/이동 | MovementComponent | 실제 이동, 높이 포함 좌표 사용 |


***

## 3. 인터페이스 및 책임 예시

### (1) IGridHeightCalculator 인터페이스

```csharp
public interface IGridHeightCalculator
{
    float GetGroundHeightAt(Vector2Int gridPosition);
    Vector3 CalculateWorldPositionWithHeight(Vector2Int gridPosition);
    float BaseHeight { get; set; }
}
```


### (2) GridController 구현 (핵심 부분)

```csharp
public float GetGroundHeightAt(Vector2Int gridPosition)
{
    if (!IsValidPosition(gridPosition)) return 0f;
    GameObject baseObj = gridState?.GetBaseAtPosition(gridPosition);
    return baseObj != null ? baseHeight : 0f;
}

public Vector3 CalculateWorldPositionWithHeight(Vector2Int gridPosition)
{
    Vector3 basePos = GridToWorldPosition(gridPosition);      // (Data Layer, Y=0)
    float groundHeight = GetGroundHeightAt(gridPosition);     // (비즈니스 규칙)
    Vector3 unitOffset = GetUnitOffset();                     // (ex: 0.5f)
    return basePos + new Vector3(0f, groundHeight, 0f) + unitOffset;
}
```


### (3) MovementComponent 활용 (공통화)

```csharp
// 모든 이동에서 높이 반영
Vector3 startPos = gridManager.GetGridController().CalculateWorldPositionWithHeight(from);
Vector3 endPos   = gridManager.GetGridController().CalculateWorldPositionWithHeight(to);
transform.position = Vector3.Lerp(startPos, endPos, progress);
```


***

## 4. 동작 원리와 검증 상세

- **Base→Ground 이동:** startPos와 endPos의 Y값이 다름 → 자동 하강 이동
- **Ground→Base 이동:** startPos와 endPos의 Y값이 다름 → 자동 상승 이동
- **Base→Base or Ground→Ground:** Y값 동일 → 평행 이동

**Unity의 Vector3.Lerp는 X/Y/Z 모든 축을 자동 보간하므로 Y축 애니메이션도 자연스럽게 구현**

***

## 5. 샘플 이동 프레임 (Y값 변화)

| Frame | Base→Ground | Ground→Base | Base→Base | Ground→Ground |
| :-- | :-- | :-- | :-- | :-- |
| 0 | 1.0 | 0.5 | 1.0 | 0.5 |
| 1 | 0.9 | 0.6 | 1.0 | 0.5 |
| 2 | 0.8 | 0.7 | 1.0 | 0.5 |
| 3 | 0.7 | 0.8 | 1.0 | 0.5 |
| 4 | 0.6 | 0.9 | 1.0 | 0.5 |
| 5 | 0.5 | 1.0 | 1.0 | 0.5 |


***

## 6. 결론 및 주의사항

- **추가 높이 관련 로직(경사로, 계단 등)은 모두 GridController에서 확장**
- **Base의 높이만 변경하면 전체 이동에 반영됨**
- 그 외 GridState 등 Data Layer는 순수성 보장, 비즈니스 규칙 의존 없음
- Inspector에서 baseHeight 값 즉시 조정 가능

***

## 7. 최종 요약

**현 설계는 Clean Architecture 계층 책임을 100% 준수하며, 상승/하강 이동을 완벽 지원한다. 별도 Y축 전용 로직 필요 없이 Lerp 하나로 "Base→Ground"와 "Ground→Base" 모두 자연스럽게 동작이 가능하다.**

