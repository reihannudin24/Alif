using System.Collections;
using UnityEngine;
using Alif.Characters;
using Alif.Core;
using Alif.Dialogue;
using Alif.UI;

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
        private int _choiceCount;

        private static readonly string[] ChapterObjectiveSteps =
        {
            "Periksa biaya dan risiko pinjaman cepat",
            "Susun alternatif dana yang aman",
            "Bantu Dimas membatalkan pengajuan"
        };

        private IEnumerator Start()
        {
            ObjectiveUI.Instance?.SetObjective("Bantu Dimas menghindari pinjol", ChapterObjectiveSteps);
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
            DialogueManager.Instance.OnChoiceSelected += HandleChoiceSelected;
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
            DialogueManager.Instance.OnChoiceSelected -= HandleChoiceSelected;
            _subscribed = false;
        }

        private void HandleChoiceSelected(DialogueChoice choice)
        {
            if (_storyFinished || choice == null)
            {
                return;
            }

            _choiceCount++;
            if (_choiceCount >= 4)
            {
                ObjectiveUI.Instance?.SetCurrentStep(2);
            }
            else if (_choiceCount >= 2)
            {
                ObjectiveUI.Instance?.SetCurrentStep(1);
            }
        }

        private void HandleDialogueCompleted(DialogueData completedDialogue)
        {
            if (_storyFinished || completedDialogue != _mainDialogue)
            {
                return;
            }

            _storyFinished = true;
            ObjectiveUI.Instance?.CompleteObjective();
            ChapterProgress.CompleteChapter(2, true);
            Debug.Log("[Alif] Chapter 2 selesai: Dimas membatalkan rencana pinjol dan menyusun solusi yang transparan.");
        }
    }
}
