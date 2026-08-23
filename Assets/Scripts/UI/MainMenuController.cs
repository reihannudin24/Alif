using Alif.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Logika tombol di Main Menu: "Main Baru" mulai game dari awal, "Lanjutkan" masuk ke
    /// scene gameplay yang sama (nonaktif sampai pernah ada sesi "Main Baru" sebelumnya,
    /// ditandai lewat PlayerPrefs karena project ini belum punya sistem save/load sungguhan).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private const string HasSaveKey = "Alif_HasSave";
        private const string HighestChapterUnlockedKey = "Alif_HighestChapterUnlocked";
        private const string SelectedChapterKey = "Alif_SelectedChapter";

        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private string _chapterSelectSceneName = "ChapterSelect";
        [SerializeField] private string _chapter1CutsceneSceneName = "Chapter1Cutscene";

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            }

            if (_continueButton != null)
            {
                _continueButton.interactable = PlayerPrefs.GetInt(HasSaveKey, 0) == 1;
            }
        }

        public void OnNewGameClicked()
        {
            PlayerPrefs.SetInt(HasSaveKey, 1);
            PlayerPrefs.SetInt(HighestChapterUnlockedKey, 1);
            PlayerPrefs.SetInt(SelectedChapterKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(_chapter1CutsceneSceneName);
        }

        public void OnContinueClicked()
        {
            SceneManager.LoadScene(_chapterSelectSceneName);
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
