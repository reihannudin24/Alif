using UnityEngine;
using Alif.Characters;
using Alif.Dialogue;

namespace Alif.World
{
    /// <summary>
    /// Benda statis di dunia (bangku, meja, dsb) yang bisa di-interact pemain untuk memicu
    /// dialog singkat/monolog — beda dari NPC, benda ini nggak butuh CharacterData (nggak
    /// ada relationship/affinity), cukup satu DialogueData saja.
    /// Setup: taruh di layer "Interactable", tambahkan Collider2D. Kalau _blockMovement true,
    /// collider dibuat solid (bukan trigger) supaya Player nggak bisa jalan menembusnya.
    /// </summary>
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueData _dialogue;
        [Tooltip("Opsional: dipakai buat nampilin portrait pembicara (misal Alif) di dialogue box saat monolog.")]
        [SerializeField] private CharacterData _speakerData;

        public void Interact()
        {
            if (_dialogue == null)
            {
                Debug.LogWarning($"[Alif] '{name}' tidak punya DialogueData yang di-assign.");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("[Alif] DialogueManager.Instance tidak ditemukan di scene.");
                return;
            }

            DialogueManager.Instance.StartDialogue(_dialogue, _speakerData);
        }
    }
}
