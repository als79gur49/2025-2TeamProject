using UnityEngine;
using System.Collections.Generic;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 소환 효과 구현
    /// ICardEffect를 구현하여 대상 위치에 유닛을 소환하는 효과입니다.
    /// </summary>
    public class SummonEffect : ICardEffect
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
        /// 소환 가능한 위치들을 찾습니다.
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
                // 범위 내 모든 유효한 위치에서 소환 가능
                for (int x = -_effectData.AffectedRange; x <= _effectData.AffectedRange; x++)
                {
                    for (int y = -_effectData.AffectedRange; y <= _effectData.AffectedRange; y++)
                    {
                        var checkPos = targetPos + new Vector2Int(x, y);
                        if (IsValidSummonPosition(checkPos, context))
                        {
                            availablePositions.Add(checkPos);
                        }
                    }
                }
            }

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

            // GridController에 유닛 위치 설정 (월드 좌표 및 시각적 배치)
            context.GridController.SetUnitWorldPosition(unitObject, position);

            // UnitService에 유닛 등록
            context.UnitService.RegisterUnit(unit);

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

        public override string ToString()
        {
            var unitName = _effectData?.UnitToSummon?.UnitName ?? "Unknown";
            return $"SummonEffect[Unit: {unitName}, Count: {_effectData?.Value}, AffectedType: {_effectData?.AffectedType}, Range: {_effectData?.AffectedRange}]";
        }
    }
}