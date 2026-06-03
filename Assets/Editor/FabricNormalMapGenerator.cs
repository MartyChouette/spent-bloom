using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool that generates a tileable fine-fabric normal map for breaking
/// up specular reflections on low-poly suit geometry.
/// Menu: Tools > Generate Fabric Normal Map
/// </summary>
public static class FabricNormalMapGenerator
{
    [MenuItem("Tools/Generate Fabric Normal Map")]
    private static void Generate()
    {
        int size = 512;
        float strength = 0.4f;

        // Generate heightmap from layered noise
        float[,] height = new float[size, size];

        // Layer 1: Fine weave pattern (high frequency)
        AddNoise(height, size, scale: 40f, amplitude: 0.5f, seed: 0);
        // Layer 2: Micro grain (very high frequency)
        AddNoise(height, size, scale: 80f, amplitude: 0.3f, seed: 100);
        // Layer 3: Subtle large-scale variation
        AddNoise(height, size, scale: 10f, amplitude: 0.2f, seed: 200);

        // Convert heightmap to normal map
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Sobel-style derivatives with wrapping for tileability
                float left  = height[(x - 1 + size) % size, y];
                float right = height[(x + 1) % size, y];
                float down  = height[x, (y - 1 + size) % size];
                float up    = height[x, (y + 1) % size];

                float dx = (left - right) * strength;
                float dy = (down - up) * strength;

                // Tangent-space normal
                Vector3 normal = new Vector3(dx, dy, 1f).normalized;

                // Encode to 0-1 range
                tex.SetPixel(x, y, new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f,
                    1f));
            }
        }

        tex.Apply();

        // Save
        string folder = "Assets/ArtAssets/3dModels/Nema_Models";
        string path = $"{folder}/suit_fabric_normal.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.Refresh();

        // Set import settings to Normal Map
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        Debug.Log($"[FabricNormalMap] Generated {path}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    }

    private static void AddNoise(float[,] height, int size, float scale, float amplitude, int seed)
    {
        float offsetX = seed * 17.3f;
        float offsetY = seed * 31.7f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / size * scale + offsetX;
                float ny = (float)y / size * scale + offsetY;
                height[x, y] += Mathf.PerlinNoise(nx, ny) * amplitude;
            }
        }
    }
}
