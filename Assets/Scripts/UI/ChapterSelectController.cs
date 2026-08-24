using UnityEngine;
using UnityEngine.SceneManagement;
using Alif.Core;

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
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private ChapterCardUI[] _chapterCards;
        [Header("Chapter entry scenes")]
        [SerializeField] private string _chapter1SceneName = "Chapter1Cutscene";
        [SerializeField] private string _chapter2SceneName = "Chapter2Cutscene";

        private void Start()
        {
            foreach (ChapterCardUI card in _chapterCards)
            {
                if (card == null)
                {
                    continue;
                }

                bool lockedByProgress = !ChapterProgress.IsUnlocked(card.ChapterNumber);
                bool chapterHasNoScene = string.IsNullOrEmpty(GetChapterSceneName(card.ChapterNumber));
                card.SetLocked(lockedByProgress || chapterHasNoScene);
            }
        }

        public void OnChapterClicked(int chapterNumber)
        {
            if (!ChapterProgress.IsUnlocked(chapterNumber))
            {
                return;
            }

            string sceneName = GetChapterSceneName(chapterNumber);
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[Alif] Chapter {chapterNumber} belum punya scene entry yang tersedia.");
                return;
            }

            ChapterProgress.SelectChapter(chapterNumber);
            SceneManager.LoadScene(sceneName);
        }

        public void OnBackClicked()
        {
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private string GetChapterSceneName(int chapterNumber)
        {
            switch (chapterNumber)
            {
                case 1: return _chapter1SceneName;
                case 2: return _chapter2SceneName;
                default: return string.Empty;
            }
        }
    }
}
