using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Alif.Characters;
using Alif.UI;

namespace Alif.Player
{
    /// <summary>
    /// Enum arah hadap karakter. Dipakai PlayerAnimation untuk memilih animasi
    /// idle/walk yang sesuai, termasuk 4 arah diagonal.
    /// </summary>
    public enum FacingDirection
    {
        South,
        North,
        East,
        West,
        SouthEast,
        SouthWest,
        NorthEast,
        NorthWest
    }

    /// <summary>
    /// Controller top-down 8 arah menggunakan Rigidbody2D dan Unity Input System (baru).
    /// Menangani movement, arah hadap, dan interaksi dengan NPC terdekat.
    ///
    /// Subscribe langsung ke InputAction "Move"/"Interact" dari sebuah InputActionAsset
    /// (bukan lewat komponen PlayerInput + "Send Messages" — behavior itu ternyata rapuh,
    /// bisa lempar MissingMethodException walau method-nya sudah ada, karena reflection
    /// internalnya sensitif terhadap urutan compile/reload domain).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Input Action Asset yang punya action map \"Player\" dengan action \"Move\" (Vector2) dan \"Interact\" (Button).")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;

        [Header("Interaction")]
        [Tooltip("Jarak maksimum untuk mendeteksi NPC terdekat saat tombol Interact ditekan.")]
        [SerializeField] private float _interactRadius = 1.2f;
        [SerializeField] private LayerMask _interactableLayer;

        [Header("References")]
        [SerializeField] private PlayerAnimation _playerAnimation;
        [Tooltip("Opsional: joystick on-screen. Kalau di-assign, nilainya digabung dengan input keyboard.")]
        [SerializeField] private VirtualJoystick _virtualJoystick;

        private Rigidbody2D _rigidbody;
        private InputAction _moveAction;
        private InputAction _interactAction;
        private Vector2 _moveInput;
        private FacingDirection _currentFacing = FacingDirection.South;

        // Bendera sederhana untuk mengunci input pergerakan, misalnya saat dialog sedang berlangsung.
        private bool _movementLocked = false;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            if (_playerAnimation == null)
            {
                _playerAnimation = GetComponent<PlayerAnimation>();
            }

