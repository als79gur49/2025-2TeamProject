using UnityEngine.UI;

/// <summary>
/// 열기/닫기 버튼을 가진 패널 인터페이스
/// 설정 패널, 인벤토리 패널 등에서 사용
/// 확장 메서드를 통해 버튼 자동 바인딩 지원
/// </summary>
public interface IOpenablePanel
{
    /// <summary>
    /// 패널을 여는 버튼 (외부에서 패널을 열 때 사용)
    /// </summary>
    Button OpenButton { get; }

    /// <summary>
    /// 패널을 닫는 버튼 (패널 내부에서 닫을 때 사용)
    /// </summary>
    Button CloseButton { get; }
}
