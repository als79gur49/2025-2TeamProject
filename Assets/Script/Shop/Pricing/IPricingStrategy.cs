/// <summary>
/// 가격 책정 전략 인터페이스
/// Strategy 패턴: 다양한 가격 책정 로직을 런타임에 교체 가능하게 함
/// </summary>
public interface IPricingStrategy
{
    /// <summary>
    /// 주어진 컨텍스트를 기반으로 가격을 계산
    /// </summary>
    /// <param name="context">가격 계산에 필요한 모든 정보</param>
    /// <returns>계산된 가격</returns>
    int CalculatePrice(PricingContext context);
}
