/// <summary>
/// 할인 적용 전략 인터페이스
/// 할인 여부 결정 및 할인율 계산 로직을 캡슐화
/// </summary>
public interface IDiscountStrategy
{
    /// <summary>
    /// 할인을 적용할지 결정
    /// </summary>
    /// <param name="config">상점 설정 (할인 확률 등)</param>
    /// <returns>true면 할인 적용, false면 미적용</returns>
    bool ShouldApplyDiscount(ShopConfiguration config);

    /// <summary>
    /// 할인율 계산 (0.0 ~ 1.0)
    /// </summary>
    /// <param name="config">상점 설정 (최소/최대 할인율 등)</param>
    /// <returns>할인율 (예: 0.2 = 20% 할인)</returns>
    float GetDiscountAmount(ShopConfiguration config);
}
