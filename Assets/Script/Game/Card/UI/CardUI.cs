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
    /// </summary>
    public class CardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("카드 UI 설정")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

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
        [SerializeField] private bool returnToOriginalPosition = true;
        [SerializeField] private float returnSpeed = 10f;

        [Header("시각적 피드백")]
        [SerializeField] private GameObject glowEffect;
        [SerializeField] private Color validDropColor = Color.green;
        [SerializeField] private Color invalidDropColor = Color.red;

        [Header("Event Channels")]
        [SerializeField] private CardInfoEventChannelSO cardDragStartChannel;
        [SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;

        // 드래그 상태 관리
        private Vector3 originalPosition;
        private Transform originalParent;
        private int originalIndex;  // 핸드 내 원래 인덱스
        private Canvas parentCanvas;
        private GraphicRaycaster graphicRaycaster;

        // 카드 데이터
        private CardData cardData;
        private bool isDraggable = false;
        private bool isDragging = false;

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
            
            // 초기 상태 설정
            SetDraggable(false);
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
                <= 1 => Color.white,
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
                imageColor = Color.Lerp(imageColor, rarityColor, 0.2f);
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
        /// 드래그 가능 여부 반환
        /// </summary>
        public bool IsDraggable => isDraggable;

        /// <summary>
        /// 현재 드래그 중인지 여부
        /// </summary>
        public bool IsDragging => isDragging;

        /// <summary>
        /// 모든 패널 활성화/비활성화
        /// </summary>
        private void SetPanelsActive(bool active)
        {
            if (cardNamePanel != null) cardNamePanel.SetActive(active);
            if (descriptionPanel != null) descriptionPanel.SetActive(active);
            if (attackParent != null) attackParent.SetActive(active);
            if (hpParent != null) hpParent.SetActive(active);
            if (movementParent != null) movementParent.SetActive(active);
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

            // 원래 위치, 부모, 인덱스 저장
            originalPosition = transform.position;
            originalParent = transform.parent;
            originalIndex = transform.GetSiblingIndex();  // 핸드 내 인덱스 저장

            // 드래그 중 시각적 변경
            if (canvasGroup != null)
                canvasGroup.alpha = dragAlpha;

            // ============================================================
            // ✅ blocksRaycasts = false 제거 (2025-10-24)
            // ============================================================
            //
            // [비활성화 이유]
            // 기존: "다른 UI와 간섭 방지" 목적으로 blocksRaycasts를 false로 설정
            // 문제: Unity UI 이벤트 시스템이 차단되어 TileDropHandler.OnPointerEnter가 호출되지 않음
            //       → 호버 중 카드 범위 프리뷰가 표시되지 않는 치명적 버그 발생
            //
            // [수정 근거]
            // 1. 드래그 중에는 다른 UI와 상호작용할 필요가 없음
            //    - 카드 드래그 = 배치 작업 진행 중
            //    - 다른 카드 클릭, 버튼 클릭 등은 잘못된 UX
            //    - 차단되는 것이 오히려 정상적인 동작
            //
            // 2. 타일 호버 감지가 핵심 기능
            //    - OnPointerEnter를 통한 실시간 범위 프리뷰 필수
            //    - blocksRaycasts = true 유지 시 정상 작동
            //
            // [예상했던 부작용과 실제]
            // 우려 1: "드래그 중 핸드의 다른 카드 클릭 차단"
            //   → 실제: 문제 없음. 드래그 중 다른 카드를 선택할 이유 없음
            //
            // 우려 2: "드래그 중 UI 버튼(설정, 종료 등) 클릭 차단"
            //   → 실제: 문제 없음. 드래그 중이면 드롭하거나 취소해야 정상
            //
            // 우려 3: "타일 위 UI 요소 클릭 차단"
            //   → 실제: 문제 없음. 드래그 중 타일 UI와 상호작용 불필요
            //
            // [대안 검토]
            // - Physics Raycast 활용: 가능하지만 불필요한 복잡도 증가
            // - 조건부 설정: blocksRaycasts의 근본 문제 해결 안됨
            //
            // [결론]
            // blocksRaycasts를 기본값(true)으로 유지하는 것이 최선
            // 타일의 OnPointerEnter/Exit 이벤트가 정상 작동하여 호버 프리뷰 표시 가능
            // ============================================================

            // if (canvasGroup != null)
            //     canvasGroup.blocksRaycasts = false;

            // 최상위로 이동 (다른 UI 위에 표시)
            if (parentCanvas != null)
                transform.SetParent(parentCanvas.transform, true);

            // 모든 패널 활성화
            SetPanelsActive(true);

            // Raise card info event to display card information
            if (cardDragStartChannel != null && cardData != null)
            {
                cardDragStartChannel.RaiseEvent(cardData);
            }

            Debug.Log($"[CardUI] Started dragging card: {cardData.CardName} (original index: {originalIndex})");
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

            // 드롭 처리 (레이캐스팅이 비활성화된 상태에서 실행)
            bool dropSuccess = HandleDrop(eventData);

            // 레이캐스팅 재활성화 (드롭 처리 완료 후)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                // blocksRaycasts는 더 이상 조작하지 않음 (OnBeginDrag에서도 변경 안함)
                // canvasGroup.blocksRaycasts = true;
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

            Debug.Log($"[CardUI] Ended dragging card: {cardData.CardName}, Drop success: {dropSuccess}");
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

            // 부드러운 이동
            while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition, returnSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = originalPosition;

            // 색상 복원
            if (cardImage != null)
                cardImage.color = Color.white;
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// 카드 사용 완료 처리 (소환 성공 시 호출)
        /// </summary>
        public void OnCardUsed()
        {
            // 카드 UI 제거 또는 비활성화
            gameObject.SetActive(false);
            Debug.Log($"[CardUI] Card used: {cardData?.CardName}");
        }

        /// <summary>
        /// 카드 데이터 반환
        /// </summary>
        public CardData GetCardData()
        {
            return cardData;
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