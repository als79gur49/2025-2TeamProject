namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 이벤트 채널 컨테이너
    /// </summary>
    public class CardUIEventChannels
    {
        public CardInfoEventChannelSO CardInfoChannel { get; set; }
        public CardDragEndEventChannelSO CardDragEndChannel { get; set; }
        public Game.UI.Events.CardDragStartEventChannelSO CardDragStartChannel { get; set; }
    }
}
