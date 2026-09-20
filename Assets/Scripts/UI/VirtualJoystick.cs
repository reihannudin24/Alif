using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Alif.UI
{
    public enum JoystickMode
    {
        Fixed,
        Floating,
        Hidden
    }

    /// <summary>
    /// Joystick virtual di layar (drag pakai jari/mouse) supaya Player bisa digerakkan tanpa
    /// keyboard — berguna buat build mobile atau kalau mau ada tombol kontrol di layar juga.
    /// Struktur: GameObject ini = area background (lingkaran diam), child "Knob" = lingkaran
    /// kecil yang ikut digeser mengikuti drag, dibatasi radius _handleRange.
    /// PlayerController baca nilai Direction ini tiap frame dan digabung dengan input keyboard.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public const string ModePreferenceKey = "Alif_JoystickMode";

        [SerializeField] private RectTransform _touchArea;
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _knob;
        /// <summary>Posisi diam alas joystick (pojok kiri-bawah). Mode Floating kembali ke sini
        /// begitu jari dilepas, jadi kontrol geraknya selalu terlihat di kiri layar.</summary>
        private Vector2 _homePosition;
        [Tooltip("Jarak maksimum (dalam pixel UI) knob bisa digeser dari titik tengah.")]
        [SerializeField] private float _handleRange = 60f;
        [SerializeField] private JoystickMode _mode;

        // Nilai -1..1 di tiap sumbu, dibaca PlayerController sebagai pengganti/tambahan input keyboard.
        public Vector2 Direction { get; private set; }
        public JoystickMode Mode => _mode;

        public void Configure(RectTransform background, RectTransform knob)
        {
            Configure(background, background, knob, JoystickMode.Fixed);
        }

        public void Configure(RectTransform touchArea, RectTransform background, RectTransform knob, JoystickMode mode)
        {
            _touchArea = touchArea;
            _background = background;
            _knob = knob;
            if (_background != null) _homePosition = _background.anchoredPosition;
            // Dulu joystick otomatis disembunyikan di desktop tanpa layar sentuh. Sekarang tetap
            // ditampilkan — tombol E di kanan juga selalu ada, dan tutorial pembuka menunjuk
            // keduanya. Yang mau layar bersih tinggal pilih "Sembunyikan" di pengaturan jeda.
            SetMode(mode);
        }

        public static JoystickMode ParseMode(int value)
        {
            return value >= (int)JoystickMode.Fixed && value <= (int)JoystickMode.Hidden
                ? (JoystickMode)value
                : JoystickMode.Fixed;
        }

        public static JoystickMode SavedMode => ParseMode(PlayerPrefs.GetInt(ModePreferenceKey, 0));

        public static void SaveMode(JoystickMode mode)
        {
            PlayerPrefs.SetInt(ModePreferenceKey, (int)ParseMode((int)mode));
            PlayerPrefs.Save();
        }

        public static Vector2 ClampFloatingCenter(Rect area, Vector2 point, float radius)
        {
            float inset = Mathf.Max(0f, radius);
            float minX = area.xMin + inset, maxX = area.xMax - inset;
            float minY = area.yMin + inset, maxY = area.yMax - inset;
            return new Vector2(
                minX <= maxX ? Mathf.Clamp(point.x, minX, maxX) : area.center.x,
                minY <= maxY ? Mathf.Clamp(point.y, minY, maxY) : area.center.y);
        }

        /// <summary>Dipakai kontrol lain yang menggantikan stik analog (D-pad di HUD Adventure):
        /// arahnya disetel dari luar, komponen ini tinggal jadi sumber baca PlayerController.</summary>
        public void SetDirection(Vector2 direction) => Direction = direction;

        public void SetMode(JoystickMode mode)
        {
            _mode = mode;
            Direction = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
            if (_background != null)
            {
                // Selalu terlihat kecuali Hidden; Floating hanya berarti alasnya melompat ke titik
                // sentuh, lalu kembali ke posisi diamnya setelah dilepas.
                _background.anchoredPosition = _homePosition;
                _background.gameObject.SetActive(mode != JoystickMode.Hidden);
            }

            var graphic = GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null) graphic.raycastTarget = mode != JoystickMode.Hidden;
        }
        private void OnDisable() => OnPointerUp(null);
        private void OnApplicationFocus(bool focused) { if (!focused) OnPointerUp(null); }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_mode == JoystickMode.Hidden || eventData == null) return;
            if (_mode == JoystickMode.Floating && _touchArea != null && _background != null && _touchArea != _background)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _touchArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
                float radius = Mathf.Max(_background.rect.width, _background.rect.height) * 0.5f;
                _background.anchoredPosition = ClampFloatingCenter(_touchArea.rect, localPoint, radius);
                _background.gameObject.SetActive(true);
            }
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_mode == JoystickMode.Hidden || eventData == null || _background == null || _knob == null)
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
            if (_mode == JoystickMode.Floating && _touchArea != null && _background != null && _touchArea != _background)
            {
                _background.anchoredPosition = _homePosition;      // kembali ke pojok kiri-bawah
            }
        }
    }
}
