using System;
using System.Collections.Generic;
using UnityEngine;
using Alif.Characters;
using Alif.Core;
using Alif.Player;
using Alif.Systems;

namespace Alif.Dialogue
{
    /// <summary>
    /// Mengatur alur dialog: menampilkan baris demi baris, menangani pilihan (branching),
    /// dan memicu event setelah dialog selesai. Tampilan visual (dialogue box UI)
    /// sebaiknya diurus oleh DialogueUI yang mendengarkan event dari class ini,
    /// supaya logic dan tampilan tetap terpisah.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Opsional: referensi ke PlayerController untuk mengunci pergerakan saat dialog berjalan.")]
        [SerializeField] private PlayerController _playerController;

        private DialogueData _currentDialogue;
        private CharacterData _currentSpeakerData;
        private CharacterData _currentLineSpeakerData;
        private int _currentLineIndex;

        public bool IsDialogueActive { get; private set; }

        /// <summary>
        /// Baris yang sedang ditampilkan saat ini, atau null kalau tidak ada dialog aktif.
        /// Dipakai DialogueUI untuk menampilkan ULANG baris yang sedang berjalan bila UI-nya
        /// terlambat subscribe (manager bertahan lintas scene via DontDestroyOnLoad) atau
        /// setelah domain reload di tengah Play Mode — tanpa ini pemain bisa terkunci oleh
        /// dialog yang kotaknya tidak pernah muncul.
        /// </summary>
        public DialogueLine CurrentLine => IsDialogueActive && _currentDialogue != null &&
            _currentLineIndex >= 0 && _currentLineIndex < _currentDialogue.Lines.Count
            ? _currentDialogue.Lines[_currentLineIndex]
            : null;

        /// <summary>
        /// Portrait pembicara saat ini (dari CharacterData yang di-pass ke StartDialogue),
        /// dibaca DialogueUI untuk ditampilkan di dialogue box. Null kalau dialog nggak
        /// punya speakerData (misal monolog tanpa portrait).
        /// </summary>
        public Sprite CurrentSpeakerPortrait => _currentLineSpeakerData != null ? _currentLineSpeakerData.Portrait : null;

        // Event untuk DialogueUI: dipanggil setiap kali baris dialog baru ditampilkan.
        public event Action<DialogueLine> OnLineDisplayed;

        // Dipanggil setelah dampak skor sebuah pilihan diterapkan. UI dan quest system bisa
        // mendengarkan event ini tanpa perlu tahu detail dialog yang sedang berjalan.
        public event Action<DialogueChoice> OnChoiceSelected;

        // Pilihan berbiaya yang uangnya tidak cukup tidak memajukan dialog. DialogueUI memakai
        // event ini untuk memberi alasan sambil tetap membiarkan tombol pilihan tampil.
        public event Action<string> OnChoiceRejected;

