using UnityEngine;
using Alif.Characters;
using Alif.Core;
using Alif.Dialogue;
using Alif.Systems;
using Alif.UI;

namespace Alif.World
{
    /// <summary>
    /// Koin rezeki tersembunyi yang berkilau di lantai ruang tunggu stasiun.
    /// Memberi pemain kejutan menyenangkan (+Rp 5.000), suara gemerincing koin,
    /// dan dorongan eksplorasi bagi pemain yang teliti.
    /// </summary>
    public class LuckyCoinPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _rewardAmount = 5000;
        [SerializeField] private DialogueData _rewardDialogue;

        private bool _isCollected;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_isCollected || _spriteRenderer == null) return;

            // Efek kilau berkelap-kelip halus
            float twinkle = 0.75f + Mathf.Sin(Time.time * 6f) * 0.25f;
            Color c = _spriteRenderer.color;
            c.a = twinkle;
            _spriteRenderer.color = c;
        }

        public void Interact()
        {
            if (_isCollected) return;
            _isCollected = true;

            // Suara gemerincing koin
            AudioManager.Instance?.PlayCoin();

            // Berikan uang ke pemain
            if (CurrencySystem.Instance != null)
            {
                CurrencySystem.Instance.AddMoney(_rewardAmount);
            }

            // Screen shake mikro kepuasan
            CameraFollow.TriggerShake(0.12f, 0.04f);

            // Munculkan emote sparkle di atas koin
            EmoteBubble.ShowOn(gameObject, EmoteType.Sparkle, 1.5f);

            // Dialog monolog Alif
            if (_rewardDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(_rewardDialogue);
            }

            // Sembunyikan visual koin
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            // Matikan collider agar tidak bisa diambil dua kali
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }
}
