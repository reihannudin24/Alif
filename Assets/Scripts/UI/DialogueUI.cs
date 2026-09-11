using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alif.Dialogue;

namespace Alif.UI
{
    /// <summary>
    /// Tampilan dialogue box: nama pembicara, teks dialog, tombol "next", dan tombol-tombol
    /// pilihan (choices) saat dialog bercabang. Murni tampilan — semua logic alur dialog
    /// ada di DialogueManager, script ini hanya mendengarkan event dan menampilkan/menyembunyikan UI.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _dialogueBoxRoot;
        [Tooltip("RectTransform Dialogue_Panel — tingginya diubah dinamis (lihat ResizePanelForChoices) supaya tombol pilihan nggak pernah overflow ke luar box.")]
        [SerializeField] private RectTransform _panelRect;

        [Header("Line Display")]
        [SerializeField] private TMP_Text _speakerNameText;
        [SerializeField] private TMP_Text _dialogueText;
        [Tooltip("Pesan singkat khusus pilihan yang ditolak, supaya tidak menimpa teks dialog atau tombol pilihan.")]
        [SerializeField] private TMP_Text _choiceStatusText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Button _nextButton;

        [Header("Choices")]
        [Tooltip("Prefab tombol pilihan, harus punya komponen Button + TMP_Text + LayoutElement sebagai child.")]
        [SerializeField] private Button _choiceButtonPrefab;
        [SerializeField] private Transform _choiceButtonContainer;
        [Tooltip("Tinggi Dialogue_Panel saat TIDAK ada pilihan (baris dialog biasa dengan tombol Lanjut).")]
        [SerializeField] private float _normalPanelHeight = 180f;
        [Tooltip("Ruang di atas container pilihan yang disisakan buat nama pembicara + baris prompt (mis. \"Pilih respons Alif:\").")]
        [SerializeField] private float _choiceTopReservedSpace = 78f;
        [SerializeField] private float _choiceBottomPadding = 16f;
        [SerializeField] private float _choiceButtonSpacing = 8f;
        [Tooltip("Batas atas tinggi panel walau teks pilihan sangat panjang/banyak, supaya nggak sampai keluar layar.")]
        [SerializeField] private float _maxChoicePanelHeight = 460f;
        [SerializeField] private float _minChoiceButtonHeight = 44f;
        [SerializeField] private float _choiceButtonVerticalPadding = 16f;

        [Header("Klik Di Luar Untuk Lanjut")]
        [Tooltip("Lapisan penuh-layar transparan di belakang portrait — klik di sini (bukan di teks/portrait/tombol) dihitung sama kayak klik Next.")]
        [SerializeField] private Button _advanceCatcherButton;

        [Header("Sembunyikan Saat Dialog Aktif")]
        [Tooltip("Virtual joystick — disembunyikan selama dialog (movement udah terkunci, jadi nggak relevan) lalu dimunculkan lagi begitu dialog selesai.")]
        [SerializeField] private GameObject _joystickRoot;
        [Tooltip("Tombol interact 'E' on-screen.")]
        [SerializeField] private GameObject _interactButtonRoot;
        [Tooltip("Bar inventory di bawah layar.")]
        [SerializeField] private GameObject _inventoryPanelRoot;
        [Tooltip("Tombol pause di pojok layar.")]
        [SerializeField] private GameObject _pauseButtonRoot;
        [Tooltip("Objective disembunyikan selama dialog agar tidak bertumpuk dengan panel pilihan yang membesar ke atas.")]
        [SerializeField] private GameObject _objectivePanelRoot;

        [Header("Audio")]
        [SerializeField] private Alif.Core.AudioManager _audioManager;
        [SerializeField] private AudioClip _buttonClickSfx;

        [Header("Portrait Animation")]
        [Tooltip("Lama animasi slide-in + fade-in portrait tiap kali pembicara berganti (detik).")]
        [SerializeField] private float _portraitEnterDuration = 0.35f;
        [Tooltip("Jarak geser awal portrait dari posisi diamnya (px), datang dari kanan.")]
        [SerializeField] private float _portraitEnterOffsetX = 120f;

        private readonly List<GameObject> _spawnedChoiceButtons = new List<GameObject>();
        private DialogueLine _currentLine;

        private RectTransform _portraitRect;
        private CanvasGroup _portraitCanvasGroup;
        private Vector2 _portraitRestPosition;
        private bool _portraitAnimStateReady;
        private Sprite _lastPortraitSprite;
        private Coroutine _portraitEnterRoutine;

