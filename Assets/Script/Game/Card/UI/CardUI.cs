using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Core;
using Game.Interfaces;
using Game.Data;
using TMPro;
using Game.Services;

namespace Game.Card.UI
{
    /// <summary>
    /// 카드 UI 컴포넌트 - 드래그 앤 드롭 및 시각적 표현을 담당
    /// Phase 3: UI 및 상호작용 구현의 핵심 컴포넌트
    /// 인벤토리 & 덱 빌딩 모드도 지원
    /// </summary>
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Context Mode")]
        [SerializeField] private CardUIMode mode = CardUIMode.InHand;

        [Header("카드 UI 설정")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Inventory/Deck Mode Settings")]
        [SerializeField] private TextMeshProUGUI ownedCountText;
        [SerializeField] private Button removeButton;

        [Header("Panel GameObjects - 드래그 시 활성화")]
        [SerializeField] private GameObject cardNamePanel;
        [SerializeField] private GameObject descriptionPanel;

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
        [SerializeField] private GameObject glowEffect;
        [SerializeField] private Color validDropColor = Color.green;
        [SerializeField] private Color invalidDropColor = Color.red;

        [Header("Event Channels")]
        [SerializeField] private CardInfoEventChannelSO cardInfoChannel;
        [SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;
        [SerializeField] private Game.UI.Events.CardDragStartEventChannelSO cardDragStartChannel;

        // 드래그 상태 관리
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Transform originalParent;
        private int originalIndex;  // 핸드 내 원래 인덱스
        private Canvas parentCanvas;
        private GraphicRaycaster graphicRaycaster;

        // 카드 데이터
        private CardData cardData;
        private bool isDraggable = false;
        private bool isDragging = false;

        // 덱 모드용 참조
        private Game.UI.Panels.DeckBuilderPanel deckPanel;

        // 서비스 참조
        private ICardSpawnService cardSpawnService;
        private ISpawnValidator spawnValidator;
        private ICardHandManager cardHandManager;
        private IGridRenderer gridRenderer;

        #region Unity Lifecycle

        private void Awake()
        {
            // 컴포넌트 참조 설정
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 부모 캔버스 찾기
            parentCanvas = GetComponentInParent<Canvas>();
            graphicRaycaster = parentCanvas?.GetComponent<GraphicRaycaster>();

            // 초기 상태: 모든 패널 비활성화
            SetPanelsActive(false);

            Debug.Log($"[CardUI] CanvasGroup.blocksRaycasts initial value: {canvasGroup?.blocksRaycasts}");
        }

        private void Start()
        {
            // 서비스 의존성 주입
            InjectDependencies();

            // Mode에 따른 초기 draggable 상태 설정
            UpdateDraggableByMode();
        }

        #endregion

        #region 의존성 주입

        /// <summary>
        /// 서비스 의존성 주입
        /// </summary>
        public void InjectDependencies()
        {
            if (ServiceLocator.IsInitialized)
            {
                var cardServiceManager = ServiceLocator.Get<ICardServiceManager>();
                cardSpawnService = cardServiceManager?.GetCardSpawnService();
                spawnValidator = cardServiceManager?.GetSpawnValidator();
                cardHandManager = cardServiceManager?.GetCardHandManager();

                var gridManager = ServiceLocator.Get<IGridManager>();
                if (gridManager != null)
                {
                    gridRenderer = gridManager.GetGridRenderer();
                }

                if (cardSpawnService == null)
                    Debug.LogError("[CardUI] CardSpawnService not found in ServiceLocator");

                if (spawnValidator == null)
                    Debug.LogError("[CardUI] SpawnValidator not found in ServiceLocator");

                if (cardHandManager == null)
                    Debug.LogError("[CardUI] CardHandManager not found in ServiceLocator");

                if (gridRenderer == null)
                    Debug.LogWarning("[CardUI] GridRenderer not found in GridManager - card preview disabled");
            }
        }

        #endregion

        #region 카드 데이터 설정

        /// <summary>
        /// 카드 데이터 설정 및 UI 업데이트
        /// </summary>
        public void SetCardData(CardData data)
        {
            cardData = data;
            UpdateCardUI();
        }

        /// <summary>
        /// Phase 3.16: 카드 UI 정보 업데이트 - 새로운 카드 구조 반영
        /// Phase 3.18: CardData/UnitData 기반 동적 텍스트 업데이트
        /// </summary>
        private void UpdateCardUI()
        {
            if (cardData == null) return;

            // 카드 기본 정보 표시
            if (cardNameText != null)
                cardNameText.text = cardData.CardName;

            // Phase 3.18: 비용 정보 - CardData.ManaCost 사용
            if (costText != null)
            {
                costText.text = cardData.ManaCost.ToString();
                // 비용에 따른 색상 적용
                costText.color = GetManaCostColor(cardData.ManaCost);
            }

            // 유닛 스탯 텍스트 업데이트 (패널 활성화는 드래그 시에만)
            if (cardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                var summonEffects = cardData.GetEffectsByType(Game.Card.Effects.EffectType.Summon);
                if (summonEffects.Count > 0 && summonEffects[0].UnitToSummon != null)
                {
                    var unitData = summonEffects[0].UnitToSummon;

                    // 텍스트만 업데이트 (패널 활성화는 드래그 시)
                    if (attackText != null)
                        attackText.text = unitData.AttackPower.ToString();
                    if (hpText != null)
                        hpText.text = unitData.MaxHealth.ToString();
                    if (movementText != null)
                        movementText.text = unitData.MovementRange.ToString();
                }
            }

            // Phase 3.18: 설명 정보 - CardData.Description 사용
            if (descriptionText != null)
            {
                descriptionText.text = cardData.Description;
            }

            // 카드 이미지 설정
            if (itemImage != null && cardData.CardArt != null)
                itemImage.sprite = cardData.CardArt;

            // Phase 3.16: 카드 레어리티에 따른 테두리 색상 적용
            ApplyRarityVisualEffects();

            // Phase 3.16: EffectData 기반 추가 시각적 표현
            ApplyEffectTypeVisualCues();
        }

        /// <summary>
        /// Phase 3.18: 카드 타입에 따른 설명 텍스트 생성
        /// 유닛 카드: UnitData 기반 스탯 표시 (공격력, 체력, 이동거리)
        /// 효과 카드: EffectData 기반 효과 설명
        /// </summary>
        private string GenerateCardDescription()
        {
            if (cardData == null) return "정보 없음";

            // 유닛 소환 카드인 경우 - UnitData 정보 표시
            if (cardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                var summonEffects = cardData.GetEffectsByType(Game.Card.Effects.EffectType.Summon);
                if (summonEffects.Count > 0 && summonEffects[0].UnitToSummon != null)
                {
                    var unitData = summonEffects[0].UnitToSummon;

                    if(attackText != null & hpText != null && movementText != null)
                    {
                        attackText.text = unitData.AttackPower.ToString();
                        hpText.text = unitData.MaxHealth.ToString();
                        movementText.text = unitData.MovementRange.ToString();
                    }

                    return $"<b>{unitData.UnitName}</b>\n" +
                           $" 공격력: {unitData.AttackPower}\n" +
                           $" 체력: {unitData.MaxHealth}\n" +
                           $" 이동거리: {unitData.MovementRange}";
                }
            }

            // 효과 기반 카드 (주문 등)
            if (cardData.IsEffectBasedCard)
            {
                return cardData.GetDetailedDescription();
            }

            // 레거시 또는 기타
            return !string.IsNullOrEmpty(cardData.Description)
                ? cardData.Description
                : "효과 정보 없음";
        }

        /// <summary>
        /// Phase 3.16: 마나 비용에 따른 색상 반환
        /// </summary>
        private Color GetManaCostColor(int manaCost)
        {
            return manaCost switch
            {
                <= 1 => Color.blue,
                <= 3 => Color.yellow,
                <= 5 => Color.cyan,
                <= 7 => Color.magenta,
                _ => Color.red
            };
        }

        /// <summary>
        /// Phase 3.16: 카드 레어리티에 따른 시각적 효과 적용
        /// </summary>
        private void ApplyRarityVisualEffects()
        {
            if (cardData == null) return;

            // 카드 테두리나 배경색 적용 (카드 이미지 컴포넌트 사용)
            if (cardImage != null)
            {
                var rarityColor = cardData.GetRarityColor();

                // 카드 이미지의 색조 조정 (미묘하게 적용)
                var imageColor = cardImage.color;
                //imageColor = Color.Lerp(imageColor, rarityColor, 0.2f);
                // 단색 조정
                imageColor = Color.Lerp(Color.white, rarityColor, 0.2f);
                cardImage.color = imageColor;
            }

            // 글로우 효과가 있다면 레어리티 색상으로 조정
            if (glowEffect != null)
            {
                var glowRenderer = glowEffect.GetComponent<Renderer>();
                if (glowRenderer != null)
                {
                    glowRenderer.material.color = cardData.GetRarityColor();
                }
            }
        }

        /// <summary>
        /// Phase 3.16: 효과 타입에 따른 시각적 단서 적용
        /// </summary>
        private void ApplyEffectTypeVisualCues()
        {
            if (cardData == null || !cardData.IsEffectBasedCard) return;

            // 주요 효과 타입 가져오기
            var primaryEffect = cardData.GetPrimaryEffectType();
            if (primaryEffect == null) return;
        }

        #endregion

        #region 드래그 상태 관리

        /// <summary>
        /// 드래그 가능 여부 설정
        /// </summary>
        public void SetDraggable(bool draggable)
        {
            isDraggable = draggable;

            // 시각적 피드백
            if (canvasGroup != null)
            {
                canvasGroup.alpha = draggable ? 1f : 0.7f;
                canvasGroup.interactable = draggable;
            }

            // 글로우 이펙트
            if (glowEffect != null)
                glowEffect.SetActive(draggable);
        }

        /// <summary>
        /// Mode에 따라 draggable 상태를 자동으로 설정
        /// InHand: false (CardHandManager가 플레이어 턴에 활성화)
        /// InInventory: true (기본적으로 드래그 가능)
        /// InDeck: true (덱 빌더에서 드래그 가능)
        /// </summary>
        private void UpdateDraggableByMode()
        {
            switch (mode)
            {
                case CardUIMode.InHand:
                    SetDraggable(false); // CardHandManager가 플레이어 턴에 활성화
                    if (removeButton != null) removeButton.gameObject.SetActive(false);
                    if (ownedCountText != null) ownedCountText.gameObject.SetActive(false);
                    break;
                case CardUIMode.InInventory:
                    SetDraggable(true); // 인벤토리에서 기본적으로 드래그 가능
                    if (removeButton != null) removeButton.gameObject.SetActive(false);
                    if (ownedCountText != null) ownedCountText.gameObject.SetActive(true); // 소유 개수 표시
                    break;
                case CardUIMode.InDeck:
                    SetDraggable(true); // 덱에서는 드래그 비활성화 (우클릭 제거만)
                    if (removeButton != null) removeButton.gameObject.SetActive(true); // 제거 버튼 표시
                    if (ownedCountText != null) ownedCountText.gameObject.SetActive(true); // 덱 내 개수 표시
                    break;
                default:
                    SetDraggable(false);
                    if (removeButton != null) removeButton.gameObject.SetActive(false);
                    if (ownedCountText != null) ownedCountText.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// 드래그 가능 여부 반환
        /// </summary>
        public bool IsDraggable => isDraggable;

        /// <summary>
        /// 현재 드래그 중인지 여부
        /// </summary>
        public bool IsDragging => isDragging;

        /// <summary>
        /// 모든 패널 활성화/비활성화
        /// Phase 3.19: SummonEffect가 있는 카드만 스탯 UI 표시
        /// </summary>
        private void SetPanelsActive(bool active)
        {
            // 기본 패널은 항상 활성화
            if (cardNamePanel != null) cardNamePanel.SetActive(active);
            if (descriptionPanel != null) descriptionPanel.SetActive(active);

            // 스탯 패널은 SummonEffect가 있을 때만 활성화
            bool hasUnitStats = active && cardData != null &&
                                cardData.HasEffectType(Game.Card.Effects.EffectType.Summon);

            if (attackParent != null) attackParent.SetActive(hasUnitStats);
            if (hpParent != null) hpParent.SetActive(hasUnitStats);
            if (movementParent != null) movementParent.SetActive(hasUnitStats);
        }

        #endregion

        #region 클릭 이벤트 (InDeck 모드)

        /// <summary>
        /// 클릭 이벤트 핸들러 (우클릭으로 덱에서 제거)
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // InDeck 모드에서만 우클릭 제거 활성화
            if (mode != CardUIMode.InDeck)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // 우클릭: 1장 제거
                RemoveFromDeck();
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                // 좌클릭: 카드 정보 표시 (선택사항)
                if (cardInfoChannel != null && cardData != null)
                {
                    cardInfoChannel.RaiseEvent(cardData);
                }
            }
        }

        /// <summary>
        /// 제거 버튼 클릭
        /// </summary>
        private void OnRemoveButtonClicked()
        {
            RemoveFromDeck();
        }

        /// <summary>
        /// 덱에서 카드 제거
        /// </summary>
        private void RemoveFromDeck()
        {
            if (deckPanel != null && cardData != null)
            {
                deckPanel.RemoveCardFromDeck(cardData);
                Debug.Log($"[CardUI] Removed {cardData.CardName} from deck");
            }
            else
            {
                Debug.LogWarning("[CardUI] Cannot remove card - deckPanel or cardData is null");
            }
        }

        #endregion

        #region 드래그 앤 드롭 이벤트

        /// <summary>
        /// 드래그 시작
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || cardData == null) return;
            
            // Defensive check: Verify GlobalStateManager is not blocking
            var stateManager = ServiceLocator.Get<IGlobalStateManager>();
            if (stateManager != null && stateManager.IsBusy(BusyType.GameFlowLock))
            {
                Debug.LogWarning("[CardUI] Cannot drag card - GameFlowLock is active (VFX playing)");
                return;
            }

            isDragging = true;

            // 원래 위치, 부모, 인덱스, 스케일 저장
            originalPosition = transform.position;
            originalScale = transform.localScale;
            originalParent = transform.parent;
            originalIndex = transform.GetSiblingIndex();  // 핸드 내 인덱스 저장

            // 드래그 중 시각적 변경
            if (canvasGroup != null)
                canvasGroup.alpha = dragAlpha;

            // 카드 크기 축소
            transform.localScale = originalScale * dragScale;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            // 최상위로 이동 (다른 UI 위에 표시)
            if (parentCanvas != null)
                transform.SetParent(parentCanvas.transform, true);

            // 모든 패널 활성화
            SetPanelsActive(true);

            // Raise card drag start event with context information
            if (cardDragStartChannel != null && cardData != null)
            {
                var dragData = new CardDragData
                {
                    cardData = cardData,
                    mode = mode,
                    sourceTransform = transform,
                    sourceIndex = originalIndex
                };
                cardDragStartChannel.RaiseDragStart(dragData);
            }

            // Also raise card info event for UI display (if available)
            if (cardInfoChannel != null && cardData != null)
            {
                cardInfoChannel.RaiseEvent(cardData);
            }

            Debug.Log($"[CardUI] Started dragging card: {cardData.CardName} (mode: {mode}, original index: {originalIndex})");
        }

