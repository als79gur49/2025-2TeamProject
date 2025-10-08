using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Card.Effects;
using Game.Data;
using Game.Interfaces;
using Game.Core;

namespace Game.VFX
{
    /// <summary>
    /// VFX 기반 효과 실행 중재자 (Mediator Pattern + ServiceLocator)
    /// Phase 2 완료: 다중 타겟 원자적 검증 시스템 구현
    /// GameInitializer에서 Inspector 직렬화를 통해 등록됨
    /// </summary>
    public class SpellEffectExecutor : MonoBehaviour, ISpellEffectExecutor
    {
        #region ServiceLocator Integration

        // ✅ Singleton 패턴 제거 - ServiceLocator 패턴으로 전환
        // GameInitializer의 Inspector에서 직렬화 필드로 참조하여 등록
        // 사용법: ServiceLocator.Get<ISpellEffectExecutor>().ExecuteBatch(...)

        #endregion

        #region Public API

        /// <summary>
        /// 여러 효과를 단일 VFX로 실행 (TriggerData 기반)
        /// </summary>
        /// <param name="effectDataList">읽기 전용 효과 데이터 리스트</param>
        /// <param name="targetPos">타겟 그리드 위치</param>
        /// <param name="context">게임 컨텍스트</param>
        public void ExecuteBatch(IReadOnlyList<EffectData> effectDataList, Vector2Int targetPos, GameContext context)
        {
            if (effectDataList == null || effectDataList.Count == 0)
            {
                Debug.LogWarning("[SpellEffectExecutor] ExecuteBatch: Empty effect list");
                return;
            }

            // 우선순위 정렬
            var sortedEffects = effectDataList.OrderBy(e => e.Priority).ToList();

            // VFX 데이터 가져오기 (첫 번째 효과의 VFX 사용)
            VFXData vfxData = sortedEffects[0].VFXData;

            if (vfxData == null || vfxData.VFXPrefab == null)
            {
                Debug.LogWarning("[SpellEffectExecutor] No VFX data, executing effects immediately");
                ExecuteEffectsImmediate(sortedEffects, targetPos, context, null);
                return;
            }

            // 🆕 모든 잠재적 타일 타겟 사전 계산 (Context에 저장됨)
            CalculateAllPotentialTargets(sortedEffects, targetPos, context);

            // Coroutine 실행 (타일 위치는 context.VFXPositions에서 가져옴)
            StartCoroutine(ExecuteWithVFX(sortedEffects, vfxData, targetPos, context));
        }

        #endregion

        #region VFX Execution Coroutine

        /// <summary>
        /// Phase 3: 타일 기반 다중 타겟 VFX 실행 Coroutine
        /// 타겟 정보는 context.VFXPositions에서 가져옴
        /// </summary>
        private IEnumerator ExecuteWithVFX(
            IReadOnlyList<EffectData> effects,
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

            // VFX 생성 및 초기화 (try-catch 사용, yield return 없음)
            try
            {
                worldPos = context.GridController.GridToWorldPosition(targetPos);
                vfxInstance = Instantiate(vfxData.VFXPrefab, worldPos, Quaternion.identity);

                // VFXEventTrigger 초기화 (리스트 콜백)
                VFXEventTrigger trigger = vfxInstance.GetComponent<VFXEventTrigger>();
                if (trigger == null)
                    trigger = vfxInstance.AddComponent<VFXEventTrigger>();

                // 🆕 리스트 기반 콜백으로 변경 (타일 위치 전달)
                trigger.Initialize(
                    vfxData.TriggerNormalizedTime,
                    (triggerDataList) => {
                        // 리스트 기반 효과 실행
                        ExecuteEffectsWithDataList(effects, triggerDataList, targetPos, context);
                        effectsExecuted = true;
                    },
                    context.VFXPositions,       // 🆕 타일 월드 좌표 리스트 전달
                    context.GridController      // 🆕 GridController 전달
                );

                maxWait = vfxData.IsLooping ? 10f : vfxData.Duration + 1f;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpellEffectExecutor] VFX initialization error: {ex.Message}\n{ex.StackTrace}");
                hasError = true;
            }

