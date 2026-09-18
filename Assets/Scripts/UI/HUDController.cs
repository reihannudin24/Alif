using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alif.Campaign;
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

        private static readonly Color EnergyColor = new Color(.55f, .88f, .36f);
        private static readonly Color FinancialLogicColor = new Color(.45f, .7f, 1f);
        private static readonly Color ShariaColor = new Color(1f, .8f, .3f);

        private void Awake()
        {
            ApplyPixelSkin();
        }

        /// <summary>
        /// Gaya papan status pixel (seperti panel minggu/jam di game manajemen retro): badan
        /// oranye, hari/minggu di tab krem yang menempel di tepi atas, tiap skor berupa bar
        /// berbingkai krem dengan persen di dalamnya, uang di kotak krem. Builder membangun
        /// panel sebagai kotak coklat transparan; tata letak baru dipasang runtime.
        /// </summary>
        private void ApplyPixelSkin()
        {
            if (TryGetComponent(out Image board))
            {
                board.sprite = PixelSkin.Button();
                board.type = Image.Type.Sliced;
                board.color = Color.white;
            }

            if (_dayWeekText != null)
            {
                RectTransform tab = NewImage("DayTab", transform, PixelSkin.Tab());
                tab.anchorMin = tab.anchorMax = new Vector2(0f, 1f);
                tab.pivot = new Vector2(0f, .5f);
                tab.anchoredPosition = new Vector2(14f, 2f);
                tab.sizeDelta = new Vector2(190f, 32f);
                Place(_dayWeekText, tab, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Style(_dayWeekText, 14f, PixelSkin.TextDark, TextAlignmentOptions.Center);
            }

            StyleBar("Energy", _energySlider, _energyPercentText, EnergyColor, 0);
            StyleBar("FinancialLogic", _financialLogicSlider, _financialLogicPercentText, FinancialLogicColor, 1);
            StyleBar("ShariaCompliance", _shariaComplianceSlider, _shariaCompliancePercentText, ShariaColor, 2);

            if (transform.Find("BalanceHint") is RectTransform hint && hint.TryGetComponent(out TMP_Text hintText))
            {
                Place(hintText, transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -164f), new Vector2(-14f, -150f));
                Style(hintText, 9f, new Color(PixelSkin.TextLight.r, PixelSkin.TextLight.g, PixelSkin.TextLight.b, .8f), TextAlignmentOptions.Left);
            }

            if (_moneyText != null)
            {
                RectTransform box = NewImage("MoneyBox", transform, PixelSkin.Slot());
                box.anchorMin = new Vector2(0f, 0f);
                box.anchorMax = new Vector2(1f, 0f);
                box.pivot = new Vector2(.5f, 0f);
                box.offsetMin = new Vector2(12f, 12f);
                box.offsetMax = new Vector2(-12f, 44f);
                Place(_moneyText, box, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
                Style(_moneyText, 16f, PixelSkin.TextDark, TextAlignmentOptions.Right);
                if (transform.Find("MoneyLabel") is RectTransform moneyLabel && moneyLabel.TryGetComponent(out TMP_Text moneyLabelText))
                {
                    Place(moneyLabelText, box, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
                    Style(moneyLabelText, 13f, PixelSkin.Accent, TextAlignmentOptions.Left);
                }
            }
        }

        /// <summary>Label terang di atas bar; bar = bingkai krem + isi berwarna ber-kilau,
        /// persen ditulis gelap di tengah bar.</summary>
        private void StyleBar(string name, Slider slider, TMP_Text percentText, Color color, int row)
        {
            if (slider == null) return;
            float top = -26f - row * 40f;
            if (transform.Find(name + "Label") is RectTransform label && label.TryGetComponent(out TMP_Text labelText))
            {
                Place(labelText, transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, top - 14f), new Vector2(-14f, top));
                Style(labelText, 12f, PixelSkin.TextLight, TextAlignmentOptions.Left);
            }

            var sliderRect = (RectTransform)slider.transform;
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.pivot = new Vector2(.5f, 1f);
            sliderRect.offsetMin = new Vector2(12f, top - 38f);
            sliderRect.offsetMax = new Vector2(-12f, top - 16f);

            if (slider.transform.Find("Background") is RectTransform background && background.TryGetComponent(out Image backgroundImage))
            {
                backgroundImage.sprite = PixelSkin.Slot();
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = Color.white;
            }

            // Isi bar diberi jarak dari bingkai krem supaya tampak "di dalam" kotak.
            if (slider.fillRect != null && slider.fillRect.parent is RectTransform fillArea)
            {
                fillArea.offsetMin = new Vector2(3f, 3f);
                fillArea.offsetMax = new Vector2(-3f, -3f);
                if (slider.fillRect.TryGetComponent(out Image fillImage))
                {
                    fillImage.sprite = PixelSkin.BarFill();
                    fillImage.type = Image.Type.Sliced;
                    fillImage.color = color;
                }
            }

            if (percentText != null)
            {
                Place(percentText, sliderRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                percentText.transform.SetAsLastSibling();
                Style(percentText, 12f, PixelSkin.TextDark, TextAlignmentOptions.Center);
            }
        }

        private static RectTransform NewImage(string name, Transform parent, Sprite sprite)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return rect;
        }

        private static void Place(TMP_Text text, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = text.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.SetAsLastSibling();
        }

        private static void Style(TMP_Text text, float size, Color color, TextAlignmentOptions alignment)
        {
            if (PixelSkin.Font != null) text.font = PixelSkin.Font;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

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
