using Alif.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Satu kartu chapter di scene Chapter Select. Nomor chapter dipakai
    /// ChapterSelectController buat menentukan terkunci/tidaknya kartu ini.
    /// Badge progres ("✓ SELESAI" / "▶ DIPUTAR") dibuat runtime — tidak perlu
    /// perubahan scene builder.
    /// </summary>
    public class ChapterCardUI : MonoBehaviour
    {
        [SerializeField] private int _chapterNumber;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _lockOverlay;

        private TMP_Text _progressBadge;

        public int ChapterNumber => _chapterNumber;

        public void SetLocked(bool locked)
        {
            if (_button != null)
            {
                _button.interactable = !locked;
            }

            if (_lockOverlay != null)
            {
                _lockOverlay.SetActive(locked);
            }
        }

        /// <summary>
        /// Tampilkan status progres di kartu: hijau "✓ SELESAI" untuk chapter yang tamat,
        /// emas "▶ DIPUTAR" untuk chapter terpilih yang masih berjalan. Kartu terkunci
        /// tidak menampilkan badge (ikon gemboknya sudah cukup).
        /// </summary>
        public void RefreshProgress()
        {
            bool completed = ChapterProgress.IsCompleted(_chapterNumber);
            bool current = !completed && ChapterProgress.SelectedChapter == _chapterNumber
                && ChapterProgress.IsUnlocked(_chapterNumber);

            if (_progressBadge == null && !completed && !current) return;
            EnsureBadge();
            _progressBadge.gameObject.SetActive(completed || current);
            _progressBadge.text = completed ? "✓ SELESAI" : "▶ DIPUTAR";
            _progressBadge.color = completed ? new Color(.48f, .78f, .39f) : new Color(.95f, .69f, .2f);
        }

        private void EnsureBadge()
        {
            if (_progressBadge != null) return;

            var go = new GameObject("ProgressBadge", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -6f);
            rect.sizeDelta = new Vector2(210f, 28f);

            _progressBadge = go.AddComponent<TextMeshProUGUI>();
            _progressBadge.fontSize = 19f;
            _progressBadge.fontStyle = FontStyles.Bold;
            _progressBadge.alignment = TextAlignmentOptions.Center;
            _progressBadge.raycastTarget = false;
        }
    }
}
