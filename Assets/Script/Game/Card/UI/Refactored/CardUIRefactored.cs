using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using TMPro;
using Game.Services;
using Game.Card;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 리팩토링된 카드 UI 컴포넌트
    /// 전략 패턴과 개선된 구조 적용
    /// </summary>
    public class CardUIRefactored : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Context Mode")]
        [SerializeField] private CardUIMode mode = CardUIMode.InHand;

        [Header("카드 UI 설정")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Inventory/Deck Mode Settings")]
        [SerializeField] private TextMeshProUGUI ownedCountText;
        [SerializeField] private Button removeButton;

        [Header("유닛 스탯 UI")]
        [SerializeField] private GameObject attackParent;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private GameObject hpParent;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private GameObject movementParent;
        [SerializeField] private TextMeshProUGUI movementText;

        [Header("드래그 설정")]
        [SerializeField] private float dragAlpha = 0.6f;
        [SerializeField] private float dragScale = 0.7f;
        [SerializeField] private bool returnToOriginalPosition = true;
        [SerializeField] private float returnSpeed = 10f;

        [Header("시각적 피드백")]
        [SerializeField] private Image glowEffect;
        [SerializeField] private Color validDropColor = Color.green;
        [SerializeField] private Color invalidDropColor = Color.red;

        [Header("Event Channels")]
        [SerializeField] private CardInfoEventChannelSO cardInfoChannel;
        [SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;
        [SerializeField] private Game.UI.Events.CardDragStartEventChannelSO cardDragStartChannel;

        // 전략 패턴
        private ICardUIStrategy currentStrategy;
        private CardUIBaseContext currentContext;

        // 카드 데이터
        private CardData cardData;
        private bool isDraggable = false;

        // Canvas 참조
        private Canvas parentCanvas;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetMode(mode); // 초기 전략 설정
        }

        private void OnDestroy()
        {
            currentStrategy?.Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            parentCanvas = GetComponentInParent<Canvas>();

            // 초기 상태: 모든 패널 비활성화
            CardUIPanelHelper.HideUnitStatPanels(CreateViewData());
        }

        private CardUIViewData CreateViewData()
        {
            return new CardUIViewData
            {
                CardImage = cardImage,
                ItemImage = itemImage,
                CostText = costText,
                OwnedCountText = ownedCountText,
                CanvasGroup = canvasGroup,
                RemoveButton = removeButton,
                GlowEffect = glowEffect,
                AttackParent = attackParent,
                AttackText = attackText,
                HpParent = hpParent,
                HpText = hpText,
                MovementParent = movementParent,
                MovementText = movementText
            };
        }

        private CardUIDragState CreateDragState()
        {
            return new CardUIDragState
            {
                IsDragging = false
            };
        }

        private CardUISettings CreateSettings()
        {
            return new CardUISettings
            {
                DragAlpha = dragAlpha,
                DragScale = dragScale,
                ReturnSpeed = returnSpeed,
                ReturnToOriginalPosition = returnToOriginalPosition,
                ValidDropColor = validDropColor,
                InvalidDropColor = invalidDropColor
            };
        }

        private CardUIEventChannels CreateEventChannels()
        {
            return new CardUIEventChannels
            {
                CardInfoChannel = cardInfoChannel,
                CardDragEndChannel = cardDragEndChannel,
                CardDragStartChannel = cardDragStartChannel
            };
        }

        #endregion

        #region Strategy Management

        /// <summary>
        /// 모드 설정 및 전략 변경
        /// </summary>
        public void SetMode(CardUIMode newMode)
        {
            if (mode == newMode && currentStrategy != null)
                return;

            // 이전 전략 정리
            currentStrategy?.Cleanup();

            mode = newMode;

            // 컨텍스트 생성
            currentContext = CreateContextForMode(mode);

            // 전략 생성 및 초기화
            currentStrategy = CreateStrategyForMode(mode);

            if (currentStrategy != null && currentContext != null)
            {
                currentStrategy.Initialize(currentContext);
                UpdateDraggableByMode();
                Debug.Log($"[CardUIRefactored] Mode changed to: {mode}");
            }
            else
            {
                Debug.LogError($"[CardUIRefactored] Failed to create strategy or context for mode: {mode}");
            }
        }

        private CardUIBaseContext CreateContextForMode(CardUIMode mode)
        {
            CardUIBaseContext context = null;

            switch (mode)
            {
                case CardUIMode.InHand:
                    var battleContext = new CardUIBattleContext();

                    // 전투 서비스 주입
                    if (ServiceLocator.IsInitialized)
                    {
                        var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
                        battleContext.CardSpawnService = cardServiceManager?.GetCardSpawnService();
                        battleContext.SpawnValidator = cardServiceManager?.GetSpawnValidator();
                        battleContext.CardHandManager = cardServiceManager?.GetCardHandManager();

                        var gridManager = ServiceLocator.Get<IGridManager>();
                        if (gridManager != null)
                        {
                            battleContext.GridManager = gridManager;
                            battleContext.GridRenderer = gridManager.GetGridRenderer();
                        }
                    }

                    context = battleContext;
                    break;

                case CardUIMode.InInventory:
                    var inventoryContext = new CardUIInventoryContext();
                    // Coordinator는 SetupForInventory에서 설정됨
                    context = inventoryContext;
                    break;

                case CardUIMode.InDeck:
                    var builderContext = new CardUIBuilderContext();
                    // Coordinator는 SetupForDeck에서 설정됨
                    context = builderContext;
                    break;
            }

            if (context != null)
            {
                // 공통 속성 설정
                context.CardData = cardData;
                context.Transform = transform;
                context.GameObject = gameObject;
                context.MonoBehaviour = this;
                context.ParentCanvas = parentCanvas;
                context.ViewData = CreateViewData();
                context.DragState = CreateDragState();
                context.Settings = CreateSettings();
                context.Events = CreateEventChannels();
            }

            return context;
        }

        private ICardUIStrategy CreateStrategyForMode(CardUIMode mode)
        {
            return mode switch
            {
                CardUIMode.InHand => new InHandStrategy(),
                CardUIMode.InInventory => new InInventoryStrategy(),
                CardUIMode.InDeck => new InDeckStrategy(),
                _ => null
            };
        }

        private void UpdateDraggableByMode()
        {
            switch (mode)
            {
                case CardUIMode.InHand:
                    isDraggable = false; // CardHandManager가 제어
                    break;
                case CardUIMode.InInventory:
                    isDraggable = true; // 항상 드래그 가능
                    break;
                case CardUIMode.InDeck:
                    isDraggable = true; // 재정렬을 위해 드래그 가능
                    break;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 카드 데이터 설정
        /// </summary>
        public void SetCardData(CardData data)
        {
            cardData = data;

            if (currentContext != null)
            {
                currentContext.CardData = data;
                currentStrategy?.UpdateUI();
            }
        }

        /// <summary>
        /// 카드 데이터 반환
        /// </summary>
        public CardData GetCardData()
        {
            return cardData;
        }

        /// <summary>
        /// 드래그 가능 상태 설정
        /// </summary>
        public void SetDraggable(bool draggable)
        {
            isDraggable = draggable;
            currentStrategy?.UpdateInteractability(draggable);

            Debug.Log($"[CardUIRefactored] SetDraggable({draggable})");
        }

        /// <summary>
        /// 상호작용 가능 상태 설정
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isDraggable = interactable;
            currentStrategy?.UpdateInteractability(interactable);

            Debug.Log($"[CardUIRefactored] SetInteractable({interactable})");
        }

        /// <summary>
        /// 인벤토리 모드용 Setup
        /// </summary>
        public void SetupForInventory(CardData card, int count, Game.UI.Coordinators.DeckInventoryCoordinator coordinator)
        {
            SetCardData(card);  // ✅ SetMode 전에 CardData 먼저 설정
            SetMode(CardUIMode.InInventory);

            // Coordinator 참조 설정 (중재자 패턴)
            if (currentContext != null)
            {
                currentContext.Coordinator = coordinator;
            }

            if (currentStrategy is InInventoryStrategy inventoryStrategy)
            {
                inventoryStrategy.SetOwnedCount(count);
            }
        }

        /// <summary>
        /// 덱 모드용 Setup
        /// </summary>
        public void SetupForDeck(CardData card, int count, Game.UI.Coordinators.DeckInventoryCoordinator coordinator)
        {
            SetCardData(card);  // ✅ SetMode 전에 CardData 먼저 설정
            SetMode(CardUIMode.InDeck);

            // Coordinator 참조 설정 (중재자 패턴)
            if (currentContext != null)
            {
                currentContext.Coordinator = coordinator;
            }

            if (currentStrategy is InDeckStrategy deckStrategy)
            {
                deckStrategy.SetCardCount(count);
            }

            Debug.Log($"[CardUIRefactored] Setup for deck: {card.CardName} x{count}");
        }

        /// <summary>
        /// 덱 내 개수 업데이트
        /// </summary>
        public void UpdateCount(int newCount)
        {
            if (currentStrategy is InDeckStrategy deckStrategy)
            {
                deckStrategy.SetCardCount(newCount);
            }
            else if (currentStrategy is InInventoryStrategy inventoryStrategy)
            {
                inventoryStrategy.SetOwnedCount(newCount);
            }
        }

        /// <summary>
        /// 카드 사용 완료 처리
        /// </summary>
        public void OnCardUsed()
        {
            Debug.Log($"[CardUIRefactored] Card used: {cardData?.CardName}");

            if (currentContext is CardUIBattleContext battleContext)
            {
                if (battleContext.CardHandManager != null && cardData != null)
                {
                    battleContext.CardHandManager.RemoveCardFromHand(cardData, this);
                    return;
                }
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 현재 드래그 중인지 여부
        /// </summary>
        public bool IsDragging => currentContext?.DragState?.IsDragging ?? false;

        /// <summary>
        /// 현재 모드 반환
        /// </summary>
        public CardUIMode GetMode() => mode;

        #endregion

        #region Drag & Drop Event Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[CardUIRefactored] OnBeginDrag");

            if (!isDraggable || currentStrategy == null || cardData == null)
            {
                Debug.Log($"[CardUIRefactored] OnBeginDrag {!isDraggable} | {currentStrategy == null} | {cardData == null}");
                return;
            }

            currentStrategy.OnDragStart(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggable || currentStrategy == null) return;
            currentStrategy.OnDragging(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDraggable || currentStrategy == null) return;
            currentStrategy.OnDragEnd(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            currentStrategy?.OnClick(eventData);
        }

        #endregion

        #region Editor Debugging

#if UNITY_EDITOR
        [Header("디버깅 도구")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            var rect = new Rect(Screen.width - 300, 10, 280, 180);
            GUILayout.BeginArea(rect);
            GUILayout.Box("CardUIRefactored Debug Info");

            GUILayout.Label($"Card: {cardData?.CardName ?? "None"}");
            GUILayout.Label($"Mode: {mode}");
            GUILayout.Label($"Strategy: {currentStrategy?.GetType().Name ?? "None"}");
            GUILayout.Label($"Context: {currentContext?.GetType().Name ?? "None"}");
            GUILayout.Label($"Draggable: {isDraggable}");
            GUILayout.Label($"Is Dragging: {IsDragging}");

            if (currentContext is CardUIBattleContext battleContext)
            {
                var hasServices = battleContext.CardSpawnService != null &&
                                  battleContext.SpawnValidator != null;
                GUILayout.Label($"Battle Services: {(hasServices ? "Connected" : "Missing")}");
            }

            if (GUILayout.Button("Toggle Draggable"))
            {
                SetDraggable(!isDraggable);
            }

            if (GUILayout.Button("Refresh Strategy"))
            {
                SetMode(mode);
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
