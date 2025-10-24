using UnityEngine;
using Game.Data;

namespace Game.Repositories
{
    /// <summary>
    /// 데미지 표시 Repository 구현
    /// 데이터 검증, 정제 및 EventChannel을 통한 이벤트 발송을 담당합니다.
    ///
    /// 역할:
    /// 1. 데이터 검증: 유효하지 않은 데이터 필터링
    /// 2. 데이터 정제: 표시 가능한 범위로 클램핑
    /// 3. 비즈니스 규칙 적용: 최소/최대 데미지 제한
    /// 4. EventChannel 추상화: HealthComponent가 EventChannel을 직접 의존하지 않도록 함
    /// </summary>
    public class DamageDisplayRepository : MonoBehaviour, IDamageDisplayRepository
    {
        [Header("Configuration")]
        [Tooltip("표시할 최소 데미지 (이보다 작으면 무시)")]
        [SerializeField] private int minDisplayDamage = 1;

        [Tooltip("표시할 최대 데미지 (UI 오버플로우 방지)")]
        [SerializeField] private int maxDisplayDamage = 9999;

        // EventChannel reference (GameInitializer에서 Initialize 시 설정)
        private DamageDisplayEventChannelSO eventChannel;

        /// <summary>
        /// Repository 초기화 (GameInitializer에서 호출)
        /// </summary>
        /// <param name="channel">DamageDisplayEventChannelSO 인스턴스</param>
        public void Initialize(DamageDisplayEventChannelSO channel)
        {
            eventChannel = channel;

            if (eventChannel == null)
            {
                Debug.LogError("[DamageDisplayRepository] EventChannel is null! Damage display will not work.");
            }
            else
            {
                Debug.Log("[DamageDisplayRepository] Initialized successfully with EventChannel");
            }
        }

        /// <summary>
        /// 데미지 표시 요청을 전송합니다.
        /// 데이터 검증 및 정제 후 EventChannel로 이벤트를 발생시킵니다.
        /// </summary>
        public void SendDamageDisplay(int amount, bool isCritical, Vector3 worldPosition)
        {
            // 1. 데이터 검증 - 유효하지 않은 데이터 필터링
            if (amount <= 0)
            {
                Debug.LogWarning($"[DamageDisplayRepository] Invalid damage amount: {amount}. Ignoring display request.");
                return;
            }

            if (eventChannel == null)
            {
                Debug.LogWarning("[DamageDisplayRepository] EventChannel not assigned! Cannot send damage display.");
                return;
            }

            // 2. 데이터 정제 - 표시 가능한 범위로 클램핑
            int sanitizedAmount = Mathf.Clamp(amount, minDisplayDamage, maxDisplayDamage);

            // 원본 데이터와 정제된 데이터가 다른 경우 로그
            if (sanitizedAmount != amount)
            {
                Debug.Log($"[DamageDisplayRepository] Damage clamped: {amount} → {sanitizedAmount}");
            }

            // 3. DTO 생성
            var data = new DamageDisplayData(sanitizedAmount, isCritical, worldPosition);

            // 4. EventChannel을 통한 이벤트 발송
            eventChannel.RaiseEvent(data);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DamageDisplayRepository] Damage display sent: {data}");
#endif
        }

        private void OnValidate()
        {
            // Inspector 값 검증
            minDisplayDamage = Mathf.Max(0, minDisplayDamage);
            maxDisplayDamage = Mathf.Max(minDisplayDamage, maxDisplayDamage);
        }
    }
}
