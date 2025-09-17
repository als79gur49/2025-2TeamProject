using UnityEngine;

public class DamageEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"데미지 효과 발동: {value} 데미지, 범위: {range}");
    }
}

public class HealEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"힐 효과 발동: {value} 회복, 범위: {range}");
    }
}

public class BuffEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"버프 효과 발동: {value} 강화, 범위: {range}");
    }
}

public class DebuffEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"디버프 효과 발동: {value} 약화, 범위: {range}");
    }
}

public class ShieldEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"실드 효과 발동: {value} 보호막, 범위: {range}");
    }
}

public class TeleportEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"텔레포트 효과 발동: 위치 {position}, 범위: {range}");
    }
}

public class SummonEffect : ISpellEffect
{
    public void Execute(Vector3 position, int value, float range)
    {
        Debug.Log($"소환 효과 발동: {value}개 소환, 위치: {position}");
    }
}