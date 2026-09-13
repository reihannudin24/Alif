using UnityEngine;

namespace Alif.Core
{
    /// <summary>
    /// Satu sumber kebenaran untuk save progress chapter. Semua menu dan cutscene memakai
    /// class ini agar aturan lock/unlock tidak berbeda-beda di tiap scene.
    /// </summary>
    public static class ChapterProgress
    {
        public const string HasSaveKey = "Alif_HasSave";
        public const string HighestChapterUnlockedKey = "Alif_HighestChapterUnlocked";
        public const string SelectedChapterKey = "Alif_SelectedChapter";

        public const int ChapterCount = 5;
        public static Alif.Adventure.AdventureState LoadAdventure(out string notice) => Alif.Adventure.AdventureSave.Load(out notice);
        public static bool SaveAdventure(Alif.Adventure.AdventureState state) => Alif.Adventure.AdventureSave.Store(state);
        public static int HighestChapterUnlocked => Mathf.Clamp(PlayerPrefs.GetInt(HighestChapterUnlockedKey, 1), 1, ChapterCount);
        public static int SelectedChapter => Mathf.Clamp(PlayerPrefs.GetInt(SelectedChapterKey, 1), 1, HighestChapterUnlocked);

        public static bool IsUnlocked(int chapterNumber)
        {
            return chapterNumber > 0 && chapterNumber <= ChapterCount && chapterNumber <= HighestChapterUnlocked;
        }

        public static bool IsCompleted(int chapterNumber)
        {
            return PlayerPrefs.GetInt(GetCompletedKey(chapterNumber), 0) == 1;
        }

        public static void StartNewGame()
        {
            PlayerPrefs.DeleteKey(Alif.Adventure.AdventureSave.Key);
            PlayerPrefs.DeleteKey(Alif.Adventure.AdventureSave.BackupKey);
            PlayerPrefs.DeleteKey(Alif.Adventure.AdventureSave.LegacyKey);
            PlayerPrefs.DeleteKey(Alif.Adventure.AdventureSave.LegacyBackupKey);
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.SetInt(HighestChapterUnlockedKey, 1);
            PlayerPrefs.SetInt(SelectedChapterKey, 1);

            // Main Baru benar-benar memulai progres chapter dari awal.
            for (int chapter = 1; chapter <= 5; chapter++)
            {
                PlayerPrefs.DeleteKey(GetCompletedKey(chapter));
                PlayerPrefs.DeleteKey($"Alif_Chapter_{chapter}_Checkpoint");
                PlayerPrefs.DeleteKey($"Alif_Chapter_{chapter}_Clues");
            }

            SaveAdventure(new Alif.Adventure.AdventureState { TutorialStep = 1 });
        }

        public static void SelectChapter(int chapterNumber)
        {
            if (!IsUnlocked(chapterNumber))
            {
                return;
            }

            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.SetInt(SelectedChapterKey, chapterNumber);
            PlayerPrefs.Save();
        }

        public static void CompleteChapter(int chapterNumber, bool unlockNextChapter = true)
        {
            if (chapterNumber <= 0 || chapterNumber > ChapterCount)
            {
                return;
            }

            var state = LoadAdventure(out _);
            if (state.Chapter != chapterNumber || !state.Completed) return;
            SaveAdventure(state);
        }

        private static string GetCompletedKey(int chapterNumber)
        {
            return $"Alif_Chapter_{chapterNumber}_Completed";
        }
    }
}
