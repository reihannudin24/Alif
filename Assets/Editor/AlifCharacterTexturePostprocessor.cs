using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Otomatis rapikan setting import untuk semua PNG di Assets/Sprites/Characters/.
    /// Dipakai supaya sprite karakter (hasil export AI generator 8-arah, kanvas 256x256
    /// per frame) jadi pixel-perfect dan pivot-nya konsisten di kaki (Bottom) — bukan
    /// auto-crop per frame (yang bikin animasi jalan "goyang" karena tiap frame beda
    /// ukuran bounding box transparannya).
    /// Jalan otomatis tiap kali file baru diimport. Untuk file yang SUDAH diimport
    /// sebelum script ini ada, dipaksa reimport lewat menu "Alif > 1) Import Character Art".
    /// </summary>
    public class AlifCharacterTexturePostprocessor : AssetPostprocessor
    {
        private const string CharactersFolder = "Assets/Sprites/Characters/";

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace("\\", "/");
            if (!path.StartsWith(CharactersFolder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
        }
    }
}
