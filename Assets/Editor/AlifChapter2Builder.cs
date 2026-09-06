using UnityEditor;
namespace Alif.EditorTools
{
    public static class AlifChapter2Builder
    {
        [MenuItem("Alif/8) Build Chapter 2 Gameplay Scene")]
        public static void BuildChapter2Gameplay() => AlifAdventureBuilder.Build();
        public static void BuildChapter2GameplayCLI() => BuildChapter2Gameplay();
    }
}
