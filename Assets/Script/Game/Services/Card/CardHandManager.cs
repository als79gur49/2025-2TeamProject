using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using Game.Card.Effects;
using Game.Card.UI.Refactored;

namespace Game.Services
{
    /// <summary>
    /// 플레이어의 카드 핸드를 관리하는 서비스 (UI 포함)
    /// 카드 드로우, 핸드 표시, 플레이어 상호작용을 담당
    /// Phase 3: UI 및 상호작용 구현 완료
    /// </summary>
    public class CardHandManager : MonoBehaviour, ICardHandManager
    {
        [Header("핸드 관리 설정")]
        [SerializeField] private int maxHandSize = 7;
        [SerializeField] private bool enableLogging = true;

        [Header("UI 설정")]
        [SerializeField] private Transform handUIParent;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private float cardSpacing = 120f;
        [SerializeField] private bool arrangeCardsInArc = false;  // 수직 배치 사용
        [SerializeField] private float arcRadius = 800f;
        [SerializeField] private bool arrangeVertically = true;   // 수직 배치 옵션

        [Header("카드 데이터 소스")]
        [SerializeField] private List<CardData> availableCards = new List<CardData>();
        [SerializeField] private List<CardData> initialHandCards = new List<CardData>();

        [Header("덱 기반 초기 핸드 설정")]
        [SerializeField] private int defaultInitialHandSizeFromDeck = 3;

        [Header("상호작용 설정")]
        [SerializeField] private bool enablePlayerInteraction = false;

        // 핸드 상태
        private List<CardData> handCards = new List<CardData>();
        private List<CardUIRefactored> cardUIComponents = new List<CardUIRefactored>();
        private bool isInitialized = false;
        private bool isPlayerSummonMode = false;

        // 덱 관리 (Phase 4: PlayerData 연동)
        private List<CardData> deckCards = new List<CardData>();
        private Dictionary<CardData, int> deckCardCounts = new Dictionary<CardData, int>();
        private bool isDeckLoaded = false;

        // 서비스 참조
        private ITurnService turnService;
        private IGlobalStateManager _stateManager;

        // 🔒 Global state tracking for card interaction blocking
        private bool _isGameFlowLocked = false;

        /// <summary>핸드 매니저가 초기화되었는지 여부</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>현재 핸드의 카드 수</summary>
        public int HandSize => handCards.Count;

        /// <summary>플레이어 소환 모드인지 여부</summary>
        public bool IsPlayerSummonMode => isPlayerSummonMode;

        #region Unity Lifecycle

        private void Awake()
        {
            // CardServiceManager에 의해 초기화되므로 여기서는 초기화하지 않음
        }

        private void OnDestroy()
        {
            // Unsubscribe from GlobalStateManager events
            if (_stateManager != null)
            {
                _stateManager.OnBusyStateChanged -= HandleGlobalBusyStateChanged;
                Log("🔓 Unsubscribed from GlobalStateManager events");
            }
        }

        #endregion

        #region CardServiceManager 호출 메서드

        /// <summary>
        /// CardServiceManager에 의해 호출되는 초기화 메서드
        /// Unit의 Init() 패턴을 따라 외부에서 의존성을 주입받음
        /// </summary>
        /// <param name="iTurnService">턴 서비스 인터페이스</param>
        public void Init(ITurnService iTurnService)
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[CardHandManager] {gameObject.name} already initialized");
                return;
            }

            Log("🖐️ Initializing CardHandManager...");

            // 외부에서 주입받은 의존성 설정
            InjectDependencies(iTurnService);

            // UI 컴포넌트 초기화
            InitializeUI();

            // 이벤트 시스템 설정
            SetupEventSystem();

            isInitialized = true;
            Log("✅ CardHandManager initialization completed");