        /// <summary>
        /// 드래그 중
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // 마우스 위치로 카드 이동
            transform.position = eventData.position;

            // 드롭 가능한 위치인지 실시간 검사
            CheckDropValidation(eventData);
        }

        /// <summary>
        /// 드래그 종료
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // ✅ 프리뷰 정리 추가 (드롭 처리 전)
            gridRenderer?.ClearCardPreview();

            bool dropSuccess = false;

            // Mode에 따른 드롭 처리
            if (mode == CardUIMode.InHand)
            {
                // InHand 모드: 기존 HandleDrop 로직 사용 (전투 중 필드 타일에 드롭)
                dropSuccess = HandleDrop(eventData);
            }
            else if (mode == CardUIMode.InInventory || mode == CardUIMode.InDeck)
            {
                // InInventory/InDeck 모드: 이벤트를 발생시키고 Coordinator가 처리
                // DeckInventoryCoordinator가 드롭 영역 판단 및 처리를 담당
                // 여기서는 단순히 이벤트만 발생 (dropSuccess는 false로 유지하여 원래 위치로 복귀)
                dropSuccess = false;
            }

            // 레이캐스팅 재활성화 (드롭 처리 완료 후)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // 드롭 실패 시 원래 위치로 복귀
            if (!dropSuccess && returnToOriginalPosition)
            {
                StartCoroutine(ReturnToOriginalPosition());
            }

            // 모든 패널 비활성화
            SetPanelsActive(false);

            // Raise drag end event to hide card information
            if (cardDragEndChannel != null)
            {
                cardDragEndChannel.RaiseEvent();
            }

            Debug.Log($"[CardUI] Ended dragging card: {cardData.CardName} (mode: {mode}), Drop success: {dropSuccess}");
        }

        #endregion

        #region 드롭 처리

        /// <summary>
        /// 실시간 드롭 유효성 검사 및 시각적 피드백
        /// </summary>
        private void CheckDropValidation(PointerEventData eventData)
        {
            // Physics Raycast로 3D 타일 검출
            Camera camera = Camera.main;
            if (camera == null)
            {
                UpdateDropFeedback(false);
                return;
            }

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // Phase 3.17: 레이캐스트 결과를 거리순으로 정렬 (가장 가까운 타일 우선)
            // 대각선 카메라 각도에서 여러 타일이 동시에 히트되는 문제 해결
            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            bool isValidDrop = false;

            foreach (var hit in hits)
            {
                var tileDropHandler = hit.collider.GetComponent<TileDropHandler>();
                if (tileDropHandler != null)
                {
                    // 카드 드롭 유효성 검사
                    Vector2Int gridPosition = tileDropHandler.GetGridPosition();
                    if (spawnValidator != null && cardData != null)
                    {
                        isValidDrop = ValidateCardDrop(cardData, gridPosition);
                    }
                    break;
                }
            }

            // 시각적 피드백 업데이트
            UpdateDropFeedback(isValidDrop);
        }

        /// <summary>
        /// 드롭 처리 실행
        /// </summary>
        private bool HandleDrop(PointerEventData eventData)
        {
            // Physics Raycast로 3D 타일 검출
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[CardUI] Main camera not found for physics raycast");
                return false;
            }

            Ray ray = camera.ScreenPointToRay(eventData.position);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // Phase 3.17: 레이캐스트 결과를 거리순으로 정렬 (가장 가까운 타일 우선)
            // 대각선 카메라 각도에서 여러 타일이 동시에 히트되는 문제 해결
            if (hits.Length > 1)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            }

            foreach (var hit in hits)
            {
                Debug.Log($"[CardUI] Physics Raycast hit: {hit.collider.gameObject.name} at distance {hit.distance:F2}");
                var tileDropHandler = hit.collider.GetComponent<TileDropHandler>();
                if (tileDropHandler != null)
                {
                    // 드롭 처리 위임
                    return tileDropHandler.HandleCardDrop(cardData, this);
                }
            }

            return false; // 유효한 드롭 위치 없음
        }

        /// <summary>
        /// 드롭 피드백 시각적 업데이트
        /// </summary>
        private void UpdateDropFeedback(bool isValid)
        {
            if (cardImage != null)
            {
                cardImage.color = isValid ? validDropColor : invalidDropColor;
            }
        }

        /// <summary>
        /// Phase 3.16: 카드 드롭 유효성 검사 - 새로운 EffectData 시스템 적용
        /// </summary>
        private bool ValidateCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            if (spawnValidator == null) return false;

            // Phase 3.16: 새로운 EffectData 시스템 우선 사용
            if (cardData.IsEffectBasedCard)
            {
                return ValidateEffectBasedCardDrop(cardData, gridPosition);
            }

            // 레거시 시스템 폴백
            return ValidateLegacyCardDrop(cardData, gridPosition);
        }

        /// <summary>
        /// Phase 3.16: EffectData 기반 카드 드롭 유효성 검사
        /// </summary>
        private bool ValidateEffectBasedCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            if (!cardData.IsEffectBasedCard) return false;

            // 각 효과별로 드롭 유효성 검사
            var effects = cardData.EffectDataList;
            bool anyEffectValid = false;

            foreach (var effect in effects)
            {
                bool effectValid = effect.Type switch
                {
                    Game.Card.Effects.EffectType.Summon => ValidateSummonEffect(cardData, effect, gridPosition),
                    Game.Card.Effects.EffectType.Damage => ValidateDamageEffect(cardData, effect, gridPosition),
                    Game.Card.Effects.EffectType.Heal => ValidateHealEffect(cardData, effect, gridPosition),
                    _ => false
                };

                if (effectValid)
                {
                    anyEffectValid = true;
                    break; // 하나라도 유효하면 드롭 가능
                }
            }

            return anyEffectValid;
        }

        /// <summary>
        /// Phase 3.16: 소환 효과 드롭 유효성 검사
        /// </summary>
        private bool ValidateSummonEffect(CardData cardData, Game.Card.Effects.EffectData effect, Vector2Int gridPosition)
        {
            // 유닛 소환 위치 검증
            return spawnValidator.CanSpawnUnit(cardData, gridPosition);
        }

        /// <summary>
        /// Phase 3.16: 데미지 효과 드롭 유효성 검사
        /// </summary>
        private bool ValidateDamageEffect(CardData cardData, Game.Card.Effects.EffectData effect, Vector2Int gridPosition)
        {
            // 데미지 대상 검증 (적군 대상인지, 범위 내에 적이 있는지 등)
            if (effect.AffectedType == Game.Card.Effects.AffectedType.Enemy)
            {
                // 적군이 있는 위치이거나 범위 내에 적이 있어야 함
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }
            else if (effect.AffectedType == Game.Card.Effects.AffectedType.Any)
            {
                // 모든 유닛 대상이므로 유닛이 있는 곳이면 OK
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }

            return true; // 기타 경우는 배치 가능
        }

        /// <summary>
        /// Phase 3.16: 회복 효과 드롭 유효성 검사
        /// </summary>
        private bool ValidateHealEffect(CardData cardData, Game.Card.Effects.EffectData effect, Vector2Int gridPosition)
        {
            // 회복 대상 검증 (아군 대상인지 확인)
            if (effect.AffectedType == Game.Card.Effects.AffectedType.Ally)
            {
                // 아군이 있는 위치이거나 범위 내에 아군이 있어야 함
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }
            else if (effect.AffectedType == Game.Card.Effects.AffectedType.Any)
            {
                // 모든 유닛 대상이므로 유닛이 있는 곳이면 OK
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }

            return true; // 기타 경우는 배치 가능
        }

        /// <summary>
        /// Phase 3.16: 레거시 시스템 카드 드롭 유효성 검사 (폴백)
        /// </summary>
        private bool ValidateLegacyCardDrop(CardData cardData, Vector2Int gridPosition)
        {
            // 효과가 없는 카드는 배치 불가
            if (!cardData.IsEffectBasedCard)
            {
                return false;
            }

            // 효과 타입에 따라 기본적인 검증 수행
            if (cardData.HasEffectType(Game.Card.Effects.EffectType.Summon))
            {
                return spawnValidator.CanSpawnUnit(cardData, gridPosition);
            }
            else if (cardData.HasEffectType(Game.Card.Effects.EffectType.Damage) ||
                     cardData.HasEffectType(Game.Card.Effects.EffectType.Heal))
            {
                return spawnValidator.CanUseSpell(cardData, gridPosition);
            }

            return false;
        }

        #endregion

        #region 위치 복귀

        /// <summary>
        /// 원래 위치로 복귀하는 코루틴
        /// </summary>
        private System.Collections.IEnumerator ReturnToOriginalPosition()
        {
            // 원래 부모로 복귀
            if (originalParent != null)
            {
                transform.SetParent(originalParent, true);

                // 원래 인덱스 위치로 복귀 (카드가 겹치지 않도록)
                transform.SetSiblingIndex(originalIndex);

                Debug.Log($"[CardUI] Returned to original index: {originalIndex}");
            }

            // CardHandManager에게 레이아웃 재정렬 요청
            if (cardHandManager != null)
            {
                cardHandManager.RefreshHandLayout();
            }

            // 부드러운 이동 및 스케일 복원
            while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition, returnSpeed * Time.deltaTime);
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, returnSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = originalPosition;
            transform.localScale = originalScale;

            // 색상 복원
            if (cardImage != null)
                cardImage.color = Color.white;
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 카드 사용 완료 처리 (소환 성공 시 호출)
        /// CardHandManager에 카드 제거를 요청하여 핸드 레이아웃 자동 업데이트
        /// this 참조를 함께 전달하여 정확한 UI 객체 제거
        /// </summary>
        public void OnCardUsed()
        {
            Debug.Log($"[CardUI] Card used: {cardData?.CardName}");

            // CardHandManager에 카드 제거 요청 (this 참조와 함께)
            if (cardHandManager != null && cardData != null)
            {
                // this 참조를 함께 전달하여 정확한 UI 제거
                cardHandManager.RemoveCardFromHand(cardData, this);
                Debug.Log($"[CardUI] Requested card removal with UI reference - layout will update automatically");
            }
            else
            {
                // fallback: 직접 파괴
                Debug.LogWarning($"[CardUI] CardHandManager is null, destroying directly");
                Destroy(gameObject);
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
        /// 인벤토리 모드용 Setup (소유 개수 포함)
        /// </summary>
        public void SetupForInventory(CardData card, int count)
        {
            SetCardData(card); // 기존 Setup 로직 재사용

            // 소유 개수 표시
            if (ownedCountText != null)
            {
                ownedCountText.gameObject.SetActive(true);
                ownedCountText.text = $"x{count}";
            }

            // SetInteractable 메서드를 사용하여 일관성 유지
            SetInteractable(count > 0);
        }

        /// <summary>
        /// 덱 모드용 Setup (덱 내 개수 포함)
        /// </summary>
        public void SetupForDeck(CardData card, int count, Game.UI.Panels.DeckBuilderPanel panel)
        {
            deckPanel = panel;
            SetCardData(card);

            // 덱 내 개수 표시
            if (ownedCountText != null)
            {
                ownedCountText.gameObject.SetActive(true);
                ownedCountText.text = $"x{count}";
            }

            // 제거 버튼 이벤트 연결
            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(OnRemoveButtonClicked);
            }

            Debug.Log($"[CardUI] Setup for deck: {card.CardName} x{count}");
        }

        /// <summary>
        /// 덱 내 개수 업데이트
        /// </summary>
        public void UpdateCount(int newCount)
        {
            if (ownedCountText != null)
            {
                ownedCountText.text = $"x{newCount}";
            }
        }

        /// <summary>
        /// 카드 상호작용 가능 여부 설정 (반투명 처리 포함)
        /// 인벤토리에서 카드 개수가 0이 되면 비활성화 처리
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (canvasGroup != null)
            {
                // 상호작용 불가능 시 반투명 처리 (alpha 0.5)
                canvasGroup.alpha = interactable ? 1f : 0.5f;
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;
            }

            // 카드 이미지 색상 처리 - 레어리티 색상 고려
            if (cardImage != null)
            {
                if (interactable)
                {
                    // 활성화 시 레어리티 색상 복원
                    if (cardData != null)
                    {
                        var rarityColor = cardData.GetRarityColor();
                        cardImage.color = Color.Lerp(Color.white, rarityColor, 0.2f);
                    }
                    else
                    {
                        cardImage.color = Color.white;
                    }
                }
                else
                {
                    // 비활성화 시 회색조 처리
                    cardImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }

            // 드래그 가능 상태도 함께 업데이트
            isDraggable = interactable;

            Debug.Log($"[CardUI] SetInteractable({interactable}) - Alpha: {canvasGroup?.alpha}, Interactable: {canvasGroup?.interactable}, BlocksRaycasts: {canvasGroup?.blocksRaycasts}");
        }

        /// <summary>
        /// 현재 모드 반환
        /// </summary>
        public CardUIMode GetMode()
        {
            return mode;
        }

        /// <summary>
        /// 모드 설정 및 draggable 상태 자동 업데이트
        /// </summary>
        public void SetMode(CardUIMode newMode)
        {
            mode = newMode;
            UpdateDraggableByMode();
        }

        #endregion

        #region 에디터용 디버깅

#if UNITY_EDITOR
        [Header("디버깅 도구")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            var rect = new Rect(Screen.width - 300, 10, 280, 150);
            GUILayout.BeginArea(rect);
            GUILayout.Box("CardUI Debug Info");

            GUILayout.Label($"Card: {cardData?.CardName ?? "None"}");
            GUILayout.Label($"Draggable: {isDraggable}");
            GUILayout.Label($"Dragging: {isDragging}");
            GUILayout.Label($"Services Connected: {(cardSpawnService != null && spawnValidator != null)}");

            if (GUILayout.Button("Toggle Draggable"))
            {
                SetDraggable(!isDraggable);
            }

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}