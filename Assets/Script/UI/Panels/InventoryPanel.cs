using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Managers;
using Game.Card.UI;
using Game.Card.UI.Refactored;

namespace Game.UI.Panels
{
    /// <summary>
    /// 인벤토리 패널 - 플레이어가 소유한 카드 표시 및 관리
    /// 필터링, 정렬, 드래그 앤 드롭을 통한 덱 빌더 연동 지원
    /// </summary>
    public class InventoryPanel : UIPanel, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Inventory Settings")]
        [SerializeField] private Transform cardGridContainer;
        [SerializeField] private GameObject cardUIPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Filtering")]
        [SerializeField] private TMP_Dropdown rarityFilter;
        [SerializeField] private TMP_InputField searchField;

        [Header("Sorting")]
        [SerializeField] private TMP_Dropdown sortDropdown;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI totalCardsText;

        [Header("Visual Feedback")]
        [SerializeField] private Image dropZoneHighlight;
        [SerializeField] private Color validDropColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);

        // 카드 컬렉션 데이터
        private List<CardData> allCards = new List<CardData>();
        private List<CardData> filteredCards = new List<CardData>();
        private Dictionary<CardData, CardUIRefactored> cardSlots = new Dictionary<CardData, CardUIRefactored>();

        // 현재 필터/정렬 상태
        private CardData.CardRarity currentRarityFilter = CardData.CardRarity.Common;
        private bool filterByRarity = false;
        private string currentSearchText = "";
        private SortCriteria currentSort = SortCriteria.Name;

        // Coordinator 참조 (중재자 패턴)
        private Game.UI.Coordinators.DeckInventoryCoordinator coordinator;

        // 현재 드래그 중인 카드 (검증용)
        private CardData currentDraggedCard = null;

        #region Lifecycle

        /// <summary>
        /// 의존성 없는 초기화 (Awake에서 호출)
        /// UI 컴포넌트 이벤트 설정 및 내부 상태 초기화
        /// </summary>
        protected override void OnInitializeSelf()
        {
            base.OnInitializeSelf();

            // 필터 드롭다운 설정
            SetupFilters();

            // 정렬 드롭다운 설정
            SetupSorting();

            // 검색 필드 이벤트 구독
            if (searchField != null)
                searchField.onValueChanged.AddListener(OnSearchTextChanged);

            Debug.Log("[InventoryPanel] Self-initialized successfully (Awake)");
        }

        /// <summary>
        /// 의존성 있는 초기화 (Start에서 호출)
        /// CollectionManager 싱글톤 의존성 처리
        /// </summary>
        protected override void OnInitializeWithDependencies()
        {
            base.OnInitializeWithDependencies();

            // 컬렉션 매니저 이벤트 구독
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnCollectionChanged += OnCollectionChanged;
            }

            // 초기 카드 로드
            LoadCardsFromCollection();

            Debug.Log("[InventoryPanel] Dependency initialization complete (Start)");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 이벤트 구독 해제
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnCollectionChanged -= OnCollectionChanged;
            }
        }

        protected override void OnShowPanel()
        {
            base.OnShowPanel();
            RefreshDisplay();
        }

        #endregion

        #region Card Management

        /// <summary>
        /// CollectionManager로부터 카드 로드
        /// </summary>
        private void LoadCardsFromCollection()
        {
            if (CollectionManager.Instance == null)
            {
                Debug.LogError("[InventoryPanel] CollectionManager not found");
                return;
            }

            allCards = CollectionManager.Instance.GetAllOwnedCards();
            ApplyFiltersAndSort();
            PopulateCardGrid();
        }

        /// <summary>
        /// 컬렉션 변경 시 호출
        /// </summary>
        private void OnCollectionChanged()
        {
            LoadCardsFromCollection();
        }

        /// <summary>
        /// 필터 및 정렬 적용
        /// </summary>
        private void ApplyFiltersAndSort()
        {
            filteredCards = allCards
                .Where(card => MatchesFilters(card))
                .OrderBy(card => GetSortKey(card))
                .ToList();
        }

        /// <summary>
        /// 필터 조건 확인
        /// </summary>
        private bool MatchesFilters(CardData card)
        {
            // 희귀도 필터
            if (filterByRarity && card.Rarity != currentRarityFilter)
                return false;

            // 텍스트 검색
            if (!string.IsNullOrEmpty(currentSearchText))
            {
                string searchLower = currentSearchText.ToLower();
                if (!card.CardName.ToLower().Contains(searchLower) &&
                    !card.Description.ToLower().Contains(searchLower))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 정렬 키 반환
        /// </summary>
        private object GetSortKey(CardData card)
        {
            return currentSort switch
            {
                SortCriteria.Name => card.CardName,
                SortCriteria.ManaCost => card.ManaCost,
                SortCriteria.Rarity => (int)card.Rarity,
                _ => card.CardName
            };
        }

        /// <summary>
        /// 카드 그리드 생성
        /// </summary>
        private void PopulateCardGrid()
        {
            // 기존 슬롯 클리어
            foreach (var slot in cardSlots.Values)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            cardSlots.Clear();

            // 필터링된 카드로 슬롯 생성
            foreach (var card in filteredCards)
            {
                CreateCardSlot(card);
            }

            UpdateTotalCardsText();
        }

        /// <summary>
        /// 개별 카드 슬롯 생성
        /// </summary>
        private void CreateCardSlot(CardData card)
        {
            if (cardUIPrefab == null || cardGridContainer == null)
            {
                Debug.LogError("[InventoryPanel] CardUI prefab or container not assigned");
                return;
            }

            GameObject slotObj = Instantiate(cardUIPrefab, cardGridContainer);
            CardUIRefactored cardUI = slotObj.GetComponent<CardUIRefactored>();

            if (cardUI != null)
            {
                int ownedCount = CollectionManager.Instance.GetOwnedCount(card);

                // 덱에 들어있는 개수를 빼서 가용성 계산 (Coordinator를 통해)
                int inDeckCount = coordinator != null ? coordinator.GetDeckCardCount(card) : 0;
                int availableCount = ownedCount - inDeckCount;

                // SetupForInventory를 사용하여 일관성 있는 초기화 (Coordinator 참조 전달)
                cardUI.SetupForInventory(card, availableCount, coordinator);

                cardSlots[card] = cardUI;

                Debug.Log($"[InventoryPanel] Created CardUI for {card.CardName}: owned={ownedCount}, inDeck={inDeckCount}, available={availableCount}");
            }
        }

        /// <summary>
        /// 특정 카드의 가용성 업데이트 (덱에 추가 시 호출)
        /// </summary>
        public void UpdateCardAvailability(CardData card, int availableCount)
        {
            if (cardSlots.TryGetValue(card, out var cardUI))
            {
                // UpdateCount가 내부적으로 SetOwnedCount → UpdateInteractability 호출
                // 별도의 SetInteractable 호출 불필요
                cardUI.UpdateCount(availableCount);

                Debug.Log($"[InventoryPanel] Updated availability for {card.CardName}: {availableCount} available, CardUI found: true");
            }
            else
            {
                Debug.LogWarning($"[InventoryPanel] UpdateCardAvailability FAILED - CardUI not found for {card.CardName}. Total slots in dictionary: {cardSlots.Count}");
                Debug.LogWarning($"[InventoryPanel] Card instance hashcode: {card.GetHashCode()}, Card name: {card.CardName}");

                // Dictionary의 모든 키를 출력하여 디버깅
                foreach (var key in cardSlots.Keys)
                {
                    if (key.CardName == card.CardName)
                    {
                        Debug.LogWarning($"[InventoryPanel] Found card with same name but different instance! HashCode: {key.GetHashCode()}");
                    }
                }
            }
        }

        #endregion

        #region Filtering & Sorting

        /// <summary>
        /// 필터 드롭다운 설정
        /// </summary>
        private void SetupFilters()
        {
            
            if (rarityFilter != null)
            {
                rarityFilter.ClearOptions();
                rarityFilter.AddOptions(new List<string>
                {
                    "전체", "일반", "희귀", "전설", "영웅", "신화"
                });
                rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);
            }
        }

        /// <summary>
        /// 정렬 드롭다운 설정
        /// </summary>
        private void SetupSorting()
        {
            if (sortDropdown != null)
            {
                sortDropdown.ClearOptions();
                sortDropdown.AddOptions(new List<string>
                {
                    "이름", "마나 비용", "희귀도"
                });
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
            }
        }

        /// <summary>
        /// 희귀도 필터 변경 시
        /// </summary>
        private void OnRarityFilterChanged(int index)
        {
            if (index == 0)
            {
                filterByRarity = false;
            }
            else
            {
                filterByRarity = true;
                currentRarityFilter = (CardData.CardRarity)(index - 1);
            }
            RefreshDisplay();
        }

        /// <summary>
        /// 검색 텍스트 변경 시
        /// </summary>
        private void OnSearchTextChanged(string searchText)
        {
            currentSearchText = searchText;
            RefreshDisplay();
        }

        /// <summary>
        /// 정렬 방식 변경 시
        /// </summary>
        private void OnSortChanged(int index)
        {
            currentSort = (SortCriteria)index;
            RefreshDisplay();
        }

        /// <summary>
        /// 화면 새로고침
        /// </summary>
        private void RefreshDisplay()
        {
            ApplyFiltersAndSort();
            PopulateCardGrid();
        }

        #endregion

        #region Drag & Drop

        /// <summary>
        /// 드래그 중인 카드 설정 (외부에서 호출)
        /// </summary>
        public void SetDraggedCard(CardData card)
        {
            currentDraggedCard = card;
            Debug.Log($"[InventoryPanel] SetDraggedCard: {card?.CardName}");
        }

        /// <summary>
        /// 드롭 존에 마우스 진입
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 실제로 드래그 중인지 확인 (eventData.pointerDrag != null)
            if (currentDraggedCard != null && dropZoneHighlight != null && eventData.pointerDrag != null)
            {
                // 덱에서 인벤토리로 드래그하는 경우 항상 드롭 가능
                dropZoneHighlight.color = validDropColor;
                Debug.Log($"[InventoryPanel] OnPointerEnter - Highlighting drop zone");
            }
        }

        /// <summary>
        /// 드롭 존에서 마우스 나감
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (dropZoneHighlight != null)
            {
                dropZoneHighlight.color = normalColor;
                Debug.Log($"[InventoryPanel] OnPointerExit - Clearing drop zone highlight");
            }
        }

        /// <summary>
        /// 카드 드롭 처리 (덱에서 인벤토리로)
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (dropZoneHighlight != null)
                dropZoneHighlight.color = normalColor;

            // 드래그 중인 카드가 없으면 무시
            if (currentDraggedCard == null)
            {
                Debug.Log("[InventoryPanel] OnDrop - No dragged card");
                return;
            }

            Debug.Log($"[InventoryPanel] Card dropped: {currentDraggedCard.CardName}");

            // 덱에서 카드 제거 (Coordinator를 통해)
            if (coordinator != null)
            {
                coordinator.RequestRemoveCardFromDeck(currentDraggedCard);
                Debug.Log($"[InventoryPanel] Requested remove {currentDraggedCard.CardName} from deck via coordinator");
            }
            else
            {
                Debug.LogWarning("[InventoryPanel] Coordinator reference is null, cannot remove card from deck");
            }

            currentDraggedCard = null;
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// 총 카드 수 텍스트 업데이트
        /// </summary>
        private void UpdateTotalCardsText()
        {
            if (totalCardsText == null || CollectionManager.Instance == null)
                return;

            int totalOwned = CollectionManager.Instance.GetTotalCardCount();
            int uniqueCards = CollectionManager.Instance.GetUniqueCardCount();

            totalCardsText.text = $" {uniqueCards}/{totalOwned}";
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

    /// <summary>
    /// 카드 정렬 기준
    /// </summary>
    public enum SortCriteria
    {
        Name = 0,
        ManaCost = 1,
        Rarity = 2
    }
}
