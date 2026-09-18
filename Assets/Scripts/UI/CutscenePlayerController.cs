using System.Collections;
using System.Collections.Generic;
using Alif.Campaign;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Alif.Core;

namespace Alif.UI
{
    /// <summary>
    /// Satu gambar dalam satu "scene" cutscene, ditampilkan penuh satu layar. Crop (0-1,
    /// dihitung builder) memotong sudut transparan panel komik yang miring; ukuran nol =
    /// gambar utuh. SpeakerName/Line kosong = panel ini nggak munculin kotak dialog.
    /// </summary>
    [System.Serializable]
    public struct CutscenePanelPart
    {
        public Sprite Image;
        public Rect Crop;
        public string SpeakerName;
        [TextArea] public string Line;
    }

    [System.Serializable]
    public struct CutsceneScene
    {
        public CutscenePanelPart[] Parts;

        // Kalau Parts kosong, scene ini dianggap "title card" teks doang (layar hitam polos +
        // teks di tengah, kayak intro RPG klasik) — dipakai buat prolog sebelum panel gambar.
        [TextArea] public string IntroText;
    }

    /// <summary>
    /// Cutscene bergaya visual novel: sederet "scene", tiap scene berisi 1-3 gambar layar penuh
    /// dengan kotak dialog ber-tab nama di bawah. Gambar pertama tiap scene muncul otomatis
    /// (zoom-out + fade di atas gambar sebelumnya), sisanya nunggu klik "Lanjut" satu-satu
    /// — bukan otomatis berturutan. Klik "Lanjut" saat animasi jalan = langsung selesaikan
    /// animasi panel itu instan; klik lagi setelah semua panel scene ini tampil = pindah ke
    /// scene berikutnya (atau ke scene gameplay kalau ini scene terakhir).
    /// </summary>
    public class CutscenePlayerController : MonoBehaviour
    {
        private const float RevealStartScale = 1.05f;
        private const float RevealDuration = 0.45f;

        [SerializeField] private CutsceneScene[] _scenes;
        [SerializeField] private Image[] _slotImages;
        [SerializeField] private RectTransform[] _slotRects;
        [SerializeField] private CanvasGroup[] _slotCanvasGroups;
        [SerializeField] private GameObject _dialogueBox;
        [SerializeField] private TMP_Text _speakerNameText;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private GameObject _introTextRoot;
        [SerializeField] private TMP_Text _introText;
        [SerializeField] private string _nextSceneName = "SampleScene";
        [Header("Chapter completion")]
        [Tooltip("0 untuk cutscene pembuka. Isi nomor chapter untuk cutscene penutup.")]
        [SerializeField] private int _completedChapterNumber;
        [SerializeField] private bool _unlockNextChapterOnFinish;

        private int _sceneIndex;
        private int _partIndex;
        private int _animatingSlotIndex = -1;
        private bool _isAnimating;
        private Coroutine _revealCoroutine;
        private readonly Dictionary<Sprite, Sprite> _croppedSprites = new Dictionary<Sprite, Sprite>();
        private AspectRatioFitter[] _slotFitters;
        private RectTransform _speakerTab;

        private void Awake()
        {
            ApplyNovelLayout();
        }

        private void OnDestroy()
        {
            foreach (Sprite cropped in _croppedSprites.Values)
            {
                Destroy(cropped);
            }
        }

