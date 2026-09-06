using UnityEditor;
namespace Alif.EditorTools
{
    public static class AlifCampaignBuilder
    {
        [MenuItem("Alif/Campaign/Build All Five Chapters")]
        public static void Build() => AlifAdventureBuilder.Build();
        public static void InstallChapter2() => AlifAdventureBuilder.Build();
    }
}
