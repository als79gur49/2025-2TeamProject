using UnityEngine;

namespace Game.VFX
{
    /// <summary>
    /// 자신을 포함한 모든 자식 ParticleSystem이 더 이상 살아 있지 않을 때
    /// GameObject를 자동으로 파괴하는 컴포넌트입니다.
    /// </summary>
    public class AutoDestroyOnParticleEnd : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("체크를 시작하기 전 대기 시간(초). 파티클이 아직 생성되지 않은 첫 프레임에서의 오검사를 방지합니다.")]
        [SerializeField] private float checkDelay = 0.1f;

        [Tooltip("파티클이 끝나지 않더라도 이 시간이 지나면 강제로 파괴합니다. 0 이하면 비활성화.")]
        [SerializeField] private float maxLifetime = 0f;

        private ParticleSystem[] particleSystems;
        private float elapsed;
        private bool initialized;

        private void Awake()
        {
            // 자신 포함 모든 자식 ParticleSystem 캐싱
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            // 최대 생존 시간 초과 시 강제 파괴
            if (maxLifetime > 0f && elapsed >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 초기 딜레이 동안에는 체크하지 않음
            if (elapsed < checkDelay)
            {
                return;
            }

            if (!initialized)
            {
                // 한 번이라도 체크를 시작한 이후부터는 초기화 완료로 간주
                initialized = true;
            }

            if (particleSystems == null || particleSystems.Length == 0)
            {
                // 파티클이 없으면 즉시 파괴
                Destroy(gameObject);
                return;
            }

            // 자신을 포함한 모든 파티클 시스템이 더 이상 살아 있지 않은지 검사
            foreach (var ps in particleSystems)
            {
                if (ps == null)
                {
                    continue;
                }

                // subEmitters까지 포함해서 아직 살아있는 파티클이 있다면 대기
                if (ps.IsAlive(true))
                {
                    return;
                }
            }

            // 여기까지 왔다는 것은 모든 파티클이 종료된 상태
            Destroy(gameObject);
        }
    }
}

