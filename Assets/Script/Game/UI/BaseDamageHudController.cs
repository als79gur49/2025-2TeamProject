using UnityEngine;
using TMPro;
using Game.Core;
using Game.Services;
using Game.Data;

namespace Game.UI
{
    /// <summary>
    /// DamageDisplayEventChannelSO를 구독하여
    /// Base에 들어온 데미지를 화면 HUD에 숫자로 표시하는 컨트롤러.
    ///
    /// - 데이터 출발점: HealthComponent -> DamageDisplayRepository -> DamageDisplayEventChannelSO
    /// - 이 컨트롤러는 EventChannel을 구독해서 Base에 가까운 데미지만 골라서 HUD에 표기
    /// </summary>
    public class BaseDamageHudController : MonoBehaviour
    {
        [Header("Event Channel")]
        [SerializeField]
        [Tooltip("데미지 표시용 EventChannel (GameInitializer에서 사용하는 것과 동일한 SO)")]
        private DamageDisplayEventChannelSO damageDisplayEventChannel;

        [Header("HUD Texts")]
        [SerializeField]
        [Tooltip("플레이어 Base 데미지 숫자 텍스트")]
        private TextMeshProUGUI playerBaseDamageText;

        [SerializeField]
        [Tooltip("적 Base 데미지 숫자 텍스트")]
        private TextMeshProUGUI enemyBaseDamageText;

        [Header("Display Settings")]
        [SerializeField]
        [Tooltip("데미지 숫자를 표시할 시간 (초)")]
        private float displayDuration = 0.6f;

        [SerializeField]
        [Tooltip("이 거리 안에서 발생한 데미지를 Base에 맞은 것으로 간주 (Unity Units)")]
        private float baseHitRadius = 2.0f;

        private float playerHideTime;
        private float enemyHideTime;

        private void OnEnable()
        {
            if (damageDisplayEventChannel != null)
            {
                damageDisplayEventChannel.Subscribe(OnDamageDisplay);
            }

            HideAll();
        }

        private void OnDisable()
        {
            if (damageDisplayEventChannel != null)
            {
                damageDisplayEventChannel.Unsubscribe(OnDamageDisplay);
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            if (playerBaseDamageText != null &&
                playerBaseDamageText.gameObject.activeSelf &&
                now >= playerHideTime)
            {
                playerBaseDamageText.gameObject.SetActive(false);
            }

            if (enemyBaseDamageText != null &&
                enemyBaseDamageText.gameObject.activeSelf &&
                now >= enemyHideTime)
            {
                enemyBaseDamageText.gameObject.SetActive(false);
            }
        }

        private void HideAll()
        {
            if (playerBaseDamageText != null)
            {
                playerBaseDamageText.gameObject.SetActive(false);
            }

            if (enemyBaseDamageText != null)
            {
                enemyBaseDamageText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// DamageDisplayEventChannelSO에서 날아오는 데미지 이벤트 핸들러
        /// </summary>
        private void OnDamageDisplay(DamageDisplayData data)
        {
            if (!ServiceLocator.TryGet<IBaseManager>(out var baseManager))
            {
                return;
            }

            var playerBase = baseManager.PlayerBase;
            var enemyBase = baseManager.EnemyBase;

            if (playerBase == null && enemyBase == null)
            {
                return;
            }

            Vector3 worldPos = data.WorldPosition;
            float bestDistSq = float.MaxValue;
            bool isPlayer = false;
            bool isEnemy = false;

            if (playerBase != null)
            {
                float d = (worldPos - playerBase.transform.position).sqrMagnitude;
                bestDistSq = d;
                isPlayer = true;
            }

            if (enemyBase != null)
            {
                float d = (worldPos - enemyBase.transform.position).sqrMagnitude;

                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    isPlayer = false;
                    isEnemy = true;
                }
            }

            float radiusSq = baseHitRadius * baseHitRadius;
            if (bestDistSq > radiusSq)
            {
                // Base와 충분히 가깝지 않으면 무시 (유닛 데미지로 간주)
                return;
            }

            if (isPlayer)
            {
                ShowPlayerDamage(data.Amount);
            }
            else if (isEnemy)
            {
                ShowEnemyDamage(data.Amount);
            }
        }

        private void ShowPlayerDamage(int amount)
        {
            if (playerBaseDamageText == null || amount <= 0)
            {
                return;
            }

            playerBaseDamageText.text = $"-{amount}";
            playerBaseDamageText.gameObject.SetActive(true);
            playerHideTime = Time.unscaledTime + displayDuration;
        }

        private void ShowEnemyDamage(int amount)
        {
            if (enemyBaseDamageText == null || amount <= 0)
            {
                return;
            }

            enemyBaseDamageText.text = $"-{amount}";
            enemyBaseDamageText.gameObject.SetActive(true);
            enemyHideTime = Time.unscaledTime + displayDuration;
        }

        private void OnValidate()
        {
            displayDuration = Mathf.Max(0.1f, displayDuration);
            baseHitRadius = Mathf.Max(0.1f, baseHitRadius);
        }
    }
}

