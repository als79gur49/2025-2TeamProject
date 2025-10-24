using UnityEngine;
using Game.Data;

/// <summary>
/// 데미지 표시 요청을 위한 이벤트 채널
/// GameEventChannelSO 패턴을 사용하여 ScriptableObject 기반 decoupling 구현
/// </summary>
[CreateAssetMenu(fileName = "DamageDisplayEventChannel",
                 menuName = "Events/Game/Damage Display Event Channel")]
public class DamageDisplayEventChannelSO : GameEventChannelSO<DamageDisplayData>
{
    /// <summary>
    /// 데미지 표시 이벤트를 발생시킵니다 (검증 포함)
    /// </summary>
    /// <param name="data">데미지 표시 데이터</param>
    public void ShowDamage(DamageDisplayData data)
    {
        // 유효성 검증
        if (data.Amount <= 0)
        {
            Debug.LogWarning("[DamageDisplayEventChannel] Invalid damage amount: " + data.Amount);
            return;
        }

        // 이벤트 발생
        RaiseEvent(data);
    }
}
