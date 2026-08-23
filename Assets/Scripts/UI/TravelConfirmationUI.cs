using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Popup konfirmasi umum "mau pindah ke [tempat]?" — dipakai SceneDoor SEBELUM benar-benar
    /// transisi/fade ke area lain, biar nggak langsung "loading" pas Player kesenggol
    /// trigger pintu. Beda dari QuitConfirmationUI karena aksi konfirmnya dinamis (callback per
    /// pemanggilan), bukan selalu satu aksi tetap.
    ///
    /// GameObject-nya SENGAJA selalu aktif (visibility diatur lewat CanvasGroup, bukan
    /// SetActive) — kalau root-nya nonaktif dari awal scene di-load, Awake() nggak akan pernah
    /// dipanggil sampai ada yang manggil SetActive(true) duluan, padahal Instance cuma di-set di
    /// Awake(). Itu bikin SceneDoor (yang manggil lewat TravelConfirmationUI.Instance, bukan
    /// referensi langsung) SELAMANYA dapet null, popup-nya nggak pernah muncul dan langsung
    /// jatuh ke fallback transisi tanpa nanya.
    /// </summary>
    public class TravelConfirmationUI : MonoBehaviour
    {
        public static TravelConfirmationUI Instance { get; private set; }

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirmed;
        private Action _onCancelled;

        public bool IsShowing => _canvasGroup != null && _canvasGroup.blocksRaycasts;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(HandleConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(HandleCancelClicked);
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

        public void Show(string message, Action onConfirmed, Action onCancelled = null)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            _onConfirmed = onConfirmed;
            _onCancelled = onCancelled;

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
            Action callback = _onConfirmed;
            _onConfirmed = null;
            _onCancelled = null;
            Hide();
            callback?.Invoke();
        }

        private void HandleCancelClicked()
        {
            Action callback = _onCancelled;
            _onConfirmed = null;
            _onCancelled = null;
            Hide();
            callback?.Invoke();
        }
    }
}
