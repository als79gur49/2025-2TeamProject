using UnityEngine;
using UnityEngine.EventSystems;
using Game.Core;
using Game.Data;
using Game.Interfaces;
using Game.Services;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 전투 중 핸드 카드 전략
    /// </summary>
    public class InHandStrategy : BaseCardUIStrategy
    {
        private CardUIBattleContext battleContext;
        private IGlobalStateManager globalStateManager;

        public override void Initialize(CardUIBaseContext context)
        {
            base.Initialize(context);

            battleContext = context as CardUIBattleContext;
            if (battleContext == null)
            {
                Debug.LogError("[InHandStrategy] Invalid context type - expected CardUIBattleContext");
                return;
            }

            // 전투 모드에서만 GlobalStateManager 사용
            if (ServiceLocator.IsInitialized)
            {
                globalStateManager = ServiceLocator.Get<IGlobalStateManager>();
            }
        }

        /// <summary>
        /// 상호작용 규칙: GameFlowLock이 활성화되지 않았을 때만 상호작용 가능
        /// </summary>
        protected override bool CanInteract()
        {
            if (!base.CanInteract()) return false;

            // GlobalStateManager 체크 (전투 모드 전용)
            if (globalStateManager != null &&
                globalStateManager.IsBusy(BusyType.GameFlowLock))
            {
                Debug.LogWarning("[InHandStrategy] Cannot interact - GameFlowLock active");
                return false;
            }

            return true;
        }

        protected override bool CanStartDrag()
        {
            return CanInteract(); // ← 중복 제거, CanInteract() 재사용
        }

        protected override void OnDragStartInternal(PointerEventData eventData)
        {
            // ✅ 1. 이전에 실행 중인 ReturnToOriginalPosition 코루틴 중단
            if (context.MonoBehaviour != null)
            {
                context.MonoBehaviour.StopAllCoroutines();
            }

            // ✅ 2. 코루틴 중단 후 시각적 상태 및 위치 즉시 복원
            // 코루틴이 중단되면서 position/scale/alpha가 중간값으로 남아있을 수 있음
            var rectTransform = context.Transform as RectTransform;
            if (rectTransform != null)
            {
                // 위치 복원
                if (context.DragState.OriginalPosition != Vector3.zero)
                {
                    rectTransform.anchoredPosition = context.DragState.OriginalPosition;
                    Debug.Log($"[InHandStrategy] Restored position from DragState: {context.DragState.OriginalPosition}");
                }

                // 스케일 복원
                if (context.DragState.OriginalScale != Vector3.zero)
                {
                    rectTransform.localScale = context.DragState.OriginalScale;
                    Debug.Log($"[InHandStrategy] Restored scale from DragState: {context.DragState.OriginalScale}");
                }
                else
                {
                    rectTransform.localScale = Vector3.one;
                    Debug.Log($"[InHandStrategy] Restored scale to default: Vector3.one");
                }
            }

            // 알파값도 복원
            if (context.ViewData.CanvasGroup != null)
            {
                context.ViewData.CanvasGroup.alpha = 1f;
                context.ViewData.CanvasGroup.blocksRaycasts = true;
            }

            // 유닛 스탯 패널 활성화
            CardUIPanelHelper.UpdateUnitStatPanels(context.ViewData, context.CardData, true);

            // ✅ 3. 드래그 시각 효과는 BaseCardUIStrategy.OnDragStart에서 이미 호출됨
            // CardUIAnimator.ApplyDragVisuals(context); // 중복 제거!

            // 이벤트 발생
            RaiseDragStartEvent(CardUIMode.InHand);
            RaiseCardInfoEvent();

            Debug.Log($"[InHandStrategy] Started dragging: {context.CardData.CardName}");
        }

        protected override void OnDraggingInternal(PointerEventData eventData)
        {
            ValidateAndShowDropFeedback(eventData);
        }

        protected override bool OnDragEndInternal(PointerEventData eventData)
        {
            Debug.Log($"[InHandStrategy] ===== OnDragEndInternal START =====");

            // 프리뷰 정리
            battleContext?.GridRenderer?.ClearCardPreview();
            CardUIAnimator.RestoreDragVisuals(context);

            // ✅ GlowEffect 초기화 추가
            CardUIColorProvider.ClearDropFeedback(context);

            // 패널 비활성화
            CardUIPanelHelper.UpdateUnitStatPanels(context.ViewData, context.CardData, false);


            // 드롭 처리
            bool dropSuccess = false;
            try
            {
                Debug.Log($"[InHandStrategy] Calling HandleDrop...");
                dropSuccess = HandleDrop(eventData);
                Debug.Log($"[InHandStrategy] HandleDrop completed: {dropSuccess}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InHandStrategy] HandleDrop exception: {ex.Message}\n{ex.StackTrace}");
                dropSuccess = false;
            }

            // 드롭 처리
            //bool dropSuccess = HandleDrop(eventData);

            // 라인 92 (새로 추가)
            Debug.Log($"[InHandStrategy] About to call RaiseDragEndEvent - dropSuccess: {dropSuccess}");
            Debug.Log($"[InHandStrategy] context.Events: {context.Events != null}");
            Debug.Log($"[InHandStrategy] CardDragEndChannel: {context.Events?.CardDragEndChannel?.name ?? "NULL"}");
            // 이벤트 발생
            RaiseDragEndEvent();

            if (dropSuccess)
            {
                OnCardUsed();
            }

            Debug.Log($"[InHandStrategy] Drop result: {dropSuccess}");
            return dropSuccess;
        }

        protected override void OnReturnComplete()
        {
            // CardHandManager 레이아웃 재정렬
            battleContext?.CardHandManager?.RefreshHandLayout();
        }

        public override void OnClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                RaiseCardInfoEvent();
            }
        }

        public override void UpdateUI()
        {
            UpdateBasicUI();
            UpdateUnitStats();

            // InHand 모드 특수 UI
            if (context.ViewData.OwnedCountText != null)
                context.ViewData.OwnedCountText.gameObject.SetActive(false);
            if (context.ViewData.RemoveButton != null)
                context.ViewData.RemoveButton.gameObject.SetActive(false);

            CardUIColorProvider.ClearDropFeedback(context);
        }

        private void ValidateAndShowDropFeedback(PointerEventData eventData)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                CardUIColorProvider.UpdateDropFeedback(context, false);
                return;
            }

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            bool isValidDrop = false;
            Vector2Int? gridPosition = null;

            foreach (var hit in hits)
            {
                var tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    gridPosition = tile.GetGridPosition();
                    isValidDrop = ValidateDropPosition(gridPosition.Value);

                    // 카드 프리뷰 표시
                    if (battleContext?.GridRenderer != null &&
                        battleContext?.GridManager != null &&
                        gridPosition.HasValue)
                    {
                        // CardPreviewHelper로 유효/무효 위치 계산
                        var (areaPos, validPos, invalidPos) = CardPreviewHelper.ValidateAffectedPositions(
                            context.CardData,
                            gridPosition.Value,
                            battleContext.GridManager,
                            true
                        );

                        // 기본/유효/무효 위치를 색상으로 구분하여 표시
                        battleContext.GridRenderer.ShowValidatedPreview(areaPos, validPos, invalidPos);
                    }
                    break;
                }
            }

            CardUIColorProvider.UpdateDropFeedback(context, isValidDrop);
        }

        private bool ValidateDropPosition(Vector2Int gridPosition)
        {
            if (battleContext?.SpawnValidator == null || context.CardData == null)
                return false;

            //if (context.CardData.EffectDefinitions.Any(d => d is Game.Card.Effects.SummonEffectDefinition))
            //{
            //    return battleContext.SpawnValidator.CanSpawnUnitFromCard(context.CardData, gridPosition);
            //}
            //else if (context.CardData.EffectDefinitions.Any(d => d is Game.Card.Effects.DamageEffectDefinition) ||
            //         context.CardData.EffectDefinitions.Any(d => d is Game.Card.Effects.HealEffectDefinition))
            //{
            //    return battleContext.SpawnValidator.CanUseSpell(context.CardData, gridPosition);
            //}

            return battleContext.SpawnValidator.CanUseCard(context.CardData, gridPosition, true);

            //return false;
        }

        private bool HandleDrop(PointerEventData eventData)
        {
            Debug.Log($"[InHandStrategy.HandleDrop] START");

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.Log($"[InHandStrategy.HandleDrop] Camera is null");
                PlayDropSound(false);
                return false;
            }

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            Debug.Log($"[InHandStrategy.HandleDrop] Raycast hits: {hits.Length}");

            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            foreach (var hit in hits)
            {
                var tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    var gridPosition = tile.GetGridPosition();
                    Debug.Log($"[InHandStrategy.HandleDrop] Found tile at: {gridPosition}");

                    // ✅ 유효성 검사 먼저 수행
                    if (!ValidateDropPosition(gridPosition))
                    {
                        Debug.Log($"[InHandStrategy.HandleDrop] Invalid drop position: {gridPosition}");
                        PlayDropSound(false);
                        return false;
                    }

                    if (battleContext?.CardSpawnService != null)
                    {
                        Debug.Log($"[InHandStrategy.HandleDrop] Calling TryExecuteCard at {gridPosition}");
                        bool result = battleContext.CardSpawnService.TryExecuteCard(context.CardData, gridPosition, TeamType.Player);
                        Debug.Log($"[InHandStrategy.HandleDrop] TryExecuteCard result: {result}");
                        PlayDropSound(result);
                        return result;
                    }
                    break;
                }
            }

            Debug.Log($"[InHandStrategy.HandleDrop] No valid tile found, returning false");
            PlayDropSound(false);
            return false;
        }

        private void OnCardUsed()
        {
            Debug.Log($"[InHandStrategy] Card used: {context.CardData?.CardName}");

            if (battleContext?.CardHandManager != null && context.CardData != null)
            {
                var cardUI = context.GameObject.GetComponent<CardUIRefactored>();
                battleContext.CardHandManager.RemoveCardFromHand(context.CardData, cardUI);
            }
            else
            {
                UnityEngine.Object.Destroy(context.GameObject);
            }
        }

        private void UpdateUnitStats()
        {
            if (context.CardData == null) return;

            var viewData = context.ViewData;

            Game.Card.Effects.SummonEffectDefinition summonDef = null;
            if (context.CardData.EffectDefinitions != null)
            {
                foreach (var def in context.CardData.EffectDefinitions)
                {
                    if (def is Game.Card.Effects.SummonEffectDefinition s && s.UnitToSummon != null)
                    {
                        summonDef = s;
                        break;
                    }
                }
            }

            if (summonDef != null)
            {
                var unitData = summonDef.UnitToSummon;

                if (viewData.AttackText != null)
                    viewData.AttackText.text = unitData.AttackPower.ToString();
                if (viewData.HpText != null)
                    viewData.HpText.text = unitData.MaxHealth.ToString();
                if (viewData.MovementText != null)
                    viewData.MovementText.text = unitData.MovementRange.ToString();
            }
        }

        /// <summary>
        /// 드롭 성공/실패 사운드 재생
        /// </summary>
        /// <param name="success">드롭 성공 여부</param>
        private void PlayDropSound(bool success)
        {
            if (battleContext?.SoundEventChannel == null)
            {
                Debug.LogWarning("[InHandStrategy] SoundEventChannel is null, cannot play drop sound");
                return;
            }

            AudioData soundData = success
                ? battleContext.DropSuccessSound
                : battleContext.DropFailSound;

            if (soundData != null)
            {
                battleContext.SoundEventChannel.RaiseSoundEvent(soundData, this);
                Debug.Log($"[InHandStrategy] Played drop sound: {soundData.name} (success={success})");
            }
            else
            {
                Debug.LogWarning($"[InHandStrategy] Drop sound data is null (success={success})");
            }
        }
    }
}
