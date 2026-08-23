using UnityEditor;
using UnityEngine;
using TMPro;

namespace Alif.EditorTools
{
    /// <summary>
    /// Bikin TMP Font Asset dari Poppins-Regular.ttf (Assets/Fonts/) dan pasang sebagai font
    /// default project (TMP Settings) supaya semua TextMeshProUGUI baru otomatis pakai Poppins.
    /// FontAssetInstance() self-healing: kalau asset-nya belum ada, otomatis di-generate saat
    /// dipanggil pertama kali oleh AlifDemoSceneBuilder.FindOrCreateText — nggak perlu jalanin
    /// tool ini manual dulu sebelum build scene manapun.
    /// </summary>
    public static class AlifFontSetup
    {
        private const string SourceTtfPath = "Assets/Fonts/Poppins-Regular.ttf";
        private const string FontAssetPath = "Assets/Fonts/Poppins-Regular SDF.asset";

        private static TMP_FontAsset _cached;

        [MenuItem("Alif/0) Setup Poppins Font")]
        public static void SetupPoppinsFont()
        {
            TMP_FontAsset asset = FontAssetInstance();
            if (asset != null)
            {
                Debug.Log("[Alif] Poppins TMP Font Asset siap dipakai di '" + FontAssetPath + "'.");
            }
        }

        internal static TMP_FontAsset FontAssetInstance()
        {
            if (_cached != null)
            {
                return _cached;
            }

            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                _cached = existing;
                return existing;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceTtfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[Alif] Font sumber '{SourceTtfPath}' tidak ditemukan, Poppins dilewati (pakai font TMP default).");
                return null;
            }

            TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (generated == null)
            {
                Debug.LogWarning("[Alif] Gagal generate TMP Font Asset dari Poppins-Regular.ttf.");
                return null;
            }

            generated.name = "Poppins-Regular SDF";
            AssetDatabase.CreateAsset(generated, FontAssetPath);
            AssetDatabase.AddObjectToAsset(generated.material, generated);
            AssetDatabase.AddObjectToAsset(generated.atlasTextures[0], generated);
            AssetDatabase.SaveAssets();

            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                SerializedProperty defaultFontProp = so.FindProperty("m_defaultFontAsset");
                if (defaultFontProp != null)
                {
                    defaultFontProp.objectReferenceValue = generated;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            _cached = generated;
            return generated;
        }
    }
}
