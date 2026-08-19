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

        [Header("Line Display")]
        [SerializeField] private TMP_Text _speakerNameText;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Button _nextButton;

        [Header("Choices")]
        [Tooltip("Prefab tombol pilihan, harus punya komponen Button + TMP_Text sebagai child.")]
        [SerializeField] private Button _choiceButtonPrefab;
        [SerializeField] private Transform _choiceButtonContainer;

        private readonly List<GameObject> _spawnedChoiceButtons = new List<GameObject>();
        private DialogueLine _currentLine;

        private void Awake()
        {
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(HandleNextClicked);
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
            _isSubscribed = false;
        }

        private void HandleDialogueStarted()
        {
            SetBoxVisible(true);
        }

        private void HandleDialogueEnded()
        {
            SetBoxVisible(false);
            ClearChoiceButtons();
        }

        /// <summary>
        /// Render baris dialog baru: update nama + teks, lalu tampilkan tombol "next"
        /// atau tombol-tombol pilihan tergantung apakah baris ini punya Choices.
        /// </summary>
        private void HandleLineDisplayed(DialogueLine line)
        {
            _currentLine = line;

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
                Sprite portrait = DialogueManager.Instance != null ? DialogueManager.Instance.CurrentSpeakerPortrait : null;
                _portraitImage.sprite = portrait;
                _portraitImage.enabled = portrait != null;
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

            foreach (DialogueChoice choice in choices)
            {
                Button choiceButton = Instantiate(_choiceButtonPrefab, _choiceButtonContainer);
                choiceButton.gameObject.SetActive(true);

                TMP_Text label = choiceButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = choice.ChoiceText;
                }

                // Simpan referensi choice lewat closure supaya klik tombol memanggil choice yang benar.
                choiceButton.onClick.AddListener(() => DialogueManager.Instance.SelectChoice(choice));

                _spawnedChoiceButtons.Add(choiceButton.gameObject);
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (GameObject buttonObject in _spawnedChoiceButtons)
            {
                Destroy(buttonObject);
            }

            _spawnedChoiceButtons.Clear();
        }

        private void HandleNextClicked()
        {
            DialogueManager.Instance.AdvanceDialogue();
        }

        private void SetNextButtonVisible(bool visible)
        {
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(visible);
            }
        }

        private void SetBoxVisible(bool visible)
        {
            if (_dialogueBoxRoot != null)
            {
                _dialogueBoxRoot.SetActive(visible);
            }
        }
    }
}
