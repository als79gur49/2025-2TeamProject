using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using TMPro;
using Game.Services;
using System.Collections;
using System.Collections.Generic;

namespace Game.Card.UI.Refactored
{
    #region Context Classes (분리된 구조)

    /// <summary>
    /// 기본 UI 데이터만 포함하는 뷰 데이터
    /// </summary>
    public class CardUIViewData
    {
        // 기본 UI 요소
        public Image CardImage { get; set; }
        public Image ItemImage { get; set; }
        public TextMeshProUGUI CostText { get; set; }
        public TextMeshProUGUI OwnedCountText { get; set; }
        public CanvasGroup CanvasGroup { get; set; }
        public Button RemoveButton { get; set; }
        
        // Visual Feedback
        public Image GlowEffect { get; set; }
        
        // Unit Stats UI
        public GameObject AttackParent { get; set; }
        public TextMeshProUGUI AttackText { get; set; }
        public GameObject HpParent { get; set; }
        public TextMeshProUGUI HpText { get; set; }
        public GameObject MovementParent { get; set; }
        public TextMeshProUGUI MovementText { get; set; }
    }

    /// <summary>
    /// 드래그 상태 관리
    /// </summary>
    public class CardUIDragState
    {
        public Vector3 OriginalPosition { get; set; }
        public Vector3 OriginalScale { get; set; }
        public Transform OriginalParent { get; set; }
        public int OriginalIndex { get; set; }
        public bool IsDragging { get; set; }
    }

    /// <summary>
    /// 카드 UI 설정값
    /// </summary>
    public class CardUISettings
    {
        public float DragAlpha { get; set; } = 0.6f;
        public float DragScale { get; set; } = 0.7f;
        public float ReturnSpeed { get; set; } = 10f;
        public bool ReturnToOriginalPosition { get; set; } = true;
        public Color ValidDropColor { get; set; } = Color.green;
        public Color InvalidDropColor { get; set; } = Color.red;
    }

    /// <summary>
    /// 이벤트 채널 컨테이너
    /// </summary>
    public class CardUIEventChannels
    {
        public CardInfoEventChannelSO CardInfoChannel { get; set; }
        public CardDragEndEventChannelSO CardDragEndChannel { get; set; }
        public Game.UI.Events.CardDragStartEventChannelSO CardDragStartChannel { get; set; }
    }

    /// <summary>
    /// 공통 컨텍스트 - 모든 전략이 공유하는 최소한의 데이터
    /// </summary>
    public class CardUIBaseContext
    {
        public CardData CardData { get; set; }
        public Transform Transform { get; set; }
        public GameObject GameObject { get; set; }
        public MonoBehaviour MonoBehaviour { get; set; }
        public Canvas ParentCanvas { get; set; }
        
        public CardUIViewData ViewData { get; set; }
        public CardUIDragState DragState { get; set; }
        public CardUISettings Settings { get; set; }
        public CardUIEventChannels Events { get; set; }
    }

    /// <summary>
    /// 전투 특화 컨텍스트
    /// </summary>
    public class CardUIBattleContext : CardUIBaseContext
    {
        public ICardSpawnService CardSpawnService { get; set; }
        public ISpawnValidator SpawnValidator { get; set; }
        public ICardHandManager CardHandManager { get; set; }
        public IGridRenderer GridRenderer { get; set; }
    }

    /// <summary>
    /// 덱 빌더 특화 컨텍스트
    /// </summary>
    public class CardUIBuilderContext : CardUIBaseContext
    {
        public Game.UI.Panels.DeckBuilderPanel DeckPanel { get; set; }
    }

    #endregion

    #region Utility Classes (중복 제거)

