using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Data;

/// <summary>
/// 카드 정보 표시 패널
/// CardUI의 드래그 이벤트에 반응하여 카드 정보를 표시합니다.
/// Event Channel 패턴으로 CardUI와 완전히 디커플링되어 있습니다.
///
/// Architecture:
/// - Subscribes to CardInfoEventChannelSO for card drag start events
/// - Subscribes to CardDragEndEventChannelSO for card drag end events
/// - Displays card information in three text areas:
///   1. Essential Info: CardName, TargetType, TargetRange
///   2. Description: Card description text
///   3. Effect Info: All EffectData details (Type, Value, AffectedType, AffectedRange)
///                   + Summoned Unit info if UnitToSummon exists
/// - Automatically hides when dragging ends
/// </summary>
public class CardInfoPanel : UIPanel
{
    [Header("Event Channels")]
    [SerializeField] private CardInfoEventChannelSO cardDragStartChannel;
    [SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;

    [Header("Panel GameObjects")]
    [SerializeField] private GameObject essentialInfoPanel;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private GameObject effectInfoPanel;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI essentialInfoText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI effectInfoText;

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
        if (essentialInfoPanel != null) essentialInfoPanel.SetActive(false);
        if (descriptionPanel != null) descriptionPanel.SetActive(false);
        if (effectInfoPanel != null) effectInfoPanel.SetActive(false);

        Debug.Log("[CardInfoPanel] Self-initialized successfully");
    }

    /// <summary>
    /// 필수 참조 검증
    /// </summary>
    private void ValidateReferences()
    {
        if (cardDragStartChannel == null)
            Debug.LogWarning("[CardInfoPanel] CardDragStartChannel not assigned!");

        if (cardDragEndChannel == null)
            Debug.LogWarning("[CardInfoPanel] CardDragEndChannel not assigned!");

        if (essentialInfoPanel == null)
            Debug.LogWarning("[CardInfoPanel] EssentialInfoPanel not assigned!");

        if (descriptionPanel == null)
            Debug.LogWarning("[CardInfoPanel] DescriptionPanel not assigned!");

        if (effectInfoPanel == null)
            Debug.LogWarning("[CardInfoPanel] EffectInfoPanel not assigned!");

        if (essentialInfoText == null)
            Debug.LogWarning("[CardInfoPanel] EssentialInfoText not assigned!");

        if (descriptionText == null)
            Debug.LogWarning("[CardInfoPanel] DescriptionText not assigned!");

        if (effectInfoText == null)
            Debug.LogWarning("[CardInfoPanel] EffectInfoText not assigned!");
    }

    #endregion

    #region Unity Lifecycle

    protected void OnEnable()
    {
        SubscribeToEvents();
    }

    protected  void OnDisable()
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
        if (cardDragStartChannel != null)
        {
            cardDragStartChannel.Subscribe(ShowCardInfo);
            Debug.Log("[CardInfoPanel] Subscribed to card drag start events");
        }