            // 초기 핸드 설정 (테스트용 / 덱 미사용 씬)
            SetupInitialHandFromConfig();
        }

        /// <summary>
        /// 외부에서 주입받은 서비스 의존성 설정
        /// </summary>
        private void InjectDependencies(ITurnService iTurnService)
        {
            turnService = iTurnService;

            if (turnService != null)
            {
                Log("✅ TurnService dependency injected successfully");
            }
            else
            {
                Debug.LogError("[CardHandManager] TurnService is null");
            }

            // Get GlobalStateManager from ServiceLocator
            _stateManager = ServiceLocator.Get<IGlobalStateManager>();
            if (_stateManager == null)
            {
                Debug.LogWarning("[CardHandManager] IGlobalStateManager not found - VFX blocking disabled");
            }
            else
            {
                Log("✅ GlobalStateManager dependency injected successfully");
            }
        }

        /// <summary>
        /// UI 컴포넌트 초기화
        /// </summary>
        private void InitializeUI()
        {
            // HandUI 부모 오브젝트 찾기 또는 생성
            if (handUIParent == null)
            {
                handUIParent = FindOrCreateHandUIParent();
            }

            // CardUI 프리팹 검증
            if (cardUIPrefab == null)
            {
                LogError("CardUI Prefab not assigned! Please assign CardUI prefab in inspector.");
            }

            Log("✅ UI components initialized");
        }

        /// <summary>
        /// 이벤트 시스템 설정
        /// </summary>
        private void SetupEventSystem()
        {
            // TurnService 이벤트 구독 (CardServiceManager에서 처리하지만 여기서도 추가 처리 가능)
            // Phase 3에서는 UI 상태 관리에 집중

            // Subscribe to GlobalStateManager for VFX blocking
            if (_stateManager != null)
            {
                _stateManager.OnBusyStateChanged += HandleGlobalBusyStateChanged;
                Log("✅ Subscribed to GlobalStateManager events");
            }

            Log("✅ Event system setup completed");
        }

        /// <summary>
        /// 초기 핸드 설정 (initialHandCards를 핸드에 추가)
        /// 덱이 없는 튜토리얼/테스트용 씬에서 사용
        /// </summary>
        private void SetupInitialHandFromConfig()
        {
            if (initialHandCards == null || initialHandCards.Count == 0)
            {
                Log("⚠️ No initial hand cards configured");
                return;
            }

            Log($"🎴 Setting up initial hand with {initialHandCards.Count} cards");

            foreach (var cardData in initialHandCards)
            {
                if (cardData == null)
                {
                    LogError("Null CardData found in initialHandCards list");
                    continue;
                }

                if (IsHandFull())
                {
                    LogError($"Hand is full! Cannot add {cardData.CardName} to initial hand");
                    break;
                }

                bool success = AddCardToHand(cardData);
                if (success)
                {
                    Log($"✅ Added initial card: {cardData.CardName}");
                }
                else
                {
                    LogError($"Failed to add initial card: {cardData.CardName}");
                }
            }

            Log($"✅ Initial hand setup completed: {handCards.Count}/{maxHandSize} cards");
        }

        /// <summary>
        /// 덱에서 초기 핸드를 드로우하여 설정
        /// PlayerData 기반 덱 전투에서 사용
        /// </summary>
        /// <param name="initialHandSize">초기 손패로 뽑을 카드 수</param>
        public void SetupInitialHandFromDeck(int initialHandSize)
        {
            if (!isInitialized)
            {
                LogError("Cannot setup initial hand from deck - CardHandManager not initialized");
                return;
            }

            if (!IsDeckLoaded())
            {
                Log("⚠️ Deck is not loaded or empty - cannot setup initial hand from deck");
                return;
            }

            if (initialHandSize <= 0)
            {
                Log("⚠️ Requested initial hand size from deck is zero or negative - skipping initial draw");
                return;
            }

            Log($"🎴 Setting up initial hand FROM DECK with requested size {initialHandSize}");

            // 기존 핸드 제거 (Config 기반 초기 핸드 포함)
            if (handCards.Count > 0)
            {
                ClearHand();
            }

            int drawnCount = 0;

            for (int i = 0; i < initialHandSize; i++)
            {
                if (IsHandFull())
                {
                    Log("⚠️ Hand is full while setting up initial hand from deck");
                    break;
                }

                // 덱에서 직접 카드 가져오기 (fallback 없이 순수 덱 기반)
                CardData nextCard = GetNextCardFromDeck();
                if (nextCard == null)
                {
                    Log("⚠️ Deck became empty while setting up initial hand from deck");
                    break;
                }

                bool success = AddCardToHand(nextCard);
                if (success)
                {
                    drawnCount++;
                }
                else
                {
                    LogError($"Failed to add initial deck card: {nextCard.CardName}");
                }
            }

            Log($"✅ Initial hand from deck setup completed: {drawnCount} cards drawn ({handCards.Count}/{maxHandSize})");
        }

        /// <summary>
        /// HandUI 부모 오브젝트 찾기 또는 생성
        /// 좌측 상단에서 하단으로 배치
        /// </summary>
        private Transform FindOrCreateHandUIParent()
        {
            // Canvas 하위에서 "HandUI" 오브젝트 찾기
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var existingHandUI = canvas.transform.Find("HandUI");
                if (existingHandUI != null)
                {
                    return existingHandUI;
                }

                // 없으면 생성
                var handUIObject = new GameObject("HandUI");
                handUIObject.transform.SetParent(canvas.transform, false);

                // RectTransform 설정 - 좌측 배치 (위에서 아래로)
                var rectTransform = handUIObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0f, 0.3f);   // 좌측 하단
                rectTransform.anchorMax = new Vector2(0.2f, 0.9f); // 좌측 상단
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                Log("🖼️ HandUI parent created on LEFT side");
                return handUIObject.transform;
            }

            LogError("Canvas not found! HandUI cannot be created.");
            return null;
        }

        #endregion

        #region GlobalStateManager Integration

        /// <summary>
        /// Handles GlobalStateManager busy state changes for card interaction blocking
        /// </summary>
        private void HandleGlobalBusyStateChanged(BusyType type, bool isBusy)
        {
            if (type == BusyType.GameFlowLock)
            {
                _isGameFlowLocked = isBusy;
                UpdateCardInteractivity(); // Immediately update card interactivity
                Log($"🔒 GameFlowLock state changed: {isBusy} - Card interactivity updated");
            }
        }

        /// <summary>
        /// Updates card interactivity based on current state
        /// Cards are draggable only if:
        /// 1. NOT GameFlowLocked (no VFX playing)
        /// 2. isPlayerSummonMode is true (in AllySummon phase)
        /// 3. enablePlayerInteraction is true
        /// </summary>
        private void UpdateCardInteractivity()
        {
            // GameFlowLock always disables cards regardless of other conditions
            bool shouldBeInteractive = !_isGameFlowLocked && isPlayerSummonMode && enablePlayerInteraction;

            foreach (var cardUI in cardUIComponents)
            {
                if (cardUI != null)
                {
                    cardUI.SetDraggable(shouldBeInteractive);
                }
            }

            Log($"🎮 Card interactivity updated: {shouldBeInteractive} (GameFlowLock: {_isGameFlowLocked}, SummonMode: {isPlayerSummonMode})");
        }

        #endregion

        #region 플레이어 상호작용 (Phase 3 구현 완료)

        /// <summary>
        /// 플레이어의 소환 모드 활성화 (AllySummon 페이즈에서 호출)
        /// </summary>
        public void EnablePlayerSummonMode()
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return;
            }

            isPlayerSummonMode = true;
            enablePlayerInteraction = true;

            // Use centralized method that checks GameFlowLock
            UpdateCardInteractivity();

            // HandUI 활성화
            if (handUIParent != null)
            {
                handUIParent.gameObject.SetActive(true);
            }

            Log("🟢 Player summon mode enabled - Card interactivity managed by UpdateCardInteractivity()");
        }

        /// <summary>
        /// 플레이어의 소환 모드 비활성화
        /// </summary>
        public void DisablePlayerSummonMode()
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return;
            }

            isPlayerSummonMode = false;
            enablePlayerInteraction = false;

            // Use centralized method that checks GameFlowLock
            UpdateCardInteractivity();

            Log("🔴 Player summon mode disabled - Card interactivity managed by UpdateCardInteractivity()");
        }

        #endregion

        #region 핸드 관리 (Phase 3 구현)

        /// <summary>
        /// 핸드에 카드 추가
        /// </summary>
        public bool AddCardToHand(CardData cardData)
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return false;
            }

            if (cardData == null)
            {
                LogError("Cannot add null card to hand");
                return false;
            }

            if (handCards.Count >= maxHandSize)
            {
                LogError($"Hand is full! Cannot add {cardData.CardName}");
                return false;
            }

            // 카드 데이터 추가
            handCards.Add(cardData);

            // UI 생성
            CreateCardUI(cardData);

            // 핸드 레이아웃 업데이트
            UpdateHandLayout();

            Log($"✅ Added {cardData.CardName} to hand ({handCards.Count}/{maxHandSize})");
            return true;
        }

        /// <summary>
        /// 핸드에서 카드 제거
        /// </summary>
        public bool RemoveCardFromHand(CardData cardData)
        {
            if (!isInitialized || cardData == null)
                return false;

            int cardIndex = handCards.IndexOf(cardData);
            if (cardIndex == -1)
                return false;

            // 카드 데이터 제거
            handCards.RemoveAt(cardIndex);

            // UI 제거
            if (cardIndex < cardUIComponents.Count)
            {
                var cardUI = cardUIComponents[cardIndex];
                if (cardUI != null)
                {
                    Destroy(cardUI.gameObject);
                }
                cardUIComponents.RemoveAt(cardIndex);
            }

            // 핸드 레이아웃 업데이트
            UpdateHandLayout();

            Log($"✅ Removed {cardData.CardName} from hand ({handCards.Count}/{maxHandSize})");
            return true;
        }

        /// <summary>
        /// 핸드에서 카드 제거 (CardUIRefactored 참조와 함께)
        /// CardUIRefactored 객체를 직접 받아서 정확하게 제거 - 드래그 중 부모 변경 문제 해결
        /// </summary>
        public bool RemoveCardFromHand(CardData cardData, CardUIRefactored cardUI)
        {
            if (!isInitialized || cardData == null || cardUI == null)
                return false;

            // 카드 데이터 제거
            int cardIndex = handCards.IndexOf(cardData);
            if (cardIndex != -1)
            {
                handCards.RemoveAt(cardIndex);
            }
            else
            {
                LogError($"⚠️ CardData {cardData.CardName} not found in handCards");
            }

            // UI 제거 - 정확한 CardUIRefactored 객체를 직접 제거
            if (cardUIComponents.Contains(cardUI))
            {
                cardUIComponents.Remove(cardUI);
                Destroy(cardUI.gameObject);
                Log($"🗑️ Destroyed CardUIRefactored for {cardData.CardName}");
            }
            else
            {
                LogError($"⚠️ CardUIRefactored not found in cardUIComponents for {cardData.CardName}");
                // 그래도 파괴는 시도
                Destroy(cardUI.gameObject);
            }

            // 핸드 레이아웃 업데이트
            UpdateHandLayout();

            Log($"✅ Removed {cardData.CardName} from hand ({handCards.Count}/{maxHandSize})");
            return true;
        }


        /// <summary>
        /// 카드 UI 생성
        /// </summary>
        private void CreateCardUI(CardData cardData)
        {
            if (cardUIPrefab == null || handUIParent == null)
            {
                LogError("Cannot create CardUI - prefab or parent missing");
                return;
            }

            // CardUIRefactored 인스턴스 생성
            var cardUIObject = Instantiate(cardUIPrefab, handUIParent);
            var cardUI = cardUIObject.GetComponent<CardUIRefactored>();

            if (cardUI != null)
            {
                // 카드 데이터 설정
                cardUI.SetCardData(cardData);

                // 드래그 가능 여부 설정
                cardUI.SetDraggable(isPlayerSummonMode && enablePlayerInteraction);

                // 리스트에 추가
                cardUIComponents.Add(cardUI);

                Log($"🎴 Created CardUIRefactored for {cardData.CardName}");
            }
            else
            {
                LogError($"CardUIRefactored component not found on prefab for {cardData.CardName}");
                Destroy(cardUIObject);
            }
        }

        /// <summary>
        /// 핸드 레이아웃 업데이트
        /// </summary>
        private void UpdateHandLayout()
        {
            if (cardUIComponents.Count == 0) return;

            Debug.Log($"[CardHandManager] UpdateHandLayout called - CardCount: {cardUIComponents.Count}, Time: {Time.frameCount}");
            Debug.Log($"[CardHandManager] UpdateHandLayout StackTrace:");
            Debug.Log(System.Environment.StackTrace);

            // 드래그 중인 카드가 있는지 확인
            var draggingCards = cardUIComponents.Where(c => c != null && c.IsDragging).ToList();
            if (draggingCards.Count > 0)
            {
                // 이제 레이아웃 유틸리티가 드래그 중 카드를 제외하고 정렬하므로
                // 이 상황은 버그가 아니라 정상적인 정보 로그로 취급한다.
                Debug.Log($"[CardHandManager] UpdateHandLayout executing while {draggingCards.Count} card(s) are DRAGGING (dragged cards are excluded from layout).");
                foreach (var card in draggingCards)
                {
                    Debug.Log($"  - Dragging card: {card.GetCardData()?.CardName}");
                }
            }

            if (arrangeCardsInArc)
            {
                ArrangeCardsInArc();
            }
            else if (arrangeVertically)
            {
                ArrangeCardsVertically();
            }
            else
            {
                ArrangeCardsInLine();
            }
        }

        /// <summary>
        /// 카드들을 호형으로 배열
        /// </summary>
        private void ArrangeCardsInArc()
        {
            CardHandLayoutManager.ArrangeInArc(cardUIComponents, arcRadius);
        }

        /// <summary>
        /// 카드들을 일직선으로 배열
        /// </summary>
        private void ArrangeCardsInLine()
        {
            CardHandLayoutManager.ArrangeInLine(cardUIComponents, cardSpacing);
        }

        /// <summary>
        /// 카드들을 수직으로 배열 (위에서 아래로) - 좌측 배치용
        /// 수직 중앙을 기준으로 균형있게 배치
        /// </summary>
        private void ArrangeCardsVertically()
        {
            CardHandLayoutManager.ArrangeVerticalCentered(cardUIComponents, cardSpacing);
        }

        /// <summary>
        /// 핸드 초기화 (모든 카드 제거)
        /// </summary>
        public void ClearHand()
        {
            // UI 제거
            foreach (var cardUI in cardUIComponents)
            {
                if (cardUI != null)
                {
                    Destroy(cardUI.gameObject);
                }
            }

            // 리스트 초기화
            handCards.Clear();
            cardUIComponents.Clear();

            Log("🗑️ Hand cleared");
        }

        /// <summary>
        /// 랜덤 카드 드로우
        /// Phase 4: 덱 기반 드로우 우선, 덱이 없으면 availableCards fallback
        /// CardServiceManager에서 호출됨
        /// </summary>
        public void DrawRandomCard()
        {
            if (!isInitialized)
            {
                LogError("CardHandManager not initialized");
                return;
            }

            if (IsHandFull())
            {
                LogError("Hand is full - cannot draw more cards");
                return;
            }

            Log("🃏 DrawRandomCard() called - Drawing a card for player");

            CardData cardToDraw = null;

            // 1순위: 로드된 덱에서 카드 드로우
            if (isDeckLoaded && deckCards.Count > 0)
            {
                cardToDraw = GetNextCardFromDeck();

                if (cardToDraw != null)
                {
                    Log($"📚 Drew from deck: {cardToDraw.CardName}");
                }
                else
                {
                    LogError("⚠️ GetNextCardFromDeck returned null");
                }
            }
            else
            {
                // 2순위 (fallback): availableCards에서 랜덤 선택
                Log("⚠️ Deck not loaded or empty - using fallback (availableCards)");

                if (availableCards != null && availableCards.Count > 0)
                {
                    // 유효한 카드 데이터만 필터링
                    var validCards = availableCards.Where(card => card != null).ToList();

                    if (validCards.Count > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, validCards.Count);
                        cardToDraw = validCards[randomIndex];

                        Log($"🎯 Selected fallback card: {cardToDraw.CardName} (index {randomIndex}/{validCards.Count})");
                    }
                    else
                    {
                        LogError("No valid CardData available in availableCards list");
                    }
                }
            }

            // 선택된 카드를 핸드에 추가
            if (cardToDraw != null)
            {
                bool success = AddCardToHand(cardToDraw);

                if (success)
                {
                    Log($"✨ Successfully drew card: {cardToDraw.CardName}");
                }
                else
                {
                    LogError($"Failed to add card to hand: {cardToDraw.CardName}");
                }
            }
            else
            {
                LogError("❌ No card to draw - all sources exhausted");
            }
        }

        #endregion

        #region 덱 관리 (Phase 4: PlayerData 연동)

        /// <summary>
        /// 덱 로드 및 셔플
        /// PlayerData의 lastUsedDeckName에서 로드된 덱을 설정
        /// </summary>
        /// <param name="deck">카드와 개수로 구성된 덱 데이터</param>
        public void LoadDeck(Dictionary<CardData, int> deck)
        {
            if (deck == null || deck.Count == 0)
            {
                LogError("❌ Cannot load empty or null deck");
                isDeckLoaded = false;
                return;
            }

            Log($"📚 Loading deck with {deck.Count} unique cards...");

            // 기존 덱 초기화
            deckCards.Clear();
            deckCardCounts.Clear();

            // count 기반으로 카드 리스트 생성
            foreach (var entry in deck)
            {
                CardData card = entry.Key;
                int count = entry.Value;

                if (card == null || count <= 0)
                {
                    LogError($"⚠️ Invalid deck entry: {(card == null ? "null card" : card.CardName)} with count {count}");
                    continue;
                }

                // count만큼 카드를 리스트에 추가
                for (int i = 0; i < count; i++)
                {
                    deckCards.Add(card);
                }

                // count 정보 저장
                deckCardCounts[card] = count;

                Log($"  📄 Added {count}x {card.CardName}");
            }

            // Fisher-Yates 알고리즘으로 셔플
            ShuffleDeck();

            isDeckLoaded = true;
            Log($"✅ Deck loaded and shuffled: {deckCards.Count} total cards");
        }

        /// <summary>
        /// Fisher-Yates 알고리즘으로 덱 셔플
        /// </summary>
        private void ShuffleDeck()
        {
            int n = deckCards.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                // Swap
                CardData temp = deckCards[i];
                deckCards[i] = deckCards[j];
                deckCards[j] = temp;
            }
            Log($"🔀 Deck shuffled ({n} cards)");
        }

        /// <summary>
        /// 셔플된 덱에서 다음 카드 반환
        /// </summary>
        /// <returns>다음 카드, 덱이 비었으면 null</returns>
        private CardData GetNextCardFromDeck()
        {
            if (deckCards.Count == 0)
            {
                Log("⚠️ Deck is empty");
                return null;
            }

            // 리스트의 마지막 카드를 꺼냄 (Stack처럼 동작)
            CardData card = deckCards[deckCards.Count - 1];
            deckCards.RemoveAt(deckCards.Count - 1);

            // count 감소 (선택사항 - 통계 목적)
            if (deckCardCounts.ContainsKey(card))
            {
                deckCardCounts[card]--;
                if (deckCardCounts[card] <= 0)
                {
                    deckCardCounts.Remove(card);
                }
            }

            Log($"🎴 Drew from deck: {card.CardName} ({deckCards.Count} cards remaining)");
            return card;
        }

        /// <summary>
        /// 덱이 로드되었는지 여부 반환
        /// </summary>
        public bool IsDeckLoaded()
        {
            return isDeckLoaded && deckCards.Count > 0;
        }

        /// <summary>
        /// 덱에 남은 총 카드 수 반환
        /// </summary>
        public int GetRemainingDeckCount()
        {
            return deckCards.Count;
        }

        #endregion

        #region 로깅 시스템

        private void Log(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[CardHandManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CardHandManager] {message}");
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 핸드 매니저 상태 정보 반환 (디버깅용)
        /// </summary>
        public string GetStatus()
        {
            return $"CardHandManager Status:\n" +
                   $"- Initialized: {isInitialized}\n" +
                   $"- Max Hand Size: {maxHandSize}\n" +
                   $"- Current Hand Size: {handCards.Count}\n" +
                   $"- Player Summon Mode: {isPlayerSummonMode}\n" +
                   $"- Player Interaction: {enablePlayerInteraction}\n" +
                   $"- Hand UI Parent: {(handUIParent != null ? "✅" : "❌")}\n" +
                   $"- Card UI Prefab: {(cardUIPrefab != null ? "✅" : "❌")}\n" +
                   $"- Available Cards: {(availableCards != null ? availableCards.Count : 0)}\n";
        }

        /// <summary>
        /// 핸드의 모든 카드 데이터 반환
        /// </summary>
        public List<CardData> GetHandCards()
        {
            return new List<CardData>(handCards);
        }

        /// <summary>
        /// 설정된 초기 핸드 카드 개수 반환 (Config 기반)
        /// 덱 기반 초기 드로우 시 기본 개수로 사용
        /// </summary>
        public int GetConfiguredInitialHandSize()
        {
            // 1순위: 씬/프리팹에서 설정된 initialHandCards 개수
            int configuredSize = initialHandCards != null ? initialHandCards.Count : 0;
            if (configuredSize > 0)
            {
                return configuredSize;
            }

            // 2순위: 인스펙터에서 설정 가능한 기본 덱 기반 초기 핸드 크기
            if (defaultInitialHandSizeFromDeck > 0)
            {
                return defaultInitialHandSizeFromDeck;
            }

            // 3순위: 잘못된 설정(0 이하)일 경우, 안전한 기본값 3 사용
            return 3;
        }

        /// <summary>
        /// 특정 인덱스의 카드 반환
        /// </summary>
        public CardData GetCardAt(int index)
        {
            if (index >= 0 && index < handCards.Count)
                return handCards[index];
            return null;
        }

        /// <summary>
        /// 핸드가 가득 찼는지 확인
        /// </summary>
        public bool IsHandFull()
        {
            return handCards.Count >= maxHandSize;
        }

        /// <summary>
        /// 특정 카드가 핸드에 있는지 확인
        /// </summary>
        public bool HasCard(CardData cardData)
        {
            return handCards.Contains(cardData);
        }

        /// <summary>
        /// 핸드 레이아웃 재정렬 (외부 호출용)
        /// CardUI에서 카드 사용 실패 시 원래 인덱스로 복귀한 후 호출됨
        /// </summary>
        public void RefreshHandLayout()
        {
            UpdateHandLayout();
        }

        #endregion
    }
}
