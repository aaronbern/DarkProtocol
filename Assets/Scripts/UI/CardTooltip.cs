// CardTooltip.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    public class CardTooltip : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public TextMeshProUGUI cardNameText;
        [SerializeField] public TextMeshProUGUI cardTypeText;
        [SerializeField] public TextMeshProUGUI cardDescriptionText;
        [SerializeField] public TextMeshProUGUI apCostText;
        [SerializeField] public TextMeshProUGUI damageText;
        [SerializeField] public TextMeshProUGUI rangeText;
        [SerializeField] public Image rarityBorder;
        [SerializeField] public Transform tagContainer;
        [SerializeField] public GameObject tagPrefab;

        [Header("Rarity Colors")]
        [SerializeField] public Color commonColor = Color.gray;
        [SerializeField] public Color uncommonColor = Color.green;
        [SerializeField] public Color rareColor = Color.blue;
        [SerializeField] public Color epicColor = new Color(0.5f, 0, 0.5f);
        [SerializeField] public Color legendaryColor = new Color(1f, 0.5f, 0);

        public void SetCardData(CardData cardData)
        {
            if (cardData == null) return;

            // Set basic info
            if (cardNameText != null)
                cardNameText.text = cardData.CardName;

            if (cardTypeText != null)
                cardTypeText.text = cardData.Category.ToString();

            if (cardDescriptionText != null)
            {
                string description = cardData.CardDescription;
                // Replace placeholders
                description = description.Replace("{damage}", cardData.BaseDamage.ToString());
                description = description.Replace("{healing}", cardData.BaseHealing.ToString());
                description = description.Replace("{range}", cardData.EffectRange.ToString());
                description = description.Replace("{duration}", cardData.EffectDuration.ToString());

                cardDescriptionText.text = description;
            }

            if (apCostText != null)
                apCostText.text = $"AP: {cardData.ActionPointCost}";

            if (damageText != null)
            {
                damageText.gameObject.SetActive(cardData.BaseDamage > 0);
                damageText.text = $"Damage: {cardData.BaseDamage}";
            }

            if (rangeText != null)
            {
                rangeText.gameObject.SetActive(cardData.EffectRange > 0);
                rangeText.text = $"Range: {cardData.EffectRange}";
            }

            // Set rarity color
            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(cardData.Rarity);
            }

            // TODO: Add synergy tags when implemented
        }

        public Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return commonColor;
                case CardRarity.Uncommon: return uncommonColor;
                case CardRarity.Rare: return rareColor;
                case CardRarity.Epic: return epicColor;
                case CardRarity.Legendary: return legendaryColor;
                default: return Color.white;
            }
        }
    }
}