using UnityEngine;
using DarkProtocol.Units;
using UnityEngine.Rendering.Universal;
using UnityEditor;

/// <summary>
/// Creates an enemy unit effect prefab with a glowing red hexagonal pattern.
/// This prefab is used for hover and selection highlighting.
/// </summary>
public class EnemyUnitEffectGenerator : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float effectRadius = 0.65f;
    [SerializeField] private float ringThickness = 0.06f;
    [SerializeField] private float effectHeight = 0.02f;
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private float pulseIntensity = 0.15f;
    [SerializeField] private float animationSpeed = 1.2f;
    
    /// <summary>
    /// Creates the enemy unit effect prefab
    /// </summary>
    public GameObject CreateEnemyUnitEffectPrefab()
    {
        // Create base effect object
        GameObject effectObject = new GameObject("EnemyUnitEffect");
        UnitHoverEffect hoverEffect = effectObject.AddComponent<UnitHoverEffect>();
        
        // Create hexagonal pattern
        GameObject hexPattern = CreateHexagonalPattern(effectRadius, ringThickness, effectHeight);
        hexPattern.transform.SetParent(effectObject.transform, false);
        
        // Create danger glow
        GameObject glowObject = CreateGlowDisc(effectRadius * 0.95f, 0.01f);
        glowObject.transform.SetParent(effectObject.transform, false);
        glowObject.transform.localPosition = new Vector3(0, 0.005f, 0);
        
        // Configure hover effect
        SerializedObject serializedEffect = new SerializedObject(hoverEffect);
        serializedEffect.FindProperty("baseColor").colorValue = enemyColor;
        serializedEffect.FindProperty("enemyColor").colorValue = enemyColor;
        serializedEffect.FindProperty("animate").boolValue = true;
        serializedEffect.FindProperty("animationSpeed").floatValue = animationSpeed;
        serializedEffect.FindProperty("pulseIntensity").floatValue = pulseIntensity;
        serializedEffect.FindProperty("rotationSpeed").floatValue = -12f; // Counter-clockwise rotation
        serializedEffect.FindProperty("fadeEffect").boolValue = true;
        serializedEffect.FindProperty("fadeDuration").floatValue = 0.2f;
        serializedEffect.ApplyModifiedProperties();
        
        return effectObject;
    }
    
    private GameObject CreateHexagonalPattern(float radius, float thickness, float height)
    {
        GameObject hexObject = new GameObject("EnemyHexPattern");
        MeshFilter meshFilter = hexObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = hexObject.AddComponent<MeshRenderer>();
        
        // Create the hexagonal pattern mesh
        Mesh mesh = new Mesh();
        
        // Create a hexagon with 6 segments
        int segments = 6;
        
        // Create vertices
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uvs = new Vector2[segments * 2];
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
            
            // Inner point
            vertices[i * 2] = new Vector3(
                Mathf.Cos(angle1) * (radius - thickness),
                height / 2,
                Mathf.Sin(angle1) * (radius - thickness)
            );
            
            // Outer point
            vertices[i * 2 + 1] = new Vector3(
                Mathf.Cos(angle1) * radius,
                height / 2,
                Mathf.Sin(angle1) * radius
            );
            
            // UVs
            uvs[i * 2] = new Vector2((float)i / segments, 0);
            uvs[i * 2 + 1] = new Vector2((float)i / segments, 1);
            
            // Two triangles for each segment to create spokes
            int nextI = (i + 1) % segments;
            
            // First triangle (connecting to center)
            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = nextI * 2;
            triangles[i * 6 + 2] = i * 2 + 1;
            
            // Second triangle (completing the spoke)
            triangles[i * 6 + 3] = i * 2 + 1;
            triangles[i * 6 + 4] = nextI * 2;
            triangles[i * 6 + 5] = nextI * 2 + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        // Create material with glow effect
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", enemyColor * 2f);
        material.SetColor("_BaseColor", enemyColor);
        material.SetFloat("_Smoothness", 0.7f);
        
        meshRenderer.material = material;
        
        return hexObject;
    }
    
    private GameObject CreateGlowDisc(float radius, float height)
    {
        GameObject discObject = new GameObject("EnemyGlow");
        MeshFilter meshFilter = discObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = discObject.AddComponent<MeshRenderer>();
        
        // Create disc mesh
        int segments = 32;
        Mesh mesh = new Mesh();
        
        // Create vertices
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        Vector2[] uvs = new Vector2[segments + 1];
        
        // Center vertex
        vertices[0] = new Vector3(0, height / 2, 0);
        uvs[0] = new Vector2(0.5f, 0.5f);
        
        // Outer vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                height / 2,
                Mathf.Sin(angle) * radius
            );
            
            uvs[i + 1] = new Vector2(
                Mathf.Cos(angle) * 0.5f + 0.5f,
                Mathf.Sin(angle) * 0.5f + 0.5f
            );
            
            // Triangles
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        // Create material with glow effect
        Material material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", enemyColor);
        material.SetColor("_BaseColor", new Color(enemyColor.r, enemyColor.g, enemyColor.b, 0.25f));
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0);  // SrcAlpha, OneMinusSrcAlpha
        material.SetFloat("_ZWrite", 0); // Don't write to depth buffer
        material.renderQueue = 3000;     // Transparent queue
        
        meshRenderer.material = material;
        
        return discObject;
    }
}