using UnityEngine;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Popup konfirmasi "Apakah anda yakin mau keluar?" — satu instance dipasang per scene yang
    /// butuh (Main Menu & gameplay), dipanggil dari tombol/aksi "Keluar" manapun di scene itu
    /// sebelum Application.Quit() beneran dieksekusi.
    /// </summary>
    public class QuitConfirmationUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public bool IsShowing => _root != null && _root.activeSelf;

        private void Awake()
        {
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

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
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
