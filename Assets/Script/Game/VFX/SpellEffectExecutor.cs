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
        /// Phase 4: GlobalStateManager 통합 - VFX 재생 중 GameFlowLock 설정
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
            float actualDuration = 10f; // VFX 정리용 기본 대기시간 (자동 계산 시 10초)

            // ✅ Phase 4: VFX 실행 시작 시 GameFlowLock 설정
            _stateManager?.SetBusy(this, BusyType.GameFlowLock);

            // VFX 생성 및 초기화 (try-catch 사용, yield return 없음)
            try
            {
                // Phase 4: Base/Ground 높이 차이 반영
                // CalculateWorldPositionWithHeight()는 IGridHeightCalculator를 통해
                // Base 타일(Y=1.85f) 또는 Ground 타일(Y=0.1f)의 높이를 자동으로 적용
                // 기존: GridToWorldPosition() → Y=0 고정
                // 변경: CalculateWorldPositionWithHeight() → Base/Ground 자동 구분
                worldPos = context.GridController.CalculateWorldPositionWithHeight(targetPos);
                vfxInstance = Instantiate(vfxData.VFXPrefab, worldPos, Quaternion.identity);

                // VFXEventTrigger 초기화 (리스트 콜백)
                VFXEventTrigger trigger = vfxInstance.GetComponent<VFXEventTrigger>();
                if (trigger == null)
                    trigger = vfxInstance.AddComponent<VFXEventTrigger>();

                // 🆕 리스트 기반 콜백으로 변경 (타일 위치 전달)
                // 수동 Duration 모드일 경우 ManualDuration 전달, 아니면 -1 (자동 계산)
                float durationParam = vfxData.UseAutoDuration ? -1f : vfxData.ManualDuration;

                trigger.Initialize(
                    vfxData.TriggerNormalizedTime,
                    (triggerDataList) => {
                        // 리스트 기반 효과 실행
                        ExecuteEffectsWithDataList(effects, triggerDataList, targetPos, context);
                        effectsExecuted = true;
                    },
                    context.VFXPositions,       // 🆕 타일 월드 좌표 리스트 전달
                    context.GridController,     // 🆕 GridController 전달
                    vfxData.PlaybackSpeed,      // 🆕 재생 속도 전달
                    durationParam               // 🆕 수동/자동 Duration 선택
                );

                // actualDuration 설정: 자동 계산 모드면 10초, 수동 모드면 설정값
                actualDuration = vfxData.UseAutoDuration ? 10f : vfxData.ManualDuration;

                // maxWait 계산: IsLooping이면 10초, 아니면 실제 Duration + 1초
                maxWait = vfxData.IsLooping ? 10f : actualDuration + 1f;
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

                // ✅ Phase 4: 에러 발생 시에도 GameFlowLock 해제
                _stateManager?.SetIdle(this, BusyType.GameFlowLock);
                Debug.Log("[SpellEffectExecutor] VFX execution error - GameFlowLock released");

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
                float cleanupDuration = vfxData.UseAutoDuration ? actualDuration : vfxData.ManualDuration;
                yield return new WaitForSeconds(cleanupDuration);
                if (vfxInstance != null)
                    Destroy(vfxInstance);
            }

            // ✅ Phase 4: VFX 실행 완료 시 GameFlowLock 해제 (finally 블록 대신 코루틴 끝에서 처리)
            _stateManager?.SetIdle(this, BusyType.GameFlowLock);
            Debug.Log("[SpellEffectExecutor] VFX execution completed - GameFlowLock released");

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
                    var relevantTriggers = FilterTriggersForEffect(triggerDataList, effectData, targetPos, context);

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
        /// 3. AffectedType 체크
        /// </summary>
        private List<VFXTriggerData> FilterTriggersForEffect(
            List<VFXTriggerData> triggerDataList,
            EffectData effectData,
            Vector2Int centerPos,
            GameContext context)
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

                bool inRange = false;
                if (effectData.AffectedRange == 0)
                {
                    // 단일 타겟: 중앙 위치만
                    inRange = (tilePos2D == centerPos);
                }
                else
                {
                    // 다중 타겟: 맨하탄 거리 기반
                    int distance = Mathf.Abs(tilePos2D.x - centerPos.x) +
                                  Mathf.Abs(tilePos2D.y - centerPos.y);
                    inRange = (distance <= effectData.AffectedRange);
                }

                if (!inRange)
                {
                    continue;
                }

                // 3. AffectedType 필터링
                if (!MatchesAffectedType(tilePos2D, effectData.AffectedType, context))
                {
                    continue;
                }

                filtered.Add(triggerData);
            }

            return filtered;
        }

        /// <summary>
        /// 타일이 AffectedType 조건을 만족하는지 확인
        /// </summary>
        private bool MatchesAffectedType(Vector2Int tilePos, AffectedType affectedType, GameContext context)
        {
            if (affectedType == AffectedType.None)
            {
                return true; // None은 항상 통과
            }

            var tile = context.GridController.GetTileAtPosition(tilePos);
            if (tile == null)
            {
                return false;
            }

            return affectedType switch
            {
                AffectedType.Ally => tile.OccupyingUnit != null && IsSameTeam(tile.OccupyingUnit, context.CasterTeam),
                AffectedType.Enemy => tile.OccupyingUnit != null && !IsSameTeam(tile.OccupyingUnit, context.CasterTeam),
                AffectedType.Any => tile.OccupyingUnit != null,
                AffectedType.NotAny => tile.OccupyingUnit == null,
                _ => true
            };
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
