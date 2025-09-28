using Game.Services;
using Game.Interfaces;

namespace Game.Card.Effects
{
    /// <summary>
    /// CardData 리팩토링 Phase 1.2: 게임 컨텍스트
    /// ICardEffect의 Execute 메소드에서 필요한 모든 서비스 참조를 담는 객체입니다.
    /// 의존성 주입을 통해 카드 효과가 게임 시스템에 접근할 수 있도록 합니다.
    /// </summary>
    public class GameContext
    {
        /// <summary>유닛 관리 서비스</summary>
        public IUnitService UnitService { get; private set; }

        /// <summary>그리드 제어 서비스</summary>
        public IGridController GridController { get; private set; }

        /// <summary>카드 소환 서비스</summary>
        public ICardSpawnService CardSpawnService { get; private set; }

        /// <summary>소환 유효성 검사 서비스</summary>
        public ISpawnValidator SpawnValidator { get; private set; }

        /// <summary>카드를 사용한 플레이어 ID</summary>
        public int PlayerId { get; private set; }

        /// <summary>카드가 사용된 원래 위치</summary>
        public UnityEngine.Vector2Int OriginPosition { get; private set; }

        /// <summary>
        /// GameContext 생성자
        /// </summary>
        /// <param name="unitService">유닛 서비스</param>
        /// <param name="gridController">그리드 컨트롤러</param>
        /// <param name="cardSpawnService">카드 소환 서비스</param>
        /// <param name="spawnValidator">소환 유효성 검사자</param>
        /// <param name="playerId">플레이어 ID</param>
        /// <param name="originPosition">카드 사용 원점</param>
        public GameContext(
            IUnitService unitService,
            IGridController gridController,
            ICardSpawnService cardSpawnService,
            ISpawnValidator spawnValidator,
            int playerId,
            UnityEngine.Vector2Int originPosition)
        {
            UnitService = unitService;
            GridController = gridController;
            CardSpawnService = cardSpawnService;
            SpawnValidator = spawnValidator;
            PlayerId = playerId;
            OriginPosition = originPosition;
        }

        /// <summary>
        /// 컨텍스트가 유효한지 검증합니다.
        /// </summary>
        /// <returns>모든 필수 서비스가 존재하면 true</returns>
        public bool IsValid()
        {
            return UnitService != null &&
                   GridController != null &&
                   CardSpawnService != null &&
                   SpawnValidator != null;
        }
    }
}