    /// <summary>
    /// 카드 UI 애니메이션 유틸리티
    /// </summary>
    public static class CardUIAnimator
    {
        public static IEnumerator ReturnToOriginalPosition(
            CardUIBaseContext context,
            System.Action onComplete = null)
        {
            var dragState = context.DragState;
            var transform = context.Transform;
            var settings = context.Settings;

            // 원래 부모로 복귀
            if (dragState.OriginalParent != null)
            {
                transform.SetParent(dragState.OriginalParent, true);
                transform.SetSiblingIndex(dragState.OriginalIndex);
            }

            // 부드러운 이동 애니메이션
            while (Vector3.Distance(transform.position, dragState.OriginalPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    dragState.OriginalPosition,
                    settings.ReturnSpeed * Time.deltaTime
                );
                transform.localScale = Vector3.Lerp(
                    transform.localScale,
                    dragState.OriginalScale,
                    settings.ReturnSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.position = dragState.OriginalPosition;
            transform.localScale = dragState.OriginalScale;

            onComplete?.Invoke();
        }

        public static void SaveDragState(CardUIBaseContext context)
        {
            var dragState = context.DragState;
            var transform = context.Transform;
            
            dragState.OriginalPosition = transform.position;
            dragState.OriginalScale = transform.localScale;
            dragState.OriginalParent = transform.parent;
            dragState.OriginalIndex = transform.GetSiblingIndex();
        }

        public static void ApplyDragVisuals(CardUIBaseContext context)
        {
            var viewData = context.ViewData;
            var settings = context.Settings;
            var transform = context.Transform;

            if (viewData.CanvasGroup != null)
            {
                viewData.CanvasGroup.alpha = settings.DragAlpha;
                viewData.CanvasGroup.blocksRaycasts = false;
            }

            transform.localScale = context.DragState.OriginalScale * settings.DragScale;

            // 최상위로 이동
            if (context.ParentCanvas != null)
            {
                transform.SetParent(context.ParentCanvas.transform, true);
            }
        }

        public static void RestoreDragVisuals(CardUIBaseContext context)
        {
            var viewData = context.ViewData;

            if (viewData.CanvasGroup != null)
            {
                viewData.CanvasGroup.alpha = 1f;
                viewData.CanvasGroup.blocksRaycasts = true;
            }
        }
    }

    /// <summary>
    /// 카드 UI 색상 제공자
    /// </summary>
    public static class CardUIColorProvider
    {
        public static Color GetManaCostColor(int manaCost)
        {
            if (manaCost <= 2) return Color.green;
            if (manaCost <= 4) return Color.yellow;
            if (manaCost <= 6) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }

        public static void UpdateDropFeedback(CardUIBaseContext context, bool isValid)
        {
            var glowEffect = context.ViewData.GlowEffect;
            if (glowEffect != null)
            {
                glowEffect.gameObject.SetActive(true);
                glowEffect.color = isValid 
                    ? context.Settings.ValidDropColor 
                    : context.Settings.InvalidDropColor;
            }
        }

        public static void ClearDropFeedback(CardUIBaseContext context)
        {
            var glowEffect = context.ViewData.GlowEffect;
            if (glowEffect != null)
            {
                glowEffect.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// UI 패널 가시성 헬퍼
    /// </summary>
    public static class CardUIPanelHelper
    {
        public static void HideUnitStatPanels(CardUIViewData viewData)
        {
            if (viewData.AttackParent != null)
                viewData.AttackParent.SetActive(false);
            if (viewData.HpParent != null)
                viewData.HpParent.SetActive(false);
            if (viewData.MovementParent != null)
                viewData.MovementParent.SetActive(false);
        }

        public static void UpdateUnitStatPanels(CardUIViewData viewData, CardData cardData, bool visible)
        {
            bool hasUnitStats = visible && cardData != null &&
                                cardData.HasEffectType(Game.Card.Effects.EffectType.Summon);

            if (viewData.AttackParent != null)
                viewData.AttackParent.SetActive(hasUnitStats);
            if (viewData.HpParent != null)
                viewData.HpParent.SetActive(hasUnitStats);
            if (viewData.MovementParent != null)
                viewData.MovementParent.SetActive(hasUnitStats);
        }
    }

    #endregion

    #region Strategy Pattern Interfaces

    /// <summary>
    /// 카드 UI 전략 인터페이스
    /// </summary>
    public interface ICardUIStrategy
    {
        void Initialize(CardUIBaseContext context);
        void OnDragStart(PointerEventData eventData);
        void OnDragging(PointerEventData eventData);
        bool OnDragEnd(PointerEventData eventData);
        void OnClick(PointerEventData eventData);
        void UpdateUI();
        void UpdateInteractability(bool interactable);
        void Cleanup();
    }

    #endregion

    #region Base Strategy Class (공통 로직)

    /// <summary>
    /// 전략 베이스 클래스 - 공통 로직 구현
    /// </summary>
    public abstract class BaseCardUIStrategy : ICardUIStrategy
    {
        protected CardUIBaseContext context;
        protected bool isDragging;
        protected Coroutine returnCoroutine;

        public virtual void Initialize(CardUIBaseContext context)
        {
            this.context = context;
            UpdateUI();
        }

        public virtual void OnDragStart(PointerEventData eventData)
        {
            if (!CanStartDrag()) return;

            isDragging = true;
            context.DragState.IsDragging = true;

            // 드래그 상태 저장
            CardUIAnimator.SaveDragState(context);
            
            // 드래그 시각적 효과 적용
            CardUIAnimator.ApplyDragVisuals(context);

            // 전략별 추가 처리
            OnDragStartInternal(eventData);
        }

        public virtual void OnDragging(PointerEventData eventData)
        {
            if (!isDragging) return;

            context.Transform.position = eventData.position;
            
            // 전략별 추가 처리
            OnDraggingInternal(eventData);
        }

        public virtual bool OnDragEnd(PointerEventData eventData)
        {
            if (!isDragging) return false;

            isDragging = false;
            context.DragState.IsDragging = false;

            // 시각적 피드백 복원
            CardUIAnimator.RestoreDragVisuals(context);

            // 전략별 드롭 처리
            bool dropSuccess = OnDragEndInternal(eventData);

            // 실패 시 원위치 복귀
            if (!dropSuccess && context.Settings.ReturnToOriginalPosition)
            {
                returnCoroutine = context.MonoBehaviour.StartCoroutine(
                    CardUIAnimator.ReturnToOriginalPosition(context, OnReturnComplete)
                );
            }

            return dropSuccess;
        }

        public abstract void OnClick(PointerEventData eventData);
        public abstract void UpdateUI();
        
        public virtual void UpdateInteractability(bool interactable)
        {
            var viewData = context.ViewData;
            
            if (viewData.CanvasGroup != null)
            {
                viewData.CanvasGroup.alpha = interactable ? 1f : 0.5f;
                viewData.CanvasGroup.interactable = interactable;
                viewData.CanvasGroup.blocksRaycasts = interactable;
            }
        }

        public virtual void Cleanup()
        {
            if (returnCoroutine != null && context.MonoBehaviour != null)
            {
                context.MonoBehaviour.StopCoroutine(returnCoroutine);
            }
        }

        // Abstract methods for strategy-specific implementation
        protected abstract bool CanStartDrag();
        protected abstract void OnDragStartInternal(PointerEventData eventData);
        protected abstract void OnDraggingInternal(PointerEventData eventData);
        protected abstract bool OnDragEndInternal(PointerEventData eventData);
        protected virtual void OnReturnComplete() { }

        // Helper methods
        protected void RaiseCardInfoEvent()
        {
            if (context.Events?.CardInfoChannel != null && context.CardData != null)
            {
                context.Events.CardInfoChannel.RaiseEvent(context.CardData);
            }
        }

        protected void RaiseDragStartEvent(CardUIMode mode)
        {
            if (context.Events?.CardDragStartChannel != null && context.CardData != null)
            {
                var dragData = new CardDragData
                {
                    cardData = context.CardData,
                    mode = mode,
                    sourceTransform = context.Transform,
                    sourceIndex = context.DragState.OriginalIndex
                };
                context.Events.CardDragStartChannel.RaiseDragStart(dragData);
            }
        }

        protected void RaiseDragEndEvent()
        {
            context.Events?.CardDragEndChannel?.RaiseEvent();
        }

        protected void UpdateBasicUI()
        {
            var viewData = context.ViewData;
            var cardData = context.CardData;

            if (cardData == null) return;

            // 비용 텍스트
            if (viewData.CostText != null)
            {
                viewData.CostText.text = cardData.ManaCost.ToString();
                viewData.CostText.color = CardUIColorProvider.GetManaCostColor(cardData.ManaCost);
            }

            // 카드 이미지
            if (viewData.ItemImage != null && cardData.CardArt != null)
            {
                viewData.ItemImage.sprite = cardData.CardArt;
            }
        }
    }

    #endregion

    #region Concrete Strategies

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

        protected override bool CanStartDrag()
        {
            if (context.CardData == null) return false;

            // GlobalStateManager 체크 (전투 모드 전용)
            if (globalStateManager != null && 
                globalStateManager.IsBusy(BusyType.GameFlowLock))
            {
                Debug.LogWarning("[InHandStrategy] Cannot drag - GameFlowLock active");
                return false;
            }

            return true;
        }

        protected override void OnDragStartInternal(PointerEventData eventData)
        {
            // 유닛 스탯 패널 활성화
            CardUIPanelHelper.UpdateUnitStatPanels(context.ViewData, context.CardData, true);

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
                    gridPosition = tile.GridPosition;
                    isValidDrop = ValidateDropPosition(gridPosition.Value);

                    if (battleContext?.GridRenderer != null && gridPosition.HasValue)
                    {
                        if (isValidDrop)
                        {
                            battleContext.GridRenderer.ShowCardPreview(context.CardData, gridPosition.Value);
                        }
                        else
                        {
                            battleContext.GridRenderer.ClearCardPreview();
                        }
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

            if (context.CardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                return battleContext.SpawnValidator.CanSpawnUnitFromCard(context.CardData, gridPosition);
            }
            else if (context.CardData.HasEffectType(Game.Card.Effects.EffectType.Damage) ||
                     context.CardData.HasEffectType(Game.Card.Effects.EffectType.Heal))
            {
                return battleContext.SpawnValidator.CanUseSpell(context.CardData, gridPosition);
            }

            return false;
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
                    var gridPosition = tile.GridPosition;
                    
                    if (battleContext?.CardSpawnService != null)
                    {
                        return battleContext.CardSpawnService.TrySpawnCard(context.CardData, gridPosition);
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
                var cardUI = context.GameObject.GetComponent<CardUI>();
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

    /// <summary>
    /// 인벤토리 카드 전략
    /// </summary>
    public class InInventoryStrategy : BaseCardUIStrategy
    {
        private int ownedCount;

        public void SetOwnedCount(int count)
        {
            ownedCount = count;
            UpdateOwnedCountDisplay();
            UpdateInteractability(count > 0);
        }

        protected override bool CanStartDrag()
        {
            return context.CardData != null && ownedCount > 0;
        }

        protected override void OnDragStartInternal(PointerEventData eventData)
        {
            RaiseDragStartEvent(CardUIMode.InInventory);
            RaiseCardInfoEvent();
            
            Debug.Log($"[InInventoryStrategy] Started dragging: {context.CardData.CardName} (x{ownedCount})");
        }

        protected override void OnDraggingInternal(PointerEventData eventData)
        {
            ValidateDropOverDeckBuilder(eventData);
        }

        protected override bool OnDragEndInternal(PointerEventData eventData)
        {
            RaiseDragEndEvent();
            
            Debug.Log($"[InInventoryStrategy] Drag ended - DeckInventoryCoordinator handles actual transfer");
            return false; // 항상 false - DeckInventoryCoordinator가 처리
        }

        public override void OnClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                RaiseCardInfoEvent();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                TryQuickAddToDeck();
            }
        }

        public override void UpdateUI()
        {
            UpdateBasicUI();
            UpdateOwnedCountDisplay();

            // 인벤토리 모드 특수 UI
            if (context.ViewData.RemoveButton != null)
                context.ViewData.RemoveButton.gameObject.SetActive(false);

            CardUIPanelHelper.HideUnitStatPanels(context.ViewData);
            CardUIColorProvider.ClearDropFeedback(context);
        }

        private void UpdateOwnedCountDisplay()
        {
            if (context.ViewData.OwnedCountText != null)
            {
                context.ViewData.OwnedCountText.gameObject.SetActive(true);
                context.ViewData.OwnedCountText.text = $"x{ownedCount}";
            }
        }

        private void ValidateDropOverDeckBuilder(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool overDeckBuilder = false;
            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<Game.UI.Panels.DeckBuilderPanel>() != null)
                {
                    overDeckBuilder = true;
                    break;
                }
            }

            CardUIColorProvider.UpdateDropFeedback(context, overDeckBuilder);
        }

        private void TryQuickAddToDeck()
        {
            var coordinator = GameObject.FindObjectOfType<DeckInventoryCoordinator>();
            coordinator?.OnInventoryCardRightClick(context.CardData);
        }
    }

    /// <summary>
    /// 덱 빌더 카드 전략
    /// </summary>
    public class InDeckStrategy : BaseCardUIStrategy
    {
        private CardUIBuilderContext builderContext;
        private int cardCount;

        public override void Initialize(CardUIBaseContext context)
        {
            base.Initialize(context);
            
            builderContext = context as CardUIBuilderContext;
            if (builderContext == null)
            {
                Debug.LogError("[InDeckStrategy] Invalid context type - expected CardUIBuilderContext");
            }
        }

        public void SetCardCount(int count)
        {
            cardCount = count;
            UpdateCardCountDisplay();
            UpdateInteractability(count > 0);
        }

        protected override bool CanStartDrag()
        {
            return context.CardData != null && cardCount > 0;
        }

        protected override void OnDragStartInternal(PointerEventData eventData)
        {
            RaiseDragStartEvent(CardUIMode.InDeck);
            
            Debug.Log($"[InDeckStrategy] Started dragging for reordering: {context.CardData.CardName} (x{cardCount})");
        }

        protected override void OnDraggingInternal(PointerEventData eventData)
        {
            // 향후 재정렬 프리뷰 구현
        }

        protected override bool OnDragEndInternal(PointerEventData eventData)
        {
            RaiseDragEndEvent();
            
            // 현재는 재정렬 미지원
            Debug.Log($"[InDeckStrategy] Reordering not implemented yet");
            return false;
        }

        public override void OnClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                RaiseCardInfoEvent();
            }
            else if (eventData.button == PointerEventData.InputButton.Right && cardCount > 0)
            {
                RemoveFromDeck();
            }
        }

        public override void UpdateUI()
        {
            UpdateBasicUI();
            UpdateCardCountDisplay();

            // 덱 모드 특수 UI
            if (context.ViewData.RemoveButton != null)
            {
                context.ViewData.RemoveButton.gameObject.SetActive(true);
                context.ViewData.RemoveButton.onClick.RemoveAllListeners();
                context.ViewData.RemoveButton.onClick.AddListener(RemoveFromDeck);
            }

            CardUIPanelHelper.HideUnitStatPanels(context.ViewData);
            CardUIColorProvider.ClearDropFeedback(context);
        }

        public override void UpdateInteractability(bool interactable)
        {
            base.UpdateInteractability(interactable);

            // 제거 버튼도 함께 제어
            if (context.ViewData.RemoveButton != null)
            {
                context.ViewData.RemoveButton.interactable = interactable;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            
            if (context.ViewData.RemoveButton != null)
            {
                context.ViewData.RemoveButton.onClick.RemoveAllListeners();
            }
        }

        private void UpdateCardCountDisplay()
        {
            if (context.ViewData.OwnedCountText != null)
            {
                context.ViewData.OwnedCountText.gameObject.SetActive(true);
                context.ViewData.OwnedCountText.text = $"x{cardCount}";
                context.ViewData.OwnedCountText.color = cardCount > 0 ? Color.white : Color.gray;
            }
        }

        private void RemoveFromDeck()
        {
            if (builderContext?.DeckPanel != null && context.CardData != null)
            {
                builderContext.DeckPanel.RemoveCardFromDeck(context.CardData);
                Debug.Log($"[InDeckStrategy] Removed {context.CardData.CardName} from deck");
            }
        }
    }

    #endregion

    // CardUI 컴포넌트는 다음 파일에 계속...
}
