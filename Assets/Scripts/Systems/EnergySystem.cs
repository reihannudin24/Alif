using System;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Sistem energi/stamina pemain. Nilai berkisar 0-100 (persen) dan dipakai
    /// untuk mengisi progress bar di HUD. Berkurang saat aktivitas (misal bekerja,
    /// berlari) dan bisa diisi ulang lewat aksi seperti tidur atau makan.
    /// </summary>
    public class EnergySystem : MonoBehaviour
    {
        public static EnergySystem Instance { get; private set; }

        [Header("Energy Settings")]
        [SerializeField] private float _maxEnergy = 100f;
        [SerializeField] private float _currentEnergy = 100f;

        // Event dipanggil setiap kali energy berubah, membawa nilai 0-1 (persentase)
        // supaya gampang dipakai langsung untuk Slider/Image fill amount di UI.
        public event Action<float> OnEnergyChanged;

        // Event dipanggil sekali saat energy mencapai 0, misalnya untuk memaksa
        // karakter pingsan/tidur otomatis.
        public event Action OnEnergyDepleted;

        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy => _maxEnergy;
        public float EnergyPercent01 => _currentEnergy / _maxEnergy;

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
            // Trigger event awal supaya UI langsung sinkron saat game dimulai.
            OnEnergyChanged?.Invoke(EnergyPercent01);
        }

        /// <summary>
        /// Kurangi energy, dipanggil saat pemain melakukan aktivitas yang menguras stamina.
        /// </summary>
        public void ConsumeEnergy(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning("ConsumeEnergy menerima nilai negatif, gunakan RestoreEnergy untuk menambah energy.");
                return;
            }

            SetEnergy(_currentEnergy - amount);
        }

        /// <summary>
        /// Tambah energy, dipanggil saat pemain tidur, makan, atau minum potion.
        /// </summary>
        public void RestoreEnergy(float amount)
        {
            if (amount < 0f)
            {
                Debug.LogWarning("RestoreEnergy menerima nilai negatif, gunakan ConsumeEnergy untuk mengurangi energy.");
                return;
            }

            SetEnergy(_currentEnergy + amount);
        }

        /// <summary>
        /// Isi penuh energy ke maksimum, biasanya dipanggil saat pemain tidur di malam hari.
        /// </summary>
        public void RestoreFull()
        {
            SetEnergy(_maxEnergy);
        }

        private void SetEnergy(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, _maxEnergy);
            bool wasDepleted = _currentEnergy <= 0f;

            _currentEnergy = clamped;
            OnEnergyChanged?.Invoke(EnergyPercent01);

            if (!wasDepleted && _currentEnergy <= 0f)
            {
                OnEnergyDepleted?.Invoke();
            }
        }
    }
}
