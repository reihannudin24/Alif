using UnityEngine;
using Alif.Characters;
using Alif.Dialogue;
using Alif.Systems;

namespace Alif.World
{
    /// <summary>
    /// Benda yang bisa diambil pemain (voucher, dsb) — nambah item ke InventorySystem sekali,
    /// opsional nunjukin dialog singkat pas diambil. Beda dari InteractableObject (itu cuma
    /// monolog, nggak nambah item) — dipakai misalnya buat Loket Karcis (voucher promo kereta).
    /// Setup: taruh di layer "Interactable", tambahkan Collider2D (biasanya solid/non-trigger
    /// kayak benda fisik lain, mis. bangku).
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _itemName = "Item";
        [SerializeField] private Sprite _itemIcon;
        [SerializeField] private int _quantity = 1;

        [Tooltip("Opsional: dialog singkat yang muncul begitu item berhasil diambil.")]
        [SerializeField] private DialogueData _pickupDialogue;
        [SerializeField] private CharacterData _speakerData;

        // Cuma bisa diambil sekali — begitu berhasil, interact berikutnya nggak ngapa-ngapain.
        private bool _collected;

        public void Interact()
        {
            if (_collected)
            {
                return;
            }

            if (InventorySystem.Instance == null)
            {
                Debug.LogWarning("[Alif] InventorySystem.Instance tidak ditemukan di scene.");
                return;
            }

            bool added = InventorySystem.Instance.AddItem(_itemName, _itemIcon, _quantity);
            if (!added)
            {
                // Inventory penuh — jangan tandai collected, biar bisa dicoba lagi kalau
                // slot-nya udah dikosongin duluan.
                return;
            }

            _collected = true;

            if (_pickupDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(_pickupDialogue, _speakerData);
            }
        }
    }
}
