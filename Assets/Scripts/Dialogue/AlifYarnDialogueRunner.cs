using System;
using UnityEngine;
using Yarn.Unity;
using Alif.Core;
using Alif.Player;

namespace Alif.Dialogue
{
    /// <summary>
    /// Bridge component between Yarn Spinner DialogueRunner and Alif game systems.
    /// Manages player movement locking, game state changes, and audio cues during Yarn dialogues.
    /// </summary>
    [RequireComponent(typeof(DialogueRunner))]
    public class AlifYarnDialogueRunner : MonoBehaviour
    {
        public static AlifYarnDialogueRunner Instance { get; private set; }

        private DialogueRunner _runner;
        private PlayerController _playerController;

        public bool IsRunning => _runner != null && _runner.IsDialogueRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _runner = GetComponent<DialogueRunner>();
            _runner.onDialogueStart.AddListener(OnDialogueStarted);
            _runner.onDialogueComplete.AddListener(OnDialogueCompleted);
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (_playerController == null)
            {
                _playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }
        }

        public void StartDialogue(string nodeName)
        {
            if (_runner == null) return;
            FindPlayer();
            _ = _runner.StartDialogue(nodeName);
        }

        private void OnDialogueStarted()
        {
            FindPlayer();
            _playerController?.SetMovementLocked(this, true);
            _playerController?.StopMotion();
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
        }

        private void OnDialogueCompleted()
        {
            FindPlayer();
            _playerController?.SetMovementLocked(this, false);
            GameManager.Instance?.SetState(GameManager.GameState.Playing);
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.onDialogueStart.RemoveListener(OnDialogueStarted);
                _runner.onDialogueComplete.RemoveListener(OnDialogueCompleted);
            }
        }
    }
}
