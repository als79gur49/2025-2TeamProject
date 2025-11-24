using UnityEngine;
using Game.Data;

/// <summary>
/// 유닛이 어떤 카드에서 소환되었는지 추적하기 위한 메타데이터 컴포넌트
/// </summary>
public class UnitCardLink : MonoBehaviour
{
    [SerializeField] private CardData sourceCard;
    [SerializeField] private string sourceCardId;

    /// <summary>이 유닛을 생성한 카드 데이터</summary>
    public CardData SourceCard => sourceCard;

    /// <summary>SourceCard의 고유 ID (CardData.CardID)</summary>
    public string SourceCardId => sourceCardId;

    /// <summary>
    /// 소환 원본 카드를 설정합니다.
    /// </summary>
    public void SetSourceCard(CardData card)
    {
        sourceCard = card;
        sourceCardId = card != null ? card.CardID : null;
    }
}

