using UnityEngine;
using UnityEngine.EventSystems;

namespace Alif.UI
{
    /// <summary>
    /// Joystick virtual di layar (drag pakai jari/mouse) supaya Player bisa digerakkan tanpa
    /// keyboard — berguna buat build mobile atau kalau mau ada tombol kontrol di layar juga.
    /// Struktur: GameObject ini = area background (lingkaran diam), child "Knob" = lingkaran
    /// kecil yang ikut digeser mengikuti drag, dibatasi radius _handleRange.
    /// PlayerController baca nilai Direction ini tiap frame dan digabung dengan input keyboard.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _knob;
        [Tooltip("Jarak maksimum (dalam pixel UI) knob bisa digeser dari titik tengah.")]
        [SerializeField] private float _handleRange = 60f;

        // Nilai -1..1 di tiap sumbu, dibaca PlayerController sebagai pengganti/tambahan input keyboard.
        public Vector2 Direction { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_background == null || _knob == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _handleRange);
            _knob.anchoredPosition = clamped;
            Direction = clamped / _handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Direction = Vector2.zero;

            if (_knob != null)
            {
                _knob.anchoredPosition = Vector2.zero;
            }
        }
    }
}
