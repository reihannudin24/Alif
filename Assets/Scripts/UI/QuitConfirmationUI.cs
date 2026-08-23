using UnityEngine;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Popup konfirmasi "Apakah anda yakin mau keluar?" — satu instance dipasang per scene yang
    /// butuh (Main Menu & gameplay), dipanggil dari tombol/aksi "Keluar" manapun di scene itu
    /// sebelum Application.Quit() beneran dieksekusi.
    ///
    /// GameObject-nya SENGAJA selalu aktif (visibility diatur lewat CanvasGroup, bukan
    /// SetActive) — kalau root-nya nonaktif dari awal scene di-load, Awake() nggak akan pernah
    /// dipanggil sampai ada yang manggil SetActive(true) duluan, padahal Instance cuma di-set di
    /// Awake(). Itu bikin skrip lain yang manggil lewat QuitConfirmationUI.Instance (bukan
    /// referensi langsung) SELAMANYA dapet null, popup nggak pernah bisa dibuka.
    /// </summary>
    public class QuitConfirmationUI : MonoBehaviour
    {
        public static QuitConfirmationUI Instance { get; private set; }

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public bool IsShowing => _canvasGroup != null && _canvasGroup.blocksRaycasts;

        private void Awake()
        {
            Instance = this;

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private void HandleConfirmClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
