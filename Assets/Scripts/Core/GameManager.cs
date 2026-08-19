using UnityEngine;

namespace Alif.Core
{
    /// <summary>
    /// State global game. Menggunakan pola Singleton agar bisa diakses dari mana saja
    /// (GameManager.Instance) dan tetap hidup saat pindah scene (DontDestroyOnLoad).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // State global sederhana, bisa dikembangkan sesuai kebutuhan game.
        public enum GameState
        {
            MainMenu,
            Playing,
            Dialogue,
            Paused
        }

        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.MainMenu;

        public GameState CurrentState => _currentState;

        private void Awake()
        {
            // Pastikan hanya ada satu instance GameManager di seluruh game.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // DontDestroyOnLoad cuma berlaku buat root GameObject — GameManager biasanya jadi
            // child di bawah "_GameManagers", jadi yang di-persist adalah root-nya supaya
            // seluruh manager lain yang ikut jadi child-nya juga ikut selamat.
            DontDestroyOnLoad(transform.root.gameObject);
        }

        /// <summary>
        /// Ganti state global game. Dipanggil oleh sistem lain, misalnya DialogueManager
        /// saat dialog dimulai/selesai, atau menu pause.
        /// </summary>
        public void SetState(GameState newState)
        {
            _currentState = newState;
        }

        public bool IsPlaying() => _currentState == GameState.Playing;
    }
}
