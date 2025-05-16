using UnityEngine;

namespace DarkProtocol.Grid.Utils
{
    /// <summary>
    /// Utilities for debugging the grid system
    /// </summary>
    public class DebugUtilities : MonoBehaviour
    {
        [Header("Unit Testing")]
        [SerializeField] private int movementPointsToSet = 5;
        
        /// <summary>
        /// Reset movement points for all units
        /// </summary>
        [ContextMenu("Reset All Units Movement Points")]
        public void ResetAllUnitsMovementPoints()
        {
            Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            Debug.Log($"Found {units.Length} units to reset movement points");
    
            foreach (Unit unit in units)
            {
                // Use reflection to access private methods if needed
                var resetMethodInfo = unit.GetType().GetMethod("ResetMovementPoints", 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                if (resetMethodInfo != null)
                {
                    resetMethodInfo.Invoke(unit, null);
                    Debug.Log($"Reset movement points for {unit.name}");
                }
                else
                {
                    // Fallback to setting points directly if possible
                    var setPointsMethodInfo = unit.GetType().GetMethod("SetMovementPoints", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (setPointsMethodInfo != null)
                    {
                        setPointsMethodInfo.Invoke(unit, new object[] { movementPointsToSet });
                        Debug.Log($"Set movement points for {unit.name} to {movementPointsToSet}");
                    }
                    else
                    {
                        Debug.LogWarning($"Could not reset movement points for {unit.name} - method not found");
                    }
                }
            }
        }
        
        /// <summary>
        /// Force register all units with the grid
        /// </summary>
        [ContextMenu("Force Register All Units")]
        public void ForceRegisterAllUnits()
        {
            Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            Debug.Log($"Found {units.Length} units to register");

            foreach (Unit unit in units)
            {
                GridServices.Units.RegisterUnitAtPosition(unit);
                Debug.Log($"Force registered {unit.name} with grid");
            }
        }
        
        /// <summary>
        /// Force player turn in GameManager
        /// </summary>
        [ContextMenu("Force Player Turn")]
        public void ForcePlayerTurn()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartPlayerTurn();
                Debug.Log("Forced player turn in GameManager");
            }
            else
            {
                Debug.LogError("GameManager.Instance is null! Cannot force player turn.");
            }
        }
    }
}