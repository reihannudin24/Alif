using System;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Sistem uang pemain. Menyimpan jumlah uang dan menyediakan event yang bisa
    /// didengarkan UI (misal HUDController) untuk update tampilan tanpa perlu polling tiap frame.
    /// </summary>
    public class CurrencySystem : MonoBehaviour
    {
        public static CurrencySystem Instance { get; private set; }

        [Header("Currency")]
        [Tooltip("Uang tunai yang dipegang Alif (di kantong) — ini yang ditampilkan HUD.")]
        [SerializeField] private int _currentMoney = 100000;

        [Header("Bank Account (ATM)")]
        [Tooltip("Saldo di rekening/ATM — terpisah dari uang tunai, cuma bisa dipindah ke uang tunai lewat AtmUI.Withdraw().")]
        [SerializeField] private int _bankBalance = 1000000;

        // Event membawa jumlah uang terbaru setelah berubah.
        public event Action<int> OnMoneyChanged;
        public event Action<int> OnBankBalanceChanged;

        public int CurrentMoney => _currentMoney;
        public int BankBalance => _bankBalance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            OnMoneyChanged?.Invoke(_currentMoney);
            OnBankBalanceChanged?.Invoke(_bankBalance);
        }

        /// <summary>
        /// Tambah uang, misalnya hasil jual barang atau reward quest.
        /// </summary>
        public void AddMoney(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("AddMoney menerima nilai negatif, gunakan SpendMoney untuk mengurangi uang.");
                return;
            }

            _currentMoney += amount;
            OnMoneyChanged?.Invoke(_currentMoney);
        }

        /// <summary>
        /// Coba kurangi uang, misalnya saat membeli barang. Mengembalikan false jika
        /// uang tidak cukup, supaya sistem toko bisa membatalkan transaksi.
        /// </summary>
        public bool SpendMoney(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("SpendMoney menerima nilai negatif, gunakan AddMoney untuk menambah uang.");
                return false;
            }

            if (_currentMoney < amount)
            {
                return false;
            }

            _currentMoney -= amount;
            OnMoneyChanged?.Invoke(_currentMoney);
            return true;
        }

        public bool HasEnoughMoney(int amount) => _currentMoney >= amount;

        /// <summary>
        /// Tarik tunai dari saldo bank (ATM) ke uang kantong. Mengembalikan false kalau saldo
        /// bank tidak cukup, supaya AtmUI bisa menolak transaksi tanpa nge-crash.
        /// </summary>
        public bool Withdraw(int amount)
        {
            if (amount <= 0 || _bankBalance < amount)
            {
                return false;
            }

            _bankBalance -= amount;
            OnBankBalanceChanged?.Invoke(_bankBalance);
            AddMoney(amount);
            return true;
        }
    }
}
