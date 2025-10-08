using UnityEngine.UI;

/// <summary>
/// 탐색 버튼(이전, 다음)을 가진 패널 인터페이스
/// 확장 메서드를 통해 탐색 버튼 자동 바인딩 지원
/// </summary>
public interface INavigablePanel
{
    /// <summary>
    /// 이전 항목으로 이동하는 버튼
    /// </summary>
    Button PreviousButton { get; }

    /// <summary>
    /// 다음 항목으로 이동하는 버튼
    /// </summary>
    Button NextButton { get; }
}
