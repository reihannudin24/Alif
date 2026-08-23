using System;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Dua skor inti dari minigame "AI Detektif" (lihat storyline Chapter 1-5): tiap kali
    /// pemain mengetik argumen buat mematahkan pelaku penipuan finansial, backend menilai
    /// argumennya lalu nambah/ngurangin dua skor ini. Nilai 0-100, dipakai buat isi dua
    /// progress bar di HUD.
    ///
    /// - Financial Logic: masuk akal secara matematis/bisnis (dari sisi logika keuangan).
    /// - Sharia Compliance: bebas dari Riba, Gharar, dan Maysir (dari sisi syariah).
    /// </summary>
    public class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem Instance { get; private set; }

        [Header("Financial Logic")]
        [SerializeField] private float _maxFinancialLogic = 100f;
        [SerializeField] private float _currentFinancialLogic = 0f;

        [Header("Sharia Compliance")]
        [SerializeField] private float _maxShariaCompliance = 100f;
        [SerializeField] private float _currentShariaCompliance = 0f;

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
            _currentFinancialLogic = Mathf.Clamp(_currentFinancialLogic + delta, 0f, _maxFinancialLogic);
            OnFinancialLogicChanged?.Invoke(FinancialLogicPercent01);
        }

        /// <summary>
        /// Ubah skor Sharia Compliance, dipanggil backend penilai argumen. Delta boleh negatif
        /// (argumen masih mengandung unsur Riba/Gharar/Maysir) maupun positif.
        /// </summary>
        public void AdjustShariaCompliance(float delta)
        {
            _currentShariaCompliance = Mathf.Clamp(_currentShariaCompliance + delta, 0f, _maxShariaCompliance);
            OnShariaComplianceChanged?.Invoke(ShariaCompliancePercent01);
        }
    }
}
