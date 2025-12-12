using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Data;

/// <summary>
/// 카드팩 오픈 패널에서 개별 카드 한 장을 표시하는 슬롯
/// </summary>
public class PackOpenCardSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button infoButton;
    [SerializeField] private CardInfoEventChannelSO cardInfoEventChannel;

    private CardData currentCard;

    /// <summary>
    /// 슬롯을 카드 데이터로 초기화
    /// </summary>
    public void Setup(CardData card)
    {
        currentCard = card;

        if (card == null)
        {
            gameObject.SetActive(false);
            if (infoButton != null)
            {
                infoButton.interactable = false;
                infoButton.onClick.RemoveAllListeners();
            }
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = card.IconSprite != null ? card.IconSprite : card.CardArt;
        }

        if (rarityFrame != null)
        {
            rarityFrame.color = card.GetRarityColor();
        }

        if (nameText != null)
        {
            nameText.text = card.CardName;
        }

        if (infoButton != null)
        {
            infoButton.interactable = true;
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(OnInfoButtonClicked);
        }

        gameObject.SetActive(true);
    }

    private void OnInfoButtonClicked()
    {
        if (currentCard == null)
        {
            Debug.LogWarning("[PackOpenCardSlot] Info button clicked but currentCard is null");
            return;
        }

        if (cardInfoEventChannel == null)
        {
            Debug.LogError("[PackOpenCardSlot] CardInfoEventChannelSO is not assigned");
            return;
        }

        cardInfoEventChannel.ShowCardInfo(currentCard);
        Debug.Log($"[PackOpenCardSlot] Info button clicked for card: {currentCard.CardName}");
    }
}
