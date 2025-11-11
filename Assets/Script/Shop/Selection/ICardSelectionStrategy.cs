using System.Collections.Generic;
using Game.Data;

/// <summary>
/// 카드 선택 전략 인터페이스
/// CardDatabase에서 상점에 진열할 카드를 선택하는 로직 캡슐화
/// </summary>
public interface ICardSelectionStrategy
{
    /// <summary>
    /// 사용 가능한 카드 목록에서 상점에 진열할 카드 선택
    /// </summary>
    /// <param name="availableCards">선택 가능한 전체 카드 목록</param>
    /// <param name="count">선택할 카드 수</param>
    /// <returns>선택된 카드 목록</returns>
    List<CardData> SelectCards(List<CardData> availableCards, int count);
}
