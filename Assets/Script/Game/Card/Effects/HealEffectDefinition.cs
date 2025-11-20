using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 회복 효과 정의용 ScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/HealEffect")]
    public class HealEffectDefinition : EffectDefinition
    {
        [Header("Heal 설정")]
        [SerializeField] private int healAmount = 1;
        [SerializeField] private float duration = 0f;

        public int HealAmount => healAmount;
        public float Duration => duration;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new HealEffect(this);
        }
    }
}

