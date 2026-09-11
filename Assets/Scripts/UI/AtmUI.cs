using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Alif.Player;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Popup ATM — nampilin saldo bank & pilihan tarik tunai kecil agar Alif mengambil uang
    /// secukupnya, bukan langsung mengosongkan tabungan.
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
        [SerializeField] private int[] _withdrawAmounts = { 10000, 20000, 50000 };

        private PlayerController _lockedPlayer;
        private CurrencySystem _currencySystem;
        private Coroutine _subscribeRoutine;

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
                    Debug.LogWarning($"[Alif] AtmUI: _withdrawButtons[{i}] kosong, tombol {CurrencySystem.FormatRupiah(amount)} nggak akan bisa diklik.");
                }
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
            if (!TrySubscribeToCurrency())
            {
                _subscribeRoutine = StartCoroutine(SubscribeWhenCurrencyReady());
            }
        }

        private void OnDisable()
        {
            if (_subscribeRoutine != null)
            {
                StopCoroutine(_subscribeRoutine);
                _subscribeRoutine = null;
            }

            if (_currencySystem != null)
            {
                _currencySystem.OnBankBalanceChanged -= HandleBalanceChanged;
                _currencySystem = null;
            }
        }

        private IEnumerator SubscribeWhenCurrencyReady()
        {
            while (!TrySubscribeToCurrency())
            {
                yield return null;
            }

            _subscribeRoutine = null;
        }

        private bool TrySubscribeToCurrency()
        {
            CurrencySystem activeCurrency = CurrencySystem.Instance;
            if (activeCurrency == null)
            {
                return false;
            }

            if (_currencySystem != activeCurrency)
            {
                if (_currencySystem != null)
                {
                    _currencySystem.OnBankBalanceChanged -= HandleBalanceChanged;
                }

                _currencySystem = activeCurrency;
                _currencySystem.OnBankBalanceChanged += HandleBalanceChanged;
            }

            HandleBalanceChanged(_currencySystem.BankBalance);
            return true;
        }

        public void Show(PlayerController player)
        {
            TrySubscribeToCurrency();
            _lockedPlayer = player;
            if (player != null)
            {
                player.SetMovementLocked(true);
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);

            if (_lockedPlayer != null)
            {
                _lockedPlayer.SetMovementLocked(false);
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
                _balanceText.text = $"Saldo ATM: {CurrencySystem.FormatRupiah(balance)}";
            }

            for (int i = 0; i < _withdrawButtons.Length && i < _withdrawAmounts.Length; i++)
            {
                if (_withdrawButtons[i] != null)
                {
                    _withdrawButtons[i].interactable = balance >= _withdrawAmounts[i];
                }
            }
        }

        private void HandleWithdrawClicked(int amount)
        {
            if (!TrySubscribeToCurrency())
            {
                Debug.LogWarning("[Alif] Tarik tunai gagal: CurrencySystem.Instance tidak ditemukan di scene.");
                return;
            }

            bool success = _currencySystem.Withdraw(amount);
            Debug.Log(success
                ? $"[Alif] Tarik tunai {CurrencySystem.FormatRupiah(amount)} berhasil. Sisa saldo ATM: {CurrencySystem.FormatRupiah(_currencySystem.BankBalance)}."
                : $"[Alif] Tarik tunai {CurrencySystem.FormatRupiah(amount)} gagal (saldo ATM cuma {CurrencySystem.FormatRupiah(_currencySystem.BankBalance)}).");
        }

    }
}
