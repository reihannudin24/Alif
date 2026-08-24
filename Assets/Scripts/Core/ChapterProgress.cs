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

        public static int HighestChapterUnlocked => Mathf.Max(1, PlayerPrefs.GetInt(HighestChapterUnlockedKey, 1));
        public static int SelectedChapter => Mathf.Max(1, PlayerPrefs.GetInt(SelectedChapterKey, 1));

        public static bool IsUnlocked(int chapterNumber)
        {
            return chapterNumber > 0 && chapterNumber <= HighestChapterUnlocked;
        }

        public static bool IsCompleted(int chapterNumber)
        {
            return PlayerPrefs.GetInt(GetCompletedKey(chapterNumber), 0) == 1;
        }

        public static void StartNewGame()
        {
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.SetInt(HighestChapterUnlockedKey, 1);
            PlayerPrefs.SetInt(SelectedChapterKey, 1);

            // Main Baru benar-benar memulai progres chapter dari awal.
            for (int chapter = 1; chapter <= 5; chapter++)
            {
                PlayerPrefs.DeleteKey(GetCompletedKey(chapter));
            }

            PlayerPrefs.Save();
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
            if (chapterNumber <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.SetInt(GetCompletedKey(chapterNumber), 1);

            int highestUnlocked = HighestChapterUnlocked;
            if (unlockNextChapter)
            {
                highestUnlocked = Mathf.Max(highestUnlocked, chapterNumber + 1);
                PlayerPrefs.SetInt(SelectedChapterKey, chapterNumber + 1);
            }

            PlayerPrefs.SetInt(HighestChapterUnlockedKey, highestUnlocked);
            PlayerPrefs.Save();
        }

        private static string GetCompletedKey(int chapterNumber)
        {
            return $"Alif_Chapter_{chapterNumber}_Completed";
        }
    }
}
