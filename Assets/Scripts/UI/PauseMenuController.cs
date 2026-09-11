using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Alif.Dialogue;
using Alif.Core;

namespace Alif.UI
{
    /// <summary>
    /// Tombol pause di pojok layar gameplay + tombol back Android/ESC ("Cancel" action dari
    /// InputSystem_Actions, map "UI") buat munculin menu pause kecil (isinya "Keluar" &
    /// "Lanjut") atau popup konfirmasi keluar langsung. Back/ESC nggak ngapa-ngapain kalau
    /// lagi dialog (biar nggak tabrakan sama input dialog) — DialogueUI yang pegang kendali
    /// input waktu itu.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Input Action Asset yang punya action map \"UI\" dengan action \"Cancel\" (dipetakan ke Escape/back Android secara default).")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "UI";
        [SerializeField] private string _cancelActionName = "Cancel";

        [Header("References")]
        [SerializeField] private GameObject _pauseMenuRoot;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private QuitConfirmationUI _quitConfirmation;

        private InputAction _cancelAction;

        private void Awake()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(OpenPauseMenu);
            }

            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(ClosePauseMenu);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(HandleExitClicked);
            }

            if (_inputActions != null)
            {
                InputActionMap map = _inputActions.FindActionMap(_actionMapName);
                if (map != null)
                {
                    _cancelAction = map.FindAction(_cancelActionName);
                }
            }

            ClosePauseMenu();
        }

        private void OnEnable()
        {
            if (_cancelAction != null)
            {
                _cancelAction.Enable();
                _cancelAction.performed += HandleCancelPerformed;
            }
        }

        private void OnDisable()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= HandleCancelPerformed;
                _cancelAction.Disable();
            }
        }

        /// <summary>
        /// Satu tombol back/ESC, tiga kemungkinan: tutup popup konfirmasi kalau lagi kebuka,
        /// atau tutup menu pause kalau lagi kebuka, atau (kalau nggak ada apa-apa yang kebuka)
        /// langsung munculin popup konfirmasi keluar.
        /// </summary>
        private void HandleCancelPerformed(InputAction.CallbackContext context)
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                return;
            }

            if (_quitConfirmation != null && _quitConfirmation.IsShowing)
            {
                _quitConfirmation.Hide();
                return;
            }

            if (_pauseMenuRoot != null && _pauseMenuRoot.activeSelf)
            {
                ClosePauseMenu();
                return;
            }

            if (_quitConfirmation != null)
            {
                _quitConfirmation.Show();
            }
        }

        private void OpenPauseMenu()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                return;
            }

            if (_pauseMenuRoot != null)
            {
                _pauseMenuRoot.SetActive(true);
            }
        }

        private void ClosePauseMenu()
        {
            if (_pauseMenuRoot != null)
            {
                _pauseMenuRoot.SetActive(false);
            }
        }

        private void HandleExitClicked()
        {
            ClosePauseMenu();

            if (_quitConfirmation != null)
            {
                _quitConfirmation.Show();
            }
        }
    }
}
