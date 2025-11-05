using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Interfaces
{
    public interface IAttackEffect
    {
        string EffectName { get; }
        int Priority { get; }
        Unit Owner { get; }

        void ApplyEffectToTiles(List<Tile> primaryTiles, ActionContext context);
    }
}
