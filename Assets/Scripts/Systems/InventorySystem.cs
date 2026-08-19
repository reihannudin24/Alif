using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Data satu slot inventory. Dipakai baik oleh InventorySystem (logic) maupun
    /// InventoryUI (tampilan). Sengaja dibuat sederhana: cukup nama item, icon, dan jumlah.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public string ItemName;
        public Sprite Icon;
        public int Quantity;

        public bool IsEmpty => string.IsNullOrEmpty(ItemName) || Quantity <= 0;

        public void Clear()
        {
            ItemName = null;
            Icon = null;
            Quantity = 0;
        }
    }

    /// <summary>
    /// Sistem inventory sederhana berbasis slot (default 5 slot horizontal sesuai referensi UI).
    /// Logic disimpan terpisah dari tampilan (InventoryUI) supaya mudah dites tanpa UI.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [Header("Inventory Settings")]
        [SerializeField] private int _slotCount = 5;
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        // Dipanggil setiap kali isi inventory berubah, supaya InventoryUI tinggal redraw semua slot.
        public event Action OnInventoryChanged;

        public IReadOnlyList<InventorySlot> Slots => _slots;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots.Clear();
            for (int i = 0; i < _slotCount; i++)
            {
                _slots.Add(new InventorySlot());
            }
        }

        /// <summary>
        /// Tambah item ke inventory. Jika item dengan nama sama sudah ada, jumlahnya ditambah.
        /// Jika belum ada, cari slot kosong. Mengembalikan false jika inventory penuh.
        /// </summary>
        public bool AddItem(string itemName, Sprite icon, int quantity = 1)
        {
            // Cek dulu apakah item sudah ada di salah satu slot (stacking).
            InventorySlot existingSlot = _slots.Find(slot => !slot.IsEmpty && slot.ItemName == itemName);
            if (existingSlot != null)
            {
                existingSlot.Quantity += quantity;
                OnInventoryChanged?.Invoke();
                return true;
            }

            // Kalau belum ada, cari slot kosong.
            InventorySlot emptySlot = _slots.Find(slot => slot.IsEmpty);
            if (emptySlot == null)
            {
                Debug.LogWarning("Inventory penuh, tidak bisa menambah item baru.");
                return false;
            }

            emptySlot.ItemName = itemName;
            emptySlot.Icon = icon;
            emptySlot.Quantity = quantity;
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Kurangi/hapus item dari slot tertentu berdasarkan index.
        /// </summary>
        public void RemoveItemAt(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return;
            }

            InventorySlot slot = _slots[slotIndex];
            if (slot.IsEmpty)
            {
                return;
            }

            slot.Quantity -= quantity;
            if (slot.Quantity <= 0)
            {
                slot.Clear();
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Tukar isi dua slot, dipakai untuk fitur drag-drop di InventoryUI.
        /// </summary>
        public void SwapSlots(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _slots.Count || indexB < 0 || indexB >= _slots.Count)
            {
                return;
            }

            (_slots[indexA], _slots[indexB]) = (_slots[indexB], _slots[indexA]);
            OnInventoryChanged?.Invoke();
        }
    }
}
