using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;
using Game.Data;
using Game.Interfaces;
using Game.Core;
using Game.Services;

namespace Game.VFX
{
    /// <summary>
    /// VFX 기반 효과 실행 중재자 (Mediator Pattern + ServiceLocator)
    /// Phase 2 완료: 다중 타겟 원자적 검증 시스템 구현
    /// Phase 3: GlobalStateManager 통합 - VFX 재생 중 게임 흐름 잠금
    /// GameInitializer에서 Inspector 직렬화를 통해 등록됨
    /// </summary>
    public class SpellEffectExecutor : MonoBehaviour, ISpellEffectExecutor
    {
        #region ServiceLocator Integration

        // ✅ Singleton 패턴 제거 - ServiceLocator 패턴으로 전환
        // GameInitializer의 Inspector에서 직렬화 필드로 참조하여 등록
        // 사용법: ServiceLocator.Get<ISpellEffectExecutor>().ExecuteBatch(...)

        // GlobalStateManager 참조 (VFX 재생 중 게임 흐름 잠금용)
        private IGlobalStateManager _stateManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // GlobalStateManager 초기화
            _stateManager = ServiceLocator.Get<IGlobalStateManager>();

            if (_stateManager == null)
            {
                Debug.LogWarning("[SpellEffectExecutor] IGlobalStateManager not found - VFX blocking disabled");
            }
        }

        private void OnDestroy()
        {
            // 혹시 잠금이 활성 상태라면 강제 해제
            _stateManager?.SetIdle(this, BusyType.GameFlowLock);
            Debug.Log("[SpellEffectExecutor] OnDestroy - Released any active locks");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 여러 EffectDefinition 기반 효과를 단일 VFX로 실행 (신규 경로)
        /// </summary>
        public void ExecuteBatch(IReadOnlyList<EffectDefinition> effectDefinitions, Vector2Int targetPos, GameContext context)
        {
            if (effectDefinitions == null || effectDefinitions.Count == 0)
            {
                Debug.LogWarning("[SpellEffectExecutor] ExecuteBatch(EffectDefinition): Empty effect list");
                return;
            }

            var sortedEffects = effectDefinitions.OrderBy(e => e.Priority).ToList();

            var vfxData = sortedEffects[0].VFX;

            if (vfxData == null || vfxData.VFXPrefab == null)
            {
                Debug.LogWarning("[SpellEffectExecutor] No VFX data (EffectDefinition), executing effects immediately");
                ExecuteEffectsImmediate(sortedEffects, targetPos, context, null);
                return;
            }

            // TileBased 효과에 대해서만 타일 타겟을 계산
            CalculateAllPotentialTargets(sortedEffects, targetPos, context);

            StartCoroutine(ExecuteWithVFX(sortedEffects, vfxData, targetPos, context));
        }

        #endregion

        #region VFX Execution Coroutine

        /// <summary>
        /// Phase 3: 타일 기반 다중 타겟 VFX 실행 Coroutine
        /// 타겟 정보는 context.VFXPositions에서 가져옴
        /// Phase 4: GlobalStateManager 통합 - VFX 재생 중 GameFlowLock 설정
        /// </summary>
        /// <summary>
        /// EffectDefinition 기반 VFX 실행 코루틴
        /// </summary>
        private IEnumerator ExecuteWithVFX(
            IReadOnlyList<EffectDefinition> effects,
            VFXData vfxData,
            Vector2Int targetPos,
            GameContext context)
        {
            GameObject vfxInstance = null;
            Vector3 worldPos = Vector3.zero;
            bool hasError = false;
            bool effectsExecuted = false;
            float maxWait = 0f;
            float elapsed = 0f;
            float actualDuration = 10f;

            _stateManager?.SetBusy(this, BusyType.GameFlowLock);

            try
            {
                worldPos = context.GridController.CalculateWorldPositionWithHeight(targetPos);

                if (vfxData.PositionOffset != Vector3.zero)
                {
                    worldPos += vfxData.PositionOffset;
                    Debug.Log($"[SpellEffectExecutor] Applied VFX offset: {vfxData.PositionOffset}, Final position: {worldPos}");
                }

                vfxInstance = Instantiate(vfxData.VFXPrefab, worldPos, Quaternion.identity);

                VFXEventTrigger trigger = vfxInstance.GetComponent<VFXEventTrigger>();
                if (trigger == null)
                    trigger = vfxInstance.AddComponent<VFXEventTrigger>();

                float durationParam = vfxData.UseAutoDuration ? -1f : vfxData.ManualDuration;

                trigger.Initialize(
                    vfxData.TriggerNormalizedTime,
                    (triggerDataList) =>
                    {
                        ExecuteEffectsWithDataList(effects, triggerDataList, targetPos, context);
                        effectsExecuted = true;
                    },
                    context.VFXPositions,
                    context.GridController,
                    vfxData.PlaybackSpeed,
                    durationParam
                );

                actualDuration = vfxData.UseAutoDuration ? 10f : vfxData.ManualDuration;
                maxWait = vfxData.IsLooping ? 10f : actualDuration + 1f;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpellEffectExecutor] VFX initialization error (EffectDefinition): {ex.Message}\n{ex.StackTrace}");
                hasError = true;
            }

            if (hasError)
            {
                ExecuteEffectsImmediate(effects, targetPos, context, null);
                if (vfxInstance != null)
                    Destroy(vfxInstance);

                _stateManager?.SetIdle(this, BusyType.GameFlowLock);
                Debug.Log("[SpellEffectExecutor] VFX execution error (EffectDefinition) - GameFlowLock released");
                yield break;
            }

            while (!effectsExecuted && elapsed < maxWait)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!effectsExecuted)
            {
                Debug.LogWarning("[SpellEffectExecutor] VFX timeout (EffectDefinition), forcing execution");
                ExecuteEffectsWithDataList(effects, new System.Collections.Generic.List<VFXTriggerData>(), targetPos, context);
            }

