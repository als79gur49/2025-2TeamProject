using UnityEngine;
using System.Collections.Generic;
using Game.VFX;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 소환 효과 구현
    /// ICardEffect를 구현하여 대상 위치에 유닛을 소환하는 효과입니다.
    /// VFX Dynamic Data System: IVFXAwareEffect 구현으로 공격 성공/실패 반응
    /// </summary>
    public class SummonEffect : IVFXAwareEffect
    {
        private readonly EffectData _effectData;

        public EffectType EffectType => EffectType.Summon;
        public int Priority => _effectData?.Priority ?? 0;

        public SummonEffect(EffectData effectData)
        {
            _effectData = effectData ?? throw new System.ArgumentNullException(nameof(effectData));

            if (_effectData.Type != EffectType.Summon)
            {
                throw new System.ArgumentException($"EffectData의 타입이 Summon이 아닙니다: {_effectData.Type}");
            }

            if (_effectData.UnitToSummon == null)
            {
                throw new System.ArgumentException("Summon Effect에는 UnitToSummon이 필요합니다.");
            }
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_effectData == null || !_effectData.IsValid())
            {
                Debug.LogWarning("SummonEffect: 유효하지 않은 EffectData입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("SummonEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 소환 가능한 위치가 있는지 확인
            var availablePositions = GetAvailableSummonPositions(targetPos, context);
            var requiredPositions = _effectData.Value; // Value는 소환할 유닛 개수

            if(availablePositions.Count < requiredPositions)
            {
                Debug.LogError($"소환 가능 위치가 필요 위치 개수보다 적다. availablePositions:{availablePositions.Count}, requirePositions:{requiredPositions}");
            }

            return availablePositions.Count >= requiredPositions;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("SummonEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            var availablePositions = GetAvailableSummonPositions(targetPos, context);
            var summonCount = Mathf.Min(_effectData.Value, availablePositions.Count);

            Debug.Log($"SummonEffect: {_effectData.UnitToSummon.UnitName}을(를) {summonCount}개 소환합니다.");

            for (int i = 0; i < summonCount; i++)
            {
                SummonUnitAtPosition(availablePositions[i], context);
            }

            // 시각적 효과 재생
            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// VFX TriggerData를 포함한 효과 실행 (IVFXAwareEffect 구현)
        /// 공격 성공/실패 여부에 따라 소환 적용 여부 결정
        /// NOTE: SummonEffect는 위치 기반이므로 다중 타겟 개념이 적용되지 않음
        ///       SpellEffectExecutor가 AffectedRange > 0일 때도 이 메서드를 호출하지만,
        ///       소환은 targetPos 중심으로 범위 내 빈 공간에 실행됨
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            // 공격 실패 시 Miss 효과만 재생하고 종료
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[SummonEffect] Summon failed: {triggerData.ValidationFailureReason}");
                PlayMissEffect(targetPos, context);
                return;
            }

            // 공격 성공: 소환 실행
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("[SummonEffect] Summon success but cannot execute (no valid positions)");
                return;
            }

            var availablePositions = GetAvailableSummonPositions(targetPos, context);
            var summonCount = Mathf.Min(_effectData.Value, availablePositions.Count);

            Debug.Log($"[SummonEffect] Summon success! Summoning {summonCount} {_effectData.UnitToSummon.UnitName} at {targetPos}");

            for (int i = 0; i < summonCount; i++)
            {
                SummonUnitAtPosition(availablePositions[i], context);
            }

            // VFX 위치 기반 Summon 효과 재생
            PlaySummonEffect(triggerData.TriggerWorldPosition, context);
        }

        /// <summary>
        /// 소환 가능한 위치들을 찾습니다.
        /// Phase 3.17: targetPos를 최우선으로 확인하여 드롭 위치와 소환 위치 일치 보장
        /// </summary>
        private List<Vector2Int> GetAvailableSummonPositions(Vector2Int targetPos, GameContext context)
        {
            var availablePositions = new List<Vector2Int>();

            // AffectedRange가 0이면 단일 위치, 1+이면 범위 내에서 소환
            if (_effectData.AffectedRange == 0)
            {
                // 단일 위치에서만 소환
                if (IsValidSummonPosition(targetPos, context))
                {
                    availablePositions.Add(targetPos);
                }
            }
            else
            {
                // Phase 3.17: targetPos를 먼저 확인하여 우선순위 부여
                // 사용자가 드롭한 위치를 가장 먼저 배치하도록 보장
                if (IsValidSummonPosition(targetPos, context))
                {
                    availablePositions.Add(targetPos);
                }

                // 범위 내 다른 유효한 위치들을 추가 (targetPos는 제외)
                for (int x = -_effectData.AffectedRange; x <= _effectData.AffectedRange; x++)
                {
                    for (int y = -_effectData.AffectedRange; y <= _effectData.AffectedRange; y++)
                    {
                        var checkPos = targetPos + new Vector2Int(x, y);

                        // targetPos는 이미 추가했으므로 중복 방지
                        if (checkPos == targetPos)
                            continue;

                        if (IsValidSummonPosition(checkPos, context))
                        {
                            availablePositions.Add(checkPos);
                        }
                    }
                }
            }

            Debug.Log($"[SummonEffect] Found {availablePositions.Count} available positions. First: {(availablePositions.Count > 0 ? availablePositions[0].ToString() : "None")}");
            return availablePositions;
        }

        /// <summary>
        /// 특정 위치에서 소환이 가능한지 확인합니다.
        /// GridController를 통해 실제 유효성 검사를 수행합니다.
        /// </summary>
        private bool IsValidSummonPosition(Vector2Int position, GameContext context)
        {
            if (context.GridController == null)
            {
                Debug.LogWarning("SummonEffect: GridController가 null입니다.");
                return false;
            }

            // 그리드 경계 내부인지, 비어있는지, 블록되지 않았는지 확인
            return IsPositionInBounds(position, context) &&
                   IsPositionEmpty(position, context) &&
                   IsValidTerrainForSummon(position, context);
        }

        /// <summary>
        /// 위치가 그리드 경계 내부인지 확인합니다.
        /// </summary>
        private bool IsPositionInBounds(Vector2Int position, GameContext context)
        {
            if (context.GridController == null)
                return false;

            return context.GridController.IsValidPosition(position);
        }

        /// <summary>
        /// 위치에 다른 유닛이 없는지 확인합니다.
        /// </summary>
        private bool IsPositionEmpty(Vector2Int position, GameContext context)
        {
            if (context.GridController == null)
                return false;

            return !context.GridController.IsPositionOccupied(position);
        }

        /// <summary>
        /// 지형이 소환에 적합한지 확인합니다 (블록되지 않았는지).
        /// </summary>
        private bool IsValidTerrainForSummon(Vector2Int position, GameContext context)
        {
            if (context.GridController == null)
                return false;

            return !context.GridController.IsPositionBlocked(position);
        }

        /// <summary>
        /// 특정 위치에 유닛을 소환합니다.
        /// UnitData의 Prefab을 인스턴스화하고 GridController와 UnitService에 등록합니다.
        /// </summary>
        private void SummonUnitAtPosition(Vector2Int position, GameContext context)
        {
            if (_effectData.UnitToSummon == null || _effectData.UnitToSummon.Prefab == null)
            {
                Debug.LogError("SummonEffect: UnitToSummon 또는 Prefab이 null입니다.");
                return;
            }

            if (context.GridController == null || context.UnitService == null)
            {
                Debug.LogError("SummonEffect: GridController 또는 UnitService가 null입니다.");
                return;
            }

            // 유닛 프리팹 인스턴스화
            Vector3 worldPosition = context.GridController.GridToWorldPosition(position);
            var unitObject = Object.Instantiate(_effectData.UnitToSummon.Prefab, worldPosition, Quaternion.identity);

            if (unitObject == null)
            {
                Debug.LogError($"SummonEffect: {_effectData.UnitToSummon.UnitName} 프리팹 인스턴스화 실패");
                return;
            }

            // Unit 컴포넌트 가져오기
            var unit = unitObject.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError($"SummonEffect: {unitObject.name}에 Unit 컴포넌트가 없습니다.");
                Object.Destroy(unitObject);
                return;
            }

            // 유닛 초기화 (플레이어 ID 기반으로 팀 결정)
            bool isPlayerUnit = (context.PlayerId == 0); // PlayerId 0이 플레이어라고 가정
            unit.Init(_effectData.UnitToSummon, position, isPlayerUnit);

            // 외부에서 모든 등록 처리 (Orchestrator 패턴)

            // 1. GridController.MoveUnit()으로 GridState + Transform 위치 동시 업데이트
            //    - GridState의 unitPositions, positionUnits 딕셔너리 업데이트
            //    - 물리적 Tile 컴포넌트 동기화
            //    - Transform.position 설정
            bool moved = context.GridController.MoveUnit(unitObject, position);
            if (!moved)
            {
                Debug.LogError($"SummonEffect: {position}에 유닛 배치 실패 - GridController.MoveUnit() failed");
                Object.Destroy(unitObject);
                return;
            }

            // 2. UnitService에 유닛 등록
            context.UnitService.RegisterUnit(unit);

            // 3. currentTile 설정 (GridController를 통해 Tile 찾기)
            var tile = context.GridController.GetTileAtPosition(position);
            if (tile != null)
            {
                unit.SetCurrentTile(tile);
                Debug.Log($"SummonEffect: {unit.name}의 currentTile 설정 완료 → {tile.name} ({position.x}, {position.y})");
            }
            else
            {
                Debug.LogWarning($"SummonEffect: 위치 ({position.x}, {position.y})에서 Tile을 찾을 수 없습니다.");
            }

            Debug.Log($"SummonEffect: {position}에 {_effectData.UnitToSummon.UnitName} 소환 완료 (Player: {isPlayerUnit})");
        }

        /// <summary>
        /// 시각적 효과를 재생합니다.
        /// </summary>
        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            if (_effectData.EffectPrefab != null)
            {
                var worldPos = new Vector3(targetPos.x, 0, targetPos.y);
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }

            if (!string.IsNullOrEmpty(_effectData.EffectAnimation))
            {
                // TODO: 애니메이션 재생 로직 구현
                Debug.Log($"SummonEffect: 애니메이션 재생 - {_effectData.EffectAnimation}");
            }
        }

        /// <summary>
        /// 소환 실패 시 Miss 효과 재생 (VFX Aware)
        /// </summary>
        private void PlayMissEffect(Vector2Int gridPos, GameContext context)
        {
            Debug.Log($"[SummonEffect] Playing miss effect at grid position {gridPos}");
            // TODO: Miss VFX 프리팹 재생 로직 구현
        }

        /// <summary>
        /// 소환 성공 시 Summon 효과 재생 (VFX Aware)
        /// </summary>
        private void PlaySummonEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[SummonEffect] Playing summon effect at world position {worldPos}");

            // VFXData의 이펙트 프리팹 사용
            if (_effectData.EffectPrefab != null)
            {
                Object.Instantiate(_effectData.EffectPrefab, worldPos, Quaternion.identity);
            }
        }

        public override string ToString()
        {
            var unitName = _effectData?.UnitToSummon?.UnitName ?? "Unknown";
            return $"SummonEffect[Unit: {unitName}, Count: {_effectData?.Value}, AffectedType: {_effectData?.AffectedType}, Range: {_effectData?.AffectedRange}]";
        }
    }
}