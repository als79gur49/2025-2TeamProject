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
        /// </summary>
        private bool IsValidSummonPosition(Vector2Int position, GameContext context)
        {
            // SpawnValidator를 통해 소환 유효성 검사
            if (context.SpawnValidator == null)
            {
                Debug.LogWarning("SummonEffect: SpawnValidator가 null입니다.");
                return false;
            }

            // TODO: SpawnValidator를 통한 실제 검증 로직 구현
            // - 그리드 경계 내부인가?
            // - 이미 유닛이 있는 위치인가?
            // - 지형이 소환 가능한가?
            // - AffectedType에 따른 소환 권한이 있는가?

            return IsPositionInBounds(position, context) &&
                   IsPositionEmpty(position, context) &&
                   IsValidTerrainForSummon(position, context) &&
                   HasSummonPermission(position, context);
        }

        /// <summary>
        /// 위치가 그리드 경계 내부인지 확인합니다.
        /// </summary>
        private bool IsPositionInBounds(Vector2Int position, GameContext context)
        {
            // TODO: GridController를 통한 실제 경계 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 위치에 다른 유닛이 없는지 확인합니다.
        /// </summary>
        private bool IsPositionEmpty(Vector2Int position, GameContext context)
        {
            // TODO: UnitService를 통한 실제 유닛 존재 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 지형이 소환에 적합한지 확인합니다.
        /// </summary>
        private bool IsValidTerrainForSummon(Vector2Int position, GameContext context)
        {
            // TODO: GridController를 통한 실제 지형 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 해당 위치에서 소환할 권한이 있는지 확인합니다.
        /// </summary>
        private bool HasSummonPermission(Vector2Int position, GameContext context)
        {
            // AffectedType에 따른 소환 권한 확인
            return _effectData.AffectedType switch
            {
                AffectedType.Ally => IsAllyTerritory(position, context.PlayerId),
                AffectedType.Enemy => IsEnemyTerritory(position, context.PlayerId),
                AffectedType.Any => true,
                AffectedType.None => false,
                _ => false
            };
        }

        /// <summary>
        /// 아군 영역인지 확인합니다.
        /// </summary>
        private bool IsAllyTerritory(Vector2Int position, int playerId)
        {
            // TODO: 실제 영역 소속 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 적군 영역인지 확인합니다.
        /// </summary>
        private bool IsEnemyTerritory(Vector2Int position, int playerId)
        {
            // TODO: 실제 영역 소속 확인 로직 구현
            return true; // placeholder
        }

        /// <summary>
        /// 특정 위치에 유닛을 소환합니다.
        /// </summary>
        private void SummonUnitAtPosition(Vector2Int position, GameContext context)
        {
            // TODO: 실제 유닛 소환 로직 구현
            // - UnitService를 통한 유닛 생성
            // - 그리드에 유닛 배치
            // - 소환된 유닛의 소유권 설정
            // - 유닛 초기화 및 활성화

            Debug.Log($"SummonEffect: {position}에 {_effectData.UnitToSummon.UnitName} 소환 완료");

            // CardSpawnService를 통한 소환 (기존 시스템과의 호환성)
            if (context.CardSpawnService != null)
            {
                // TODO: CardSpawnService의 실제 소환 메서드 호출
                // context.CardSpawnService.SummonUnit(_effectData.UnitToSummon, position, context.PlayerId);
            }
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