            if (!vfxData.IsLooping)
            {
                float cleanupDuration = vfxData.UseAutoDuration ? actualDuration : vfxData.ManualDuration;
                yield return new WaitForSeconds(cleanupDuration);
                if (vfxInstance != null)
                    Destroy(vfxInstance);
            }

            _stateManager?.SetIdle(this, BusyType.GameFlowLock);
            Debug.Log("[SpellEffectExecutor] VFX execution completed (EffectDefinition) - GameFlowLock released");
        }

        #endregion

        #region Effect Execution with TriggerData

        /// <summary>
        /// EffectDefinition 기반 효과 실행 (TriggerData 리스트 사용)
        /// Global / TileBased를 구분하여 실행합니다.
        /// </summary>
        private void ExecuteEffectsWithDataList(
            IReadOnlyList<EffectDefinition> effects,
            List<VFXTriggerData> triggerDataList,
            Vector2Int targetPos,
            GameContext context)
        {
            Debug.Log($"[SpellEffectExecutor] Executing {effects.Count} EffectDefinitions on {triggerDataList.Count} validated targets");

            foreach (var definition in effects)
            {
                try
                {
                    ICardEffect effect = CreateEffectInstance(definition);
                    if (effect == null)
                    {
                        Debug.LogWarning($"[SpellEffectExecutor] Failed to create effect from definition: {definition.name}");
                        continue;
                    }

                    // 전역(Global) 효과: 타일/TriggerData에 의존하지 않고 한 번만 실행
                    if (definition.TargetScope == EffectTargetScope.Global)
                    {
                        ExecuteGlobalEffect(definition, effect, targetPos, context);
                        continue;
                    }

                    // TileBased 효과: TriggerData 리스트를 필터링하여 각 타겟에 대해 실행
                    var relevantTriggers = FilterTriggersForEffect(triggerDataList, definition, targetPos, context);

                    Debug.Log($"[SpellEffectExecutor] EffectDefinition {definition.EffectType}: {relevantTriggers.Count} relevant targets");

                    foreach (var triggerData in relevantTriggers)
                    {
                        Vector2Int gridPos2D = new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y);

                        if (effect is IVFXAwareEffect vfxAwareEffect)
                        {
                            vfxAwareEffect.ExecuteWithVFXData(gridPos2D, context, triggerData);
                        }
                        else
                        {
                            effect.Execute(gridPos2D, context);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SpellEffectExecutor] EffectDefinition execution error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// EffectDefinition 기반 TriggerData 필터링
        /// AreaShape + TargetFilter 조합을 사용합니다.
        /// </summary>
        private List<VFXTriggerData> FilterTriggersForEffect(
            List<VFXTriggerData> triggerDataList,
            EffectDefinition definition,
            Vector2Int centerPos,
            GameContext context)
        {
            var filtered = new List<VFXTriggerData>();

            if (definition.TargetScope == EffectTargetScope.Global)
            {
                // 전역 효과는 타일 기반 TriggerData와 무관
                return filtered;
            }

            if (definition.AreaShape == null)
            {
                Debug.LogWarning($"[SpellEffectExecutor] EffectDefinition {definition.name} has no AreaShape");
                return filtered;
            }

            foreach (var triggerData in triggerDataList)
            {
                if (!triggerData.AttackSuccess)
                {
                    Debug.Log($"[SpellEffectExecutor] Target validation failed: {triggerData.ValidationFailureReason}");
                    continue;
                }

                if (triggerData.TileGridPosition == Vector3Int.zero && centerPos != Vector2Int.zero)
                {
                    Debug.LogError($"[SpellEffectExecutor] Invariant violation (EffectDefinition): AttackSuccess is true but TileGridPosition is invalid at {triggerData.TileWorldPosition}");
                    continue;
                }

                Vector2Int tilePos2D = new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y);

                // 1. AreaShape 범위 안인지 확인
                if (!definition.AreaShape.IsInArea(centerPos, tilePos2D, context.GridController))
                {
                    continue;
                }

                // 2. TargetFilter 조건 확인
                var tile = context.GridController.GetTileAtPosition(tilePos2D);
                var unit = tile?.OccupyingUnit;

                if (definition.TargetFilter != null &&
                    !definition.TargetFilter.Matches(tile, unit, context))
                {
                    continue;
                }

                filtered.Add(triggerData);
            }

            return filtered;
        }

        /// <summary>
        /// 유닛이 시전자와 같은 팀인지 확인
        /// </summary>
        private bool IsSameTeam(Unit unit, TeamType casterTeam)
        {
            if (unit == null) return false;
            TeamType unitTeam = unit.IsPlayerUnit ? TeamType.Player : TeamType.Enemy;
            return unitTeam == casterTeam;
        }


        /// <summary>
        /// EffectDefinition 기반 즉시 효과 실행 (VFX 없이)
        /// </summary>
        private void ExecuteEffectsImmediate(IReadOnlyList<EffectDefinition> effects, Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            foreach (var definition in effects)
            {
                try
                {
                    ICardEffect effect = CreateEffectInstance(definition);
                    if (effect == null) continue;

                    if (definition.TargetScope == EffectTargetScope.Global)
                    {
                        ExecuteGlobalEffect(definition, effect, targetPos, context);
                        continue;
                    }

                    if (effect is IVFXAwareEffect vfxAwareEffect && triggerData != null)
                    {
                        vfxAwareEffect.ExecuteWithVFXData(targetPos, context, triggerData);
                    }
                    else
                    {
                        effect.Execute(targetPos, context);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SpellEffectExecutor] Immediate execution error (EffectDefinition): {ex.Message}");
                }
            }
        }

        #endregion

        #region Effect Factory

        private ICardEffect CreateEffectInstance(EffectDefinition definition)
        {
            return CardEffectFactory.CreateEffect(definition);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 지정된 그리드 위치의 GameObject를 반환합니다.
        /// VFXEventTrigger가 타겟 유닛을 추적하기 위해 사용됩니다.
        /// </summary>
        private GameObject GetTargetObject(Vector2Int gridPos, GameContext context)
        {
            if (context == null || context.GridController == null)
            {
                Debug.LogWarning("[SpellEffectExecutor] GetTargetObject: Invalid context or GridController");
                return null;
            }

            // GridController를 통해 해당 위치의 유닛 GameObject 검색
            GameObject targetUnit = context.GridController.GetUnitAtPosition(gridPos);

            if (targetUnit != null)
            {
                Debug.Log($"[SpellEffectExecutor] Target unit found at ({gridPos.x}, {gridPos.y}): {targetUnit.name}");
            }
            else
            {
                Debug.Log($"[SpellEffectExecutor] No unit at position ({gridPos.x}, {gridPos.y})");
            }

            return targetUnit;
        }

        /// <summary>
        /// <summary>
        /// EffectDefinition 기반 타일 타겟 계산
        /// TileBased 효과만 포함하여 PredeterminedTiles 및 VFXPositions를 설정합니다.
        /// </summary>
        private List<GameObject> CalculateAllPotentialTargets(
            IReadOnlyList<EffectDefinition> effects,
            Vector2Int targetPos,
            GameContext context)
        {
            context.PredeterminedTiles.Clear();
            context.VFXPositions.Clear();

            var uniqueTiles = new HashSet<Tile>();

            foreach (var definition in effects)
            {
                if (definition.TargetScope == EffectTargetScope.Global)
                {
                    // 전역 효과는 타일 타겟 계산에 포함하지 않음
                    continue;
                }

                var tiles = EffectTargetingHelper.GetTargetTiles(
                    targetPos,
                    definition,
                    context);

                foreach (var tile in tiles)
                {
                    uniqueTiles.Add(tile);
                }
            }

            context.PredeterminedTiles.AddRange(uniqueTiles);
            context.VFXPositions = EffectTargetingHelper.TilesToWorldPositions(
                context.PredeterminedTiles);

            var targetGameObjects = new List<GameObject>();
            foreach (var tile in uniqueTiles)
            {
                if (tile.OccupyingUnit != null)
                {
                    targetGameObjects.Add(tile.OccupyingUnit.gameObject);
                }
                else
                {
                    targetGameObjects.Add(tile.gameObject);
                }
            }

            return targetGameObjects;
        }

        /// <summary>
        /// 전역(Global) 효과 실행 헬퍼
        /// 타일 기반 TriggerData와 무관하게 한 번만 실행합니다.
        /// </summary>
        private void ExecuteGlobalEffect(EffectDefinition definition, ICardEffect effect, Vector2Int targetPos, GameContext context)
        {
            if (effect is IVFXAwareEffect vfxAwareEffect)
            {
                var dummyTriggerData = new VFXTriggerData();
                dummyTriggerData.SetTileTargetValid(
                    new Vector3Int(targetPos.x, targetPos.y, 0),
                    context.GridController.GridToWorldPosition(targetPos));

                vfxAwareEffect.ExecuteWithVFXData(targetPos, context, dummyTriggerData);
            }
            else
            {
                effect.Execute(targetPos, context);
            }

            Debug.Log($"[SpellEffectExecutor] Global effect {definition.EffectType} executed");
        }


        #endregion
    }
}
