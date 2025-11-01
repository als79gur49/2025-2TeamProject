using UnityEngine.UI;

/// <summary>
/// 2개의 닫기 버튼을 가진 패널 인터페이스
/// CardInfoPanel 등에서 사용되며, 자동 버튼 바인딩을 통해 두 버튼 모두 패널 닫기 동작 수행
/// 확장 메서드를 통한 버튼 자동 바인딩 지원
/// </summary>
public interface IDualClosePanel
{
    /// <summary>
    /// 첫 번째 닫기 버튼 (주요 닫기 버튼)
    /// </summary>
    Button PrimaryCloseButton { get; }

    /// <summary>
    /// 두 번째 닫기 버튼 (보조 닫기 버튼)
    /// </summary>
    Button SecondaryCloseButton { get; }
}
