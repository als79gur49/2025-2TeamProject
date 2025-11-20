using UnityEngine;

namespace Game.Card.Effects
{
    /// <summary>
    /// 데미지 효과 정의용 ScriptableObject
    /// 기존 EffectData.Value, IgnoreArmor, Duration 등을 타입 전용 필드로 옮긴 형태입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Effects/Definitions/DamageEffect")]
    public class DamageEffectDefinition : EffectDefinition
    {
        [Header("Damage 설정")]
        [SerializeField] private int damageAmount = 1;
        [SerializeField] private bool ignoreArmor = false;
        [SerializeField] private float duration = 0f;

        public int DamageAmount => damageAmount;
        public bool IgnoreArmor => ignoreArmor;
        public float Duration => duration;

        public override ICardEffect CreateRuntimeEffect()
        {
            return new DamageEffect(this);
        }
    }
}

