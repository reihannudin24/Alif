using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Alif.UI
{
    /// <summary>
    /// Satu potongan gambar di dalam komposisi satu "scene" cutscene (mis. panel atas, panel
    /// kiri-bawah, dst). AnchorMin/AnchorMax menentukan posisinya di dalam PanelContainer
    /// (0-1, sama seperti anchor RectTransform biasa) — jadi satu scene bisa berupa 2 panel
    /// (atas 50% + bawah 50%) atau 3 panel (atas 50% + bawah kiri 25% + bawah kanan 25%).
    /// SpeakerName/Line kosong = panel ini nggak munculin dialogue overlay.
    /// </summary>
    [System.Serializable]
    public struct CutscenePanelPart
    {
        public Sprite Image;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public string SpeakerName;
        [TextArea] public string Line;
    }

    [System.Serializable]
    public struct CutsceneScene
    {
        public CutscenePanelPart[] Parts;
    }

    /// <summary>
    /// Cutscene pembuka: sederet "scene", tiap scene terdiri dari 2-3 panel gambar. Panel
    /// pertama tiap scene muncul otomatis (slide+fade), sisanya nunggu klik "Lanjut" satu-satu
    /// — bukan otomatis berturutan. Klik "Lanjut" saat animasi jalan = langsung selesaikan
    /// animasi panel itu instan; klik lagi setelah semua panel scene ini tampil = pindah ke
    /// scene berikutnya (atau ke scene gameplay kalau ini scene terakhir).
    /// </summary>
    public class CutscenePlayerController : MonoBehaviour
    {
        private const float SlideDistance = 60f;
        private const float RevealDuration = 0.45f;

        [SerializeField] private CutsceneScene[] _scenes;
        [SerializeField] private Image[] _slotImages;
        [SerializeField] private RectTransform[] _slotRects;
        [SerializeField] private CanvasGroup[] _slotCanvasGroups;
        [SerializeField] private GameObject _dialogueBox;
        [SerializeField] private TMP_Text _speakerNameText;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private string _nextSceneName = "SampleScene";

        private int _sceneIndex;
        private int _partIndex;
        private int _animatingSlotIndex = -1;
        private bool _isAnimating;
        private Coroutine _revealCoroutine;

        private void Start()
        {
            _sceneIndex = 0;
            _partIndex = 0;
            ClearAllSlots();
            RevealNextPart();
        }

        public void OnNextClicked()
        {
            if (_isAnimating)
            {
                CompleteAnimatingPartInstantly();
                return;
            }

            CutscenePanelPart[] parts = _scenes[_sceneIndex].Parts;
            if (_partIndex < parts.Length)
            {
                RevealNextPart();
                return;
            }

            _sceneIndex++;
            _partIndex = 0;
            if (_sceneIndex >= _scenes.Length)
            {
                SceneManager.LoadScene(_nextSceneName);
                return;
            }

            ClearAllSlots();
            RevealNextPart();
        }

        public void OnSkipClicked()
        {
            SceneManager.LoadScene(_nextSceneName);
        }

        private void ClearAllSlots()
        {
            ShowDialogue("", "");
            for (int i = 0; i < _slotImages.Length; i++)
            {
                _slotImages[i].gameObject.SetActive(false);
            }
        }

        private void RevealNextPart()
        {
            int index = _partIndex;
            CutscenePanelPart part = _scenes[_sceneIndex].Parts[index];

            SetupSlot(index, part);
            _animatingSlotIndex = index;
            _revealCoroutine = StartCoroutine(RevealThenShowDialogueRoutine(index, part));
            _partIndex++;
        }

        private IEnumerator RevealThenShowDialogueRoutine(int index, CutscenePanelPart part)
        {
            _isAnimating = true;
            yield return StartCoroutine(RevealSlotRoutine(index));
            _isAnimating = false;
            _animatingSlotIndex = -1;
            ShowDialogue(part.SpeakerName, part.Line);
        }

        private void CompleteAnimatingPartInstantly()
        {
            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
            }

            int index = _animatingSlotIndex;
            if (index < 0)
            {
                return;
            }

            _slotRects[index].anchoredPosition = Vector2.zero;
            _slotCanvasGroups[index].alpha = 1f;
            _isAnimating = false;
            _animatingSlotIndex = -1;

            CutscenePanelPart part = _scenes[_sceneIndex].Parts[index];
            ShowDialogue(part.SpeakerName, part.Line);
        }

        private void SetupSlot(int index, CutscenePanelPart part)
        {
            RectTransform rt = _slotRects[index];
            rt.anchorMin = part.AnchorMin;
            rt.anchorMax = part.AnchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _slotImages[index].sprite = part.Image;
            _slotImages[index].gameObject.SetActive(true);
            _slotCanvasGroups[index].alpha = 0f;

            // Panel di separuh atas slide turun dari atas, panel di separuh bawah slide naik
            // dari bawah — arah ditentukan dari posisi vertikalnya sendiri, bukan field terpisah.
            float verticalCenter = (part.AnchorMin.y + part.AnchorMax.y) / 2f;
            float fromY = verticalCenter >= 0.5f ? SlideDistance : -SlideDistance;
            rt.anchoredPosition = new Vector2(0f, fromY);
        }

        private IEnumerator RevealSlotRoutine(int index)
        {
            RectTransform rt = _slotRects[index];
            CanvasGroup group = _slotCanvasGroups[index];
            Vector2 startPos = rt.anchoredPosition;

            float t = 0f;
            while (t < RevealDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / RevealDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                rt.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, eased);
                group.alpha = eased;
                yield return null;
            }

            rt.anchoredPosition = Vector2.zero;
            group.alpha = 1f;
        }

        private void ShowDialogue(string speakerName, string line)
        {
            bool hasLine = !string.IsNullOrEmpty(line);
            if (_dialogueBox != null)
            {
                _dialogueBox.SetActive(hasLine);
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = speakerName;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = line;
            }
        }
    }
}
