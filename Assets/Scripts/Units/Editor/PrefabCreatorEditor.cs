using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrefabCreator))]
public class PrefabCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        PrefabCreator creator = (PrefabCreator)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Create Prefabs", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if(GUILayout.Button("Create Player Prefab", GUILayout.Height(30)))
        {
            creator.CreateAndSavePlayerPrefab();
        }
        
        if(GUILayout.Button("Create Enemy Prefab", GUILayout.Height(30)))
        {
            creator.CreateAndSaveEnemyPrefab();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if(GUILayout.Button("Create All Prefabs", GUILayout.Height(40)))
        {
            creator.CreateAndSaveAllPrefabs();
        }
    }
}