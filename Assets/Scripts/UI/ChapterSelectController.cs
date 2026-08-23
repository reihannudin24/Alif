using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alif.UI
{
    /// <summary>
    /// Layar "Chapter Select" yang dibuka dari tombol Lanjutkan di Main Menu. Progress
    /// disimpan lewat PlayerPrefs (project ini belum punya sistem save/load sungguhan,
    /// sama seperti MainMenuController) — chapter baru terbuka begitu chapter sebelumnya
    /// pernah dipilih.
    /// </summary>
    public class ChapterSelectController : MonoBehaviour
    {
        private const string HighestChapterUnlockedKey = "Alif_HighestChapterUnlocked";
        private const string SelectedChapterKey = "Alif_SelectedChapter";
        private const string GameplaySceneName = "SampleScene";
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private ChapterCardUI[] _chapterCards;

        private void Start()
        {
            int highestUnlocked = PlayerPrefs.GetInt(HighestChapterUnlockedKey, 1);
            foreach (ChapterCardUI card in _chapterCards)
            {
                card.SetLocked(card.ChapterNumber > highestUnlocked);
            }
        }

        public void OnChapterClicked(int chapterNumber)
        {
            int highestUnlocked = PlayerPrefs.GetInt(HighestChapterUnlockedKey, 1);
            if (chapterNumber > highestUnlocked)
            {
                return;
            }

            PlayerPrefs.SetInt(SelectedChapterKey, chapterNumber);
            PlayerPrefs.Save();
            SceneManager.LoadScene(GameplaySceneName);
        }

        public void OnBackClicked()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }
    }
}
