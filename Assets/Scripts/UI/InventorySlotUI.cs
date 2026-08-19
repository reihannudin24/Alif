using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Satu slot visual di InventoryUI. Menangani klik (untuk pilih/gunakan item) dan
    /// drag-drop sederhana antar slot menggunakan Unity Event System.
    /// Butuh komponen Image (untuk icon), dan idealnya CanvasGroup untuk efek transparansi saat drag.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _quantityLabel;

        private int _slotIndex;
        private InventoryUI _owner;
        private Vector3 _dragStartPosition;

        public void Setup(int slotIndex, InventoryUI owner)
        {
            _slotIndex = slotIndex;
            _owner = owner;
        }

        /// <summary>
        /// Update tampilan icon dan jumlah item berdasarkan data slot terbaru.
        /// </summary>
        public void Refresh(InventorySlot slotData)
        {
            bool hasItem = slotData != null && !slotData.IsEmpty;

            if (_iconImage != null)
            {
                _iconImage.sprite = hasItem ? slotData.Icon : null;
                _iconImage.enabled = hasItem;
            }

            if (_quantityLabel != null)
            {
                _quantityLabel.text = hasItem && slotData.Quantity > 1 ? slotData.Quantity.ToString() : string.Empty;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner.HandleSlotClicked(_slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragStartPosition = transform.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Kembalikan posisi visual slot (posisi tetap ditentukan oleh layout, bukan drag).
            transform.position = _dragStartPosition;

            // Cek apakah drag dilepas di atas slot lain.
            GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
            if (targetObject == null)
            {
                return;
            }

            InventorySlotUI targetSlot = targetObject.GetComponentInParent<InventorySlotUI>();
            if (targetSlot != null && targetSlot != this)
            {
                _owner.HandleSlotDropped(_slotIndex, targetSlot._slotIndex);
            }
        }
    }
}
