using UnityEngine;
using Game.Core;
using Game.Interfaces;
using Game.VFX;

namespace Game.Components
{
    /// <summary>
    /// Base(또는 HealthComponent가 붙은 오브젝트)의 피격 위치를 감지하여
    /// 그리드 기반 VFX를 생성하는 컴포넌트입니다.
    ///
    /// - HealthComponent.OnHitGridPosition 이벤트를 구독합니다.
    /// - 전달받은 GridPosition을 IGridManager를 통해 월드 좌표로 변환합니다.
    /// - 변환된 위치 + 추가 Y 오프셋 + VFXData.PositionOffset에 VFX를 생성합니다.
    /// </summary>
    public class BaseHitVFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthComponent healthComponent;

        [Header("VFX Settings")]
        [SerializeField] private VFXData hitVfxData;

        [Tooltip("Grid 타일 기준 추가 Y 오프셋")]
        [SerializeField] private float additionalYOffset = 0f;

        // 의존성
        private IGridManager gridManager;

        private void Awake()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }
        }

        private void OnEnable()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<HealthComponent>();
            }

            if (healthComponent != null)
            {
                healthComponent.OnHitGridPosition += HandleHitGridPosition;
            }

            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<IGridManager>();

                if (gridManager == null)
                {
                    Debug.LogWarning($"[BaseHitVFX] IGridManager not found in ServiceLocator for {gameObject.name} - will use transform.position as fallback.");
                }
            }
        }

        private void OnDisable()
        {
            if (healthComponent != null)
            {
                healthComponent.OnHitGridPosition -= HandleHitGridPosition;
            }
        }

        /// <summary>
        /// HealthComponent에서 피격 그리드 좌표를 전달받았을 때 호출됩니다.
        /// </summary>
        private void HandleHitGridPosition(Vector2Int gridPosition)
        {
            if (hitVfxData == null || !hitVfxData.IsValid())
            {
                return;
            }

            // 1) Grid 기반 월드 위치 계산
            Vector3 worldPos;

            if (gridManager != null)
            {
                // Base 높이 계산을 지원하는 GridManager 메서드 사용
                worldPos = gridManager.CalculateWorldPositionWithHeight(gridPosition);
            }
            else
            {
                // 폴백: 현재 오브젝트 위치 사용
                worldPos = transform.position;
            }

            // 추가 Y 오프셋 및 VFXData의 PositionOffset 적용
            worldPos.y += additionalYOffset;
            worldPos += hitVfxData.PositionOffset;

            // 2) VFX 생성
            var prefab = hitVfxData.VFXPrefab;
            if (prefab == null)
            {
                return;
            }

            Instantiate(prefab, worldPos, Quaternion.identity);
        }
    }
}

