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
            // 유닛 스탯 패널 활성화
            CardUIPanelHelper.UpdateUnitStatPanels(context.ViewData, context.CardData, true);
            CardUIAnimator.ApplyDragVisuals(context);

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
            // 프리뷰 정리
            battleContext?.GridRenderer?.ClearCardPreview();
            CardUIAnimator.RestoreDragVisuals(context);
            // 패널 비활성화
            CardUIPanelHelper.UpdateUnitStatPanels(context.ViewData, context.CardData, false);

            // 드롭 처리
            bool dropSuccess = HandleDrop(eventData);

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
                        battleContext?.SpawnValidator != null &&
                        gridPosition.HasValue)
                    {
                        // CardPreviewHelper로 유효/무효 위치 계산
                        var (validPos, invalidPos) = CardPreviewHelper.ValidateAffectedPositions(
                            context.CardData,
                            gridPosition.Value,
                            battleContext.GridManager,
                            battleContext.SpawnValidator
                        );

                        // 유효/무효 위치를 색상으로 구분하여 표시
                        battleContext.GridRenderer.ShowValidatedPreview(validPos, invalidPos);
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

            //if (context.CardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            //{
            //    return battleContext.SpawnValidator.CanSpawnUnitFromCard(context.CardData, gridPosition);
            //}
            //else if (context.CardData.HasEffectType(Game.Card.Effects.EffectType.Damage) ||
            //         context.CardData.HasEffectType(Game.Card.Effects.EffectType.Heal))
            //{
            //    return battleContext.SpawnValidator.CanUseSpell(context.CardData, gridPosition);
            //}

            return battleContext.SpawnValidator.CanUseCard(context.CardData, gridPosition, true);

            //return false;
        }

        private bool HandleDrop(PointerEventData eventData)
        {
            Camera camera = Camera.main;
            if (camera == null) return false;

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

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

                    if (battleContext?.CardSpawnService != null)
                    {
                        return battleContext.CardSpawnService.TryExecuteCard(context.CardData, gridPosition, TeamType.Player);
                    }
                    break;
                }
            }

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

            if (context.CardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                var summonEffects = context.CardData.GetEffectsByType(Game.Card.Effects.EffectType.Summon);
                if (summonEffects.Count > 0 && summonEffects[0].UnitToSummon != null)
                {
                    var unitData = summonEffects[0].UnitToSummon;

                    if (viewData.AttackText != null)
                        viewData.AttackText.text = unitData.AttackPower.ToString();
                    if (viewData.HpText != null)
                        viewData.HpText.text = unitData.MaxHealth.ToString();
                    if (viewData.MovementText != null)
                        viewData.MovementText.text = unitData.MovementRange.ToString();
                }
            }
        }
    }
}
