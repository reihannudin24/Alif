namespace Alif.EditorTools
{
    public static class AlifBattleBuilder
    {
        // Older tooling entrypoints build the replacement instead of reinstalling combat.
        public static void Install() => AlifAdventureBuilder.Build();
        public static void InstallIntoScene(Alif.World.Chapter2StoryController story) => Install();
    }
}
