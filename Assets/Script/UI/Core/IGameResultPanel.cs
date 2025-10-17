using UnityEngine.UI;

/// <summary>
/// 게임 결과 패널의 씬 네비게이션 인터페이스
/// UIPanelManager가 자동으로 버튼 기능을 바인딩함
/// Victory, Defeat 등 게임 결과를 표시하는 패널에서 사용
/// </summary>
public interface IGameResultPanel
{
    /// <summary>
    /// 주요 액션 버튼
    /// - VictoryPanel: 다음 레벨 또는 스테이지 선택
    /// - DefeatPanel: 재시작
    /// </summary>
    Button PrimaryActionButton { get; }

    /// <summary>
    /// 주요 액션 실행 메서드
    /// UIPanelManager가 PrimaryActionButton에 자동으로 바인딩함
    /// </summary>
    void OnPrimaryAction();

    /// <summary>
    /// 보조 액션 버튼
    /// - 일반적으로 메인 메뉴로 이동
    /// </summary>
    Button SecondaryActionButton { get; }

    /// <summary>
    /// 보조 액션 실행 메서드
    /// UIPanelManager가 SecondaryActionButton에 자동으로 바인딩함
    /// </summary>
    void OnSecondaryAction();
}
