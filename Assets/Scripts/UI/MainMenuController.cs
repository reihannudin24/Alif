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
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private string _chapterSelectSceneName = "ChapterSelect";
        [SerializeField] private string _chapter1CutsceneSceneName = "Chapter1Cutscene";

        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private AudioClip _backgroundMusic;

        [SerializeField] private QuitConfirmationUI _quitConfirmation;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.MainMenu);
            }

            if (_continueButton != null)
            {
                _continueButton.interactable = PlayerPrefs.GetInt(ChapterProgress.HasSaveKey, 0) == 1;
            }

            if (_audioManager != null)
            {
                _audioManager.PlayMusic(_backgroundMusic);
            }
        }

        public void OnNewGameClicked()
        {
            ChapterProgress.StartNewGame();
            SceneTransition.Load(_chapter1CutsceneSceneName);
        }

        public void OnContinueClicked()
        {
            SceneTransition.Load(_chapterSelectSceneName);
        }

        public void OnQuitClicked()
        {
            // Sekarang cuma munculin popup konfirmasi — Application.Quit() yang beneran
            // dieksekusi QuitConfirmationUI sendiri kalau user nge-tap "Ya".
            if (_quitConfirmation != null)
            {
                _quitConfirmation.Show();
            }
        }
    }
}
