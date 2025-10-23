namespace Game.SceneManagement
{
    /// <summary>
    /// 씬 카테고리 분류 - 씬을 목적별로 구분
    /// </summary>
    public enum SceneCategory
    {
        /// <summary>메뉴 씬 (타이틀, 설정 등)</summary>
        Menu,

        /// <summary>게임플레이 씬 (실제 게임 진행)</summary>
        Gameplay,

        /// <summary>프로토타입 테스트 씬</summary>
        Prototype,

        /// <summary>개별 기능 테스트 씬</summary>
        FeatureTest,

        /// <summary>도구/유틸리티 씬</summary>
        Tool
    }
}