            // 에러 발생 시 즉시 실행 후 종료
            if (hasError)
            {
                ExecuteEffectsImmediate(effects, targetPos, context, null);
                if (vfxInstance != null)
                    Destroy(vfxInstance);
                yield break;
            }

            // VFX 재생 대기 (try-catch 없이 yield return 사용 가능)
            while (!effectsExecuted && elapsed < maxWait)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            // Timeout 처리
            if (!effectsExecuted)
            {
                Debug.LogWarning($"[SpellEffectExecutor] VFX timeout, forcing execution");
                // 🆕 빈 리스트로 강제 실행
                ExecuteEffectsWithDataList(effects, new List<VFXTriggerData>(), targetPos, context);
            }

            // VFX 정리 (루핑이 아닌 경우)
            if (!vfxData.IsLooping)
            {
                yield return new WaitForSeconds(vfxData.Duration);
                if (vfxInstance != null)
                    Destroy(vfxInstance);
            }

            // ✅ ServiceLocator 패턴으로 변경되어 더 이상 gameObject를 파괴하지 않음
            // GameObject는 GameInitializer에서 관리됨
        }

        #endregion

        #region Effect Execution with TriggerData

        /// <summary>
        /// Phase 2: 검증된 TriggerData 리스트를 기반으로 효과 실행
        /// AffectedType.None 효과 별도 처리 포함 (문제 3 해결)
        /// </summary>
        private void ExecuteEffectsWithDataList(
            IReadOnlyList<EffectData> effects,
            List<VFXTriggerData> triggerDataList,
            Vector2Int targetPos,
            GameContext context)
        {
            Debug.Log($"[SpellEffectExecutor] Executing {effects.Count} effects on {triggerDataList.Count} validated targets");

            foreach (var effectData in effects)
            {
                try
                {
                    ICardEffect effect = CreateEffectInstance(effectData);
                    if (effect == null)
                    {
                        Debug.LogWarning($"[SpellEffectExecutor] Failed to create effect: {effectData.Type}");
                        continue;
                    }

                    // AffectedType.None 효과는 타겟 없이 즉시 실행 (문제 3 해결)
                    if (effectData.AffectedType == AffectedType.None)
                    {
                        if (effect is IVFXAwareEffect vfxEffect)
                        {
                            // 더미 VFXTriggerData 생성 (AttackSuccess = true)
                            var dummyTriggerData = new VFXTriggerData();
                            dummyTriggerData.SetTileTargetValid(
                                new Vector3Int(targetPos.x, targetPos.y, 0),
                                context.GridController.GridToWorldPosition(targetPos)
                            );
                            vfxEffect.ExecuteWithVFXData(targetPos, context, dummyTriggerData);
                        }
                        else
                        {
                            effect.Execute(targetPos, context);
                        }
                        Debug.Log($"[SpellEffectExecutor] Effect {effectData.Type} (AffectedType.None) executed immediately");
                        continue; // 다음 효과로
                    }

                    // 기존 로직: triggerDataList 기반 실행
                    var relevantTriggers = FilterTriggersForEffect(triggerDataList, effectData, targetPos);

                    Debug.Log($"[SpellEffectExecutor] Effect {effectData.Type}: {relevantTriggers.Count} relevant targets");

                    // 각 타겟에 효과 적용
                    foreach (var triggerData in relevantTriggers)
                    {
                        // AttackSuccess 및 TileGridPosition 체크는 FilterTriggersForEffect에서 이미 처리됨
                        // FilterTriggersForEffect를 통과한 경우 TileGridPosition은 반드시 유효한 값을 가짐
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
                    Debug.LogError($"[SpellEffectExecutor] Effect execution error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 특정 효과에 적용 가능한 TriggerData 필터링
        /// 1. 공격 성공 여부 체크 (AttackSuccess)
        /// 2. 범위 체크 (AffectedRange)
        /// </summary>
        private List<VFXTriggerData> FilterTriggersForEffect(
            List<VFXTriggerData> triggerDataList,
            EffectData effectData,
            Vector2Int centerPos)
        {
            var filtered = new List<VFXTriggerData>();

            foreach (var triggerData in triggerDataList)
            {
                // 1. 공격 성공 여부 체크
                if (!triggerData.AttackSuccess)
                {
                    Debug.Log($"[SpellEffectExecutor] Target validation failed: {triggerData.ValidationFailureReason}");
                    continue;
                }

                // 1-1. TileGridPosition 유효성 검증 (불변 조건 검증)
                // TileGridPosition이 기본값인 경우 검증 실패로 간주
                if (triggerData.TileGridPosition == Vector3Int.zero && centerPos != Vector2Int.zero)
                {
                    Debug.LogError($"[SpellEffectExecutor] Invariant violation: AttackSuccess is true but TileGridPosition is invalid at {triggerData.TileWorldPosition}");
                    continue;
                }

                // 2. 범위 체크 (TileGridPosition 사용)
                Vector2Int tilePos2D = new Vector2Int(triggerData.TileGridPosition.x, triggerData.TileGridPosition.y);

                if (effectData.AffectedRange == 0)
                {
                    // 단일 타겟: 중앙 위치만
                    if (tilePos2D == centerPos)
                    {
                        filtered.Add(triggerData);
                    }
                }
                else
                {
                    // 다중 타겟: 맨하탄 거리 기반
                    int distance = Mathf.Abs(tilePos2D.x - centerPos.x) +
                                  Mathf.Abs(tilePos2D.y - centerPos.y);

                    if (distance <= effectData.AffectedRange)
                    {
                        filtered.Add(triggerData);
                    }
                }

                // 3. AffectedType 필터링은 이미 CalculateAllPotentialTargets에서 처리됨
            }

            return filtered;
        }


        /// <summary>
        /// 즉시 효과 실행 (VFX 없이)
        /// </summary>
        private void ExecuteEffectsImmediate(IReadOnlyList<EffectData> effects, Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
            foreach (var effectData in effects)
            {
                try
                {
                    ICardEffect effect = CreateEffectInstance(effectData);
                    if (effect == null) continue;

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
                    Debug.LogError($"[SpellEffectExecutor] Immediate execution error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Effect Factory

        private ICardEffect CreateEffectInstance(EffectData effectData)
        {
            // CardEffectFactory를 사용하여 효과 인스턴스 생성
            return CardEffectFactory.CreateEffect(effectData);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 지정된 그리드 위치의 GameObject를 반환합니다.
        /// VFXEventTrigger가 타겟 유닛을 추적하기 위해 사용됩니다.
        /// </summary>
        /// <param name="gridPos">검색할 그리드 위치</param>
        /// <param name="context">게임 컨텍스트 (GridController 포함)</param>
        /// <returns>해당 위치의 유닛 GameObject, 없으면 null</returns>
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
        /// Phase 2 리팩토링: 타일 기반 타겟 계산 (EffectTargetingHelper 통합)
        /// 모든 효과의 타겟 타일을 사전 계산하고 Context에 저장
        /// </summary>
        private List<GameObject> CalculateAllPotentialTargets(
            IReadOnlyList<EffectData> effects,
            Vector2Int targetPos,
            GameContext context)
        {
            context.PredeterminedTiles.Clear();
            context.VFXPositions.Clear();

            var uniqueTiles = new HashSet<Tile>();

            // ✅ 통합 타일 타겟팅 시스템 (EffectTargetingHelper 사용)
            foreach (var effectData in effects)
            {
                var tiles = EffectTargetingHelper.GetTargetTiles(
                    targetPos,
                    effectData,
                    context);

                foreach (var tile in tiles)
                {
                    uniqueTiles.Add(tile);
                }
            }

            // Context에 타일 저장
            context.PredeterminedTiles.AddRange(uniqueTiles);
            context.VFXPositions = EffectTargetingHelper.TilesToWorldPositions(
                context.PredeterminedTiles);

            // VFXEventTrigger 호환성: 타일의 GameObject 반환
            var targetGameObjects = new List<GameObject>();
            foreach (var tile in uniqueTiles)
            {
                // 타일 자체의 GameObject 전달 (NotAny용) 또는 유닛 GameObject (Ally/Enemy용)
                if (tile.OccupyingUnit != null)
                {
                    targetGameObjects.Add(tile.OccupyingUnit.gameObject);
                }
                else
                {
                    // 빈 타일: 타일 GameObject 전달
                    targetGameObjects.Add(tile.gameObject);
                }
            }

            return targetGameObjects;
        }


        #endregion
    }
}
