using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;

namespace Game.UI.Menu
{
    /// <summary>
    /// 메인(또는 스테이지 선택) 화면에서
    /// Cinemachine DollyTrack을 따라 카메라를 이동시키는 컨트롤러입니다.
    /// 
    /// - A/B/C 버튼 클릭 시 지정된 PathPosition으로 부드럽게 이동
    /// - 이동 중 다른 버튼 클릭 시, 현재 위치 기준으로 새 목표로 재설정
    /// - 이동 중 마우스 클릭 시 스킵: 즉시 목표 위치로 점프
    /// </summary>
    public class MainMenuCameraController : MonoBehaviour
    {
        [Serializable]
        public class CanvasGroupState
        {
            public CanvasGroup canvasGroup;

            [Range(0f, 1f)]
            public float alpha = 1f;
            public bool blocksRaycasts = true;
        }

        [Serializable]
        public class CameraPointConfig
        {
            public CameraPointId id;

            /// <summary>
            /// DollyTrack 상의 위치 값 (PositionUnits 설정에 맞춘 값)
            /// </summary>
            public float pathPosition;

            /// <summary>
            /// 이 포인트에서 카메라가 바라볼 대상 Transform
            /// (CinemachineVirtualCamera.LookAt 에 연결됩니다)
            /// </summary>
            public Transform lookAtTarget;

            [Header("UI Canvas Groups")]
            public CanvasGroupState canvasGroup1;
            public CanvasGroupState canvasGroup2;
        }

        public enum CameraPointId
        {
            A,
            B,
            C
        }

        [Header("Initial Point")]
        [SerializeField]
        private bool useExplicitInitialPoint;

        [SerializeField]
        private CameraPointId explicitInitialPointId = CameraPointId.A;

        [Header("Cinemachine")]
        [SerializeField]
        private CinemachineVirtualCamera virtualCamera;

        /// <summary>
        /// DollyTrack 상에서의 이동 속도 (pathPosition 단위/초)
        /// Path의 PositionUnits 설정(Distance/Path/Normalized)에 따라 의미가 달라집니다.
        /// </summary>
        [SerializeField]
        private float moveSpeed = 5f;

        [Header("Camera Points")]
        [SerializeField]
        private List<CameraPointConfig> pointConfigs = new List<CameraPointConfig>();

        [Header("Dolly Path")]
        [SerializeField]
        private CinemachineSmoothPath trackedDolly;

        private Dictionary<CameraPointId, CameraPointConfig> pointMap;

        private CameraPointId currentPointId;
        private CameraPointId? targetPointId;
        private float currentPathPosition;
        private float targetPathPosition;
        private bool isMoving;

        private void Awake()
        {
            if (virtualCamera == null)
            {
                virtualCamera = GetComponent<CinemachineVirtualCamera>();
            }

            if (virtualCamera == null)
            {
                Debug.LogError("[MainMenuCameraController] CinemachineVirtualCamera reference is not assigned.");
                enabled = false;
                return;
            }

            if (trackedDolly == null)
            {
                Debug.LogError("[MainMenuCameraController] CinemachineSmoothPath reference is not assigned.");
                enabled = false;
                return;
            }

            BuildPointMap();
            InitializeCurrentPoint();
        }

        private void Update()
        {
            if (!isMoving)
            {
                return;
            }

            // 이동 중 마우스 클릭 시 스킵: 즉시 목표 위치로 점프
            if (Input.GetMouseButtonDown(0))
            {
                SkipCurrentMove();
                return;
            }

            float newPos = Mathf.MoveTowards(currentPathPosition, targetPathPosition, moveSpeed * Time.deltaTime);
            currentPathPosition = newPos;

            ApplyCameraTransformAt(currentPathPosition);

            if (Mathf.Approximately(currentPathPosition, targetPathPosition))
            {
                isMoving = false;

                if (targetPointId.HasValue)
                {
                    currentPointId = targetPointId.Value;
                }

                targetPointId = null;
            }
        }

        /// <summary>
        /// 인스펙터에서 A/B/C 버튼에 직접 연결할 수 있는 헬퍼 메서드들
        /// </summary>
        public void MoveToPointA() => MoveToPoint(CameraPointId.A);
        public void MoveToPointB() => MoveToPoint(CameraPointId.B);
        public void MoveToPointC() => MoveToPoint(CameraPointId.C);

        /// <summary>
        /// 지정한 포인트로 이동을 시작합니다.
        /// 이동 중이라도, 현재 DollyTrack 상 위치를 기준으로 새 목표로 재설정됩니다.
        /// </summary>
        public void MoveToPoint(CameraPointId id)
        {
            if (trackedDolly == null || pointMap == null)
            {
                Debug.LogWarning($"[MainMenuCameraController] trackedDolly:{trackedDolly}, pointMap:{pointMap}");
                return;
            }

            if (!pointMap.TryGetValue(id, out var config))
            {
                Debug.LogWarning($"[MainMenuCameraController] No CameraPointConfig found for id: {id}");
                return;
            }

            // 이 포인트에 해당하는 LookAt 타겟으로 전환
            if (virtualCamera != null && config.lookAtTarget != null)
            {
                virtualCamera.LookAt = config.lookAtTarget;
            }

            // 이 포인트에서 표시할 UI 상태 적용
            ApplyUIForPoint(config);

            float currentPos = currentPathPosition;
            float destPos = config.pathPosition;

            // 이미 거의 같은 위치라면 이동 종료 상태로 간주
            if (Mathf.Approximately(currentPos, destPos))
            {
                isMoving = false;
                currentPointId = id;
                targetPointId = null;
                return;
            }

            // startPos는 별도로 저장하지 않고,
            // 항상 "현재 DollyTrack 위치"에서 MoveTowards로 이동하도록 설계
            targetPointId = id;
            targetPathPosition = destPos;
            isMoving = true;

            Debug.Log($"[MainMenuCameraController] ID({currentPointId} -> {targetPointId}), isMoving({isMoving})");
        }

