using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Menghubungkan TimeSystem, EnergySystem, dan CurrencySystem ke tampilan HUD
    /// di pojok kiri atas layar: hari/minggu, jam, energy bar, dan uang.
    /// Script ini murni "listener" — tidak menyimpan logic game, hanya update UI
    /// setiap kali sistem terkait memanggil event.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Time Display")]
        [SerializeField] private TMP_Text _dayWeekText;
        [SerializeField] private TMP_Text _clockText;

        [Header("Energy Display")]
        [SerializeField] private Slider _energySlider;

        [Header("Currency Display")]
        [SerializeField] private TMP_Text _moneyText;

        private void OnEnable()
        {
            // Subscribe ke event sistem-sistem terkait. Dilakukan di OnEnable/OnDisable
            // (bukan Awake/OnDestroy) supaya aman jika HUD di-nonaktifkan sementara lalu diaktifkan lagi.
            if (TimeSystem.Instance != null)
            {
                TimeSystem.Instance.OnMinuteChanged += HandleTimeChanged;
                HandleTimeChanged();
            }

            if (EnergySystem.Instance != null)
            {
                EnergySystem.Instance.OnEnergyChanged += HandleEnergyChanged;
                HandleEnergyChanged(EnergySystem.Instance.EnergyPercent01);
            }

            if (CurrencySystem.Instance != null)
            {
                CurrencySystem.Instance.OnMoneyChanged += HandleMoneyChanged;
                HandleMoneyChanged(CurrencySystem.Instance.CurrentMoney);
            }
        }

        private void OnDisable()
        {
            if (TimeSystem.Instance != null)
            {
                TimeSystem.Instance.OnMinuteChanged -= HandleTimeChanged;
            }

            if (EnergySystem.Instance != null)
            {
                EnergySystem.Instance.OnEnergyChanged -= HandleEnergyChanged;
            }

            if (CurrencySystem.Instance != null)
            {
                CurrencySystem.Instance.OnMoneyChanged -= HandleMoneyChanged;
            }
        }

        private void HandleTimeChanged()
        {
            if (_dayWeekText != null)
            {
                _dayWeekText.text = TimeSystem.Instance.GetFormattedDay();
            }

            if (_clockText != null)
            {
                _clockText.text = TimeSystem.Instance.GetFormattedTime();
            }
        }

        private void HandleEnergyChanged(float percent01)
        {
            if (_energySlider != null)
            {
                _energySlider.value = percent01;
            }
        }

        private void HandleMoneyChanged(int amount)
        {
            if (_moneyText != null)
            {
                _moneyText.text = amount.ToString("N0");
            }
        }
    }
}
