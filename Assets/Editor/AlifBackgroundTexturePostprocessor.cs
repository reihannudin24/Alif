using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Setting import untuk gambar background/interior di Assets/Sprites/Backgrounds/.
    /// Beda dari karakter: art ini shading halus (bukan pixel art blocky), jadi pakai
    /// filter Bilinear (bukan Point) supaya nggak kelihatan pecah/bergerigi waktu di-scale.
    /// PPU 200 dipilih supaya proporsinya nyambung sama karakter (Assets/Sprites/Characters,
    /// PPU 256 dengan tinggi konten ~225px) — lihat AlifDemoSceneBuilder.BuildBackground().
    /// </summary>
    public class AlifBackgroundTexturePostprocessor : AssetPostprocessor
    {
        private const string BackgroundsFolder = "Assets/Sprites/Backgrounds/";

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace("\\", "/");
            if (!path.StartsWith(BackgroundsFolder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 200f;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 4096;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }
    }
}
