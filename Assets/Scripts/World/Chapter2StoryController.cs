using System.Collections;
using UnityEngine;
using Alif.Characters;
using Alif.Core;
using Alif.Dialogue;

namespace Alif.World
{
    /// <summary>
    /// Mengelola percakapan utama Chapter 2. Dialog dimulai otomatis setelah pemain masuk
    /// kamar Dimas, lalu dapat diputar ulang lewat titik interaksi Dimas setelah selesai.
    /// </summary>
    public class Chapter2StoryController : MonoBehaviour
    {
        [SerializeField] private DialogueData _mainDialogue;
        [SerializeField] private DialogueData _finishedDialogue;
        [SerializeField] private CharacterData _dimasData;
        [SerializeField] private float _openingDelay = 0.65f;

        private bool _storyFinished;
        private bool _subscribed;

        private IEnumerator Start()
        {
            Subscribe();
            yield return new WaitForSeconds(_openingDelay);
            RequestConversation();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void RequestConversation()
        {
            Subscribe();
            if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive)
            {
                return;
            }

            DialogueData dialogue = _storyFinished ? _finishedDialogue : _mainDialogue;
            if (dialogue != null)
            {
                DialogueManager.Instance.StartDialogue(dialogue, _dimasData);
            }
        }

        private void Subscribe()
        {
            if (_subscribed || DialogueManager.Instance == null)
            {
                return;
            }

            DialogueManager.Instance.OnDialogueCompleted += HandleDialogueCompleted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || DialogueManager.Instance == null)
            {
                _subscribed = false;
                return;
            }

            DialogueManager.Instance.OnDialogueCompleted -= HandleDialogueCompleted;
            _subscribed = false;
        }

        private void HandleDialogueCompleted(DialogueData completedDialogue)
        {
            if (_storyFinished || completedDialogue != _mainDialogue)
            {
                return;
            }

            _storyFinished = true;
            ChapterProgress.CompleteChapter(2, true);
            Debug.Log("[Alif] Chapter 2 selesai: Dimas membatalkan rencana pinjol dan menyusun solusi yang transparan.");
        }
    }
}