        private void SkipCurrentMove()
        {
            currentPathPosition = targetPathPosition;
            ApplyCameraTransformAt(currentPathPosition);
            isMoving = false;

            if (targetPointId.HasValue)
            {
                currentPointId = targetPointId.Value;
            }

            targetPointId = null;
        }

        private void BuildPointMap()
        {
            pointMap = new Dictionary<CameraPointId, CameraPointConfig>();

            if (pointConfigs == null)
            {
                Debug.LogWarning("[MainMenuCameraController] pointConfigs list is null.");
                return;
            }

            foreach (var config in pointConfigs)
            {
                if (config == null)
                {
                    continue;
                }

                if (pointMap.ContainsKey(config.id))
                {
                    Debug.LogWarning($"[MainMenuCameraController] Duplicate CameraPointConfig for id: {config.id}. The first one will be used.");
                    continue;
                }

                pointMap.Add(config.id, config);
            }
        }

        private void InitializeCurrentPoint()
        {
            if (pointMap == null || pointMap.Count == 0 || trackedDolly == null)
            {
                return;
            }

            CameraPointConfig initialConfig = null;

            // 1) 인스펙터에서 명시적으로 시작 포인트를 지정한 경우 우선 사용
            if (useExplicitInitialPoint && pointMap.TryGetValue(explicitInitialPointId, out var explicitConfig))
            {
                initialConfig = explicitConfig;
            }
            else
            {
                // 2) 그렇지 않다면 기존 로직 유지:
                //    현재 카메라 위치와 가장 가까운 포인트를 초기 포인트로 설정
                var cameraPosition = virtualCamera != null ? virtualCamera.transform.position : Vector3.zero;
                float nearestDistance = float.MaxValue;

                foreach (var kvp in pointMap)
                {
                    var config = kvp.Value;
                    Vector3 pointPos = trackedDolly.EvaluatePosition(config.pathPosition);
                    float distance = (pointPos - cameraPosition).sqrMagnitude;

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        initialConfig = config;
                    }
                }
            }

            if (initialConfig == null)
            {
                return;
            }

            currentPointId = initialConfig.id;
            currentPathPosition = initialConfig.pathPosition;
            targetPathPosition = currentPathPosition;

            // 초기 포인트의 LookAt 타겟을 설정
            if (virtualCamera != null &&
                initialConfig.lookAtTarget != null)
            {
                virtualCamera.LookAt = initialConfig.lookAtTarget;
            }

            ApplyCameraTransformAt(currentPathPosition);
            ApplyUIForPoint(initialConfig);
        }

        private void ApplyCameraTransformAt(float pathPosition)
        {
            if (trackedDolly == null || virtualCamera == null)
            {
                return;
            }

            Vector3 position = trackedDolly.EvaluatePosition(pathPosition);
            Quaternion rotation = trackedDolly.EvaluateOrientation(pathPosition);

            virtualCamera.transform.position = position;
            virtualCamera.transform.rotation = rotation;
        }

        /// <summary>
        /// 지정한 포인트에 맞춰 UI CanvasGroup 들의 alpha/blockRaycasts를 설정합니다.
        /// - 모든 포인트에 연결된 CanvasGroup을 기본값(숨김)으로 리셋한 뒤
        /// - 활성 포인트의 CanvasGroupState 설정을 적용합니다.
        /// </summary>
        private void ApplyUIForPoint(CameraPointConfig activeConfig)
        {
            if (pointConfigs == null)
            {
                return;
            }

            // 모든 포인트의 CanvasGroup을 비활성 상태로 리셋
            foreach (var config in pointConfigs)
            {
                if (config == null)
                {
                    continue;
                }

                ResetCanvasGroupState(config.canvasGroup1);
                ResetCanvasGroupState(config.canvasGroup2);
            }

            if (activeConfig == null)
            {
                return;
            }

            // 활성 포인트 설정 적용
            ApplyCanvasGroupState(activeConfig.canvasGroup1);
            ApplyCanvasGroupState(activeConfig.canvasGroup2);
        }

        private void ResetCanvasGroupState(CanvasGroupState state)
        {
            if (state == null || state.canvasGroup == null)
            {
                return;
            }

            state.canvasGroup.alpha = 0f;
            state.canvasGroup.blocksRaycasts = false;
        }

        private void ApplyCanvasGroupState(CanvasGroupState state)
        {
            if (state == null || state.canvasGroup == null)
            {
                return;
            }

            state.canvasGroup.alpha = state.alpha;
            state.canvasGroup.blocksRaycasts = state.blocksRaycasts;
        }
    }
}
