namespace Game.SaveSystem
{
    /// <summary>
    /// 저장 파일 타입 열거형
    /// 각 데이터 카테고리별로 파일을 분리하여 관리
    /// </summary>
    public enum SaveFileType
    {
        /// <summary>
        /// 오디오 설정 (볼륨, 뮤트 상태 등)
        /// </summary>
        AudioSettings,

        /// <summary>
        /// 플레이어 데이터 (ID, 이름, 골드, 플레이 시간 등)
        /// </summary>
        PlayerData,

        /// <summary>
        /// 스테이지 진행도 (현재 챕터, 스테이지, 클리어 기록 등)
        /// </summary>
        StageProgress,

        /// <summary>
        /// 카드 컬렉션 (보유 카드, 레벨, 강화 정보 등)
        /// </summary>
        CardCollection,

        /// <summary>
        /// 상점 데이터 (진열 아이템, 재고, 할인 정보 등)
        /// </summary>
        ShopData
    }
}
