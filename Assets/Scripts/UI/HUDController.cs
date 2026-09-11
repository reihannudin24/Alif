using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alif.Systems;

namespace Alif.UI
{
    /// <summary>
    /// Menghubungkan TimeSystem, EnergySystem, dan CurrencySystem ke tampilan HUD
    /// di pojok kiri atas layar: hari/minggu, energy bar, dan uang. Jam (jam:menit) sengaja
    /// tidak ditampilkan — TimeSystem tetap jalan di belakang layar buat hitungan hari/minggu.
    /// Script ini murni "listener" — tidak menyimpan logic game, hanya update UI
    /// setiap kali sistem terkait memanggil event.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Time Display")]
        [SerializeField] private TMP_Text _dayWeekText;

        [Header("Energy Display")]
        [SerializeField] private Slider _energySlider;
        [SerializeField] private TMP_Text _energyPercentText;

        [Header("Score Display")]
        [SerializeField] private Slider _financialLogicSlider;
        [SerializeField] private TMP_Text _financialLogicPercentText;
        [SerializeField] private Slider _shariaComplianceSlider;
        [SerializeField] private TMP_Text _shariaCompliancePercentText;

        [Header("Currency Display")]
        [SerializeField] private TMP_Text _moneyText;

        private TimeSystem _timeSystem;
        private EnergySystem _energySystem;
        private CurrencySystem _currencySystem;
        private ScoreSystem _scoreSystem;
        private Coroutine _systemsSubscribeRoutine;

        private void OnEnable()
        {
            // Urutan Awake antarroots Unity tidak dijamin. Canvas kadang aktif lebih dulu dari
            // _GameManagers, sehingga slider tertinggal di nilai prefab 100% dan tidak pernah
            // menerima perubahan berikutnya. Retry sampai SEMUA sistem siap, lalu cache instance
            // yang benar supaya HUD selalu langsung sinkron.
            if (!TrySubscribeToSystems())
            {
                _systemsSubscribeRoutine = StartCoroutine(SubscribeWhenSystemsReady());
            }
        }

        private void OnDisable()
        {
            if (_systemsSubscribeRoutine != null)
            {
                StopCoroutine(_systemsSubscribeRoutine);
                _systemsSubscribeRoutine = null;
            }

            if (_timeSystem != null)
            {
                _timeSystem.OnMinuteChanged -= HandleTimeChanged;
                _timeSystem = null;
            }

            if (_energySystem != null)
            {
                _energySystem.OnEnergyChanged -= HandleEnergyChanged;
                _energySystem = null;
            }

            if (_currencySystem != null)
            {
                _currencySystem.OnMoneyChanged -= HandleMoneyChanged;
                _currencySystem = null;
            }

            if (_scoreSystem != null)
            {
                _scoreSystem.OnFinancialLogicChanged -= HandleFinancialLogicChanged;
                _scoreSystem.OnShariaComplianceChanged -= HandleShariaComplianceChanged;
                _scoreSystem = null;
            }
        }

        private void HandleTimeChanged()
        {
            if (_dayWeekText != null)
            {
                TimeSystem activeTime = _timeSystem != null ? _timeSystem : TimeSystem.Instance;
                if (activeTime != null)
                {
                    _dayWeekText.text = activeTime.GetFormattedDay();
                }
            }
        }

        private void HandleEnergyChanged(float percent01)
        {
            if (_energySlider != null)
            {
                _energySlider.value = percent01;
            }

            SetPercentText(_energyPercentText, percent01);
        }

        private void HandleMoneyChanged(int amount)
        {
            if (_moneyText != null)
            {
                _moneyText.text = CurrencySystem.FormatRupiah(amount);
            }
        }

        private IEnumerator SubscribeWhenSystemsReady()
        {
            while (!TrySubscribeToSystems())
            {
                yield return null;
            }

            _systemsSubscribeRoutine = null;
        }

        private bool TrySubscribeToSystems()
        {
            bool timeReady = TrySubscribeToTime();
            bool energyReady = TrySubscribeToEnergy();
            bool currencyReady = TrySubscribeToCurrency();
            bool scoreReady = TrySubscribeToScore();
            return timeReady && energyReady && currencyReady && scoreReady;
        }

        private bool TrySubscribeToTime()
        {
            TimeSystem activeTime = TimeSystem.Instance;
            if (activeTime == null)
            {
                return false;
            }

            if (_timeSystem != activeTime)
            {
                if (_timeSystem != null)
                {
                    _timeSystem.OnMinuteChanged -= HandleTimeChanged;
                }

                _timeSystem = activeTime;
                _timeSystem.OnMinuteChanged += HandleTimeChanged;
                HandleTimeChanged();
            }

            return true;
        }

        private bool TrySubscribeToEnergy()
        {
            EnergySystem activeEnergy = EnergySystem.Instance;
            if (activeEnergy == null)
            {
                return false;
            }

            if (_energySystem != activeEnergy)
            {
                if (_energySystem != null)
                {
                    _energySystem.OnEnergyChanged -= HandleEnergyChanged;
                }

                _energySystem = activeEnergy;
                _energySystem.OnEnergyChanged += HandleEnergyChanged;
                HandleEnergyChanged(_energySystem.EnergyPercent01);
            }

            return true;
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
                    _currencySystem.OnMoneyChanged -= HandleMoneyChanged;
                }

                _currencySystem = activeCurrency;
                _currencySystem.OnMoneyChanged += HandleMoneyChanged;
                HandleMoneyChanged(_currencySystem.CurrentMoney);
            }

            return true;
        }

        private bool TrySubscribeToScore()
        {
            ScoreSystem activeScore = ScoreSystem.Instance;
            if (activeScore == null)
            {
                return false;
            }

            if (_scoreSystem != activeScore)
            {
                if (_scoreSystem != null)
                {
                    _scoreSystem.OnFinancialLogicChanged -= HandleFinancialLogicChanged;
                    _scoreSystem.OnShariaComplianceChanged -= HandleShariaComplianceChanged;
                }

                _scoreSystem = activeScore;
                _scoreSystem.OnFinancialLogicChanged += HandleFinancialLogicChanged;
                _scoreSystem.OnShariaComplianceChanged += HandleShariaComplianceChanged;
                HandleFinancialLogicChanged(_scoreSystem.FinancialLogicPercent01);
                HandleShariaComplianceChanged(_scoreSystem.ShariaCompliancePercent01);
            }

            return true;
        }

        private void HandleFinancialLogicChanged(float percent01)
        {
            if (_financialLogicSlider != null)
            {
                _financialLogicSlider.value = percent01;
            }

            SetPercentText(_financialLogicPercentText, percent01);
        }

        private void HandleShariaComplianceChanged(float percent01)
        {
            if (_shariaComplianceSlider != null)
            {
                _shariaComplianceSlider.value = percent01;
            }

            SetPercentText(_shariaCompliancePercentText, percent01);
        }

        private static void SetPercentText(TMP_Text text, float percent01)
        {
            if (text == null)
            {
                return;
            }

            text.text = $"{Mathf.RoundToInt(percent01 * 100f)}%";
        }
    }
}
