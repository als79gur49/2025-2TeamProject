using UnityEngine.UI;

/// <summary>
/// 기본 버튼(닫기, 뒤로가기)을 가진 패널 인터페이스
/// 확장 메서드를 통해 버튼 자동 바인딩 지원
/// </summary>
public interface IButtonedPanel
{
    /// <summary>
    /// 패널을 닫는 버튼
    /// </summary>
    Button CloseButton { get; }

    /// <summary>
    /// 이전 화면으로 돌아가는 버튼
    /// </summary>
    Button BackButton { get; }
}
