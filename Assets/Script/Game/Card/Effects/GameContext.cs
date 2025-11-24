using Game.Services;
using Game.Interfaces;
using System.Collections.Generic;

namespace Game.Card.Effects
{
    /// <summary>
    /// 게임 컨텍스트 - 타일 기반 설계
    /// Phase 1.4: GameObject 기반 → 타일 기반으로 전환
    /// ICardEffect의 Execute 메소드에서 필요한 모든 서비스 참조를 담는 객체입니다.
    /// 의존성 주입을 통해 카드 효과가 게임 시스템에 접근할 수 있도록 합니다.
    /// </summary>
    public class GameContext
    {
        /// <summary>이번에 실행 중인 카드 데이터 (소환/효과의 원본)</summary>
        public Game.Data.CardData SourceCard { get; private set; }

        /// <summary>유닛 관리 서비스</summary>
        public IUnitService UnitService { get; private set; }

        /// <summary>그리드 제어 서비스</summary>
        public IGridController GridController { get; private set; }

        /// <summary>카드 소환 서비스</summary>
        public ICardSpawnService CardSpawnService { get; private set; }

        /// <summary>소환 유효성 검사 서비스</summary>
        public ISpawnValidator SpawnValidator { get; private set; }

        /// <summary>카드를 사용한 팀 (시전자의 팀)</summary>
        public TeamType CasterTeam { get; private set; }

        /// <summary>카드가 사용된 원래 위치</summary>
        public UnityEngine.Vector2Int OriginPosition { get; private set; }

        #region Tile-Based VFX Integration (Phase 1.4)

        /// <summary>
        /// 사전 계산된 타겟 타일 리스트
        /// CalculateAllPotentialTiles()에서 설정됨
        /// </summary>
        public List<Tile> PredeterminedTiles { get; set; } = new List<Tile>();

        /// <summary>
        /// 타일 기반 VFX 재생 좌표 리스트
        /// </summary>
        public List<UnityEngine.Vector3> VFXPositions { get; set; } = new List<UnityEngine.Vector3>();

        #endregion

        /// <summary>
        /// GameContext 생성자
        /// </summary>
        /// <param name="unitService">유닛 서비스</param>
        /// <param name="gridController">그리드 컨트롤러</param>
        /// <param name="cardSpawnService">카드 소환 서비스</param>
        /// <param name="spawnValidator">소환 유효성 검사자</param>
        /// <param name="casterTeam">카드를 사용한 팀</param>
        /// <param name="originPosition">카드 사용 원점</param>
        public GameContext(
            Game.Data.CardData sourceCard,
            IUnitService unitService,
            IGridController gridController,
            ICardSpawnService cardSpawnService,
            ISpawnValidator spawnValidator,
            TeamType casterTeam,
            UnityEngine.Vector2Int originPosition)
        {
            SourceCard = sourceCard;
            UnitService = unitService;
            GridController = gridController;
            CardSpawnService = cardSpawnService;
            SpawnValidator = spawnValidator;
            CasterTeam = casterTeam;
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
