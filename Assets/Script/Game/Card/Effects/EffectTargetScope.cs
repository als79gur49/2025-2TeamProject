namespace Game.Card.Effects
{
    /// <summary>
    /// 효과가 타일 기반인지, 전역(Global)인지 구분하는 스코프
    /// TileBased: 기존처럼 타일/유닛을 대상으로 하는 효과
    /// Global   : 타일에 의존하지 않는 전역 효과 (자원 증감, 기지 체력 회복 등)
    /// </summary>
    public enum EffectTargetScope
    {
        TileBased,
        Global
    }
}

