using System;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Neraca pilihan inti game. Dalam Shared Balance Mode (default), Logika Finansial dan
    /// Kepatuhan Syariah berbagi satu total 100%: ketika satu sisi naik, sisi lain turun.
    /// Ini membuat pilihan yang terlalu mengejar uang atau terlalu menghindari risiko terlihat
    /// jelas, sementara jalan terbaik berada di sekitar titik tengah 50% / 50%.
    ///
    /// - Financial Logic: masuk akal secara matematis/bisnis (dari sisi logika keuangan).
    /// - Sharia Compliance: bebas dari Riba, Gharar, dan Maysir (dari sisi syariah).
    /// </summary>
    public class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem Instance { get; private set; }

        [Header("Financial Logic")]
        [SerializeField] private float _maxFinancialLogic = 100f;
        [SerializeField] private float _currentFinancialLogic = 50f;

        [Header("Sharia Compliance")]
        [SerializeField] private float _maxShariaCompliance = 100f;
        [SerializeField] private float _currentShariaCompliance = 50f;

        [Header("Balance Rule")]
        [Tooltip("Jika aktif, kedua bar selalu berbagi total 100%. Nonaktifkan hanya untuk mode penilaian lama yang memakai dua skor terpisah.")]
        [SerializeField] private bool _useSharedBalance = true;

        // Event membawa nilai 0-1 (persentase), langsung dipakai buat Slider.value di UI.
        public event Action<float> OnFinancialLogicChanged;
        public event Action<float> OnShariaComplianceChanged;

        public float CurrentFinancialLogic => _currentFinancialLogic;
        public float CurrentShariaCompliance => _currentShariaCompliance;
        public float FinancialLogicPercent01 => _currentFinancialLogic / _maxFinancialLogic;
        public float ShariaCompliancePercent01 => _currentShariaCompliance / _maxShariaCompliance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            NormalizeSharedBalance();
        }

        private void Start()
        {
            OnFinancialLogicChanged?.Invoke(FinancialLogicPercent01);
            OnShariaComplianceChanged?.Invoke(ShariaCompliancePercent01);
        }

        /// <summary>
        /// Ubah skor Financial Logic, dipanggil backend penilai argumen. Delta boleh negatif
        /// (argumen lemah/salah secara logika bisnis) maupun positif (argumen kuat).
        /// </summary>
        public void AdjustFinancialLogic(float delta)
        {
            SetFinancialLogic(_currentFinancialLogic + delta);
        }

        /// <summary>
        /// Ubah skor Sharia Compliance, dipanggil backend penilai argumen. Delta boleh negatif
        /// (argumen masih mengandung unsur Riba/Gharar/Maysir) maupun positif.
        /// </summary>
        public void AdjustShariaCompliance(float delta)
        {
            if (_useSharedBalance)
            {
                // Sharia naik berarti porsi finansial turun, sehingga total tetap 100%.
                float nextFinancial = _currentFinancialLogic - delta;
                SetFinancialLogic(nextFinancial);
                return;
            }

            _currentShariaCompliance = Mathf.Clamp(_currentShariaCompliance + delta, 0f, _maxShariaCompliance);
            NotifyScoresChanged();
        }

        /// <summary>
        /// Tarik neraca menuju jalan tengah. Nilai 10 berarti paling banyak bergerak 10 poin
        /// menuju 50/50, tanpa pernah melewati titik tengah.
        /// </summary>
        public void MoveTowardsBalance(float amount)
        {
            if (!_useSharedBalance)
            {
                return;
            }

            float targetFinancial = _maxFinancialLogic * 0.5f;
            SetFinancialLogic(Mathf.MoveTowards(_currentFinancialLogic, targetFinancial, Mathf.Abs(amount)));
        }

        private void SetFinancialLogic(float value)
        {
            _currentFinancialLogic = Mathf.Clamp(value, 0f, _maxFinancialLogic);

            if (_useSharedBalance)
            {
                float financialPercent = _maxFinancialLogic <= 0f ? 0.5f : _currentFinancialLogic / _maxFinancialLogic;
                _currentShariaCompliance = (1f - financialPercent) * _maxShariaCompliance;
            }

            NotifyScoresChanged();
        }

        private void NormalizeSharedBalance()
        {
            if (_useSharedBalance)
            {
                SetFinancialLogic(_currentFinancialLogic);
            }
        }

        private void NotifyScoresChanged()
        {
            OnFinancialLogicChanged?.Invoke(FinancialLogicPercent01);
            OnShariaComplianceChanged?.Invoke(ShariaCompliancePercent01);
        }
    }
}
