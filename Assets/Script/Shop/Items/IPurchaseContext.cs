using System;
using Game.Managers;

/// <summary>
/// 구매 작업에 필요한 의존성을 제공하는 컨텍스트 인터페이스
/// ServiceLocator 직접 호출 대신 명시적 의존성 주입
/// </summary>
public interface IPurchaseContext
{
    /// <summary>플레이어 데이터 매니저</summary>
    IPlayerDataManager PlayerData { get; }

    /// <summary>카드 컬렉션 매니저</summary>
    ICardCollection CardCollection { get; }
}

/// <summary>
/// IPurchaseContext의 기본 구현체
/// </summary>
public class PurchaseContext : IPurchaseContext
{
    public IPlayerDataManager PlayerData { get; }
    public ICardCollection CardCollection { get; }

    public PurchaseContext(IPlayerDataManager playerData, ICardCollection cardCollection)
    {
        PlayerData = playerData ?? throw new ArgumentNullException(nameof(playerData));
        CardCollection = cardCollection ?? throw new ArgumentNullException(nameof(cardCollection));
    }
}
