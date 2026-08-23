using UnityEngine;
using UnityEngine.UI;

namespace Alif.UI
{
    /// <summary>
    /// Satu kartu chapter di scene Chapter Select. Nomor chapter dipakai
    /// ChapterSelectController buat menentukan terkunci/tidaknya kartu ini.
    /// </summary>
    public class ChapterCardUI : MonoBehaviour
    {
        [SerializeField] private int _chapterNumber;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _lockOverlay;

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
    }
}
