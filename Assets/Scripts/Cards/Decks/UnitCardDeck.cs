using System.Collections.Generic;
using UnityEngine;

namespace DarkProtocol.Cards
{
    /// <summary>
    /// Defines the card deck for a specific unit
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDeck", menuName = "Dark Protocol/Cards/Unit Card Deck")]
    public class UnitCardDeck : ScriptableObject
    {
        [Header("Deck Configuration")]
        [Tooltip("Name of this deck")]
        [SerializeField] private string deckName;
        
        [Tooltip("Description of this deck")]
        [TextArea(3, 5)]
        [SerializeField] private string deckDescription;
        
        [Tooltip("Specialized cards for this unit")]
        [SerializeField] private List<CardData> specializedCards = new List<CardData>();
        
        [Tooltip("Number of copies of each common card to include")]
        [Range(1, 3)]
        [SerializeField] private int commonCardCopies = 1;
        
        [Tooltip("Maximum number of specialized cards allowed")]
        [SerializeField] private int maxSpecializedCards = 6;
        
        [Header("Deck Stats")]
        [Tooltip("Minimum action point cost of cards in this deck")]
        [SerializeField] private int minActionPointCost = 0;
        
        [Tooltip("Maximum action point cost of cards in this deck")]
        [SerializeField] private int maxActionPointCost = 3;
        
        [Tooltip("Average action point cost of this deck (calculated)")]
        [SerializeField] private float averageActionPointCost = 0f;
        
        // Public properties
        public string DeckName => deckName;
        public string DeckDescription => deckDescription;
        public List<CardData> SpecializedCards => specializedCards;
        public int CommonCardCopies => commonCardCopies;
        public int MaxSpecializedCards => maxSpecializedCards;
        public int MinActionPointCost => minActionPointCost;
        public int MaxActionPointCost => maxActionPointCost;
        public float AverageActionPointCost => averageActionPointCost;
        
        #region Editor Validation
        private void OnValidate()
        {
            // Calculate stats
            CalculateDeckStats();
            
            // Validate specialized cards count
            if (specializedCards.Count > maxSpecializedCards)
            {
                Debug.LogWarning($"Deck {deckName} has {specializedCards.Count} specialized cards, but the limit is {maxSpecializedCards}.");
            }
        }
        
        /// <summary>
        /// Calculate deck statistics
        /// </summary>
        private void CalculateDeckStats()
        {
            if (specializedCards.Count == 0)
            {
                minActionPointCost = 0;
                maxActionPointCost = 0;
                averageActionPointCost = 0f;
                return;
            }
            
            // Initialize with extreme values
            minActionPointCost = int.MaxValue;
            maxActionPointCost = int.MinValue;
            int totalCost = 0;
            
            // Calculate min, max, and total cost
            foreach (CardData card in specializedCards)
            {
                if (card != null)
                {
                    int cost = card.ActionPointCost;
                    minActionPointCost = Mathf.Min(minActionPointCost, cost);
                    maxActionPointCost = Mathf.Max(maxActionPointCost, cost);
                    totalCost += cost;
                }
            }
            
            // Calculate average
            averageActionPointCost = specializedCards.Count > 0 ? 
                (float)totalCost / specializedCards.Count : 0f;
        }
        #endregion
    }
}