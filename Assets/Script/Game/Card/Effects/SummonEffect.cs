using UnityEngine;
using System.Collections.Generic;
using Game.VFX;
using Game.Interfaces;
namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 소환 효과 구현
    /// ICardEffect를 구현하여 대상 위치에 유닛을 소환하는 효과입니다.
    /// VFX Dynamic Data System: IVFXAwareEffect 구현으로 공격 성공/실패 반응
    /// </summary>
    public class SummonEffect : IVFXAwareEffect
    {
        private readonly SummonEffectDefinition _definition;

        public EffectType EffectType => EffectType.Summon;
        public int Priority => _definition?.Priority ?? 0;

        public SummonEffect(SummonEffectDefinition definition)
        {
            _definition = definition ?? throw new System.ArgumentNullException(nameof(definition));

            if (_definition.UnitToSummon == null)
            {
                throw new System.ArgumentException("SummonEffectDefinition에는 UnitToSummon이 필요합니다.");
            }
        }

        public bool CanExecute(Vector2Int targetPos, GameContext context)
        {
            if (_definition == null)
            {
                Debug.LogWarning("SummonEffect: 유효하지 않은 SummonEffectDefinition입니다.");
                return false;
            }

            if (context == null || !context.IsValid())
            {
                Debug.LogWarning("SummonEffect: 유효하지 않은 GameContext입니다.");
                return false;
            }

            // 타일 기반: 사전 계산된 타일이 있는지 확인
            return context.PredeterminedTiles != null && context.PredeterminedTiles.Count > 0;
        }

        public void Execute(Vector2Int targetPos, GameContext context)
        {
            if (!CanExecute(targetPos, context))
            {
                Debug.LogWarning("SummonEffect: 실행 조건을 만족하지 않습니다.");
                return;
            }

            // 타일 기반: 사전 계산된 타일에 소환
            var tilesToSummon = context.PredeterminedTiles;
            int count = _definition.Count;
            var summonCount = Mathf.Min(count, tilesToSummon.Count);

            var unitData = _definition.UnitToSummon;

            Debug.Log($"SummonEffect: {unitData.UnitName}을(를) {summonCount}개 소환합니다.");

            for (int i = 0; i < summonCount; i++)
            {
                SummonUnitAtTile(tilesToSummon[i], context);
            }

            // 시각적 효과 재생
            PlayVisualEffect(targetPos, context);
        }

        /// <summary>
        /// VFX TriggerData를 포함한 효과 실행 (IVFXAwareEffect 구현)
        /// Phase 3.1: 타일 기반 타겟팅으로 리팩토링
        /// 공격 성공/실패 여부에 따라 소환 적용 여부 결정
        /// </summary>
        public void ExecuteWithVFXData(Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            // VFX 트리거 시점 검증
            if (!triggerData.AttackSuccess)
            {
                Debug.Log($"[SummonEffect] Summon failed: {triggerData.ValidationFailureReason}");
                PlayMissEffect(targetPos, context);
                return;
            }

            // ✅ 타일 기반 타겟팅: 타일 좌표로 타일 조회
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

        /// <summary>
        /// Phase 3.1: 타일 기반 소환 헬퍼 메서드 (신규)
        /// 타일 객체를 받아 해당 타일에 유닛을 소환합니다.
        /// </summary>
        private void SummonUnitAtTile(Tile tile, GameContext context)
        {
            if (tile == null)
            {
                Debug.LogError("[SummonEffect] Tile is null");
                return;
            }

            Vector2Int position = tile.GetGridPosition();
            Vector3 worldPosition = tile.transform.position;

            var unitData = _definition.UnitToSummon;

            if (unitData == null || unitData.Prefab == null)
            {
                Debug.LogError("SummonEffect: UnitToSummon 또는 Prefab이 null입니다.");
                return;
            }

            if (context.GridController == null || context.UnitService == null)
            {
                Debug.LogError("SummonEffect: GridController 또는 UnitService가 null입니다.");
                return;
            }

            // 유닛 프리팹 인스턴스화 (타일의 월드 좌표 사용)
            var unitObject = Object.Instantiate(unitData.Prefab, worldPosition, Quaternion.identity);

            if (unitObject == null)
            {
                Debug.LogError($"SummonEffect: {unitData.UnitName} 프리팹 인스턴스화 실패");
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

            // 유닛 초기화 (팀 타입 기반으로 팀 결정)
            bool isPlayerUnit = (context.CasterTeam == TeamType.Player);
            unit.Init(unitData, position, isPlayerUnit);

            // 소환 원본 카드 메타데이터 설정
            var cardLink = unitObject.GetComponent<UnitCardLink>();
            if (cardLink == null)
            {
                cardLink = unitObject.AddComponent<UnitCardLink>();
            }
            cardLink.SetSourceCard(context.SourceCard);

            // GridController에 유닛 배치
            bool moved = context.GridController.MoveUnit(unitObject, position);
            if (!moved)
            {
                Debug.LogError($"SummonEffect: {position}에 유닛 배치 실패 - GridController.MoveUnit() failed");
                Object.Destroy(unitObject);
                return;
            }

            // UnitService에 유닛 등록
            context.UnitService.RegisterUnit(unit);

            // currentTile 설정
            unit.SetCurrentTile(tile);

            // 팀에 따라 유닛의 Z축 스케일 조정
            Vector3 scale = unitObject.transform.localScale;
            scale.z *= isPlayerUnit ? 1f : -1f;
            unitObject.transform.localScale = scale;

            Debug.Log($"[SummonEffect] Unit spawned at tile {tile.GetGridPosition()}");
        }

        /// <summary>
        /// 시각적 효과를 재생합니다.
        /// VFX는 SpellEffectExecutor에서 처리되므로 여기서는 로그만 남김
        /// </summary>
        private void PlayVisualEffect(Vector2Int targetPos, GameContext context)
        {
            Debug.Log($"[SummonEffect] Visual effect requested at {targetPos}");
            // VFX는 VFXData를 통해 SpellEffectExecutor에서 처리됨
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
        /// VFX는 SpellEffectExecutor에서 처리되므로 여기서는 로그만 남김
        /// </summary>
        private void PlaySummonEffect(Vector3 worldPos, GameContext context)
        {
            Debug.Log($"[SummonEffect] Playing summon effect at world position {worldPos}");
            // VFX는 VFXData를 통해 SpellEffectExecutor에서 처리됨
        }

        public override string ToString()
        {
            var unitName = _definition?.UnitToSummon?.UnitName ?? "Unknown";
            return $"SummonEffect[Unit: {unitName}, Count: {_definition?.Count}, Scope: {_definition?.TargetScope}]";
        }
    }
}
