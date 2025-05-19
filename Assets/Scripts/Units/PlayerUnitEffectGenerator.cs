using UnityEngine;
using DarkProtocol.Units;
using UnityEngine.Rendering.Universal;
using UnityEditor;

/// <summary>
/// Creates a player unit effect prefab with a glowing blue ring.
/// This prefab is used for hover and selection highlighting.
/// </summary>
public class PlayerUnitEffectGenerator : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float ringRadius = 0.6f;
    [SerializeField] private float ringThickness = 0.08f;
    [SerializeField] private float ringHeight = 0.02f;
    [SerializeField] private Color playerColor = new Color(0.2f, 0.6f, 1f, 0.7f);
    [SerializeField] private float pulseIntensity = 0.2f;
    [SerializeField] private float animationSpeed = 1.5f;
    
    /// <summary>
    /// Creates the player unit effect prefab
    /// </summary>
    public GameObject CreatePlayerUnitEffectPrefab()
    {
        // Create base effect object
        GameObject effectObject = new GameObject("PlayerUnitEffect");
        UnitHoverEffect hoverEffect = effectObject.AddComponent<UnitHoverEffect>();
        
        // Create ring
        GameObject ringObject = CreateRingMesh(ringRadius, ringThickness, ringHeight);
        ringObject.transform.SetParent(effectObject.transform, false);
        
        // Create glow
        GameObject glowObject = CreateGlowDisc(ringRadius * 0.95f, 0.01f);
        glowObject.transform.SetParent(effectObject.transform, false);
        glowObject.transform.localPosition = new Vector3(0, 0.005f, 0);
        
        // Configure hover effect
        SerializedObject serializedEffect = new SerializedObject(hoverEffect);
        serializedEffect.FindProperty("baseColor").colorValue = playerColor;
        serializedEffect.FindProperty("playerColor").colorValue = playerColor;
        serializedEffect.FindProperty("animate").boolValue = true;
        serializedEffect.FindProperty("animationSpeed").floatValue = animationSpeed;
        serializedEffect.FindProperty("pulseIntensity").floatValue = pulseIntensity;
        serializedEffect.FindProperty("rotationSpeed").floatValue = 15f;
        serializedEffect.FindProperty("fadeEffect").boolValue = true;
        serializedEffect.FindProperty("fadeDuration").floatValue = 0.2f;
        serializedEffect.ApplyModifiedProperties();
        
        return effectObject;
    }
    
    private GameObject CreateRingMesh(float radius, float thickness, float height)
    {
        GameObject ringObject = new GameObject("PlayerRing");
        MeshFilter meshFilter = ringObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = ringObject.AddComponent<MeshRenderer>();
        
        // Create the ring mesh
        int segments = 32;
        float innerRadius = radius - thickness / 2;
        float outerRadius = radius + thickness / 2;
        
        Mesh mesh = new Mesh();
        
        // Create vertices
        Vector3[] vertices = new Vector3[segments * 4];
        int[] triangles = new int[segments * 6];
        Vector2[] uvs = new Vector2[segments * 4];
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
            
            // Inner vertices
            vertices[i * 4] = new Vector3(
                Mathf.Cos(angle1) * innerRadius,
                height / 2,
                Mathf.Sin(angle1) * innerRadius
            );
            
            vertices[i * 4 + 1] = new Vector3(
                Mathf.Cos(angle2) * innerRadius,
                height / 2,
                Mathf.Sin(angle2) * innerRadius
            );
            
            // Outer vertices
            vertices[i * 4 + 2] = new Vector3(
                Mathf.Cos(angle1) * outerRadius,
                height / 2,
                Mathf.Sin(angle1) * outerRadius
            );
            
            vertices[i * 4 + 3] = new Vector3(
                Mathf.Cos(angle2) * outerRadius,
                height / 2,
                Mathf.Sin(angle2) * outerRadius
            );
            
            // Triangles
            triangles[i * 6] = i * 4;
            triangles[i * 6 + 1] = i * 4 + 3;
            triangles[i * 6 + 2] = i * 4 + 1;
            
            triangles[i * 6 + 3] = i * 4;
            triangles[i * 6 + 4] = i * 4 + 2;
            triangles[i * 6 + 5] = i * 4 + 3;
            
            // UVs
            uvs[i * 4] = new Vector2((float)i / segments, 0);
            uvs[i * 4 + 1] = new Vector2((float)(i + 1) / segments, 0);
            uvs[i * 4 + 2] = new Vector2((float)i / segments, 1);
            uvs[i * 4 + 3] = new Vector2((float)(i + 1) / segments, 1);
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
        material.SetColor("_EmissionColor", playerColor * 2f);
        material.SetColor("_BaseColor", playerColor);
        material.SetFloat("_Smoothness", 0.8f);
        
        meshRenderer.material = material;
        
        return ringObject;
    }
    
    private GameObject CreateGlowDisc(float radius, float height)
    {
        GameObject discObject = new GameObject("PlayerGlow");
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
        material.SetColor("_EmissionColor", playerColor);
        material.SetColor("_BaseColor", new Color(playerColor.r, playerColor.g, playerColor.b, 0.3f));
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0);  // SrcAlpha, OneMinusSrcAlpha
        material.SetFloat("_ZWrite", 0); // Don't write to depth buffer
        material.renderQueue = 3000;     // Transparent queue
        
        meshRenderer.material = material;
        
        return discObject;
    }
}