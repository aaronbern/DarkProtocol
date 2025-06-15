using UnityEngine;
using UnityEngine.EventSystems;
using DarkProtocol.Cards;

namespace DarkProtocol.UI
{
    /// <summary>
    /// Component that triggers tooltips when hovering over UI elements
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Tooltip Type")]
        [SerializeField] public TooltipType tooltipType = TooltipType.Simple;

        [Header("Simple Tooltip")]
        [SerializeField] public string simpleText = "Tooltip text";

        [Header("References")]
        [SerializeField] public Unit unitReference;
        [SerializeField] public CardData cardDataReference;
        [SerializeField] public StatusEffectData statusEffectReference;

        public enum TooltipType
        {
            Simple,
            Unit,
            Card,
            StatusEffect
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSystem.Hide();
        }

        public void ShowTooltip()
        {
            switch (tooltipType)
            {
                case TooltipType.Simple:
                    TooltipSystem.ShowSimple(simpleText);
                    break;
                case TooltipType.Unit:
                    if (unitReference != null)
                        TooltipSystem.ShowUnit(unitReference);
                    break;
                case TooltipType.Card:
                    if (cardDataReference != null)
                        TooltipSystem.ShowCard(cardDataReference);
                    break;
                case TooltipType.StatusEffect:
                    if (statusEffectReference != null)
                        TooltipSystem.ShowStatusEffect(statusEffectReference);
                    break;
            }
        }

        // Methods to set tooltip data dynamically
        public void SetSimpleTooltip(string text)
        {
            tooltipType = TooltipType.Simple;
            simpleText = text;
        }

        public void SetUnit(Unit unit)
        {
            tooltipType = TooltipType.Unit;
            unitReference = unit;
        }

        public void SetCard(CardData cardData)
        {
            tooltipType = TooltipType.Card;
            cardDataReference = cardData;
        }

        public void SetStatusEffect(StatusEffectData effectData)
        {
            tooltipType = TooltipType.StatusEffect;
            statusEffectReference = effectData;
        }

        public void OnDestroy()
        {
            // Hide tooltip if this object is being destroyed
            TooltipSystem.Hide();
        }
    }
}