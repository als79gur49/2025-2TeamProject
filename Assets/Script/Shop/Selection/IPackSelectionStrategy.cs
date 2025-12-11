using System.Collections.Generic;
using Game.Data;

/// <summary>
/// 카드팩 선택 전략 인터페이스
/// 사용 가능한 카드팩 목록에서 상점에 진열할 팩을 선택하는 로직 캡슐화
/// </summary>
public interface IPackSelectionStrategy
{
    /// <summary>
    /// 사용 가능한 카드팩 목록에서 상점에 진열할 팩 선택
    /// </summary>
    /// <param name="availablePacks">선택 가능한 카드팩 목록</param>
    /// <param name="count">선택할 팩 수</param>
    /// <returns>선택된 카드팩 목록</returns>
    List<CardPackDefinition> SelectPacks(List<CardPackDefinition> availablePacks, int count);
}

