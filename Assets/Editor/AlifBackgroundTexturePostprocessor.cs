using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Setting import untuk gambar background/interior (Assets/Sprites/Backgrounds/) dan panel
    /// cutscene (Assets/Sprites/Cutscenes/). Keduanya art ilustrasi shading halus (bukan pixel
    /// art blocky kayak karakter), jadi pakai filter Bilinear (bukan Point) supaya nggak
    /// kelihatan pecah/bergerigi waktu di-scale. PPU 200 dipilih supaya proporsinya nyambung
    /// sama karakter (Assets/Sprites/Characters, PPU 256 dengan tinggi konten ~225px) — lihat
    /// AlifDemoSceneBuilder.BuildBackground(). Untuk panel cutscene (dipakai di UI Image, bukan
    /// SpriteRenderer dunia) PPU ini nggak berpengaruh, jadi dibiarkan sama biar satu aturan saja.
    /// </summary>
    public class AlifBackgroundTexturePostprocessor : AssetPostprocessor
    {
        private static readonly string[] IllustratedArtFolders =
        {
            "Assets/Sprites/Backgrounds/",
            "Assets/Sprites/Cutscenes/",
        };

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace("\\", "/");
            bool inIllustratedArtFolder = false;
            foreach (string folder in IllustratedArtFolders)
            {
                if (path.StartsWith(folder))
                {
                    inIllustratedArtFolder = true;
                    break;
                }
            }

            if (!inIllustratedArtFolder)
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
