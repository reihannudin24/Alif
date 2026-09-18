using Alif.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Drag & drop satu slot InventorySystem ke slot lain (hotbar maupun kartu Tas): ikon
    /// "hantu" mengikuti pointer, dilepas di slot lain = tukar isi kedua slot. Slot kosong
    /// tidak bisa diseret; slot tujuan boleh kosong (barang pindah ke sana).
    /// </summary>
    public sealed class InventorySlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private const float GhostSize = 56f;

        private int _index;
        private Image _icon;
        private RectTransform _ghost;

        public void Setup(int index, Image icon)
        {
            _index = index;
            _icon = icon;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var inventory = InventorySystem.Instance;
            if (inventory == null || _index >= inventory.Slots.Count || inventory.Slots[_index].IsEmpty)
            {
                eventData.pointerDrag = null; // batalkan: tidak ada yang diseret
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            _ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _ghost.SetParent(canvas.transform, false);
            _ghost.SetAsLastSibling();
            _ghost.sizeDelta = Vector2.one * GhostSize;
            var ghostImage = _ghost.GetComponent<Image>();
            ghostImage.sprite = inventory.Slots[_index].Icon;
            ghostImage.preserveAspect = true;
            ghostImage.raycastTarget = false; // supaya slot di bawah pointer menerima OnDrop
            _ghost.position = eventData.position;
            if (_icon != null) _icon.color = new Color(1f, 1f, 1f, .35f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null) _ghost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
            if (_icon != null) _icon.color = Color.white;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var source = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<InventorySlotDrag>() : null;
            if (source == null || source == this || source._ghost == null) return;
            InventorySystem.Instance?.SwapSlots(source._index, _index);
        }
    }
}