        /// <summary>
        /// Scene dibangun builder dengan komposisi komik (panel kecil di tengah, overlay dialog
        /// hitam); tata letak visual novel dipasang di sini supaya tidak perlu rebuild scene:
        /// gambar menutupi layar, kotak dialog bingkai oranye-krem dengan tab nama di atasnya.
        /// </summary>
        private void ApplyNovelLayout()
        {
            _slotFitters = new AspectRatioFitter[_slotRects.Length];
            for (int i = 0; i < _slotRects.Length; i++)
            {
                if (_slotRects[i].parent is RectTransform container)
                {
                    Stretch(container);
                }

                _slotImages[i].preserveAspect = false;
                _slotFitters[i] = _slotRects[i].gameObject.AddComponent<AspectRatioFitter>();
                _slotFitters[i].aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            }

            if (_dialogueBox != null && _dialogueBox.transform is RectTransform box)
            {
                box.anchorMin = new Vector2(.03f, 0f);
                box.anchorMax = new Vector2(.97f, 0f);
                box.pivot = new Vector2(.5f, 0f);
                box.anchoredPosition = new Vector2(0f, 20f);
                box.sizeDelta = new Vector2(0f, 170f);
                if (box.TryGetComponent(out Image boxImage))
                {
                    PixelSkin.StylePanel(boxImage);
                }
            }

            if (_speakerNameText != null && _speakerNameText.transform.parent is RectTransform tab)
            {
                _speakerTab = tab;
                tab.anchorMin = tab.anchorMax = new Vector2(0f, 1f);
                tab.pivot = new Vector2(0f, .5f);
                tab.anchoredPosition = new Vector2(28f, 0f);
                tab.sizeDelta = new Vector2(180f, 40f);
                if (tab.TryGetComponent(out Image tabImage))
                {
                    tabImage.sprite = PixelSkin.Tab();
                    tabImage.type = Image.Type.Sliced;
                    tabImage.color = Color.white;
                }
                StyleText(_speakerNameText, 18, PixelSkin.TextDark);
                _speakerNameText.alignment = TextAlignmentOptions.Center;
            }

            if (_dialogueText != null)
            {
                StyleText(_dialogueText, 22, PixelSkin.TextDark);
                _dialogueText.alignment = TextAlignmentOptions.TopLeft;
                RectTransform textRect = _dialogueText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                // Kiri/atas: lewati bingkai & tab; kanan: sisakan ruang tombol LANJUT.
                textRect.offsetMin = new Vector2(30f, 22f);
                textRect.offsetMax = new Vector2(-200f, -32f);
            }

            if (_introText != null)
            {
                StyleText(_introText, 22, PixelSkin.Cream);
            }

            // LANJUT duduk di pojok kanan-bawah kotak dialog, di dalam bingkainya.
            if (transform.Find("NextButton") is RectTransform next)
            {
                next.anchoredPosition = new Vector2(-58f, 38f);
                next.sizeDelta = new Vector2(150f, 46f);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void StyleText(TMP_Text text, float size, Color color)
        {
            if (PixelSkin.Font != null) text.font = PixelSkin.Font;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = size;
            text.color = color;
            text.margin = Vector4.zero;
        }

        private void Start()
        {
            _sceneIndex = 0;
            PlayCurrentScene();
        }

        /// <summary>
        /// Keyboard: Space/Enter = LANJUT (menyelesaikan animasi panel yang sedang jalan,
        /// sama seperti klik), Escape = LEWATI. Cutscene jadi bisa dinikmati tanpa mouse.
        /// </summary>
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                OnNextClicked();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                OnSkipClicked();
            }
        }

        public void OnNextClicked()
        {
            bool isTextOnlyScene = IsTextOnlyScene(_scenes[_sceneIndex]);

            if (!isTextOnlyScene && _isAnimating)
            {
                CompleteAnimatingPartInstantly();
                return;
            }

            if (!isTextOnlyScene && _partIndex < _scenes[_sceneIndex].Parts.Length)
            {
                RevealNextPart();
                return;
            }

            _sceneIndex++;
            if (_sceneIndex >= _scenes.Length)
            {
                FinishCutscene();
                return;
            }

            PlayCurrentScene();
        }

        public void OnSkipClicked()
        {
            FinishCutscene();
        }

        private void FinishCutscene()
        {
            if (_completedChapterNumber > 0)
            {
                ChapterProgress.CompleteChapter(_completedChapterNumber, _unlockNextChapterOnFinish);
            }

            if (string.IsNullOrEmpty(_nextSceneName) || !Application.CanStreamedLevelBeLoaded(_nextSceneName))
            {
                Debug.LogError($"[Alif] Scene tujuan cutscene '{_nextSceneName}' tidak tersedia di Build Settings.");
                return;
            }

            SceneTransition.Load(_nextSceneName);
        }

