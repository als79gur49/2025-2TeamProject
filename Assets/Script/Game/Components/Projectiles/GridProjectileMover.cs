using Game.Data.Modifiers;
using UnityEngine;

namespace Game.Components
{
    /// <summary>
    /// GridProjectile 시각 효과용 이동/스케일 조정 스크립트.
    /// 현재는 ExecutionType이 InstantLaser인 경우에만
    /// Modifier Range(= GridProjectile.MaxRange)를 기준으로
    /// Z축 스케일을 늘려 레이저 길이를 표현한다.
    /// </summary>
    public class GridProjectileMover : MonoBehaviour
    {
        [SerializeField]
        private GridProjectile projectile;

        [SerializeField]
        [Tooltip("기본 이동 속도 계수")]
        private float baseSpeed = 5f;

        [SerializeField]
        [Tooltip("Range=1일 때의 기본 Z 스케일 값")]
        private float baseZScale = 1f;

        private float moveSpeed;
        private Vector3 moveDirection;

        private void Reset()
        {
            if (projectile == null)
            {
                projectile = GetComponent<GridProjectile>();
            }
        }

        private void Start()
        {
            if (projectile == null)
            {
                projectile = GetComponent<GridProjectile>();
            }

            if (projectile == null)
            {
                return;
            }

            // ExecutionType에 따라 동작 분리
            if (projectile.ExecutionType == ProjectileExecutionType.InstantLaser)
            {
                SetupInstantLaserScale();
            }
            else if (projectile.ExecutionType == ProjectileExecutionType.Moving)
            {
                SetupMovingProjectile();
            }
        }

        private void Update()
        {
            if (projectile == null)
            {
                return;
            }

            if (projectile.ExecutionType != ProjectileExecutionType.Moving)
            {
                return;
            }

            if (moveSpeed <= 0f)
            {
                return;
            }

            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        private void SetupInstantLaserScale()
        {
            var t = transform;
            var scale = t.localScale;

            // baseZScale이 0이거나 기본값이면, 현재 스케일을 1타일 기준값으로 사용
            if (Mathf.Approximately(baseZScale, 0f))
            {
                baseZScale = Mathf.Approximately(scale.z, 0f) ? 1f : scale.z;
            }

            int range = Mathf.Max(1, projectile.MaxRange);

            // 요구사항: Modifier의 Range만큼 Z값에 곱셈
            scale.z = baseZScale * range;
            t.localScale = scale;
        }

        private void SetupMovingProjectile()
        {
            // baseSpeed 방어 코드
            if (baseSpeed <= 0f)
            {
                baseSpeed = 0.1f;
            }

            int range = Mathf.Max(1, projectile.MaxRange);

            // projectile.MaxRange의 로그값을 사용하여 최종 이동 속도 계산
            // log(1) = 0이므로 log(range + 1)를 사용해 최소 속도를 확보
            float logRange = Mathf.Log(range + 1f);
            moveSpeed = baseSpeed * logRange;

            // 이동 방향은 현재 Transform의 forward를 기준으로 한다.
            moveDirection = transform.forward.normalized;
        }
    }
}
