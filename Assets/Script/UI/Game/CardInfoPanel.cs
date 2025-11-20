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
/// - Automatically hides when dragging ends
/// - Implements IDualClosePanel for manual close button support
/// </summary>
public class CardInfoPanel : UIPanel, IDualClosePanel
{
    [Header("Event Channels")]
    [SerializeField] private CardInfoEventChannelSO cardDragStartChannel;
    [SerializeField] private CardDragEndEventChannelSO cardDragEndChannel;

    // 반드시 자기 자신이 아닌 하위 패널 넣기. 스스로 비활성화된 상태에서 이벤트 전달 받지를 못 함.
    [Header("Panel GameObjects")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject dim; // 필요한 경우 사용
    [Header("Close Buttons")]
    [SerializeField] private Button primaryCloseButton;
    [SerializeField] private Button secondaryCloseButton;

    // IDualClosePanel 구현
    public Button PrimaryCloseButton => primaryCloseButton;
    public Button SecondaryCloseButton => secondaryCloseButton;

    [Header("UI Elements")]

    /// <summary>
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI effectText;

    #region Initialization
    /// 의존성 없는 초기화 (Awake에서 호출됨)
    /// UI 컴포넌트 검증 및 초기 상태 설정
    /// </summary>
    protected override void OnInitializeSelf()
    {
        base.OnInitializeSelf();
        ValidateReferences();

        // 초기 상태: 모든 자식 패널 비활성화
        if (infoPanel != null) infoPanel.SetActive(false);
        if(dim != null) dim.SetActive(false);
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

        if (infoPanel == null)
            Debug.LogWarning("[CardInfoPanel] InfoPanel not assigned!");
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

        if(infoPanel != null) infoPanel.SetActive(true);
        if (dim != null) dim.SetActive(true);

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

        if (infoPanel != null) infoPanel.SetActive(false);
        if (dim != null) dim.SetActive(false);

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
        if (nameText != null)
        {
            string essentialInfo = $" {cardData.CardName}\n" +
                                  $"Target: {cardData.Target} / Range: {FormatTargetRange(cardData.TargetRange)}";
            nameText.text = essentialInfo;
        }

        // 2. 설명 텍스트
        if (descriptionText != null)
        {
            descriptionText.text = cardData.Description;
        }

        // 3. 효과 정보 (EffectDefinition 기반)
        if (effectText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (cardData.EffectDefinitions == null || cardData.EffectDefinitions.Count == 0)
            {
                sb.AppendLine(" (No effects)");
            }
            else
            {
                int index = 1;
                foreach (var def in cardData.EffectDefinitions)
                {
                    if (def == null) continue;

                    sb.AppendLine($" [Effect {index}]");
                    sb.AppendLine($"Type: {def.EffectType}");
                    sb.AppendLine($"Scope: {def.TargetScope}");

                    switch (def)
                    {
                        case Game.Card.Effects.DamageEffectDefinition dmg:
                            sb.AppendLine($"Value: {dmg.DamageAmount}");
                            break;
                        case Game.Card.Effects.HealEffectDefinition heal:
                            sb.AppendLine($"Value: {heal.HealAmount}");
                            break;
                        case Game.Card.Effects.SummonEffectDefinition summon when summon.UnitToSummon != null:
                            sb.AppendLine($"Summon: {summon.UnitToSummon.UnitName} x{summon.Count}");
                            break;
                    }

                    index++;
                    sb.AppendLine("");
                }
            }

            effectText.text = sb.ToString();
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