        if (cardDragEndChannel != null)
        {
            cardDragEndChannel.Subscribe(Hide);
            Debug.Log("[CardInfoPanel] Subscribed to card drag end events");
        }
    }

    /// <summary>
    /// 이벤트 채널 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (cardDragStartChannel != null)
        {
            cardDragStartChannel.Unsubscribe(ShowCardInfo);
            Debug.Log("[CardInfoPanel] Unsubscribed from card drag start events");
        }

        if (cardDragEndChannel != null)
        {
            cardDragEndChannel.Unsubscribe(Hide);
            Debug.Log("[CardInfoPanel] Unsubscribed from card drag end events");
        }
    }

    #endregion

    #region UIPanel Overrides

    /// <summary>
    /// 패널 표시 - 자식 패널들만 활성화 (CardInfoPanel 자체는 항상 활성화 상태 유지)
    /// </summary>
    public override void OnShow()
    {
        // base.OnShow()를 호출하지 않음 (gameObject.SetActive 방지)
        if (currentState == UIPanelState.Active) return;

        currentState = UIPanelState.Showing;

        // 자식 패널들만 활성화
        if (essentialInfoPanel != null) essentialInfoPanel.SetActive(true);
        if (descriptionPanel != null) descriptionPanel.SetActive(true);
        if (effectInfoPanel != null) effectInfoPanel.SetActive(true);

        OnShowPanel();
        currentState = UIPanelState.Active;
        RaiseOnPanelShown();

        Debug.Log("[CardInfoPanel] Child panels shown");
    }

    /// <summary>
    /// 패널 숨김 - 자식 패널들만 비활성화 (CardInfoPanel 자체는 활성화 상태 유지)
    /// </summary>
    public override void OnHide()
    {
        // base.OnHide()를 호출하지 않음 (gameObject.SetActive 방지)
        if (currentState == UIPanelState.Inactive) return;

        currentState = UIPanelState.Hiding;
        OnHidePanel();

        // 자식 패널들만 비활성화
        if (essentialInfoPanel != null) essentialInfoPanel.SetActive(false);
        if (descriptionPanel != null) descriptionPanel.SetActive(false);
        if (effectInfoPanel != null) effectInfoPanel.SetActive(false);

        currentState = UIPanelState.Inactive;
        RaiseOnPanelHidden();

        Debug.Log("[CardInfoPanel] Child panels hidden");
    }

    #endregion

    #region Card Info Display

    /// <summary>
    /// 카드 정보를 표시합니다
    /// CardDragStartChannel 이벤트에 의해 호출됩니다
    /// </summary>
    /// <param name="cardData">표시할 카드 데이터</param>
    private void ShowCardInfo(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("[CardInfoPanel] Received null CardData");
            return;
        }

        // 1. 필수 정보 (카드 이름, TargetType, TargetRange)
        if (essentialInfoText != null)
        {
            string essentialInfo = $" {cardData.CardName}\n" +
                                  $"Target: {cardData.Target} / Range: {FormatTargetRange(cardData.TargetRange)}";
            essentialInfoText.text = essentialInfo;
        }

        // 2. 설명 텍스트
        if (descriptionText != null)
        {
            descriptionText.text = cardData.Description;
        }

        // 3. 효과 정보
        if (effectInfoText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (cardData.EffectDataList.Count == 0)
            {
                sb.AppendLine(" (No effects)");
            }
            else
            {
                int index = 1;
                foreach (var effect in cardData.EffectDataList)
                {
                    sb.AppendLine($" [Effect {index}]");
                    sb.AppendLine($"Type: {effect.Type}");
                    sb.AppendLine($"Value: {effect.Value}");
                    sb.AppendLine($"AffectedType: {effect.AffectedType}");
                    sb.AppendLine($"AffectedRange: {effect.AffectedRange}");

                    index++;

                    // 소환 유닛 정보 (UnitToSummon이 있을 때만)
                    if (effect.UnitToSummon != null)
                    {
                        var unit = effect.UnitToSummon;
                        sb.AppendLine($" [Summoned Unit Info]");
                        sb.AppendLine($"  UnitName: {unit.UnitName}");
                        sb.AppendLine($"  MaxHealth: {unit.MaxHealth}");
                        sb.AppendLine($"  AttackPower: {unit.AttackPower}");
                        sb.AppendLine($"  MovementRange: {unit.MovementRange}");
                    }

                    sb.AppendLine(""); // 효과 구분용 빈 줄
                }
            }

            effectInfoText.text = sb.ToString();
        }

        // Show the panel
        OnShow();

        Debug.Log($"[CardInfoPanel] Displaying info for card: {cardData.CardName}");
    }

    /// <summary>
    /// 카드 정보 패널을 숨깁니다
    /// CardDragEndChannel 이벤트에 의해 호출됩니다
    /// </summary>
    private void Hide()
    {
        OnHide();
        Debug.Log("[CardInfoPanel] Hidden");
    }

    /// <summary>
    /// TargetRange 값을 표시 문자열로 변환
    /// </summary>
    /// <param name="targetRange">-1: 거리 무관, 0+: 해당 거리까지</param>
    /// <returns>포맷된 문자열</returns>
    private string FormatTargetRange(int targetRange)
    {
        return targetRange == -1 ? "무제한" : targetRange.ToString();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 수동으로 특정 카드 정보를 표시합니다 (외부 호출용)
    /// </summary>
    /// <param name="cardData">표시할 카드 데이터</param>
    public void DisplayCardInfo(CardData cardData)
    {
        ShowCardInfo(cardData);
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