        private void Awake()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(HandleNextClicked);
            }

            if (_advanceCatcherButton != null)
            {
                _advanceCatcherButton.onClick.AddListener(HandleBackgroundClicked);
            }

            // Background box dialog sendiri juga punya Button (dipasang di scene builder) buat
            // area di dalam box yang nggak ke-tutup teks — dengerin klik-nya di sini juga.
            if (_dialogueBoxRoot != null)
            {
                Button panelButton = _dialogueBoxRoot.GetComponent<Button>();
                if (panelButton != null)
                {
                    panelButton.onClick.AddListener(HandleBackgroundClicked);
                }
            }
        }

        private bool _isSubscribed;

        private void OnEnable()
        {
            // DialogueManager.Instance kadang belum sempat di-set (Awake antar GameObject
            // beda root nggak dijamin urutannya, apalagi DialogueManager dipindah ke
            // DontDestroyOnLoad di tengah proses Awake) — kalau langsung subscribe di sini
            // dan Instance-nya masih null, DialogueUI SELAMANYA nggak pernah dengerin event
            // apa pun (nggak ada retry). Makanya di-loop tiap frame sampai beneran siap.
            if (DialogueManager.Instance != null)
            {
                SubscribeToDialogueManager();
            }
            else
            {
                StartCoroutine(SubscribeWhenReady());
            }

            SetBoxVisible(false);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            UnsubscribeFromDialogueManager();
        }

        private IEnumerator SubscribeWhenReady()
        {
            while (DialogueManager.Instance == null)
            {
                yield return null;
            }

            SubscribeToDialogueManager();
        }

        private void SubscribeToDialogueManager()
        {
            if (_isSubscribed || DialogueManager.Instance == null)
            {
                return;
            }

            DialogueManager.Instance.OnDialogueStarted += HandleDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            DialogueManager.Instance.OnLineDisplayed += HandleLineDisplayed;
            DialogueManager.Instance.OnChoiceRejected += HandleChoiceRejected;
            _isSubscribed = true;
        }

        private void UnsubscribeFromDialogueManager()
        {
            if (!_isSubscribed || DialogueManager.Instance == null)
            {
                _isSubscribed = false;
                return;
            }

            DialogueManager.Instance.OnDialogueStarted -= HandleDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            DialogueManager.Instance.OnLineDisplayed -= HandleLineDisplayed;
            DialogueManager.Instance.OnChoiceRejected -= HandleChoiceRejected;
            _isSubscribed = false;
        }

        private void HandleChoiceRejected(string reason)
        {
            if (_choiceStatusText != null)
            {
                _choiceStatusText.text = reason;
                _choiceStatusText.gameObject.SetActive(true);
            }
        }

        private void HandleDialogueStarted()
        {
            ClearChoiceStatus();
            SetBoxVisible(true);
        }

        private void HandleDialogueEnded()
        {
            SetBoxVisible(false);
            ClearChoiceButtons();
            ClearChoiceStatus();

            // Reset supaya dialog berikutnya (walau kebetulan mulai dari NPC yang sama) tetap
            // mainin animasi masuk dari awal, bukan dianggap "pembicara sama, skip animasi".
            _lastPortraitSprite = null;
        }

        /// <summary>
        /// Render baris dialog baru: update nama + teks, lalu tampilkan tombol "next"
        /// atau tombol-tombol pilihan tergantung apakah baris ini punya Choices.
        /// </summary>
        private void HandleLineDisplayed(DialogueLine line)
        {
            _currentLine = line;
            ClearChoiceStatus();

            if (_speakerNameText != null)
            {
                _speakerNameText.text = line.SpeakerName;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = line.Text;
            }

            if (_portraitImage != null)
            {
                EnsurePortraitAnimState();

                Sprite portrait = DialogueManager.Instance != null ? DialogueManager.Instance.CurrentSpeakerPortrait : null;
                // Cuma mainin animasi masuk (slide + fade) waktu pembicaranya BENERAN ganti,
                // bukan tiap baris — kalau NPC yang sama ngomong beberapa baris berturut-turut,
                // portrait-nya diem aja di tempat, nggak keluar-masuk tiap klik "next".
                bool isNewSpeaker = portrait != null && portrait != _lastPortraitSprite;

                _portraitImage.sprite = portrait;
                _portraitImage.enabled = portrait != null;
                _lastPortraitSprite = portrait;

                if (isNewSpeaker)
                {
                    PlayPortraitEnterAnimation();
                }
            }

            ClearChoiceButtons();

            if (line.HasChoices)
            {
                SpawnChoiceButtons(line.Choices);
                SetNextButtonVisible(false);
            }
            else
            {
                SetNextButtonVisible(true);
            }
        }

        private void SpawnChoiceButtons(List<DialogueChoice> choices)
        {
            if (_choiceButtonPrefab == null || _choiceButtonContainer == null)
            {
                return;
            }

            // Lebar container SUDAH valid sekarang walau belum ada layout pass (anchor-nya
            // stretch horizontal terhadap panel), jadi bisa langsung dipakai buat mengukur
            // berapa tinggi tiap tombol butuh SEBELUM tombolnya benar-benar di-layout —
            // menghindari delay 1 frame yang biasanya muncul kalau andalkan ContentSizeFitter.
            RectTransform containerRect = _choiceButtonContainer as RectTransform;
            float availableWidth = containerRect != null ? containerRect.rect.width : 800f;

            float totalContentHeight = 0f;

            foreach (DialogueChoice choice in choices)
            {
                Button choiceButton = Instantiate(_choiceButtonPrefab, _choiceButtonContainer);
                choiceButton.gameObject.SetActive(true);

                TMP_Text label = choiceButton.GetComponentInChildren<TMP_Text>();
                float buttonHeight = _minChoiceButtonHeight;
                if (label != null)
                {
                    label.text = choice.ChoiceText;

                    float textWidth = Mathf.Max(40f, availableWidth - label.margin.x - label.margin.z);
                    Vector2 preferred = label.GetPreferredValues(textWidth, 0f);
                    buttonHeight = Mathf.Max(_minChoiceButtonHeight, preferred.y + _choiceButtonVerticalPadding);
                }

                LayoutElement layoutElement = choiceButton.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = buttonHeight;
                }

                totalContentHeight += buttonHeight;

                // Simpan referensi choice lewat closure supaya klik tombol memanggil choice yang benar.
                choiceButton.onClick.AddListener(() =>
                {
                    PlayClickSfx();
                    DialogueManager.Instance.SelectChoice(choice);
                });

                _spawnedChoiceButtons.Add(choiceButton.gameObject);
            }

            if (choices.Count > 1)
            {
                totalContentHeight += _choiceButtonSpacing * (choices.Count - 1);
            }

            ResizePanelForChoices(totalContentHeight);
        }

        /// <summary>
        /// Panel dialog anchor-nya di bawah layar (pivot bottom) jadi membesarkan sizeDelta.y
        /// tumbuh ke ATAS, bukan menutupi HUD — dipanggil dengan tinggi total konten pilihan
        /// (0 kalau nggak ada pilihan sama sekali, otomatis ke-clamp balik ke _normalPanelHeight).
        /// </summary>
        private void ResizePanelForChoices(float choicesContentHeight)
        {
            if (_panelRect == null)
            {
                return;
            }

            float targetHeight = Mathf.Clamp(
                _choiceTopReservedSpace + choicesContentHeight + _choiceBottomPadding,
                _normalPanelHeight, _maxChoicePanelHeight);

            Vector2 size = _panelRect.sizeDelta;
            size.y = targetHeight;
            _panelRect.sizeDelta = size;
        }

        private void ClearChoiceButtons()
        {
            foreach (GameObject buttonObject in _spawnedChoiceButtons)
            {
                Destroy(buttonObject);
            }

            _spawnedChoiceButtons.Clear();
            ResizePanelForChoices(0f);
        }

        private void ClearChoiceStatus()
        {
            if (_choiceStatusText == null)
            {
                return;
            }

            _choiceStatusText.text = string.Empty;
            _choiceStatusText.gameObject.SetActive(false);
        }

        private void HandleNextClicked()
        {
            PlayClickSfx();
            DialogueManager.Instance.AdvanceDialogue();
        }

        /// <summary>
        /// Klik di area lain (bukan teks/portrait/tombol) — sama kayak klik Next, TAPI cuma
        /// kalau NextButton emang lagi ditampilkan. Kalau lagi nunjukin choices, klik sembarangan
        /// nggak boleh nge-skip pemilihan pilihan (user wajib klik salah satu choice-nya).
        /// </summary>
        private void HandleBackgroundClicked()
        {
            if (_nextButton == null || !_nextButton.gameObject.activeSelf)
            {
                return;
            }

            PlayClickSfx();
            DialogueManager.Instance.AdvanceDialogue();
        }

        private void PlayClickSfx()
        {
            if (_audioManager != null && _buttonClickSfx != null)
            {
                _audioManager.PlaySfx(_buttonClickSfx);
            }
        }

        private void SetNextButtonVisible(bool visible)
        {
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Cache RectTransform/posisi diam portrait + pasang CanvasGroup (buat fade) sekali di
        /// awal. Ditunda sampai portrait pertama kali dipakai (bukan di Awake) karena posisi
        /// diamnya (anchoredPosition) baru valid setelah layout dari scene builder ke-load.
        /// </summary>
        private void EnsurePortraitAnimState()
        {
            if (_portraitAnimStateReady || _portraitImage == null)
            {
                return;
            }

            _portraitRect = _portraitImage.rectTransform;
            _portraitRestPosition = _portraitRect.anchoredPosition;

            _portraitCanvasGroup = _portraitImage.GetComponent<CanvasGroup>();
            if (_portraitCanvasGroup == null)
            {
                _portraitCanvasGroup = _portraitImage.gameObject.AddComponent<CanvasGroup>();
            }

            // Portrait SENGAJA nge-block raycast (bukan pass-through) — klik di area karakter
            // harus "diserap" diam-diam, TIDAK dihitung sebagai klik "area lain" yang mestinya
            // memicu lanjut/tutup dialog (lihat DialogueAdvanceCatcher di belakangnya). Ini aman
            // buat NextButton/tombol choice karena keduanya ada di Dialogue_Panel yang sekarang
            // di-render DI DEPAN portrait (lihat urutan sibling di AlifDemoSceneBuilder).
            _portraitCanvasGroup.blocksRaycasts = true;
            _portraitCanvasGroup.interactable = false;

            _portraitAnimStateReady = true;
        }

        private void PlayPortraitEnterAnimation()
        {
            if (!_portraitAnimStateReady)
            {
                return;
            }

            if (_portraitEnterRoutine != null)
            {
                StopCoroutine(_portraitEnterRoutine);
            }

            _portraitEnterRoutine = StartCoroutine(PortraitEnterRoutine());
        }

        /// <summary>
        /// Slide-in dari kanan + fade-in, ease-out cubic. Posisi start selalu dihitung dari
        /// _portraitRestPosition (bukan posisi rect saat ini) supaya kalau kepotong/ke-interupsi
        /// di tengah jalan, animasi berikutnya tetap mulai dari titik yang benar.
        /// </summary>
        private IEnumerator PortraitEnterRoutine()
        {
            Vector2 startPosition = _portraitRestPosition + new Vector2(_portraitEnterOffsetX, 0f);
            float elapsed = 0f;

            while (elapsed < _portraitEnterDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _portraitEnterDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);

                _portraitRect.anchoredPosition = Vector2.Lerp(startPosition, _portraitRestPosition, eased);
                _portraitCanvasGroup.alpha = eased;

                yield return null;
            }

            _portraitRect.anchoredPosition = _portraitRestPosition;
            _portraitCanvasGroup.alpha = 1f;
            _portraitEnterRoutine = null;
        }

        private void SetBoxVisible(bool visible)
        {
            if (_dialogueBoxRoot != null)
            {
                _dialogueBoxRoot.SetActive(visible);
            }

            // Portrait & advance-catcher sekarang GameObject terpisah (bukan child dialogueBoxRoot
            // lagi) — jadi harus di-toggle manual bareng box-nya di sini.
            if (_portraitImage != null)
            {
                _portraitImage.gameObject.SetActive(visible);
            }

            if (_advanceCatcherButton != null)
            {
                _advanceCatcherButton.gameObject.SetActive(visible);
            }

            // Joystick, tombol interact, & bar inventory nggak relevan waktu dialog (movement
            // udah terkunci) dan cuma nutupin layar visual novel — sembunyikan pas dialog
            // muncul, munculin lagi pas dialog selesai.
            if (_joystickRoot != null)
            {
                _joystickRoot.SetActive(!visible);
            }

            if (_interactButtonRoot != null)
            {
                _interactButtonRoot.SetActive(!visible);
            }

            if (_inventoryPanelRoot != null)
            {
                _inventoryPanelRoot.SetActive(!visible);
            }

            if (_pauseButtonRoot != null)
            {
                _pauseButtonRoot.SetActive(!visible);
            }

            if (_objectivePanelRoot != null)
            {
                _objectivePanelRoot.SetActive(!visible);
            }
        }
    }
}
