using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;
using Game.Interfaces;
using Game.Services;

namespace Game.UI
{
    public class ManaHudUI : MonoBehaviour
    {
        [Header("Player Mana UI")]
        [SerializeField] private TextMeshProUGUI playerManaText;
        [SerializeField] private Slider playerManaSlider;

        [Header("Enemy Mana UI")]
        [SerializeField] private TextMeshProUGUI enemyManaText;
        [SerializeField] private Slider enemyManaSlider;

        private IResourceManager resourceManager;

        private void Start()
        {
            resourceManager = ServiceLocator.Get<IResourceManager>();

            if (resourceManager == null)
            {
                Debug.LogError("[ManaHudUI] IResourceManager not found. Ensure GameInitializer has registered it before ManaHudUI.Start.");
                enabled = false;
                return;
            }

            resourceManager.OnPlayerResourcesChanged += HandlePlayerManaChanged;
            resourceManager.OnEnemyResourcesChanged += HandleEnemyManaChanged;

            HandlePlayerManaChanged(new ManaData
            {
                currentMana = resourceManager.PlayerMana,
                maxMana = resourceManager.PlayerMaxMana
            });

            HandleEnemyManaChanged(new ManaData
            {
                currentMana = resourceManager.EnemyMana,
                maxMana = resourceManager.EnemyMaxMana
            });
        }

        private void OnDestroy()
        {
            if (resourceManager == null) return;

            resourceManager.OnPlayerResourcesChanged -= HandlePlayerManaChanged;
            resourceManager.OnEnemyResourcesChanged -= HandleEnemyManaChanged;
        }

        private void HandlePlayerManaChanged(ManaData mana)
        {
            if (playerManaText != null)
            {
                playerManaText.text = $"{mana.currentMana}/{mana.maxMana}";
            }

            if (playerManaSlider != null)
            {
                playerManaSlider.maxValue = mana.maxMana;
                playerManaSlider.value = mana.currentMana;
            }
        }

        private void HandleEnemyManaChanged(ManaData mana)
        {
            if (enemyManaText != null)
            {
                enemyManaText.text = $"{mana.currentMana}/{mana.maxMana}";
            }

            if (enemyManaSlider != null)
            {
                enemyManaSlider.maxValue = mana.maxMana;
                enemyManaSlider.value = mana.currentMana;
            }
        }
    }
}

