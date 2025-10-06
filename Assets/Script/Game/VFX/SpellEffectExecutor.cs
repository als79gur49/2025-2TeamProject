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
    /// TriggerData를 통해 공격 성공/실패 정보 전달
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

            // 타겟 GameObject 가져오기
            GameObject targetObject = GetTargetObject(targetPos, context);

            // Coroutine 실행 (더 이상 새 인스턴스 생성 안 함)
            StartCoroutine(ExecuteWithVFX(sortedEffects, vfxData, targetPos, targetObject, context));
        }

        #endregion

        #region VFX Execution Coroutine

        private IEnumerator ExecuteWithVFX(IReadOnlyList<EffectData> effects, VFXData vfxData, Vector2Int targetPos, GameObject targetObject, GameContext context)
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

                // VFXEventTrigger 초기화 (TriggerData 콜백)
                VFXEventTrigger trigger = vfxInstance.GetComponent<VFXEventTrigger>();
                if (trigger == null)
                    trigger = vfxInstance.AddComponent<VFXEventTrigger>();

                trigger.Initialize(
                    vfxData.TriggerNormalizedTime,
                    (triggerData) => {
                        // TriggerData를 통해 효과 실행
                        ExecuteEffectsWithData(effects, targetPos, context, triggerData);
                        effectsExecuted = true;
                    },
                    targetObject
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
                VFXTriggerData timeoutData = new VFXTriggerData
                {
                    TriggerWorldPosition = worldPos,
                    NormalizedProgress = 1.0f
                };
                timeoutData.SetTargetInvalid(targetObject, "VFX timeout");
                ExecuteEffectsWithData(effects, targetPos, context, timeoutData);
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
        /// TriggerData를 통해 효과 실행 (공격 성공/실패 반영)
        /// </summary>
        private void ExecuteEffectsWithData(IReadOnlyList<EffectData> effects, Vector2Int targetPos, GameContext context, VFXTriggerData triggerData)
        {
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

                    // IVFXAwareEffect 체크 (고급 효과)
                    if (effect is IVFXAwareEffect vfxAwareEffect)
                    {
                        vfxAwareEffect.ExecuteWithVFXData(targetPos, context, triggerData);
                    }
                    else
                    {
                        // 기본 효과 (AttackSuccess 여부만 확인)
                        if (triggerData.AttackSuccess)
                        {
                            effect.Execute(targetPos, context);
                        }
                        else
                        {
                            Debug.Log($"[SpellEffectExecutor] Effect skipped due to attack failure: {effectData.Type}");
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

        private GameObject GetTargetObject(Vector2Int gridPos, GameContext context)
        {
            // GridManager를 통해 해당 위치의 GameObject 검색
            // 실제 구현은 프로젝트의 Grid 시스템에 따라 다름

            // 예시: context.GridManager.GetObjectAtPosition(gridPos);
            return null; // Placeholder
        }

        #endregion
    }
}