        // Event untuk DialogueUI: dipanggil saat dialog dimulai/selesai.
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<DialogueData> OnDialogueCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.gameObject.scene != gameObject.scene)
                {
                    Instance = this;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Mulai dialog baru dari DialogueData tertentu. Dipanggil oleh NPCController saat
        /// pemain berinteraksi dengan NPC.
        /// </summary>
        public void StartDialogue(DialogueData dialogueData, CharacterData speakerData = null)
        {
            if (dialogueData == null || dialogueData.Lines == null || dialogueData.Lines.Count == 0)
            {
                Debug.LogWarning("DialogueData kosong, tidak ada yang bisa ditampilkan.");
                return;
            }

            _currentDialogue = dialogueData;
            _currentSpeakerData = speakerData;
            _currentLineIndex = 0;
            IsDialogueActive = true;
            // Managers survive scene changes; their original player reference does not.
            if (_playerController == null) _playerController = FindFirstObjectByType<PlayerController>();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Dialogue);
            }

            if (_playerController != null)
            {
                _playerController.SetMovementLocked(this, true);
            }

            OnDialogueStarted?.Invoke();
            ShowCurrentLine();
        }

        /// <summary>
        /// Tampilkan baris dialog pada index saat ini via event, supaya DialogueUI bisa render teksnya.
        /// </summary>
        private void ShowCurrentLine()
        {
            DialogueLine line = _currentDialogue.Lines[_currentLineIndex];
            _currentLineSpeakerData = line.SpeakerData != null ? line.SpeakerData : _currentSpeakerData;
            OnLineDisplayed?.Invoke(line);
        }

        /// <summary>
        /// Lanjut ke baris berikutnya. Dipanggil oleh DialogueUI saat pemain klik "next"
        /// pada baris yang tidak punya pilihan (linear dialogue).
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!IsDialogueActive || GameManager.Instance?.CurrentState == GameManager.GameState.Paused)
            {
                return;
            }

            DialogueLine currentLine = _currentDialogue.Lines[_currentLineIndex];
            if (currentLine.HasChoices)
            {
                // Baris dengan pilihan harus dilanjutkan lewat SelectChoice, bukan AdvanceDialogue biasa.
                return;
            }

            int nextIndex = currentLine.NextLineIndex >= 0
                ? currentLine.NextLineIndex
                : _currentLineIndex + 1;
            if (nextIndex >= _currentDialogue.Lines.Count)
            {
                EndDialogue();
                return;
            }

            _currentLineIndex = nextIndex;
            ShowCurrentLine();
        }

        /// <summary>
        /// Dipanggil oleh DialogueUI saat pemain memilih salah satu pilihan (choice) pada
        /// baris dialog yang bercabang. Menangani lompatan ke baris tujuan dan perubahan affinity.
        /// </summary>
        public void SelectChoice(DialogueChoice choice)
        {
            if (!IsDialogueActive || choice == null || GameManager.Instance?.CurrentState == GameManager.GameState.Paused)
            {
                return;
            }

            if (choice.MoneyCost > 0)
            {
                CurrencySystem currency = CurrencySystem.Instance;
                if (currency == null)
                {
                    OnChoiceRejected?.Invoke("Sistem uang belum siap. Coba pilih kembali sebentar lagi.");
                    return;
                }

                if (!currency.SpendMoney(choice.MoneyCost))
                {
                    int shortage = Mathf.Max(0, choice.MoneyCost - currency.CurrentMoney);
                    OnChoiceRejected?.Invoke(
                        $"Uang tunai kurang {CurrencySystem.FormatRupiah(shortage)}. " +
                        "Pilih menu lain atau tarik tunai di ATM.");
                    return;
                }
            }

            if (choice.AffinityChange != 0 && _currentSpeakerData != null)
            {
                _currentSpeakerData.AddAffinity(choice.AffinityChange);
            }

            if (ScoreSystem.Instance != null)
            {
                if (!Mathf.Approximately(choice.FinancialLogicChange, 0f))
                {
                    ScoreSystem.Instance.AdjustFinancialLogic(choice.FinancialLogicChange);
                }

                if (!Mathf.Approximately(choice.ShariaComplianceChange, 0f))
                {
                    ScoreSystem.Instance.AdjustShariaCompliance(choice.ShariaComplianceChange);
                }

                if (!Mathf.Approximately(choice.BalanceCorrection, 0f))
                {
                    ScoreSystem.Instance.MoveTowardsBalance(choice.BalanceCorrection);
                }
            }

            OnChoiceSelected?.Invoke(choice);

            if (choice.NextLineIndex < 0 || choice.NextLineIndex >= _currentDialogue.Lines.Count)
            {
                EndDialogue();
                return;
            }

            _currentLineIndex = choice.NextLineIndex;
            ShowCurrentLine();
        }

        /// <summary>
        /// Akhiri dialog, kembalikan kontrol ke pemain, dan trigger event OnComplete
        /// yang sudah didefinisikan di DialogueData (misalnya untuk quest system).
        /// </summary>
        private void EndDialogue()
        {
            DialogueData completedDialogue = _currentDialogue;
            string completedEventId = completedDialogue.OnCompleteEventId;

            IsDialogueActive = false;
            _currentDialogue = null;
            _currentSpeakerData = null;
            _currentLineSpeakerData = null;
            _currentLineIndex = 0;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Playing);
            }

            if (_playerController != null)
            {
                _playerController.SetMovementLocked(this, false);
            }

            OnDialogueEnded?.Invoke();
            OnDialogueCompleted?.Invoke(completedDialogue);

            if (!string.IsNullOrEmpty(completedEventId))
            {
                Debug.Log($"Dialogue selesai, event id: {completedEventId}");
                // TODO: hubungkan ke quest/event system sesuai kebutuhan game.
            }
        }
    }
}
