#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Put this file in Assets/Editor. It is a menu tool, not a scene component.
// Creates reusable UI sprites; assign them to Image components with Type = Sliced.
public static class KitchenCostRoundedSpriteBuilder
{
    private const string MenuPath = "Tools/KitchenCost/Create Rounded UI Sprites";
    private const string AssetFolder = "Assets/Resources/KitchenCostUI";
    private const int Size = 256;

    [MenuItem(MenuPath, false, 200)]
    private static void CreateSprites()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            string directory = Path.Combine(Application.dataPath, "Resources", "KitchenCostUI");
            Directory.CreateDirectory(directory);

            WriteSpriteIfMissing(Path.Combine(directory, "RoundedCard.png"), 48f, 0f);
            WriteSpriteIfMissing(Path.Combine(directory, "RoundedControl.png"), 32f, 0f);
            WriteSpriteIfMissing(Path.Combine(directory, "RoundedOutline.png"), 32f, 3f);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureSprite(AssetFolder + "/RoundedCard.png", 56f);
            ConfigureSprite(AssetFolder + "/RoundedControl.png", 40f);
            ConfigureSprite(AssetFolder + "/RoundedOutline.png", 40f);

            Sprite card = AssetDatabase.LoadAssetAtPath<Sprite>(AssetFolder + "/RoundedCard.png");
            Selection.activeObject = card;
            EditorGUIUtility.PingObject(card);
            EditorUtility.DisplayDialog("KitchenCost rounded sprites ready",
                "Three sprites are ready in Assets/Resources/KitchenCostUI.\n\n" +
                "Assign RoundedCard to the cost card, RoundedControl to filled buttons, " +
                "and RoundedOutline to outlined buttons. Set each UI Image Type to Sliced.", "OK");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Could not create KitchenCost sprites",
                "Read the first error in the Console.\n\n" + error.Message, "OK");
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool CanCreateSprites()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
    }

    private static void WriteSpriteIfMissing(string path, float radius, float outlineWidth)
    {
        // Re-running the tool keeps existing texture bytes and their asset GUIDs.
        if (File.Exists(path))
            return;

        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        try
        {
            var pixels = new Color32[Size * Size];
            float centre = Size * 0.5f;
            float halfExtent = centre - 2f; // Two transparent edge pixels for filtering.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Signed distance to a rounded rectangle in texture pixels.
                    float qx = Mathf.Abs(x + 0.5f - centre) - (halfExtent - radius);
                    float qy = Mathf.Abs(y + 0.5f - centre) - (halfExtent - radius);
                    float outsideX = Mathf.Max(qx, 0f);
                    float outsideY = Mathf.Max(qy, 0f);
                    float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY)
                        + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float alpha = Mathf.Clamp01(0.5f - distance);
                    if (outlineWidth > 0f)
                        alpha -= Mathf.Clamp01(0.5f - (distance + outlineWidth));
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, a);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static void ConfigureSprite(string path, float border)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Texture importer not found for " + path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = Size;
        importer.isReadable = false;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
#endif
