using UnityEngine;

namespace Alif.UI
{
    /// <summary>
    /// Titik pusat untuk membuka/menutup panel-panel UI (HUD, Inventory, Dialogue, dsb).
    /// HUDController, InventoryUI, dan DialogueUI cukup fokus pada tampilan masing-masing,
    /// sedangkan UIManager yang mengatur panel mana yang aktif.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject _hudPanel;
        [SerializeField] private GameObject _inventoryPanel;
        [SerializeField] private GameObject _dialoguePanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ShowHUD(bool show)
        {
            if (_hudPanel != null) _hudPanel.SetActive(show);
        }

        public void ShowInventory(bool show)
        {
            if (_inventoryPanel != null) _inventoryPanel.SetActive(show);
        }

        public void ShowDialogue(bool show)
        {
            if (_dialoguePanel != null) _dialoguePanel.SetActive(show);
        }

        /// <summary>
        /// Toggle panel inventory, dipanggil misalnya dari input action "ToggleInventory".
        /// </summary>
        public void ToggleInventory()
        {
            if (_inventoryPanel == null)
            {
                return;
            }

            _inventoryPanel.SetActive(!_inventoryPanel.activeSelf);
        }
    }
}
