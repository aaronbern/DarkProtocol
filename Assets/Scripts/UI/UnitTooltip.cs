// UnitTooltip.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    public class UnitTooltip : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public TextMeshProUGUI unitNameText;
        [SerializeField] public TextMeshProUGUI unitClassText;
        [SerializeField] public TextMeshProUGUI healthText;
        [SerializeField] public TextMeshProUGUI apText;
        [SerializeField] public TextMeshProUGUI mpText;
        [SerializeField] public Image healthBar;
        [SerializeField] public Image apBar;
        [SerializeField] public Image mpBar;
        [SerializeField] public Transform statusEffectContainer;
        [SerializeField] public GameObject statusEffectIconPrefab;

        public void SetUnit(Unit unit)
        {
            if (unit == null) return;

            // Set basic info
            if (unitNameText != null)
                unitNameText.text = unit.UnitName;

            // TODO: Add unit class when implemented
            if (unitClassText != null)
                unitClassText.text = "Soldier"; // Placeholder

            // Health
            if (healthText != null)
                healthText.text = $"{unit.CurrentHealth}/{unit.MaxHealth}";

            if (healthBar != null)
                healthBar.fillAmount = (float)unit.CurrentHealth / unit.MaxHealth;

            // Action Points
            if (apText != null)
                apText.text = $"{unit.CurrentActionPoints}/{unit.MaxActionPoints}";

            if (apBar != null)
                apBar.fillAmount = (float)unit.CurrentActionPoints / unit.MaxActionPoints;

            // Movement Points
            if (mpText != null)
                mpText.text = $"{unit.CurrentMovementPoints}/{unit.MaxMovementPoints}";

            if (mpBar != null)
                mpBar.fillAmount = (float)unit.CurrentMovementPoints / unit.MaxMovementPoints;

            // Status Effects
            DisplayStatusEffects(unit);
        }

        public void DisplayStatusEffects(Unit unit)
        {
            if (statusEffectContainer == null) return;

            // Clear existing
            foreach (Transform child in statusEffectContainer)
            {
                Destroy(child.gameObject);
            }

            // Get status effect manager
            StatusEffectManager statusManager = unit.GetComponent<StatusEffectManager>();
            if (statusManager == null) return;

            // Add status effect icons
            foreach (var effect in statusManager.GetAllActiveEffects())
            {
                if (statusEffectIconPrefab != null && effect.EffectData.EffectIcon != null)
                {
                    GameObject iconObj = Instantiate(statusEffectIconPrefab, statusEffectContainer);
                    Image iconImage = iconObj.GetComponent<Image>();

                    if (iconImage != null)
                    {
                        iconImage.sprite = effect.EffectData.EffectIcon;
                    }

                    // Add tooltip trigger for the icon
                    TooltipTrigger trigger = iconObj.AddComponent<TooltipTrigger>();
                    trigger.SetStatusEffect(effect.EffectData);
                }
            }
        }
    }
}