using UnityEditor;
using UnityEditor.SceneManagement;

namespace Alif.EditorTools
{
    /// <summary>
    /// Memaksa Play mode di Editor selalu mulai dari MainMenu.unity, apapun scene yang lagi
    /// dibuka di Hierarchy (mis. lagi edit SampleScene). Ini murni kenyamanan Editor — meniru
    /// perilaku build asli (device selalu boot dari scene index 0 di Build Settings) tanpa perlu
    /// gonta-ganti scene aktif tiap mau nge-tes alur dari Main Menu.
    ///
    /// [InitializeOnLoad] supaya field ini ke-set ulang tiap kali Editor domain reload
    /// (buka project, selesai compile script, dst) — playModeStartScene sendiri tidak
    /// persisten otomatis antar sesi Editor.
    /// </summary>
    [InitializeOnLoad]
    public static class AlifPlayModeBootScene
    {
        private const string BootScenePath = "Assets/Scenes/MainMenu.unity";

        static AlifPlayModeBootScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
        }
    }
}
