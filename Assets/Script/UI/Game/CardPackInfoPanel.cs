using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Data;

/// <summary>
/// 카드팩 정보 표시 패널
/// ShopItemUI의 카드팩 Info 버튼 또는 기타 이벤트에 반응하여
/// 카드팩 이름과 구성 요약을 Description 형태로 표시합니다.
/// </summary>
public class CardPackInfoPanel : UIPanel, IDualClosePanel
{
    [Header("Event Channels")]
    [SerializeField] private CardPackInfoEventChannelSO packInfoEventChannel;

    // 반드시 자기 자신이 아닌 하위 패널 넣기. 스스로 비활성화된 상태에서 이벤트 전달 받지를 못 함.
    [Header("Panel GameObjects")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject dim;

    [Header("Close Buttons")]
    [SerializeField] private Button primaryCloseButton;
    [SerializeField] private Button secondaryCloseButton;

    // IDualClosePanel 구현
    public Button PrimaryCloseButton => primaryCloseButton;
    public Button SecondaryCloseButton => secondaryCloseButton;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    #region Initialization

    /// <summary>
    /// 의존성 없는 초기화 (Awake에서 호출됨)
    /// UI 컴포넌트 검증 및 초기 상태 설정
    /// </summary>
    protected override void OnInitializeSelf()
    {
        base.OnInitializeSelf();
        ValidateReferences();

        // 초기 상태: 모든 자식 패널 비활성화
        if (infoPanel != null) infoPanel.SetActive(false);
        if (dim != null) dim.SetActive(false);
        Debug.Log("[CardPackInfoPanel] Self-initialized successfully");
    }

    /// <summary>
    /// 필수 참조 검증
    /// </summary>
    private void ValidateReferences()
    {
        if (packInfoEventChannel == null)
            Debug.LogWarning("[CardPackInfoPanel] PackInfoEventChannel is not assigned!");

        if (infoPanel == null)
            Debug.LogWarning("[CardPackInfoPanel] InfoPanel is not assigned!");
    }

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Event Subscription

    /// <summary>
    /// 이벤트 채널 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (packInfoEventChannel != null)
        {
            packInfoEventChannel.Subscribe(ShowPackInfo);
            Debug.Log("[CardPackInfoPanel] Subscribed to card pack info events");
        }
    }

    /// <summary>
    /// 이벤트 채널 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (packInfoEventChannel != null)
        {
            packInfoEventChannel.Unsubscribe(ShowPackInfo);
            Debug.Log("[CardPackInfoPanel] Unsubscribed from card pack info events");
        }
    }

    #endregion

    #region UIPanel Overrides

    /// <summary>
    /// 패널 표시 - 자식 패널들만 활성화 (CardPackInfoPanel 자체는 항상 활성화 상태 유지)
    /// </summary>
    public override void OnShow()
    {
        // base.OnShow()를 호출하지 않음 (gameObject.SetActive 방지)
        if (currentState == UIPanelState.Active) return;

        currentState = UIPanelState.Showing;

        if (infoPanel != null) infoPanel.SetActive(true);
        if (dim != null) dim.SetActive(true);

        OnShowPanel();
        currentState = UIPanelState.Active;
        RaiseOnPanelShown();

        Debug.Log("[CardPackInfoPanel] Child panels shown");
    }

    /// <summary>
    /// 패널 숨김 - 자식 패널들만 비활성화 (CardPackInfoPanel 자체는 활성화 상태 유지)
    /// </summary>
    public override void OnHide()
    {
        // base.OnHide()를 호출하지 않음 (gameObject.SetActive 방지)
        if (currentState == UIPanelState.Inactive) return;

        currentState = UIPanelState.Hiding;
        OnHidePanel();

        if (infoPanel != null) infoPanel.SetActive(false);
        if (dim != null) dim.SetActive(false);

        currentState = UIPanelState.Inactive;
        RaiseOnPanelHidden();

        Debug.Log("[CardPackInfoPanel] Child panels hidden");
    }

    #endregion

    #region Card Pack Info Display

    /// <summary>
    /// 카드팩 정보를 표시합니다.
    /// CardPackInfoEventChannelSO 이벤트에 의해 호출됩니다.
    /// </summary>
    /// <param name="packDefinition">표시할 카드팩 정의</param>
    private void ShowPackInfo(CardPackDefinition packDefinition)
    {
        if (packDefinition == null)
        {
            Debug.LogWarning("[CardPackInfoPanel] Received null CardPackDefinition");
            return;
        }

        // 카드팩 이름
        if (nameText != null)
        {
            nameText.text = packDefinition.DisplayName;
        }

        // 설명 텍스트 (기본 Description + 자동 생성 요약)
        if (descriptionText != null)
        {
            string baseDescription = string.IsNullOrWhiteSpace(packDefinition.Description)
                ? string.Empty
                : packDefinition.Description.Trim();

            // 자동 요약기능 미사용.
            string summary ="";
            
            //BuildPackSummary(packDefinition);

            if (!string.IsNullOrEmpty(baseDescription) && !string.IsNullOrEmpty(summary))
            {
                descriptionText.text = baseDescription + "\n\n" + summary;
            }
            else if (!string.IsNullOrEmpty(baseDescription))
            {
                descriptionText.text = baseDescription;
            }
            else
            {
                descriptionText.text = summary;
            }
        }

        // 패널 표시
        OnShow();

        Debug.Log($"[CardPackInfoPanel] Displaying info for card pack: {packDefinition.PackId}");
    }

    /// <summary>
    /// 카드팩 구성 요약 텍스트 생성
    /// - DrawCount와 RarityGuarantees를 기반으로 자동 설명을 생성합니다.
    /// </summary>
    private string BuildPackSummary(CardPackDefinition packDefinition)
    {
        var sb = new StringBuilder();

        int drawCount = packDefinition.DrawCount;
        if (drawCount > 0)
        {
            sb.AppendLine($"Contains {drawCount} cards.");
        }

        var guarantees = packDefinition.RarityGuarantees;
        if (guarantees != null && guarantees.Count > 0)
        {
            bool hasValidGuarantee = false;

            foreach (var guarantee in guarantees)
            {
                if (guarantee.minCount <= 0)
                    continue;

                if (!hasValidGuarantee)
                {
                    sb.AppendLine("Guaranteed contents:");
                    hasValidGuarantee = true;
                }

                sb.AppendLine($"- At least {guarantee.minCount} card(s) of rarity {guarantee.rarity}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 카드팩 정보 패널을 숨깁니다 (내부/이벤트용)
    /// </summary>
    private void Hide()
    {
        OnHide();
        Debug.Log("[CardPackInfoPanel] Hidden");
    }

    /// <summary>
    /// 수동으로 패널을 숨깁니다 (외부 호출용)
    /// </summary>
    public void HidePanel()
    {
        Hide();
    }

    #endregion
}

