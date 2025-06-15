using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.Cards;  // Added for StatusEffectData and StatusEffectType

namespace DarkProtocol.UI
{
    /// <summary>
    /// Tooltip component for displaying status effect information
    /// </summary>
    public class StatusEffectTooltip : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public TextMeshProUGUI effectNameText;
        [SerializeField] public TextMeshProUGUI effectTypeText;
        [SerializeField] public TextMeshProUGUI effectDescriptionText;
        [SerializeField] public TextMeshProUGUI durationText;
        [SerializeField] public Image effectIcon;
        [SerializeField] public Image rarityBorder;
        [SerializeField] public GameObject stackableIndicator;
        [SerializeField] public TextMeshProUGUI stackText;

        [Header("Type Colors")]
        [SerializeField] private Color buffColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color debuffColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color neutralColor = new Color(0.8f, 0.8f, 0.2f);

        public void SetEffectData(StatusEffectData effectData)
        {
            if (effectData == null) return;

            // Set basic info
            if (effectNameText != null)
                effectNameText.text = effectData.EffectName;

            if (effectTypeText != null)
            {
                effectTypeText.text = effectData.EffectType.ToString();
                effectTypeText.color = GetTypeColor(effectData.EffectType);
            }

            if (effectDescriptionText != null)
            {
                string description = effectData.EffectDescription;
                // Replace placeholders if any
                if (effectData.EffectValue != 0)
                {
                    description = description.Replace("{value}", Mathf.Abs(effectData.EffectValue).ToString());
                    description = description.Replace("{damage}", effectData.EffectValue.ToString());
                }
                if (effectData.EffectValuePerTurn != 0)
                {
                    description = description.Replace("{valuePerTurn}", effectData.EffectValuePerTurn.ToString());
                }
                effectDescriptionText.text = description;
            }

            if (durationText != null)
            {
                // Duration is managed by the ActiveStatusEffect, not the data
                // Hide for now since it's runtime data
                durationText.gameObject.SetActive(false);
            }

            if (effectIcon != null && effectData.EffectIcon != null)
            {
                effectIcon.sprite = effectData.EffectIcon;
                effectIcon.color = effectData.EffectColor;
                effectIcon.gameObject.SetActive(true);
            }
            else if (effectIcon != null)
            {
                effectIcon.gameObject.SetActive(false);
            }

            // Handle stackable effects
            if (stackableIndicator != null)
            {
                stackableIndicator.SetActive(effectData.IsStackable);
                if (stackText != null && effectData.IsStackable)
                {
                    stackText.text = $"Max Stacks: {effectData.MaxStackCount}";
                }
            }

            // Set border color based on type
            if (rarityBorder != null)
            {
                rarityBorder.color = GetTypeColor(effectData.EffectType);
            }
        }

        private Color GetTypeColor(StatusEffectType effectType)
        {
            switch (effectType)
            {
                case StatusEffectType.HealOverTime:
                case StatusEffectType.StatBuff:
                case StatusEffectType.Shield:
                    return buffColor;

                case StatusEffectType.DamageOverTime:
                case StatusEffectType.StatDebuff:
                case StatusEffectType.Stun:
                case StatusEffectType.Root:
                case StatusEffectType.Confusion:
                    return debuffColor;

                case StatusEffectType.Stealth:
                case StatusEffectType.Taunt:
                case StatusEffectType.Custom:
                default:
                    return neutralColor;
            }
        }
    }
}