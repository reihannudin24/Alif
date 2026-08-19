using UnityEngine;
using Alif.Dialogue;

namespace Alif.Characters
{
    /// <summary>
    /// Komponen yang ditempel ke GameObject NPC di scene. Menghubungkan data reusable
    /// (CharacterData) dan dialog (DialogueData) dengan DialogueManager saat pemain
    /// berinteraksi (dipanggil dari PlayerController.TryInteractWithNearestNPC).
    /// Pastikan GameObject NPC punya Collider2D (Is Trigger boleh dimatikan/nyalakan sesuai
    /// kebutuhan) di layer yang sama dengan _interactableLayer pada PlayerController.
    /// </summary>
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("Data")]
        [SerializeField] private CharacterData _characterData;
        [SerializeField] private DialogueData _defaultDialogue;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public CharacterData Data => _characterData;

        private void Awake()
        {
            // Terapkan AnimatorController dari data ke Animator NPC jika tersedia,
            // supaya NPC dengan data berbeda bisa pakai prefab NPC yang sama.
            if (_characterData != null && _characterData.AnimatorController != null)
            {
                Animator animator = GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = _characterData.AnimatorController;
                }
            }
        }

        public void Interact()
        {
            StartDialogue();
        }

        /// <summary>
        /// Dipanggil oleh PlayerController saat pemain menekan tombol Interact di dekat NPC ini.
        /// Meneruskan permintaan ke DialogueManager untuk menampilkan dialog default NPC.
        /// </summary>
        public void StartDialogue()
        {
            if (_defaultDialogue == null)
            {
                Debug.LogWarning($"NPC '{name}' tidak punya DialogueData yang di-assign.");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("DialogueManager.Instance tidak ditemukan di scene.");
                return;
            }

            DialogueManager.Instance.StartDialogue(_defaultDialogue, _characterData);
        }
    }
}
