using System;
using System.Collections.Generic;
using UnityEngine;
using Alif.Characters;
using Alif.Core;
using Alif.Player;

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
        private int _currentLineIndex;

        public bool IsDialogueActive { get; private set; }

        /// <summary>
        /// Portrait pembicara saat ini (dari CharacterData yang di-pass ke StartDialogue),
        /// dibaca DialogueUI untuk ditampilkan di dialogue box. Null kalau dialog nggak
        /// punya speakerData (misal monolog tanpa portrait).
        /// </summary>
        public Sprite CurrentSpeakerPortrait => _currentSpeakerData != null ? _currentSpeakerData.Portrait : null;

        // Event untuk DialogueUI: dipanggil setiap kali baris dialog baru ditampilkan.
        public event Action<DialogueLine> OnLineDisplayed;

        // Event untuk DialogueUI: dipanggil saat dialog dimulai/selesai.
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Dialogue);
            }

            if (_playerController != null)
            {
                _playerController.SetMovementLocked(true);
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
            OnLineDisplayed?.Invoke(line);
        }

        /// <summary>
        /// Lanjut ke baris berikutnya. Dipanggil oleh DialogueUI saat pemain klik "next"
        /// pada baris yang tidak punya pilihan (linear dialogue).
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!IsDialogueActive)
            {
                return;
            }

            DialogueLine currentLine = _currentDialogue.Lines[_currentLineIndex];
            if (currentLine.HasChoices)
            {
                // Baris dengan pilihan harus dilanjutkan lewat SelectChoice, bukan AdvanceDialogue biasa.
                return;
            }

            int nextIndex = _currentLineIndex + 1;
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
            if (!IsDialogueActive || choice == null)
            {
                return;
            }

            if (choice.AffinityChange != 0 && _currentSpeakerData != null)
            {
                _currentSpeakerData.AddAffinity(choice.AffinityChange);
            }

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
            string completedEventId = _currentDialogue.OnCompleteEventId;

            IsDialogueActive = false;
            _currentDialogue = null;
            _currentSpeakerData = null;
            _currentLineIndex = 0;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameManager.GameState.Playing);
            }

            if (_playerController != null)
            {
                _playerController.SetMovementLocked(false);
            }

            OnDialogueEnded?.Invoke();

            if (!string.IsNullOrEmpty(completedEventId))
            {
                Debug.Log($"Dialogue selesai, event id: {completedEventId}");
                // TODO: hubungkan ke quest/event system sesuai kebutuhan game.
            }
        }
    }
}
