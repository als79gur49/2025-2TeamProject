namespace Game.Card.Effects
{
    /// <summary>
    /// EffectDefinition 기반 타일 VFX 배치 모드
    /// - CenterOnly      : 효과는 기존 로직 그대로, VFX는 중심 타일 기준 연출
    /// - AllAreaTiles    : AreaShape에 포함된 모든 타일을 VFX 후보로 사용
    /// - ValidTilesOnly  : TargetFilter를 통과한 타일만 VFX 후보로 사용
    /// - InvalidTilesOnly: (향후 확장용) 유효 타일 외의 타일을 강조하는 용도
    /// </summary>
    public enum VFXTilePlacementMode
    {
        CenterOnly,
        AllAreaTiles,
        ValidTilesOnly,
        InvalidTilesOnly
    }
}

