using UnityEngine.UI;

/// <summary>
/// 확인/취소 버튼을 가진 다이얼로그 패널 인터페이스
/// 확장 메서드를 통해 다이얼로그 버튼 자동 바인딩 지원
/// </summary>
public interface IDialogPanel
{
    /// <summary>
    /// 확인 버튼
    /// </summary>
    Button ConfirmButton { get; }

    /// <summary>
    /// 취소 버튼
    /// </summary>
    Button CancelButton { get; }
}