        private static bool IsTextOnlyScene(CutsceneScene scene)
        {
            return scene.Parts == null || scene.Parts.Length == 0;
        }

        private void PlayCurrentScene()
        {
            _partIndex = 0;
            CutsceneScene scene = _scenes[_sceneIndex];

            if (IsTextOnlyScene(scene))
            {
                ClearAllSlots();
                if (_introTextRoot != null)
                {
                    _introTextRoot.SetActive(true);
                }

                if (_introText != null)
                {
                    _introText.text = scene.IntroText;
                }

                return;
            }

            if (_introTextRoot != null)
            {
                _introTextRoot.SetActive(false);
            }

            ClearAllSlots();
            RevealNextPart();
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
            HideSlotsBelow(index);
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

            _slotRects[index].localScale = Vector3.one;
            _slotCanvasGroups[index].alpha = 1f;
            _isAnimating = false;
            _animatingSlotIndex = -1;
            HideSlotsBelow(index);

            CutscenePanelPart part = _scenes[_sceneIndex].Parts[index];
            ShowDialogue(part.SpeakerName, part.Line);
        }

        private void SetupSlot(int index, CutscenePanelPart part)
        {
            // Hanya gambar lama yang ditimpa gambar baru ini yang disembunyikan setelah reveal —
            // gambar baru fade-in di atasnya (slot berindeks lebih besar tergambar di atas).
            Sprite sprite = CroppedSprite(part);
            _slotImages[index].sprite = sprite;
            if (sprite != null)
            {
                _slotFitters[index].aspectRatio = sprite.rect.width / sprite.rect.height;
            }

            _slotImages[index].gameObject.SetActive(true);
            _slotCanvasGroups[index].alpha = 0f;
            _slotRects[index].localScale = Vector3.one * RevealStartScale;
        }

        private void HideSlotsBelow(int index)
        {
            for (int i = 0; i < index; i++)
            {
                _slotImages[i].gameObject.SetActive(false);
            }
        }

        private Sprite CroppedSprite(CutscenePanelPart part)
        {
            Sprite source = part.Image;
            if (source == null || part.Crop.width <= 0f || part.Crop.height <= 0f)
            {
                return source;
            }

            if (_croppedSprites.TryGetValue(source, out Sprite cropped))
            {
                return cropped;
            }

            // Crop dinormalisasi terhadap sprite, jadi tetap benar walau tekstur diperkecil importer.
            Rect full = source.rect;
            float xMin = Mathf.Ceil(full.x + part.Crop.xMin * full.width);
            float yMin = Mathf.Ceil(full.y + part.Crop.yMin * full.height);
            float xMax = Mathf.Floor(full.x + part.Crop.xMax * full.width);
            float yMax = Mathf.Floor(full.y + part.Crop.yMax * full.height);
            cropped = Sprite.Create(source.texture, Rect.MinMaxRect(xMin, yMin, xMax, yMax), new Vector2(.5f, .5f), source.pixelsPerUnit);
            _croppedSprites[source] = cropped;
            return cropped;
        }

        private IEnumerator RevealSlotRoutine(int index)
        {
            RectTransform rt = _slotRects[index];
            CanvasGroup group = _slotCanvasGroups[index];

            float t = 0f;
            while (t < RevealDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / RevealDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                rt.localScale = Vector3.one * Mathf.Lerp(RevealStartScale, 1f, eased);
                group.alpha = eased;
                yield return null;
            }

            rt.localScale = Vector3.one;
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

            if (_speakerTab != null)
            {
                bool hasSpeaker = !string.IsNullOrEmpty(speakerName);
                _speakerTab.gameObject.SetActive(hasSpeaker);
                if (hasSpeaker)
                {
                    _speakerTab.sizeDelta = new Vector2(_speakerNameText.GetPreferredValues(speakerName).x + 44f, _speakerTab.sizeDelta.y);
                }
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = line;
            }
        }
    }
}
