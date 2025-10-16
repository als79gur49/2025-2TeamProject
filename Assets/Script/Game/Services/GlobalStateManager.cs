using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Services
{
    /// <summary>
    /// 전역 게임 상태를 관리하는 서비스 클래스.
    /// ServiceLocator를 통해 전역적으로 접근 가능합니다.
    /// </summary>
    public class GlobalStateManager : MonoBehaviour, IGlobalStateManager
    {
        // 각 BusyType별로 요청자 집합을 관리
        private readonly Dictionary<BusyType, HashSet<object>> _requesters =
            new Dictionary<BusyType, HashSet<object>>();

        // 타임아웃 메커니즘 - 각 요청자별 만료 시간 추적
        private readonly Dictionary<object, float> _requesterTimeouts =
            new Dictionary<object, float>();

        // 기본 타임아웃 시간 (초)
        private const float DEFAULT_TIMEOUT = 10f;

        // 상태 변경 이벤트
        public event Action<BusyType, bool> OnBusyStateChanged;

        // 초기화
        private void Awake()
        {
            // ServiceLocator에 자신을 등록
            ServiceLocator.Register<IGlobalStateManager>(this);

            // Dictionary 초기화 (모든 BusyType에 대해 빈 HashSet 생성)
            foreach (BusyType type in Enum.GetValues(typeof(BusyType)))
            {
                if (type != BusyType.None)
                {
                    _requesters[type] = new HashSet<object>();
                }
            }

            Debug.Log("[GlobalStateManager] Initialized and registered with ServiceLocator");
        }

        // 씬 전환 시 자동 정리
        private void OnDestroy()
        {
            ServiceLocator.Unregister<IGlobalStateManager>();
            Debug.Log("[GlobalStateManager] Unregistered from ServiceLocator");
        }

        /// <summary>
        /// Busy 상태 설정
        /// 타임아웃 파라미터 추가 - 기본값 10초
        /// </summary>
        public void SetBusy(object requester, BusyType type, float timeout = DEFAULT_TIMEOUT)
        {
            if (requester == null)
            {
                Debug.LogWarning("[GlobalStateManager] SetBusy called with null requester, ignoring");
                return;
            }

            if (type == BusyType.None)
            {
                Debug.LogWarning("[GlobalStateManager] SetBusy called with BusyType.None, ignoring");
                return;
            }

            if (!_requesters.ContainsKey(type))
            {
                _requesters[type] = new HashSet<object>();
            }

            bool wasEmpty = _requesters[type].Count == 0;
            _requesters[type].Add(requester);

            // 타임아웃 설정 (메모리 누수 및 영구 잠금 방지)
            _requesterTimeouts[requester] = Time.time + timeout;

            // 처음으로 Busy 상태가 된 경우에만 이벤트 발생
            if (wasEmpty && _requesters[type].Count > 0)
            {
                Debug.Log($"[GlobalStateManager] {type} state changed to Busy (requester: {requester.GetType().Name}, timeout: {timeout}s)");
                OnBusyStateChanged?.Invoke(type, true);
            }
        }

        /// <summary>
        /// Idle 상태 설정
        /// 타임아웃 Dictionary에서도 제거
        /// </summary>
        public void SetIdle(object requester, BusyType type)
        {
            if (requester == null)
            {
                Debug.LogWarning("[GlobalStateManager] SetIdle called with null requester, ignoring");
                return;
            }

            if (type == BusyType.None)
            {
                return;
            }

            if (!_requesters.ContainsKey(type))
            {
                return;
            }

            bool hadRequesters = _requesters[type].Count > 0;
            _requesters[type].Remove(requester);

            // 타임아웃 Dictionary에서도 제거
            _requesterTimeouts.Remove(requester);

            // 마지막 요청자가 제거되어 Idle 상태가 된 경우에만 이벤트 발생
            if (hadRequesters && _requesters[type].Count == 0)
            {
                Debug.Log($"[GlobalStateManager] {type} state changed to Idle (last requester: {requester.GetType().Name})");
                OnBusyStateChanged?.Invoke(type, false);
            }
        }

        /// <summary>
        /// 특정 BusyType의 상태 확인
        /// </summary>
        public bool IsBusy(BusyType type)
        {
            if (type == BusyType.None)
                return false;

            return _requesters.ContainsKey(type) && _requesters[type].Count > 0;
        }

        /// <summary>
        /// 시스템 전체가 Busy 상태인지 확인
        /// </summary>
        public bool IsSystemBusy()
        {
            foreach (var kvp in _requesters)
            {
                if (kvp.Value.Count > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 타임아웃 메커니즘 - 만료된 잠금을 자동 해제
        /// 매 프레임 타임아웃 체크하여 영구 잠금 방지
        /// </summary>
        private void Update()
        {
            // 타임아웃된 요청자 목록 (역순회 중 수정 방지)
            var timedOutRequesters = new List<object>();

            foreach (var kvp in _requesterTimeouts)
            {
                if (Time.time > kvp.Value)
                {
                    timedOutRequesters.Add(kvp.Key);
                }
            }

            // 타임아웃된 요청자들을 강제 해제
            foreach (var requester in timedOutRequesters)
            {
                Debug.LogWarning($"[GlobalStateManager] Requester timeout: {requester.GetType().Name} - forcing release");
                AutoReleaseRequester(requester);
            }
        }

        /// <summary>
        /// 타임아웃된 요청자를 모든 BusyType에서 강제 해제
        /// </summary>
        private void AutoReleaseRequester(object requester)
        {
            foreach (var type in _requesters.Keys)
            {
                if (_requesters[type].Contains(requester))
                {
                    SetIdle(requester, type);
                    Debug.LogWarning($"[GlobalStateManager] Auto-released {type} for {requester.GetType().Name}");
                }
            }
        }
    }
}