            if (_inputActions != null)
            {
                InputActionMap map = _inputActions.FindActionMap(_actionMapName);
                if (map != null)
                {
                    _moveAction = map.FindAction("Move");
                    _interactAction = map.FindAction("Interact");
                }
            }
        }

        private void OnEnable()
        {
            _moveAction?.Enable();

            if (_interactAction != null)
            {
                _interactAction.Enable();
                _interactAction.performed += HandleInteractPerformed;
            }
        }

        private void OnDisable()
        {
            _moveAction?.Disable();

            if (_interactAction != null)
            {
                _interactAction.performed -= HandleInteractPerformed;
                _interactAction.Disable();
            }
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteractWithNearestNPC();
        }

        private void Update()
        {
            _moveInput = ReadMoveInput();

            UpdateFacingDirection();
            UpdateAnimation();
            UpdateInteractionPrompt();
            HandleMouseClick();
        }

        /// <summary>
        /// Gabungkan input keyboard (InputAction "Move") dengan joystick on-screen (kalau ada).
        /// Yang dipakai adalah mana yang magnitude-nya lebih besar, supaya pemain bisa pakai
        /// keduanya secara bebas tanpa saling mengganggu.
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            if (_movementLocked)
            {
                return Vector2.zero;
            }

            Vector2 keyboardInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 joystickInput = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;

            return keyboardInput.sqrMagnitude >= joystickInput.sqrMagnitude ? keyboardInput : joystickInput;
        }

        private void FixedUpdate()
        {
            if (_movementLocked)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            // Normalize supaya gerak diagonal tidak lebih cepat dari gerak lurus.
            Vector2 velocity = _moveInput.normalized * _moveSpeed;
            _rigidbody.linearVelocity = velocity;
        }

        /// <summary>
        /// Tentukan arah hadap (termasuk diagonal) berdasarkan vector input pergerakan.
        /// </summary>
        private void UpdateFacingDirection()
        {
            if (_moveInput.sqrMagnitude < 0.01f)
            {
                // Tidak ada input, pertahankan arah hadap terakhir (untuk idle animation).
                return;
            }

            float x = _moveInput.x;
            float y = _moveInput.y;

            // Ambang batas untuk mendeteksi gerakan diagonal murni vs lurus.
            const float diagonalThreshold = 0.4f;

            bool movingRight = x > diagonalThreshold;
            bool movingLeft = x < -diagonalThreshold;
            bool movingUp = y > diagonalThreshold;
            bool movingDown = y < -diagonalThreshold;

            if (movingUp && movingRight) _currentFacing = FacingDirection.NorthEast;
            else if (movingUp && movingLeft) _currentFacing = FacingDirection.NorthWest;
            else if (movingDown && movingRight) _currentFacing = FacingDirection.SouthEast;
            else if (movingDown && movingLeft) _currentFacing = FacingDirection.SouthWest;
            else if (movingUp) _currentFacing = FacingDirection.North;
            else if (movingDown) _currentFacing = FacingDirection.South;
            else if (movingRight) _currentFacing = FacingDirection.East;
            else if (movingLeft) _currentFacing = FacingDirection.West;
        }

        private void UpdateAnimation()
        {
            if (_playerAnimation == null)
            {
                return;
            }

            bool isMoving = _moveInput.sqrMagnitude > 0.01f;
            _playerAnimation.SetMovementState(_currentFacing, isMoving);
        }

        // GameObject panah "InteractionArrow" yang lagi ditampilkan (child dari interactable
        // terdekat saat ini), supaya bisa disembunyikan lagi begitu Player menjauh/pindah target.
        private GameObject _activePromptArrow;

        /// <summary>
        /// Cari collider IInteractable terdekat dalam radius interact (NPC atau benda statis
        /// seperti bangku). Dipakai baik oleh tombol Interact maupun klik mouse.
        /// </summary>
        private Collider2D FindNearestInteractableCollider()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _interactRadius, _interactableLayer);
            if (hits.Length == 0)
            {
                return null;
            }

            Collider2D nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = hit;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Trigger interaksi dengan objek IInteractable terdekat, kalau ada.
        /// </summary>
        private void TryInteractWithNearestNPC()
        {
            Collider2D nearest = FindNearestInteractableCollider();
            if (nearest == null)
            {
                return;
            }

            IInteractable interactable = nearest.GetComponent<IInteractable>();
            interactable?.Interact();
        }

        /// <summary>
        /// Tampilkan panah "InteractionArrow" (child object di NPC/benda interactable, dibuat
        /// oleh scene builder) di atas target terdekat saat ini, sembunyikan yang lama kalau
        /// targetnya berubah/hilang dari jangkauan.
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            Collider2D nearest = FindNearestInteractableCollider();
            GameObject newArrow = null;

            if (nearest != null)
            {
                Transform arrowTransform = nearest.transform.Find("InteractionArrow");
                if (arrowTransform != null)
                {
                    newArrow = arrowTransform.gameObject;
                }
            }

            if (newArrow == _activePromptArrow)
            {
                return;
            }

            if (_activePromptArrow != null)
            {
                _activePromptArrow.SetActive(false);
            }

            _activePromptArrow = newArrow;

            if (_activePromptArrow != null)
            {
                _activePromptArrow.SetActive(true);
            }
        }

        /// <summary>
        /// Klik langsung ke NPC/benda interactable (lewat mouse, kalau masih dalam jangkauan
        /// interact) sebagai alternatif tombol Interact/E. Diabaikan kalau klik-nya kena
        /// elemen UI (tombol HUD dsb), supaya nggak dobel trigger.
        /// </summary>
        private void HandleMouseClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 worldPoint = cam.ScreenToWorldPoint(mouseScreenPos);

            Collider2D hit = Physics2D.OverlapPoint(worldPoint, _interactableLayer);
            if (hit == null)
            {
                return;
            }

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance > _interactRadius)
            {
                return;
            }

            IInteractable interactable = hit.GetComponent<IInteractable>();
            interactable?.Interact();
        }

        /// <summary>
        /// Trigger interaksi dengan NPC terdekat. Dipanggil dari tombol Interact on-screen
        /// (Button.onClick), selain dari InputAction "Interact" via keyboard/gamepad.
        /// </summary>
        public void Interact()
        {
            TryInteractWithNearestNPC();
        }

        /// <summary>
        /// Kunci/lepas pergerakan player, dipanggil misalnya oleh DialogueManager saat dialog aktif.
        /// </summary>
        public void SetMovementLocked(bool isLocked)
        {
            _movementLocked = isLocked;
            if (isLocked)
            {
                _moveInput = Vector2.zero;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualisasi radius interact di Scene view untuk memudahkan tuning.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
