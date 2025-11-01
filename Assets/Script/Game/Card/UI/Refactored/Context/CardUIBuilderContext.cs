namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 덱 빌더 특화 컨텍스트
    /// Coordinator를 통한 중재자 패턴 사용 (BaseContext.Coordinator 사용)
    /// </summary>
    public class CardUIBuilderContext : CardUIBaseContext
    {
        // DeckPanel 프로퍼티 제거 - Coordinator를 통해 통신
    }
}
