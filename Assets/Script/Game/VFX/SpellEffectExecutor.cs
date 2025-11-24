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

        #region Tile Buffers

        // AreaShape에 포함된 모든 타일 (모든 EffectDefinition 합집합)
        private readonly List<Tile> _areaTilesBuffer = new List<Tile>();

        // TargetFilter를 통과한 유효 타일 (모든 EffectDefinition 합집합)
        private readonly List<Tile> _validTilesBuffer = new List<Tile>();

        // VFXEventTrigger용 논리 타일 위치 (월드 좌표)
        private readonly List<Vector3> _logicPositionsBuffer = new List<Vector3>();

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

            var sortedEffects = effectDefinitions.OrderByDescending(e => e.Priority).ToList();

            var vfxData = sortedEffects[0].VFX;
            var vfxPlacementMode = sortedEffects[0].VFXPlacementMode;

            if (vfxData == null || vfxData.VFXPrefab == null)
            {
                Debug.LogWarning("[SpellEffectExecutor] No VFX data (EffectDefinition), executing effects immediately");
                ExecuteEffectsImmediate(sortedEffects, targetPos, context, null);
                return;
            }

            // TileBased 효과에 대해서만 타일 타겟을 계산
            CalculateAllPotentialTargets(sortedEffects, targetPos, context);

            StartCoroutine(ExecuteWithVFX(sortedEffects, vfxData, vfxPlacementMode, targetPos, context));
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
            VFXTilePlacementMode vfxPlacementMode,
            Vector2Int targetPos,
            GameContext context)
        {
            GameObject logicAnchor = null;
            bool hasError = false;
            bool effectsExecuted = false;
            float elapsed = 0f;

            float maxWaitTime = 0f;

            try
            {
                // 논리 앵커 위치: 항상 중심 타일 기준 (PositionOffset 없음)
                Vector3 anchorPos = context.GridController.CalculateWorldPositionWithHeight(targetPos);

                // 논리 앵커는 VFX 프리팹을 기반으로 생성하되,
                // ParticleSystem 및 Renderer를 제거하여 화면에는 보이지 않도록 합니다.
                // 이렇게 하면 프리팹에 설정된 VFXEventTrigger의 maxLifetime, maxWaitNormalizedTime 등이 그대로 적용됩니다.
                logicAnchor = Instantiate(vfxData.VFXPrefab, anchorPos, Quaternion.identity);
                StripVisualComponentsFromAnchor(logicAnchor);

                // 팀 기반 방향 적용: 플레이어는 정방향(+1), 적은 반대 방향(-1)으로 바라보도록
                // 루트 앵커의 z축 스케일을 조정합니다. 자식 VFX들은 이 스케일을 상속받습니다.
                ApplyTeamFacing(logicAnchor, context.CasterTeam);

                // 시각적 VFX 인스턴스를 PlacementMode에 따라 타일 위에 생성합니다.
                // 논리 앵커를 부모로 사용하여 PlaybackSpeed 및 생명 주기를 함께 관리합니다.
                SpawnVisualVFXInstances(
                    logicAnchor,
                    vfxData,
                    _areaTilesBuffer,
                    _validTilesBuffer,
                    context.GridController,
                    targetPos,
                    vfxPlacementMode);

                VFXEventTrigger trigger = logicAnchor.GetComponent<VFXEventTrigger>();
                if(trigger == null)
                {
                    trigger = logicAnchor.AddComponent<VFXEventTrigger>();
                }
                
                trigger.Initialize(
                    vfxData.TriggerNormalizedTime,
                    (triggerDataList) =>
                    {
                        ExecuteEffectsWithDataList(effects, triggerDataList, targetPos, context);
                        effectsExecuted = true;
                    },
                    _logicPositionsBuffer,
                    context.GridController,
                    vfxData.PlaybackSpeed
                );

                maxWaitTime = trigger.MaxWaitTime;

                _stateManager?.SetBusy(this, BusyType.GameFlowLock, maxWaitTime + 1f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpellEffectExecutor] VFX initialization error (EffectDefinition): {ex.Message}\n{ex.StackTrace}");
                hasError = true;
            }

            if (hasError)
            {
                ExecuteEffectsImmediate(effects, targetPos, context, null);
                if (logicAnchor != null)
                    Destroy(logicAnchor);

                _stateManager?.SetIdle(this, BusyType.GameFlowLock);
                Debug.Log("[SpellEffectExecutor] VFX execution error (EffectDefinition) - GameFlowLock released");
                yield break;
            }

            // maxWaitTime 동안은 항상 GameFlowLock을 유지합니다.
            // 효과(트리거)는 triggerNormalizedTime 시점에 먼저 실행될 수 있지만,
            // Idle 해제는 maxWaitTime 기준으로만 결정됩니다.
            while (elapsed < maxWaitTime)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!effectsExecuted)
            {
                Debug.LogWarning("[SpellEffectExecutor] VFX timeout (EffectDefinition), forcing execution");
                ExecuteEffectsWithDataList(effects, new System.Collections.Generic.List<VFXTriggerData>(), targetPos, context);
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
        /// 팀 기준으로 VFX의 z축 스케일을 조정하여
        /// 플레이어는 정방향(+1), 적군은 반대 방향(-1)을 바라보도록 합니다.
        /// </summary>
        private void ApplyTeamFacing(GameObject vfxInstance, TeamType casterTeam)
        {
            if (vfxInstance == null)
            {
                return;
            }

            float sign = casterTeam == TeamType.Enemy ? -1f : 1f;
            Transform t = vfxInstance.transform;
            Vector3 scale = t.localScale;
            scale.z *= sign;
            t.localScale = scale;
        }

        /// <summary>
        /// 논리 앵커에서 ParticleSystem 및 Renderer를 제거하여
        /// 화면에는 보이지 않고, VFXEventTrigger 설정만 유지되도록 합니다.
        /// </summary>
        private void StripVisualComponentsFromAnchor(GameObject logicAnchor)
        {
            if (logicAnchor == null)
            {
                return;
            }

            // ParticleSystem 제거
            var particleSystems = logicAnchor.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    Destroy(ps);
                }
            }

            // Renderer 제거 (MeshRenderer, SkinnedMeshRenderer 등 포함)
            var renderers = logicAnchor.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    Destroy(renderer);
                }
            }
        }

        /// <summary>
        /// PlacementMode에 따라 시각적 VFX 인스턴스를 타일 위에 생성합니다.
        /// 논리 앵커를 부모로 사용하여 PlaybackSpeed 및 생명 주기를 함께 관리합니다.
        /// </summary>
        private void SpawnVisualVFXInstances(
            GameObject logicAnchor,
            VFXData vfxData,
            List<Tile> areaTilesAll,
            List<Tile> validTilesAll,
            IGridController gridController,
            Vector2Int centerGridPos,
            VFXTilePlacementMode placementMode)
        {
            if (logicAnchor == null || vfxData == null || vfxData.VFXPrefab == null)
            {
                return;
            }

            if (gridController == null)
            {
                return;
            }

            var visualTiles = new List<Tile>();
            var centerTile = gridController.GetTileAtPosition(centerGridPos);
            var validSet = new HashSet<Tile>(validTilesAll);
            var invalidTilesAll = areaTilesAll
                .Where(t => t != null && !validSet.Contains(t))
                .ToList();

            switch (placementMode)
            {
                case VFXTilePlacementMode.CenterOnly:
                    if (centerTile != null)
                    {
                        visualTiles.Add(centerTile);
                    }
                    break;

                case VFXTilePlacementMode.AllAreaTiles:
                    visualTiles.AddRange(areaTilesAll.Where(t => t != null));
                    break;

                case VFXTilePlacementMode.ValidTilesOnly:
                    visualTiles.AddRange(validTilesAll.Where(t => t != null && t != centerTile));
                    break;

                case VFXTilePlacementMode.InvalidTilesOnly:
                    visualTiles.AddRange(invalidTilesAll);
                    break;

                default:
                    visualTiles.AddRange(validTilesAll.Where(t => t != null));
                    break;
            }

            foreach (var tile in visualTiles)
            {
                if (tile == null) continue;

                var gridPos = tile.GetGridPosition();
                Vector3 worldWithHeight = gridController.CalculateWorldPositionWithHeight(gridPos);
                var spawnPos = worldWithHeight + vfxData.PositionOffset;

                var clone = Instantiate(
                    vfxData.VFXPrefab,
                    spawnPos,
                    Quaternion.identity,
                    logicAnchor.transform);

                RemoveVFXEventTriggersFromClone(clone);
            }
        }

        /// <summary>
        /// 서브 VFX 인스턴스에서 VFXEventTrigger 컴포넌트를 제거하여
        /// 타일 검증, 콜백, 오디오 재생 등이 중복 실행되지 않도록 합니다.
        /// </summary>
        private void RemoveVFXEventTriggersFromClone(GameObject clone)
        {
            if (clone == null)
            {
                return;
            }

            var triggers = clone.GetComponentsInChildren<VFXEventTrigger>(true);
            foreach (var t in triggers)
            {
                if (t != null)
                {
                    Destroy(t);
                }
            }
        }

        /// <summary>
        /// EffectDefinition 기반 타일 타겟 계산
        /// TileBased 효과만 포함하여 PredeterminedTiles를 설정합니다.
        /// 내부 버퍼(_areaTilesBuffer, _validTilesBuffer, _logicPositionsBuffer)를 갱신하여
        /// 이후 VFX 논리/시각 타일 계산에 사용합니다.
        /// </summary>
        private void CalculateAllPotentialTargets(IReadOnlyList<EffectDefinition> effects, Vector2Int targetPos, GameContext context)
        {
            context.PredeterminedTiles.Clear();
            context.VFXPositions.Clear();

            _areaTilesBuffer.Clear();
            _validTilesBuffer.Clear();
            _logicPositionsBuffer.Clear();

            var areaSet = new HashSet<Tile>();
            var validSet = new HashSet<Tile>();

            foreach (var definition in effects)
            {
                if (definition == null)
                    continue;

                if (definition.TargetScope == EffectTargetScope.Global)
                {
                    // 전역 효과는 타일 타겟 계산에 포함하지 않음
                    continue;
                }

                // 1) AreaShape 전체 타일 (VFX 및 논리 superset)
                var areaTiles = EffectTargetingHelper.GetAreaTiles(
                    targetPos,
                    definition,
                    context);

                foreach (var tile in areaTiles)
                {
                    if (tile != null)
                    {
                        areaSet.Add(tile);
                    }
                }

                // 2) 유효 타겟 타일 (AreaShape + TargetFilter)
                var targetTiles = EffectTargetingHelper.GetTargetTiles(
                    targetPos,
                    definition,
                    context);

                foreach (var tile in targetTiles)
                {
                    if (tile != null)
                    {
                        validSet.Add(tile);
                    }
                }
            }

            // PredeterminedTiles: 항상 "유효 타겟 타일" 기준
            _validTilesBuffer.AddRange(validSet);
            context.PredeterminedTiles.AddRange(_validTilesBuffer);

            // AreaTiles: VFX 및 논리 superset
            _areaTilesBuffer.AddRange(areaSet);

            // 논리용 타일 위치 (VFXEventTrigger 검증용) - Area 전체 기준
            _logicPositionsBuffer.AddRange(
                EffectTargetingHelper.TilesToWorldPositions(_areaTilesBuffer));

            // 기존 VFXPositions 필드가 필요하다면 논리 타일 위치를 그대로 저장
            context.VFXPositions.AddRange(_logicPositionsBuffer);
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
