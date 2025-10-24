using UnityEngine;
using System.Collections.Generic;
using Game.UI;
using Game.Data;

namespace Game.Services
{
    /// <summary>
    /// 데미지 팝업 UI 관리 서비스
    /// 오브젝트 풀링을 통해 성능을 최적화하며, EventChannel을 구독하여 데미지 표시를 처리합니다.
    ///
    /// 책임:
    /// - EventChannel 구독 및 데미지 표시 이벤트 수신
    /// - 오브젝트 풀링을 통한 DamagePopupUI 생명주기 관리
    /// - 팝업 재사용 및 GC 압력 최소화
    /// </summary>
    public class DamageDisplayService : MonoBehaviour, IDamageDisplayService
    {
        private const int POOL_SIZE = 20; // 기본 풀 사이즈

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // EventChannel reference
        private DamageDisplayEventChannelSO eventChannel;

        // Prefab reference
        private GameObject popupPrefab;

        // Object Pool
        private Queue<DamagePopupUI> availablePopups = new Queue<DamagePopupUI>();
        private List<DamagePopupUI> activePopups = new List<DamagePopupUI>();

        /// <summary>
        /// 서비스 초기화
        /// EventChannel 구독 및 오브젝트 풀 생성
        /// </summary>
        public void Initialize(DamageDisplayEventChannelSO channel, GameObject prefab)
        {
            eventChannel = channel;
            popupPrefab = prefab;

            if (eventChannel == null)
            {
                Debug.LogError("[DamageDisplayService] EventChannel is null! Service will not function.");
                return;
            }

            if (popupPrefab == null)
            {
                Debug.LogError("[DamageDisplayService] Popup prefab is null! Cannot create popups.");
                return;
            }

            // EventChannel 구독
            eventChannel.Subscribe(ShowDamage);

            // 오브젝트 풀 초기화
            InitializePool();

            Debug.Log($"[DamageDisplayService] Initialized successfully with pool size {POOL_SIZE}");
        }

        /// <summary>
        /// 오브젝트 풀 초기화 - 미리 팝업을 생성하여 풀에 저장
        /// </summary>
        private void InitializePool()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                CreateNewPopup();
            }

            if (showDebugLogs)
                Debug.Log($"[DamageDisplayService] Object pool initialized with {POOL_SIZE} popups");
        }

        /// <summary>
        /// 새로운 팝업 생성 및 풀에 추가
        /// </summary>
        private DamagePopupUI CreateNewPopup()
        {
            var popupObj = Instantiate(popupPrefab, transform);
            var popup = popupObj.GetComponent<DamagePopupUI>();

            if (popup == null)
            {
                Debug.LogError("[DamageDisplayService] Popup prefab is missing DamagePopupUI component!");
                Destroy(popupObj);
                return null;
            }

            popup.ResetForPool();
            availablePopups.Enqueue(popup);

            return popup;
        }

        /// <summary>
        /// 데미지 표시 이벤트 핸들러 (EventChannel 구독)
        /// </summary>
        /// <param name="data">데미지 표시 데이터</param>
        private void ShowDamage(DamageDisplayData data)
        {
            // 풀에서 팝업 가져오기
            DamagePopupUI popup = GetPopupFromPool();

            if (popup == null)
            {
                // 풀이 고갈된 경우 새로 생성
                if (showDebugLogs)
                    Debug.LogWarning("[DamageDisplayService] Pool exhausted, creating new popup");

                popup = CreateNewPopup();

                if (popup == null)
                {
                    Debug.LogError("[DamageDisplayService] Failed to create new popup!");
                    return;
                }

                // 새로 생성한 팝업은 이미 풀에 들어가 있으므로 다시 가져옴
                popup = GetPopupFromPool();
            }

            // 활성 팝업 리스트에 추가
            activePopups.Add(popup);

            // 팝업 표시 (완료 콜백과 함께)
            popup.Show(data, OnPopupComplete);

            if (showDebugLogs)
                Debug.Log($"[DamageDisplayService] Showing damage popup: {data}");
        }

        /// <summary>
        /// 풀에서 사용 가능한 팝업 가져오기
        /// </summary>
        private DamagePopupUI GetPopupFromPool()
        {
            if (availablePopups.Count > 0)
            {
                return availablePopups.Dequeue();
            }
            return null;
        }

        /// <summary>
        /// 팝업 애니메이션 완료 콜백 - 팝업을 풀로 반환
        /// </summary>
        private void OnPopupComplete(DamagePopupUI popup)
        {
            // 활성 리스트에서 제거
            activePopups.Remove(popup);

            // 재사용을 위해 초기화
            popup.ResetForPool();

            // 풀로 반환
            availablePopups.Enqueue(popup);

            if (showDebugLogs)
                Debug.Log("[DamageDisplayService] Popup returned to pool");
        }

        /// <summary>
        /// 서비스 종료 시 EventChannel 구독 해제
        /// </summary>
        private void OnDestroy()
        {
            eventChannel?.Unsubscribe(ShowDamage);
            Debug.Log("[DamageDisplayService] Unsubscribed from EventChannel");
        }

#if UNITY_EDITOR
        [Header("Editor Debug Info")]
        [SerializeField] private int currentPoolSize;
        [SerializeField] private int currentActiveCount;

        private void Update()
        {
            // 에디터에서 풀 상태 모니터링
            currentPoolSize = availablePopups.Count;
            currentActiveCount = activePopups.Count;
        }
#endif
    }
}
