using System;
using Game.Data;
using Game.Managers;

/// <summary>
/// 가격 계산에 필요한 컨텍스트 정보
/// Strategy 패턴에서 가격 계산 시 필요한 모든 데이터를 담는 컨테이너
/// 확장성: 새로운 Strategy가 추가 데이터를 필요로 할 때 인터페이스 변경 없이 속성만 추가
/// </summary>
public class PricingContext
{
    /// <summary>카드 등급 (필수)</summary>
    public CardData.CardRarity Rarity { get; set; }

    /// <summary>상점 설정 (필수)</summary>
    public ShopConfiguration Config { get; set; }

    /// <summary>카드 컬렉션 (선택적, 동적 가격 책정에 사용)</summary>
    public ICardCollection CardCollection { get; set; }

    /// <summary>플레이어 데이터 (선택적, 플레이어 레벨 기반 가격 책정에 사용)</summary>
    public IPlayerDataManager PlayerData { get; set; }

    /// <summary>카드 데이터 (선택적, 카드별 특수 가격 책정에 사용)</summary>
    public CardData CardData { get; set; }

    /// <summary>
    /// PricingContext 생성자 (필수 데이터만)
    /// </summary>
    /// <param name="rarity">카드 등급</param>
    /// <param name="config">상점 설정</param>
    public PricingContext(CardData.CardRarity rarity, ShopConfiguration config)
    {
        Rarity = rarity;
        Config = config ?? throw new ArgumentNullException(nameof(config));
    }
}
