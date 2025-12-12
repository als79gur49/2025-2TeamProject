using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using Game.Data;

/// <summary>
/// 상점 아이템 UI 컴포넌트
/// ViewModel 기반으로 UI를 업데이트하고 사용자 입력을 처리
/// </summary>
public class ShopItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private Image glowEffect;

    [Header("Optional Visual Elements")]
    [SerializeField] private GameObject discountBadge; // 할인 표시 (선택적)

    [Header("Info Panel")]
    [SerializeField] private Button infoButton; // Info 버튼
    [SerializeField] private CardInfoEventChannelSO cardInfoEventChannel; // CardInfoPanel 표시 이벤트 채널
    [SerializeField] private CardPackInfoEventChannelSO cardPackInfoEventChannel; // CardPackInfoPanel 표시 이벤트 채널

    // ViewModel
    private ShopItemViewModel viewModel;

    // 클릭 콜백
    private Action<string> onLeftClick;
    private Action<string> onRightClick;

    // 애니메이션용 원래 위치 저장
    private Vector2 originalPosition;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// ShopItemUI 초기화
    /// </summary>
    /// <param name="vm">ViewModel (UI 데이터)</param>
    /// <param name="onLeftClickCallback">좌클릭 콜백 (장바구니 추가)</param>
    /// <param name="onRightClickCallback">우클릭 콜백 (장바구니 제거)</param>
    public void Initialize(ShopItemViewModel vm, Action<string> onLeftClickCallback, Action<string> onRightClickCallback)
    {
        if (vm == null)
        {
            Debug.LogError("[ShopItemUI] ViewModel is null");
            return;
        }

        this.viewModel = vm;
        this.onLeftClick = onLeftClickCallback;
        this.onRightClick = onRightClickCallback;

        // Info 버튼 클릭 리스너 등록
        if (infoButton != null)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(OnInfoButtonClicked);
        }

        // UI 업데이트
        UpdateUI();
    }

    /// <summary>
    /// 포인터 클릭 이벤트 처리 (IPointerClickHandler)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (viewModel == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 좌클릭: 장바구니에 추가
            onLeftClick?.Invoke(viewModel.ItemID);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 우클릭: 장바구니에서 제거
            onRightClick?.Invoke(viewModel.ItemID);
        }
    }

    /// <summary>
    /// ViewModel 기반으로 UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (viewModel == null) return;

        // 아이콘
        if (iconImage != null && viewModel.Icon != null)
        {
            iconImage.sprite = viewModel.Icon;

            // 재고 상태에 따른 색상 변경
            if (viewModel.RemainingStock <= 0)
            {
                // 품절: 회색빛 + 반투명
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
            else
            {
                // 재고 있음: 원래 색상
                iconImage.color = Color.white;
            }
        }

        // 가격 (할인 표시 포함)
        if (priceText != null)
        {
            priceText.text = viewModel.PriceText;
        }

        // 재고
        if (stockText != null)
        {
            stockText.text = viewModel.StockText;
        }

        // 할인 배지 표시 (선택적)
        if (discountBadge != null)
        {
            discountBadge.SetActive(viewModel.HasDiscount);
        }
    }

    /// <summary>
    /// ViewModel 업데이트 (외부에서 데이터 변경 시)
    /// </summary>
    /// <param name="newViewModel">새로운 ViewModel</param>
    public void UpdateViewModel(ShopItemViewModel newViewModel)
    {
        this.viewModel = newViewModel;
        UpdateUI();
    }

    /// <summary>
    /// Info 버튼 클릭 핸들러
    /// CardData가 있는 경우 CardInfoPanel을 표시
    /// </summary>
    private void OnInfoButtonClicked()
    {
        if (viewModel == null)
        {
            Debug.LogWarning("[ShopItemUI] ViewModel is null");
            return;
        }

        // 카드팩 아이템인 경우: 카드팩 정보 패널로 라우팅
        if (viewModel.IsCardPack)
        {
            if (viewModel.PackDefinition == null)
            {
                Debug.LogWarning("[ShopItemUI] PackDefinition is null for card pack item");
                return;
            }

            if (cardPackInfoEventChannel == null)
            {
                Debug.LogError("[ShopItemUI] CardPackInfoEventChannel is not assigned");
                return;
            }

            cardPackInfoEventChannel.ShowPackInfo(viewModel.PackDefinition);
            Debug.Log($"[ShopItemUI] Info button clicked for card pack: {viewModel.DisplayName}");
            return;
        }

        // 단일 카드 아이템인 경우: 기존 카드 정보 패널로 라우팅
        if (viewModel.CardData == null)
        {
            Debug.LogWarning("[ShopItemUI] CardData is null - this item may not be a card type");
            return;
        }

        if (cardInfoEventChannel == null)
        {
            Debug.LogError("[ShopItemUI] CardInfoEventChannel is not assigned");
            return;
        }

        // CardInfoPanel 표시 이벤트 발생
        cardInfoEventChannel.ShowCardInfo(viewModel.CardData);
        Debug.Log($"[ShopItemUI] Info button clicked for card: {viewModel.DisplayName}");
    }

    #region Animations

    /// <summary>
    /// 장바구니에 추가될 때 애니메이션 (클릭 피드백)
    /// </summary>
    /// <param name="cartTargetPosition">장바구니 목표 위치 (사용하지 않음)</param>
    public void PlayAddToCartAnimation(RectTransform cartTargetPosition)
    {
        // 클릭 피드백 애니메이션 시퀀스
        Sequence clickSequence = DOTween.Sequence();

        // 1. Press 효과: 눌리는 느낌 (0.1초)
        clickSequence.Append(transform.DOScale(0.9f, 0.1f).SetEase(Ease.OutQuad));

        // 2. Bounce 효과: 튀어나오는 느낌 (0.15초)
        clickSequence.Append(transform.DOScale(1.05f, 0.15f).SetEase(Ease.OutBack));

        // 3. Return 효과: 원래 크기로 복귀 (0.1초)
        clickSequence.Append(transform.DOScale(1.0f, 0.1f).SetEase(Ease.InOutQuad));

        // Glow 효과 (동시 진행)
        if (glowEffect != null)
        {
            Sequence glowSequence = DOTween.Sequence();
            glowSequence.Append(glowEffect.DOFade(1f, 0.15f));
            glowSequence.Append(glowEffect.DOFade(0f, 0.2f));
        }
    }

    /// <summary>
    /// 장바구니에서 제거될 때 애니메이션 (클릭 피드백)
    /// </summary>
    public void PlayRemoveFromCartAnimation()
    {
        // 제거 피드백 애니메이션 시퀀스
        Sequence removeSequence = DOTween.Sequence();

        // 1. Quick Press: 빠르게 축소 (0.08초)
        removeSequence.Append(transform.DOScale(0.85f, 0.08f).SetEase(Ease.OutQuad));

        // 2. Bounce Back: 원래 크기로 튀어나옴 (0.12초)
        removeSequence.Append(transform.DOScale(1.0f, 0.12f).SetEase(Ease.OutBack));

        // Glow 효과 (제거 피드백)
        if (glowEffect != null)
        {
            Sequence glowSequence = DOTween.Sequence();
            glowSequence.Append(glowEffect.DOFade(0.7f, 0.1f));
            glowSequence.Append(glowEffect.DOFade(0f, 0.15f));
        }
    }

    /// <summary>
    /// 클릭 실패 시 애니메이션 (좌우 흔들림)
    /// </summary>
    public void PlayFailAnimation()
    {
        // 좌우 흔들림 효과
        transform.DOShakePosition(
            duration: 0.3f,
            strength: 10f,
            vibrato: 20,
            randomness: 90,
            snapping: false,
            fadeOut: true
        );

        // 붉은 깜빡임 효과
        if (glowEffect != null)
        {
            Color originalColor = glowEffect.color;
            glowEffect.color = Color.red;

            Sequence failGlow = DOTween.Sequence();
            failGlow.Append(glowEffect.DOFade(0.8f, 0.1f));
            failGlow.Append(glowEffect.DOFade(0f, 0.2f));
            failGlow.OnComplete(() => glowEffect.color = originalColor);
        }
    }

    #endregion
}
