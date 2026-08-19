using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alif.Core
{
    /// <summary>
    /// Helper untuk pindah antar scene. Dipisah dari GameManager supaya tanggung jawabnya
    /// jelas: GameManager mengurus state, SceneLoader mengurus perpindahan scene.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Sama seperti GameManager: SceneLoader biasanya jadi child "_GameManagers",
            // jadi yang di-persist adalah root-nya, bukan dirinya sendiri.
            DontDestroyOnLoad(transform.root.gameObject);
        }

        /// <summary>
        /// Load scene berdasarkan nama secara langsung (synchronous).
        /// </summary>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load scene secara asynchronous, berguna kalau nanti butuh loading screen.
        /// </summary>
        public void LoadSceneAsync(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private System.Collections.IEnumerator LoadSceneRoutine(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

            // Menunggu sampai proses loading benar-benar selesai sebelum lanjut.
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        public void ReloadCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            LoadScene(currentScene.name);
        }
    }
}
