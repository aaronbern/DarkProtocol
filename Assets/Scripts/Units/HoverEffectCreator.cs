#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class HoverEffectCreator
{
    [MenuItem("Tools/Create Default Hover Effect")]
    public static void CreateDefaultHoverEffect()
    {
        GameObject root = new GameObject("DefaultHoverEffect");
        MeshFilter meshFilter = root.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();

        // Create the ring mesh
        meshFilter.sharedMesh = CreateRingMesh(0.6f, 0.08f);

        // Create URP unlit emissive material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(0.3f, 0.7f, 1f, 1f); // Cyan-blue
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", mat.color);
        mat.renderQueue = 3000;
        mat.SetFloat("_Surface", 1);  // Transparent
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_ZWrite", 0);
        mat.SetFloat("_Cull", 2);
        mat.EnableKeyword("_ALPHABLEND_ON");

        meshRenderer.sharedMaterial = mat;

        // Add the hover effect logic
        root.AddComponent<DarkProtocol.Units.UnitHoverEffect>();

        // Save as prefab
        string path = "Assets/Prefabs/UI/HoverEffects/DefaultHoverEffect.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI/HoverEffects");
        PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);

        Debug.Log("Default hover effect created at: " + path);
    }

    private static Mesh CreateRingMesh(float radius, float thickness)
    {
        int segments = 64;
        float inner = radius - thickness / 2f;
        float outer = radius + thickness / 2f;

        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float nextAngle = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector3 innerVertex = new Vector3(Mathf.Cos(angle) * inner, 0, Mathf.Sin(angle) * inner);
            Vector3 outerVertex = new Vector3(Mathf.Cos(angle) * outer, 0, Mathf.Sin(angle) * outer);
            Vector3 innerNext = new Vector3(Mathf.Cos(nextAngle) * inner, 0, Mathf.Sin(nextAngle) * inner);
            Vector3 outerNext = new Vector3(Mathf.Cos(nextAngle) * outer, 0, Mathf.Sin(nextAngle) * outer);

            vertices[i * 2] = innerVertex;
            vertices[i * 2 + 1] = outerVertex;

            int tri = i * 6;
            int v = i * 2;
            int vNext = (i + 1) * 2 % (segments * 2);

            triangles[tri] = v;
            triangles[tri + 1] = vNext + 1;
            triangles[tri + 2] = vNext;

            triangles[tri + 3] = v;
            triangles[tri + 4] = v + 1;
            triangles[tri + 5] = vNext + 1;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
#endif
