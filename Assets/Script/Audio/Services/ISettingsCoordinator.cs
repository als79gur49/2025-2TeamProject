/// <summary>
/// SettingsCoordinator의 공개 인터페이스
/// 설정 패널의 저장/로드 타이밍을 조율
/// </summary>
public interface ISettingsCoordinator
{
    /// <summary>
    /// SettingsCoordinator 초기화
    /// </summary>
    /// <param name="settingsPanel">연결할 SettingsPanel</param>
    void Initialize(SettingsPanel settingsPanel);

    /// <summary>
    /// 정리 (이벤트 구독 해제)
    /// </summary>
    void Cleanup();
}
