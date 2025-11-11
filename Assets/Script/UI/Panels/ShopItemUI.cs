using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 아이템 UI 컴포넌트
/// ViewModel 기반으로 UI를 업데이트하고 사용자 입력을 처리
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private Image glowEffect;

    [Header("Optional Visual Elements")]
    [SerializeField] private GameObject discountBadge; // 할인 표시 (선택적)

    // ViewModel
    private ShopItemViewModel viewModel;

    /// <summary>
    /// ShopItemUI 초기화
    /// </summary>
    /// <param name="vm">ViewModel (UI 데이터)</param>
    public void Initialize(ShopItemViewModel vm)
    {
        if (vm == null)
        {
            Debug.LogError("[ShopItemUI] ViewModel is null");
            return;
        }

        this.viewModel = vm;

        // UI 업데이트
        UpdateUI();
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
}
