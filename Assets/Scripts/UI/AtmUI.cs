using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Alif.Player;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Popup ATM — nampilin saldo bank & tombol tarik tunai beberapa nominal + "Ambil Semua".
    /// Tarik tunai mindahin saldo dari CurrencySystem.BankBalance ke CurrencySystem.CurrentMoney
    /// (uang kantong, yang ditampilkan HUD). Movement Player dikunci selama popup ini kebuka.
    ///
    /// GameObject-nya SENGAJA selalu aktif (visibility diatur lewat CanvasGroup, bukan
    /// SetActive) — kalau root-nya nonaktif dari awal scene di-load, Awake() nggak akan pernah
    /// dipanggil sampai ada yang manggil SetActive(true) duluan, padahal Instance cuma di-set di
    /// Awake(). Itu bikin AtmController (yang manggil lewat AtmUI.Instance, bukan referensi
    /// langsung) SELAMANYA dapet null, popup-nya nggak pernah bisa dibuka.
    /// </summary>
    public class AtmUI : MonoBehaviour
    {
        public static AtmUI Instance { get; private set; }

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _balanceText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button[] _withdrawButtons;
        [SerializeField] private int[] _withdrawAmounts = { 50000, 100000, 200000, 500000 };
        [SerializeField] private Button _withdrawAllButton;

        private PlayerController _lockedPlayer;

        private void Awake()
        {
            Instance = this;

            Debug.Log($"[Alif] AtmUI.Awake: {_withdrawButtons.Length} tombol tarik tunai, {_withdrawAmounts.Length} nominal terdaftar.");

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }

            for (int i = 0; i < _withdrawButtons.Length && i < _withdrawAmounts.Length; i++)
            {
                int amount = _withdrawAmounts[i];
                Button button = _withdrawButtons[i];
                if (button != null)
                {
                    button.onClick.AddListener(() => HandleWithdrawClicked(amount));
                }
                else
                {
                    Debug.LogWarning($"[Alif] AtmUI: _withdrawButtons[{i}] kosong, tombol Rp {amount:N0} nggak akan bisa diklik.");
                }
            }

            if (_withdrawAllButton != null)
            {
                _withdrawAllButton.onClick.AddListener(HandleWithdrawAllClicked);
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

        private void OnEnable()
        {
            if (CurrencySystem.Instance != null)
            {
                CurrencySystem.Instance.OnBankBalanceChanged += HandleBalanceChanged;
                HandleBalanceChanged(CurrencySystem.Instance.BankBalance);
            }
        }

        private void OnDisable()
        {
            if (CurrencySystem.Instance != null)
            {
                CurrencySystem.Instance.OnBankBalanceChanged -= HandleBalanceChanged;
            }
        }

        public void Show(PlayerController player)
        {
            _lockedPlayer = player;
            if (player != null)
            {
                player.SetMovementLocked(this, true);
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);

            if (_lockedPlayer != null)
            {
                _lockedPlayer.SetMovementLocked(this, false);
                _lockedPlayer = null;
            }
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

        private void HandleBalanceChanged(int balance)
        {
            if (_balanceText != null)
            {
                _balanceText.text = $"Saldo ATM: Rp {balance:N0}";
            }
        }

        private void HandleWithdrawClicked(int amount)
        {
            if (CurrencySystem.Instance == null)
            {
                Debug.LogWarning("[Alif] Tarik tunai gagal: CurrencySystem.Instance tidak ditemukan di scene.");
                return;
            }

            bool success = CurrencySystem.Instance.Withdraw(amount);
            Debug.Log(success
                ? $"[Alif] Tarik tunai Rp {amount:N0} berhasil. Sisa saldo ATM: Rp {CurrencySystem.Instance.BankBalance:N0}."
                : $"[Alif] Tarik tunai Rp {amount:N0} gagal (saldo ATM cuma Rp {CurrencySystem.Instance.BankBalance:N0}).");
        }

        private void HandleWithdrawAllClicked()
        {
            if (CurrencySystem.Instance != null)
            {
                CurrencySystem.Instance.Withdraw(CurrencySystem.Instance.BankBalance);
            }
        }
    }
}
