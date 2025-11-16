using System.Collections.Generic;
using Game.Components;

namespace Game.Core.Effects
{
    /// <summary>
    /// Effect 실행 시 전달되는 최소 컨텍스트 정보입니다.
    /// 현재 요구사항 기준으로 영향을 받은 타일 목록만 포함합니다.
    /// </summary>
    public class EffectContext
    {
        public List<Tile> AffectedTiles { get; }

        public EffectContext(List<Tile> affectedTiles = null)
        {
            AffectedTiles = affectedTiles ?? new List<Tile>();
        }
    }
}

