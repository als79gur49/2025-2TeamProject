using Game.Interfaces;

namespace Game.Card.UI.Refactored
{
    /// <summary>
    /// 전투 특화 컨텍스트
    /// </summary>
    public class CardUIBattleContext : CardUIBaseContext
    {
        public ICardSpawnService CardSpawnService { get; set; }
        public ISpawnValidator SpawnValidator { get; set; }
        public ICardHandManager CardHandManager { get; set; }
        public IGridRenderer GridRenderer { get; set; }
        public IGridManager GridManager { get; set; }

        // 드롭 피드백 사운드
        public AudioData DropSuccessSound { get; set; }
        public AudioData DropFailSound { get; set; }
        public SoundEventChannelSO SoundEventChannel { get; set; }
    }
}
