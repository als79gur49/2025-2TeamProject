using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 카드팩 정의 ScriptableObject
    /// 상점에서 판매되는 카드팩의 메타데이터와 드로우 규칙을 포함
    /// </summary>
    [CreateAssetMenu(fileName = "CardPack_", menuName = "Shop/Card Pack")]
    public class CardPackDefinition : ScriptableObject
    {
        [Serializable]
        public struct RarityGuarantee
        {
            public CardData.CardRarity rarity;
            public int minCount;
        }

        [Header("Identification")]
        [SerializeField] private string packId = "";
        [SerializeField] private string displayName = "New Card Pack";
        [SerializeField] private Sprite icon;
        [TextArea]
        [SerializeField] private string description = "";

        [Header("Pricing")]
        [SerializeField] private int basePrice = 100;

        [Header("Draw Settings")]
        [SerializeField] private CardPoolSet poolSet;
        [SerializeField] private CardRarityTable rarityTableOverride;
        [SerializeField] private int drawCount = 5;
        [SerializeField] private List<RarityGuarantee> rarityGuarantees = new List<RarityGuarantee>();

        public string PackId => string.IsNullOrEmpty(packId) ? name : packId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public string Description => description;
        public int BasePrice => basePrice;

        public CardPoolSet PoolSet => poolSet;
        public CardRarityTable RarityTableOverride => rarityTableOverride;
        public int DrawCount => drawCount;
        public IReadOnlyList<RarityGuarantee> RarityGuarantees => rarityGuarantees;

        /// <summary>
        /// 대표 레어리티 계산
        /// - 보장된 등급이 있는 경우, 가장 높은 등급 사용
        /// - 없으면 Common 반환
        /// </summary>
        public CardData.CardRarity GetRepresentativeRarity()
        {
            CardData.CardRarity highest = CardData.CardRarity.Common;
            bool found = false;

            if (rarityGuarantees != null)
            {
                foreach (var guarantee in rarityGuarantees)
                {
                    if (guarantee.minCount <= 0)
                        continue;

                    if (!found || guarantee.rarity > highest)
                    {
                        highest = guarantee.rarity;
                        found = true;
                    }
                }
            }

            return found ? highest : CardData.CardRarity.Common;
        }

        private void OnValidate()
        {
            if (drawCount < 0)
            {
                drawCount = 0;
            }

            int minTotal = 0;
            if (rarityGuarantees != null)
            {
                for (int i = 0; i < rarityGuarantees.Count; i++)
                {
                    var g = rarityGuarantees[i];
                    if (g.minCount < 0)
                    {
                        g.minCount = 0;
                        rarityGuarantees[i] = g;
                    }

                    minTotal += g.minCount;
                }
            }

            if (drawCount > 0 && minTotal > drawCount)
            {
                Debug.LogWarning($"[CardPackDefinition] Rarity guarantees ({minTotal}) exceed drawCount ({drawCount}) in pack '{PackId}'.");
            }
        }
    }
}

