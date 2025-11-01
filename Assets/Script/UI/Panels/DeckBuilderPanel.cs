using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using Game.Data;
using Game.Validators;
using Game.Systems;
using Game.Card.UI.Refactored;

namespace Game.UI.Panels
{
    /// <summary>
    /// 덱 빌더 패널 - 덱 구성 및 편집 기능 제공
    /// 드래그 앤 드롭으로 카드 추가/제거, 덱 검증, 마나 커브 표시
    /// </summary>
    public class DeckBuilderPanel : UIPanel, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Deck Settings")]
        [SerializeField] private Transform deckListContainer;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private ScrollRect deckScrollRect;

        [Header("Deck Info")]
        [SerializeField] private TMP_InputField deckNameInput;
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private TextMeshProUGUI validationStatusText;

        [Header("Mana Curve")]
        [SerializeField] private Transform manaCurveContainer;
        [SerializeField] private GameObject manaCurveBarPrefab;
        private List<Image> manaCurveBars = new List<Image>();

        [Header("Deck Validation")]
        [SerializeField] private int minDeckSize = 30;
        [SerializeField] private int maxDeckSize = 30;
        [SerializeField] private int maxCopiesPerCard = 3;

        [Header("Visual Feedback")]
        [SerializeField] private Image dropZoneHighlight;
        [SerializeField] private Color validDropColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color invalidDropColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);

        [Header("Buttons")]
        [SerializeField] private Button saveDeckButton;
        [SerializeField] private Button clearDeckButton;
        [SerializeField] private Button loadDeckButton;

        // 덱 데이터
        private Dictionary<CardData, int> deckCards = new Dictionary<CardData, int>();
        private List<GameObject> deckSlots = new List<GameObject>();

        // 현재 드래그 중인 카드 (검증용)
        private CardData currentDraggedCard = null;

        // Coordinator 참조 (중재자 패턴)
        private Game.UI.Coordinators.DeckInventoryCoordinator coordinator;

        // 이벤트
        public event Action<CardData> OnCardAddedToDeck;
        public event Action<CardData> OnCardRemovedFromDeck;
        public event Action<Dictionary<CardData, int>> OnDeckChanged;

        #region Lifecycle

        /// <summary>
        /// 의존성 없는 초기화 (Awake에서 호출)
        /// UI 컴포넌트 이벤트 설정 및 내부 상태 초기화
        /// </summary>
        protected override void OnInitializeSelf()
        {
            base.OnInitializeSelf();

            // 버튼 이벤트 설정
            if (saveDeckButton != null)
                saveDeckButton.onClick.AddListener(OnSaveDeckClicked);
            if (clearDeckButton != null)
                clearDeckButton.onClick.AddListener(OnClearDeckClicked);
            if (loadDeckButton != null)
                loadDeckButton.onClick.AddListener(OnLoadDeckClicked);

            // 덱 이름 기본값
            if (deckNameInput != null)
                deckNameInput.text = "새로운 덱";

            // 마나 커브 초기화
            InitializeManaCurve();

            UpdateDeckDisplay();

            Debug.Log("[DeckBuilderPanel] Self-initialized successfully (Awake)");
        }

        /// <summary>
        /// 의존성 있는 초기화 (Start에서 호출)
        /// 현재 ServiceLocator 의존성 없음
        /// </summary>
        protected override void OnInitializeWithDependencies()
        {
            base.OnInitializeWithDependencies();

            Debug.Log("[DeckBuilderPanel] Dependency initialization complete (Start)");
        }

        protected override void OnShowPanel()
        {
            base.OnShowPanel();
            UpdateDeckDisplay();
        }

        #endregion

        #region Card Management

        /// <summary>
        /// 카드를 덱에 추가할 수 있는지 확인
        /// </summary>
        public bool CanAddCardToDeck(CardData card)
        {
            // DeckValidator를 사용한 검증
            bool canAdd = DeckValidator.CanAddCard(deckCards, card);

            if (!canAdd)
            {
                Debug.Log($"[DeckBuilder] {card?.CardName ?? "Unknown"} 카드를 추가할 수 없습니다.");
            }

            return canAdd;
        }

        /// <summary>
        /// 덱에 카드 추가
        /// </summary>
        public void AddCardToDeck(CardData card)
        {
            if (!CanAddCardToDeck(card))
                return;

            if (deckCards.ContainsKey(card))
            {
                deckCards[card]++;
                UpdateExistingSlot(card);
            }
            else
            {
                deckCards[card] = 1;
                CreateNewSlot(card);
            }

            UpdateDeckDisplay();
            OnCardAddedToDeck?.Invoke(card);
            OnDeckChanged?.Invoke(deckCards);

            Debug.Log($"[DeckBuilder] Added {card.CardName} to deck");
        }

        /// <summary>
        /// 덱에서 카드 제거
        /// </summary>
        public void RemoveCardFromDeck(CardData card)
        {
            if (!deckCards.ContainsKey(card))
                return;

            deckCards[card]--;

            if (deckCards[card] <= 0)
            {
                deckCards.Remove(card);
                RemoveSlot(card);
            }
            else
            {
                UpdateExistingSlot(card);
            }

            UpdateDeckDisplay();
            OnCardRemovedFromDeck?.Invoke(card);
            OnDeckChanged?.Invoke(deckCards);

            Debug.Log($"[DeckBuilder] Removed {card.CardName} from deck");
        }

        /// <summary>
        /// 새 슬롯 생성
        /// </summary>
        private void CreateNewSlot(CardData card)
        {
            if (cardUIPrefab == null || deckListContainer == null)
            {
                Debug.LogError("[DeckBuilder] CardUI prefab or container not assigned");
                return;
            }

            GameObject slotObj = Instantiate(cardUIPrefab, deckListContainer);

            // CardUIRefactored 컴포넌트 가져와서 Setup 호출
            var cardUI = slotObj.GetComponent<CardUIRefactored>();
            if (cardUI != null)
            {
                //cardUI.SetMode(Game.Card.CardUIMode.InDeck);
                cardUI.SetupForDeck(card, deckCards[card], coordinator);
                Debug.Log($"[DeckBuilder] Created new CardUI for {card.CardName} with count {deckCards[card]}");
            }

            deckSlots.Add(slotObj);

            // 마나 비용 순으로 정렬
            SortDeckSlots();
        }

        /// <summary>
        /// 기존 슬롯 업데이트
        /// </summary>
        private void UpdateExistingSlot(CardData card)
        {
            // 해당 카드의 슬롯을 찾아서 개수 업데이트
            foreach (var slotObj in deckSlots)
            {
                var cardUI = slotObj.GetComponent<CardUIRefactored>();
                if (cardUI != null && cardUI.GetCardData() == card)
                {
                    cardUI.UpdateCount(deckCards[card]);
                    Debug.Log($"[DeckBuilder] Updated CardUI for {card.CardName}: {deckCards[card]}");
                    return;
                }
            }

            Debug.LogWarning($"[DeckBuilder] Could not find CardUI for {card.CardName}");
        }

        /// <summary>
        /// 슬롯 제거
        /// </summary>
        private void RemoveSlot(CardData card)
        {
            // 해당 카드의 슬롯을 찾아서 제거
            for (int i = 0; i < deckSlots.Count; i++)
            {
                var cardUI = deckSlots[i].GetComponent<CardUIRefactored>();
                if (cardUI != null && cardUI.GetCardData() == card)
                {
                    Destroy(deckSlots[i]);
                    deckSlots.RemoveAt(i);
                    Debug.Log($"[DeckBuilder] Removed CardUI for {card.CardName}");
                    return;
                }
            }

            Debug.LogWarning($"[DeckBuilder] Could not find CardUI to remove for {card.CardName}");
        }

        /// <summary>
        /// 덱 슬롯 정렬 (마나 비용 순)
        /// </summary>
        private void SortDeckSlots()
        {
            // TODO: DeckCardSlot 기반 정렬 구현
        }

        /// <summary>
        /// 총 카드 개수 반환
        /// </summary>
        private int GetTotalCardCount()
        {
            return deckCards.Values.Sum();
        }

        /// <summary>
        /// 특정 카드가 덱에 몇 장 들어있는지 반환
        /// </summary>
        public int GetDeckCardCount(CardData card)
        {
            return deckCards.ContainsKey(card) ? deckCards[card] : 0;
        }

        #endregion

        #region Drag & Drop

        /// <summary>
        /// 드래그 중인 카드 설정 (외부에서 호출)
        /// </summary>
        public void SetDraggedCard(CardData card)
        {
            currentDraggedCard = card;
        }

        /// <summary>
        /// 드롭 존에 마우스 진입
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 실제로 드래그 중인지 확인 (eventData.pointerDrag != null)
            if (currentDraggedCard != null && dropZoneHighlight != null && eventData.pointerDrag != null)
            {
                // 드롭 가능 여부에 따라 색상 변경
                bool canDrop = CanAddCardToDeck(currentDraggedCard);
                dropZoneHighlight.color = canDrop ? validDropColor : invalidDropColor;
            }
        }

        /// <summary>
        /// 드롭 존에서 마우스 나감
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (dropZoneHighlight != null)
                dropZoneHighlight.color = normalColor;
        }

        /// <summary>
        /// 카드 드롭
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (dropZoneHighlight != null)
                dropZoneHighlight.color = normalColor;

            if (currentDraggedCard == null)
                return;

            if (CanAddCardToDeck(currentDraggedCard))
            {
                AddCardToDeck(currentDraggedCard);
            }
            else
            {
                // 실패 피드백
                Debug.Log("[DeckBuilder] Cannot add card - validation failed");
            }

            currentDraggedCard = null;
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// 덱 화면 전체 업데이트
        /// </summary>
        private void UpdateDeckDisplay()
        {
            UpdateDeckCountText();
            UpdateManaCurve();
            UpdateValidationStatus();
            UpdateSaveButtonState();
        }

        /// <summary>
        /// 덱 개수 텍스트 업데이트
        /// </summary>
        private void UpdateDeckCountText()
        {
            if (deckCountText == null) return;

            int totalCards = GetTotalCardCount();
            deckCountText.text = $"{totalCards} / {maxDeckSize}";

            // 색상 변경
            if (totalCards < minDeckSize)
                deckCountText.color = Color.yellow;
            else if (totalCards > maxDeckSize)
                deckCountText.color = Color.red;
            else
                deckCountText.color = Color.green;
        }

        /// <summary>
        /// 마나 커브 초기화
        /// </summary>
        private void InitializeManaCurve()
        {
            if (manaCurveContainer == null || manaCurveBarPrefab == null)
                return;

            // 0~9+ 마나용 막대 생성
            for (int i = 0; i <= 9; i++)
            {
                GameObject barObj = Instantiate(manaCurveBarPrefab, manaCurveContainer);
                Image barImage = barObj.GetComponent<Image>();
                if (barImage != null)
                {
                    manaCurveBars.Add(barImage);
                }
            }
        }

        /// <summary>
        /// 마나 커브 업데이트
        /// </summary>
        private void UpdateManaCurve()
        {
            if (manaCurveBars.Count == 0) return;

            // 마나별 카드 수 계산
            int[] manaCounts = new int[10]; // 0~9+

            foreach (var kvp in deckCards)
            {
                int manaCost = Mathf.Min(kvp.Key.ManaCost, 9); // 9+ 처리
                manaCounts[manaCost] += kvp.Value;
            }

            // 최대값 찾기 (정규화용)
            int maxCount = manaCounts.Max();
            if (maxCount == 0) maxCount = 1; // 0으로 나누기 방지

            // 막대 높이 업데이트
            for (int i = 0; i < manaCurveBars.Count && i < manaCounts.Length; i++)
            {
                float normalizedHeight = (float)manaCounts[i] / maxCount;
                RectTransform rectTransform = manaCurveBars[i].GetComponent<RectTransform>();

                if (rectTransform != null)
                {
                    // 높이 조정 (최대 100px)
                    rectTransform.sizeDelta = new Vector2(
                        rectTransform.sizeDelta.x,
                        normalizedHeight * 100f
                    );
                }

                // 색상 (마나별)
                manaCurveBars[i].color = GetManaColor(i);
            }
        }

        /// <summary>
        /// 마나 비용에 따른 색상 반환
        /// </summary>
        private Color GetManaColor(int manaCost)
        {
            // 마나 비용에 따른 그라데이션
            float t = manaCost / 9f;
            return Color.Lerp(new Color(0.2f, 1f, 0.2f), new Color(1f, 0.2f, 0.2f), t);
        }

        /// <summary>
        /// 검증 상태 업데이트
        /// </summary>
        private void UpdateValidationStatus()
        {
            if (validationStatusText == null) return;

            // DeckValidator를 사용한 전체 검증
            DeckValidationResult result = DeckValidator.ValidateDeck(deckCards);

            if (result.IsValid)
            {
                if (result.HasWarnings)
                {
                    validationStatusText.text = $"⚠ 유효 (경고: {result.Warnings.Count})";
                    validationStatusText.color = Color.yellow;
                }
                else
                {
                    validationStatusText.text = "✓ 유효한 덱";
                    validationStatusText.color = Color.green;
                }
            }
            else
            {
                // 첫 번째 오류 메시지 표시
                string firstError = result.Errors.Count > 0 ? result.Errors[0] : "유효하지 않은 덱";
                validationStatusText.text = $"✗ {firstError}";
                validationStatusText.color = Color.red;
            }
        }

        /// <summary>
        /// 저장 버튼 상태 업데이트
        /// </summary>
        private void UpdateSaveButtonState()
        {
            if (saveDeckButton == null) return;

            // DeckValidator를 사용한 검증
            DeckValidationResult result = DeckValidator.ValidateDeck(deckCards);
            saveDeckButton.interactable = result.IsValid;
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// 덱 저장 버튼 클릭
        /// </summary>
        private void OnSaveDeckClicked()
        {
            // DeckValidator를 사용한 검증
            DeckValidationResult result = DeckValidator.ValidateDeck(deckCards);

            if (!result.IsValid)
            {
                Debug.LogWarning($"[DeckBuilder] 유효하지 않은 덱입니다: {result.GetErrorMessage()}");
                return;
            }

            // 덱 이름 확인
            string deckName = deckNameInput != null ? deckNameInput.text : "새로운 덱";
            if (string.IsNullOrWhiteSpace(deckName))
            {
                Debug.LogWarning("[DeckBuilder] 덱 이름을 입력해주세요");
                return;
            }

            // 덱 저장
            bool success = DeckSaveSystem.SaveDeck(deckName, deckCards);
            if (success)
            {
                Debug.Log($"[DeckBuilder] Deck '{deckName}' saved successfully!");
            }
            else
            {
                Debug.LogError($"[DeckBuilder] Failed to save deck '{deckName}'");
            }
        }

        /// <summary>
        /// 덱 초기화 버튼 클릭
        /// </summary>
        private void OnClearDeckClicked()
        {
            List<CardData> cardsToRemove = new List<CardData>(deckCards.Keys);

            foreach (var card in cardsToRemove)
            {
                while (deckCards.ContainsKey(card))
                {
                    RemoveCardFromDeck(card);
                }
            }

            Debug.Log("[DeckBuilder] Deck cleared");
        }

        /// <summary>
        /// 덱 불러오기 버튼 클릭
        /// </summary>
        private void OnLoadDeckClicked()
        {
            // 저장된 덱 목록 가져오기
            List<string> savedDecks = DeckSaveSystem.GetSavedDeckNames();

            if (savedDecks.Count == 0)
            {
                Debug.LogWarning("[DeckBuilder] 저장된 덱이 없습니다");
                return;
            }

            // TODO: 덱 선택 UI 패널 표시
            // 임시로 첫 번째 덱을 로드
            string deckToLoad = savedDecks[0];
            LoadDeckFromFile(deckToLoad);

            Debug.Log($"[DeckBuilder] Available decks: {string.Join(", ", savedDecks)}");
        }

        /// <summary>
        /// 파일에서 덱 로드
        /// </summary>
        private void LoadDeckFromFile(string deckName)
        {
            Dictionary<CardData, int> loadedDeck = DeckSaveSystem.LoadDeck(deckName);

            if (loadedDeck == null)
            {
                Debug.LogError($"[DeckBuilder] Failed to load deck '{deckName}'");
                return;
            }

            // 현재 덱 초기화
            OnClearDeckClicked();

            // 로드된 덱 카드 추가
            foreach (var kvp in loadedDeck)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    AddCardToDeck(kvp.Key);
                }
            }

            // 덱 이름 업데이트
            if (deckNameInput != null)
            {
                deckNameInput.text = deckName;
            }

            Debug.Log($"[DeckBuilder] Deck '{deckName}' loaded successfully!");
        }

        /// <summary>
        /// Coordinator 참조 설정 (중재자 패턴)
        /// </summary>
        public void SetCoordinator(Game.UI.Coordinators.DeckInventoryCoordinator coord)
        {
            coordinator = coord;
        }

        #endregion
    }
}
