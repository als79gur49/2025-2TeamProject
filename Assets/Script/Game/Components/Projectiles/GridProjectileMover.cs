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

        [SerializeField]
        [Tooltip("BallisticEqualTime 모드에서 전체 비행 시간(초)")]
        private float flightDuration = 0.5f;

        [SerializeField]
        [Tooltip("BallisticEqualTime 모드에서 정규화 시간(0~1)에 따른 높이 오프셋 곡선")]
        private AnimationCurve heightCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        private float moveSpeed;
        private Vector3 moveDirection;

        // BallisticEqualTime 전용 상태
        private bool ballisticActive;
        private float ballisticElapsed;
        private Vector3 ballisticOrigin;
        private Vector3 ballisticTarget;

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
            else if (projectile.ExecutionType == ProjectileExecutionType.Moving ||
                     projectile.ExecutionType == ProjectileExecutionType.MovingDelayed)
            {
                SetupMovingProjectile();
            }
            else if (projectile.ExecutionType == ProjectileExecutionType.BallisticEqualTime)
            {
                SetupBallisticProjectile();
            }
        }

        private void Update()
        {
            if (projectile == null)
            {
                return;
            }

            if (projectile.ExecutionType == ProjectileExecutionType.BallisticEqualTime)
            {
                UpdateBallisticProjectile();
                return;
            }

            if (projectile.ExecutionType != ProjectileExecutionType.Moving &&
                projectile.ExecutionType != ProjectileExecutionType.MovingDelayed)
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

            // InstantLaser 모드에서는 실제로 맞게 될 적들 중
            // 가장 먼 타일까지의 타일 수(InstantLaserEffectiveRange)를 기준으로 스케일을 조정한다.
            int range = Mathf.Max(1, projectile.InstantLaserEffectiveRange);

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

        private void SetupBallisticProjectile()
        {
            ballisticActive = true;
            ballisticElapsed = 0f;

            if (flightDuration <= 0f)
            {
                flightDuration = 0.1f;
            }

            if (projectile != null)
            {
                ballisticOrigin = projectile.OriginWorldPosition;

                if (projectile.HasTargetWorldPosition)
                {
                    ballisticTarget = projectile.TargetWorldPosition;
                }
                else
                {
                    float fallbackDistance = Mathf.Max(1f, projectile.MaxRange);
                    ballisticTarget = ballisticOrigin + transform.forward.normalized * fallbackDistance;
                }
            }
            else
            {
                ballisticOrigin = transform.position;
                ballisticTarget = transform.position + transform.forward.normalized;
            }

            if (heightCurve == null || heightCurve.length == 0)
            {
                heightCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1f, 0f));
            }

            transform.position = ballisticOrigin;
        }

        private void UpdateBallisticProjectile()
        {
            if (!ballisticActive)
            {
                return;
            }

            ballisticElapsed += Time.deltaTime;

            float duration = Mathf.Max(0.0001f, flightDuration);
            float u = Mathf.Clamp01(ballisticElapsed / duration);

            Vector3 basePos = Vector3.Lerp(ballisticOrigin, ballisticTarget, u);
            float height = heightCurve != null ? heightCurve.Evaluate(u) : 0f;

            transform.position = basePos + Vector3.up * height;

            if (u >= 1f)
            {
                ballisticActive = false;
            }
        }
    }
}
