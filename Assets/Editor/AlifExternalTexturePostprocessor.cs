using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Otomatis mengatur import settings untuk asset eksternal di Assets/Sprites/External/.
    /// Membedakan antara UI pack (Bilinear, 200 PPU) dan RPG pixel art (Point, 256 PPU).
    /// </summary>
    public class AlifExternalTexturePostprocessor : AssetPostprocessor
    {
        private const string ExternalUiFolder = "Assets/Sprites/External/UI_Pack/";
        private const string ExternalRpgFolder = "Assets/Sprites/External/RPG_Pack/";

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace("\\", "/");
            if (!path.StartsWith("Assets/Sprites/External/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            if (path.StartsWith(ExternalUiFolder))
            {
                importer.spritePixelsPerUnit = 200f;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }
            else if (path.StartsWith(ExternalRpgFolder))
            {
                // PPU 100 = skala yang dipakai dunia TileRPG demo yang sudah ter-build
                // (mengubah ke 256 akan mengecilkan tile 2.56x dan merusak layout prefab).
                // Filter Point supaya tile pixel-art tidak blur.
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }

            importer.SetTextureSettings(settings);
        }
    }
}
