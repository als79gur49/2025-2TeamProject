using UnityEngine;
using Game.Data;

namespace Game.Card.Effects
{
    /// <summary>
    /// 소환 효과 정의용 ScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/SummonEffect")]
    public class SummonEffectDefinition : EffectDefinition
    {
        [Header("Summon 설정")]
        [SerializeField] private UnitData unitToSummon;
        [SerializeField] private int count = 1;

        public UnitData UnitToSummon => unitToSummon;
        public int Count => count;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new SummonEffect(this);
        }
    }
}

