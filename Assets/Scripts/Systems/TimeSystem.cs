using System;
using UnityEngine;

namespace Alif.Systems
{
    /// <summary>
    /// Sistem waktu in-game. Mengatur jam, hari, dan minggu, lalu berjalan otomatis
    /// dengan kecepatan yang bisa diatur dari Inspector.
    /// Contoh tampilan: "Tuesday, 1st week" dan jam "15:03" (format 24 jam).
    /// </summary>
    public class TimeSystem : MonoBehaviour
    {
        public static TimeSystem Instance { get; private set; }

        [Header("Kecepatan Waktu")]
        [Tooltip("Berapa menit in-game yang berlalu untuk setiap 1 detik real time.")]
        [SerializeField] private float _gameMinutesPerRealSecond = 1f;

        [Header("Waktu Saat Ini")]
        [SerializeField] private int _currentHour = 6;   // Format 24 jam, mulai jam 06:00
        [SerializeField] private int _currentMinute = 0;
        [SerializeField] private int _currentDayIndex = 0; // 0 = Monday, dst.
        [SerializeField] private int _currentWeek = 1;

        private float _minuteAccumulator = 0f;

        private static readonly string[] DayNames =
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };

        // Event yang dipanggil setiap kali menit berganti, berguna untuk update UI jam.
        public event Action OnMinuteChanged;

        // Event yang dipanggil setiap kali hari berganti, berguna untuk trigger event harian
        // (misalnya reset toko, jadwal NPC baru, dsb).
        public event Action OnDayChanged;

        public int CurrentHour => _currentHour;
        public int CurrentMinute => _currentMinute;
        public int CurrentWeek => _currentWeek;
        public string CurrentDayName => DayNames[_currentDayIndex];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            // Akumulasi waktu real time, lalu konversi ke menit in-game berdasarkan kecepatan.
            _minuteAccumulator += Time.deltaTime * _gameMinutesPerRealSecond;

            while (_minuteAccumulator >= 1f)
            {
                _minuteAccumulator -= 1f;
                AdvanceMinute();
            }
        }

        /// <summary>
        /// Majukan waktu sebanyak 1 menit in-game, lalu tangani pergantian jam/hari/minggu.
        /// </summary>
        private void AdvanceMinute()
        {
            _currentMinute++;

            if (_currentMinute >= 60)
            {
                _currentMinute = 0;
                _currentHour++;

                if (_currentHour >= 24)
                {
                    _currentHour = 0;
                    AdvanceDay();
                }
            }

            OnMinuteChanged?.Invoke();
        }

        /// <summary>
        /// Majukan hari sebanyak 1, dan tambah minggu jika sudah melewati hari Sunday.
        /// </summary>
        private void AdvanceDay()
        {
            _currentDayIndex++;

            if (_currentDayIndex >= DayNames.Length)
            {
                _currentDayIndex = 0;
                _currentWeek++;
            }

            OnDayChanged?.Invoke();
        }

        /// <summary>
        /// Mengubah angka minggu (1, 2, 3, ...) menjadi ordinal ("1st", "2nd", "3rd", "4th").
        /// </summary>
        private string GetOrdinalWeek(int week)
        {
            if (week % 100 is 11 or 12 or 13)
            {
                return week + "th";
            }

            return (week % 10) switch
            {
                1 => week + "st",
                2 => week + "nd",
                3 => week + "rd",
                _ => week + "th",
            };
        }

        /// <summary>
        /// Format tampilan hari + minggu, contoh: "Tuesday, 1st week".
        /// </summary>
        public string GetFormattedDay()
        {
            return $"{CurrentDayName}, {GetOrdinalWeek(_currentWeek)} week";
        }

        /// <summary>
        /// Format tampilan jam 24 jam dengan leading zero, contoh: "15:03".
        /// </summary>
        public string GetFormattedTime()
        {
            return $"{_currentHour:00}:{_currentMinute:00}";
        }

        /// <summary>
        /// Mengubah kecepatan waktu saat runtime, misalnya dipercepat saat tidur.
        /// </summary>
        public void SetTimeSpeed(float gameMinutesPerRealSecond)
        {
            _gameMinutesPerRealSecond = gameMinutesPerRealSecond;
        }
    }
}
