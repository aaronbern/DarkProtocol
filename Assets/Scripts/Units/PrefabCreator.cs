using UnityEngine;
using UnityEditor;

public class PrefabCreator : MonoBehaviour
{
    [Header("Effect Generators")]
    public PlayerUnitEffectGenerator playerEffectGenerator;
    public EnemyUnitEffectGenerator enemyEffectGenerator;
    
    [Header("Output Settings")]
    public string prefabFolderPath = "Assets/Prefabs";
    public string playerPrefabName = "PlayerUnitEffect";
    public string enemyPrefabName = "EnemyUnitEffect";
    
    public void CreateAndSavePlayerPrefab()
    {
        if (playerEffectGenerator == null)
        {
            Debug.LogError("Player Effect Generator reference is missing!");
            return;
        }
        
        // Create the effect GameObject
        GameObject effectObject = playerEffectGenerator.CreatePlayerUnitEffectPrefab();
        
        // Save the prefab
        SavePrefab(effectObject, playerPrefabName);
    }
    
    public void CreateAndSaveEnemyPrefab()
    {
        if (enemyEffectGenerator == null)
        {
            Debug.LogError("Enemy Effect Generator reference is missing!");
            return;
        }
        
        // Create the effect GameObject
        GameObject effectObject = enemyEffectGenerator.CreateEnemyUnitEffectPrefab();
        
        // Save the prefab
        SavePrefab(effectObject, enemyPrefabName);
    }
    
    private void SavePrefab(GameObject effectObject, string prefabName)
    {
        // Make sure the prefabs folder exists
        if (!AssetDatabase.IsValidFolder(prefabFolderPath))
        {
            string[] folderPath = prefabFolderPath.Split('/');
            string currentPath = folderPath[0];
            
            for (int i = 1; i < folderPath.Length; i++)
            {
                string newFolder = folderPath[i];
                if (!AssetDatabase.IsValidFolder($"{currentPath}/{newFolder}"))
                {
                    AssetDatabase.CreateFolder(currentPath, newFolder);
                }
                currentPath += $"/{newFolder}";
            }
        }
        
        // Save the prefab
        string localPath = $"{prefabFolderPath}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(effectObject, localPath, out bool success);
        
        if (success)
        {
            Debug.Log($"Prefab '{prefabName}' created successfully at: {localPath}");
            
            // Destroy the temporary GameObject
            DestroyImmediate(effectObject);
        }
        else
        {
            Debug.LogError($"Failed to create prefab '{prefabName}'!");
        }
    }
    
    public void CreateAndSaveAllPrefabs()
    {
        CreateAndSavePlayerPrefab();
        CreateAndSaveEnemyPrefab();
    }
}