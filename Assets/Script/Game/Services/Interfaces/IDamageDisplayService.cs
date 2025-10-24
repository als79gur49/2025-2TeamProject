using UnityEngine;

namespace Game.Services
{
    /// <summary>
    /// 데미지 팝업 UI 관리 서비스 인터페이스
    /// 데미지 표시 UI의 생명주기 및 오브젝트 풀링을 담당합니다.
    /// </summary>
    public interface IDamageDisplayService
    {
        /// <summary>
        /// 서비스 초기화
        /// EventChannel 구독 및 오브젝트 풀 생성을 수행합니다.
        /// </summary>
        /// <param name="eventChannel">데미지 표시 이벤트 채널</param>
        /// <param name="popupPrefab">데미지 팝업 프리팹</param>
        void Initialize(DamageDisplayEventChannelSO eventChannel, GameObject popupPrefab);
    }
}
