using System.Collections.Generic;
using UnityEngine;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Tampilan inventory berbasis slot (5 slot horizontal di bawah layar, sesuai referensi UI).
    /// Membaca data dari InventorySystem dan redraw setiap kali InventorySystem.OnInventoryChanged terpanggil.
    /// Setup: buat 1 parent horizontal (misal pakai Horizontal Layout Group) berisi 5 GameObject slot,
    /// masing-masing punya komponen InventorySlotUI, lalu drag ke-5 slot itu ke list _slotViews di Inspector.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Slot Views")]
        [Tooltip("Urutan harus sama dengan urutan slot di InventorySystem (default 5 slot).")]
        [SerializeField] private List<InventorySlotUI> _slotViews = new List<InventorySlotUI>();

        private void Awake()
        {
            for (int i = 0; i < _slotViews.Count; i++)
            {
                // Jaga-jaga kalau ada entry kosong di list (misal slot belum sempat di-assign
                // di Inspector) — tanpa ini satu entry null bisa bikin seluruh Awake() gagal.
                if (_slotViews[i] != null)
                {
                    _slotViews[i].Setup(i, this);
                }
            }
        }

        private void OnEnable()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged += RefreshAllSlots;
                RefreshAllSlots();
            }
        }

        private void OnDisable()
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged -= RefreshAllSlots;
            }
        }

        private void RefreshAllSlots()
        {
            IReadOnlyList<InventorySlot> slots = InventorySystem.Instance.Slots;

            for (int i = 0; i < _slotViews.Count && i < slots.Count; i++)
            {
                if (_slotViews[i] != null)
                {
                    _slotViews[i].Refresh(slots[i]);
                }
            }
        }

        /// <summary>
        /// Dipanggil InventorySlotUI saat slot diklik. Sederhana: bisa dipakai untuk
        /// "select item" atau "use item" sesuai kebutuhan game nantinya.
        /// </summary>
        public void HandleSlotClicked(int slotIndex)
        {
            Debug.Log($"Inventory slot {slotIndex} diklik.");
            // TODO: hubungkan ke logic pakai/pilih item sesuai kebutuhan game.
        }

        /// <summary>
        /// Dipanggil InventorySlotUI saat drag-drop selesai di atas slot lain. Menukar isi
        /// dua slot lewat InventorySystem supaya data dan tampilan tetap konsisten.
        /// </summary>
        public void HandleSlotDropped(int fromIndex, int toIndex)
        {
            InventorySystem.Instance.SwapSlots(fromIndex, toIndex);
        }
    }
}
