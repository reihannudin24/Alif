using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Alif.Characters;
using Alif.Core;
using Alif.UI;
using Alif.World;

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
        [SerializeField] private float _sprintMultiplier = 1.4f;
        [Tooltip("Percepatan menuju kecepatan target saat ada input (unit/detik²). Membuat start berhenti terasa instan.")]
        [SerializeField] private float _acceleration = 34f;
        [Tooltip("Perlambatan ke diam saat input dilepas (unit/detik²).")]
        [SerializeField] private float _deceleration = 46f;

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
        private Vector2 _feetOffset;
        private Vector2 _feetHalfExtents = new Vector2(0.16f, 0.12f);
        private bool _isSprinting;
        private float _footstepTimer;
        private float _lastBumpTime;
        private Vector2 _smoothVelocity;
        private CameraFollow _cameraFollow;
        private bool _sprintZoomActive;

        // Bendera sederhana untuk mengunci input pergerakan, misalnya saat dialog sedang berlangsung.
        private bool _movementLocked = false;
        private readonly HashSet<Object> _movementOwners = new HashSet<Object>();
        private PhysicsMaterial2D _runtimeMaterial;
        public Vector2 FacingVector => PlayerAnimation.FacingToVector(_currentFacing);
        public Vector2 MoveInput => _moveInput;
        public bool IsSprinting => _isSprinting;
        private Vector2 _adventureDirection;
        public void ConfigureAdventure() { _interactableLayer = 1 << 7; _interactRadius = 1.35f; _moveSpeed = 3f; }
        public void SetAdventureDirection(Vector2 direction) { _adventureDirection = direction; }
        public bool MovementLocked => _movementLocked || _movementOwners.Count > 0;
        private bool InputBlocked => MovementLocked || (GameManager.Instance != null &&
            (GameManager.Instance.CurrentState == GameManager.GameState.Paused || GameManager.Instance.CurrentState == GameManager.GameState.Dialogue || GameManager.Instance.CurrentState == GameManager.GameState.Combat));
        private bool InteractionBlocked => InputBlocked;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            // Pastikan physics material tanpa friksi agar karakter meluncur mulus tanpa tersangkut di dinding
            PhysicsMaterial2D frictionlessMat = _runtimeMaterial = new PhysicsMaterial2D("PlayerFrictionless") { friction = 0f, bounciness = 0f };
            _rigidbody.sharedMaterial = frictionlessMat;

            CapsuleCollider2D feetCollider = GetComponent<CapsuleCollider2D>();
            if (feetCollider != null)
            {
                feetCollider.sharedMaterial = frictionlessMat;
                _feetOffset = feetCollider.offset;
                _feetHalfExtents = feetCollider.size * 0.5f;
            }

            // Pastikan kedalaman render Y-sorting otomatis terpasang
            if (GetComponent<YSortOrder>() == null)
            {
                gameObject.AddComponent<YSortOrder>();
            }

            // Physics2D biasanya berjalan 50 Hz, sedangkan render umumnya 60 Hz atau lebih.
            // Tanpa interpolation, sprite terlihat melompat antar-tick saat berjalan dan
            // kamera yang mengikuti Player ikut tampak tersendat.
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

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
            _movementOwners.RemoveWhere(owner => owner == null);
            bool shiftPressed = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            _moveInput = ReadMoveInput();
            _isSprinting = shiftPressed && _moveInput.sqrMagnitude > 0.01f;

            // Sinkronkan zoom kamera sprint hanya saat statusnya berubah (bukan tiap frame).
            if (_isSprinting != _sprintZoomActive)
            {
                _sprintZoomActive = _isSprinting;
                if (_cameraFollow == null) _cameraFollow = FindAnyObjectByType<CameraFollow>();
                _cameraFollow?.SetSprintZoom(_isSprinting);
            }

            UpdateFacingDirection();
            UpdateAnimation();
            UpdateInteractionPrompt();
            HandleMouseClick();
            HandleFootstepSound();
            if (_inputActions == null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                TryInteractWithNearestNPC();
        }

        private void HandleFootstepSound()
        {
            if (InputBlocked || _rigidbody.linearVelocity.sqrMagnitude < 0.01f)
            {
                _footstepTimer = 0.06f;
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = _isSprinting ? 0.22f : 0.34f;
                AudioManager.Instance?.PlayFootstep(0.08f, _isSprinting ? 0.62f : 0.42f);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Time.time - _lastBumpTime > 0.35f && collision.relativeVelocity.sqrMagnitude > 3.5f)
            {
                _lastBumpTime = Time.time;
                AudioManager.Instance?.PlayBump(0.65f);
            }
        }

        /// <summary>
        /// Gabungkan input keyboard (InputAction "Move") dengan joystick on-screen (kalau ada).
        /// Yang dipakai adalah mana yang magnitude-nya lebih besar, supaya pemain bisa pakai
        /// keduanya secara bebas tanpa saling mengganggu.
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            if (InputBlocked)
            {
                return Vector2.zero;
            }

            Vector2 keyboardInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            if (_moveAction == null && Keyboard.current != null)
            {
                var k = Keyboard.current;
                keyboardInput = new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
                if (keyboardInput.sqrMagnitude < .01f) keyboardInput = _adventureDirection;
            }
            Vector2 joystickInput = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;

            return keyboardInput.sqrMagnitude >= joystickInput.sqrMagnitude ? keyboardInput : joystickInput;
        }

        private void FixedUpdate()
        {
            if (InputBlocked)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _smoothVelocity = Vector2.zero;
                return;
            }

            // Normalize supaya gerak diagonal tidak lebih cepat dari gerak lurus. WalkableArea
            // menjadi whitelist lantai; collider physics biasa tetap menangani tembok/objek.
            float currentSpeed = _isSprinting ? (_moveSpeed * _sprintMultiplier) : _moveSpeed;
            Vector2 target = Vector2.ClampMagnitude(_moveInput, 1f) * currentSpeed;
            float rate = _moveInput.sqrMagnitude > .01f ? _acceleration : _deceleration;
            _smoothVelocity = Vector2.MoveTowards(_smoothVelocity, target, rate * Time.fixedDeltaTime);
            Vector2 velocity = ConstrainVelocityToWalkableFloor(_smoothVelocity);
            _smoothVelocity = velocity; // constraint bisa memangkas kecepatan (dinding/lantai) — jangan menumpuk selisihnya
            _rigidbody.linearVelocity = velocity;
        }

        private Vector2 ConstrainVelocityToWalkableFloor(Vector2 requestedVelocity)
        {
            if (requestedVelocity.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            Vector2 currentFeet = _rigidbody.position + _feetOffset;

            // Hanya aktif ketika Player sedang berada di map yang punya whitelist lantai.
            // Area lain yang belum dimigrasikan tetap berjalan dengan collider lamanya.
            if (!WalkableArea.ContainsPoint(currentFeet))
            {
                return requestedVelocity;
            }

            Vector2 displacement = requestedVelocity * Time.fixedDeltaTime;
            if (WalkableArea.ContainsFootprint(currentFeet + displacement, _feetHalfExtents))
            {
                return requestedVelocity;
            }

            // Coba tiap sumbu terpisah supaya Player tetap bisa meluncur menyusuri tepi lantai,
            // alih-alih terasa tersangkut total ketika input diagonal menyentuh batas.
            Vector2 xVelocity = new Vector2(requestedVelocity.x, 0f);
            Vector2 yVelocity = new Vector2(0f, requestedVelocity.y);
            bool canMoveX = Mathf.Abs(xVelocity.x) > 0.0001f
                && WalkableArea.ContainsFootprint(currentFeet + xVelocity * Time.fixedDeltaTime, _feetHalfExtents);
            bool canMoveY = Mathf.Abs(yVelocity.y) > 0.0001f
                && WalkableArea.ContainsFootprint(currentFeet + yVelocity * Time.fixedDeltaTime, _feetHalfExtents);

            if (canMoveX && canMoveY)
            {
                // Evaluasi apakah langkah diagonal parsial bisa lolos sebelum mengorbankan salah satu sumbu
                Vector2 partialDisplacement = requestedVelocity * 0.65f * Time.fixedDeltaTime;
                if (WalkableArea.ContainsFootprint(currentFeet + partialDisplacement, _feetHalfExtents))
                {
                    return requestedVelocity * 0.65f;
                }
                return Mathf.Abs(requestedVelocity.x) >= Mathf.Abs(requestedVelocity.y) ? xVelocity : yVelocity;
            }

            if (canMoveX) return xVelocity;
            if (canMoveY) return yVelocity;
            return Vector2.zero;
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

            bool isMoving = _rigidbody.linearVelocity.sqrMagnitude > 0.01f;
            _playerAnimation.SetMovementState(_currentFacing, isMoving, _isSprinting);
        }

        // GameObject panah "InteractionArrow" yang lagi ditampilkan (child dari interactable
        // terdekat saat ini), supaya bisa disembunyikan lagi begitu Player menjauh/pindah target.
        private GameObject _activePromptArrow;

        /// <summary>
        /// Cari collider IInteractable terdekat dalam radius interact (NPC atau benda statis
        /// seperti bangku). Dipakai baik oleh tombol Interact maupun klik mouse.
        /// </summary>
        private static string HierarchyPath(Transform value)
        {
            string path = value.name + ":" + value.GetSiblingIndex();
            while (value.parent != null) { value = value.parent; path = value.name + ":" + value.GetSiblingIndex() + "/" + path; }
            return path;
        }
        public Collider2D InteractionTarget { get; private set; }
        private Collider2D FindNearestInteractableCollider()
        {
            if (InteractionBlocked) return null;
            var hits = Physics2D.OverlapCircleAll(transform.position, _interactRadius, _interactableLayer);
            Collider2D best = null;
            int bestPriority = int.MinValue;
            float bestFacing = float.MinValue, bestDistance = float.MaxValue;
            foreach (var hit in hits)
            {
                if (GetInteractable(hit) == null) continue;
                Vector2 delta = hit.ClosestPoint(transform.position) - (Vector2)transform.position;
                float distance = delta.magnitude;
                int priority = hit.GetComponentInParent<InteractionPriority>()?.Priority ?? 0;
                float facing = Vector2.Dot(FacingVector, delta.normalized) >= 0 ? 1 : 0;
                if (best == null || priority > bestPriority || (priority == bestPriority &&
                    (facing > bestFacing || (facing == bestFacing && (distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) && string.CompareOrdinal(HierarchyPath(hit.transform), HierarchyPath(best.transform)) < 0))))))
                { best = hit; bestPriority = priority; bestFacing = facing; bestDistance = distance; }
            }
            return best;
        }

        /// <summary>
        /// Trigger interaksi dengan objek IInteractable terdekat, kalau ada.
        /// </summary>
        private void TryInteractWithNearestNPC()
        {
            Collider2D nearest = InteractionTarget = FindNearestInteractableCollider();
            if (nearest == null)
            {
                return;
            }

            IInteractable interactable = GetInteractable(nearest);
            interactable?.Interact();
        }

        /// <summary>
        /// Tampilkan panah "InteractionArrow" (child object di NPC/benda interactable, dibuat
        /// oleh scene builder) di atas target terdekat saat ini, sembunyikan yang lama kalau
        /// targetnya berubah/hilang dari jangkauan.
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            Collider2D nearest = InteractionTarget = FindNearestInteractableCollider();
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
                // Panah interaksi mendapat animasi pop + bob ringan saat muncul (sekali pasang,
                // OnEnable komponen me-restart animasinya setiap kali panah ditampilkan ulang).
                if (_activePromptArrow.GetComponent<InteractionPromptFX>() == null)
                {
                    _activePromptArrow.AddComponent<InteractionPromptFX>();
                }
            }
        }

        /// <summary>
        /// Klik langsung ke NPC/benda interactable (lewat mouse, kalau masih dalam jangkauan
        /// interact) sebagai alternatif tombol Interact/E. Diabaikan kalau klik-nya kena
        /// elemen UI (tombol HUD dsb), supaya nggak dobel trigger.
        /// </summary>
        private void HandleMouseClick()
        {
            if (InteractionBlocked) return;
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

            Collider2D target = InteractionTarget = FindNearestInteractableCollider();
            if (target != null && target.OverlapPoint(worldPoint)) GetInteractable(target)?.Interact();
        }

        private static IInteractable GetInteractable(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            Transform current = collider.transform;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour != null && behaviour.isActiveAndEnabled && behaviour is IInteractable interactable)
                    {
                        return interactable;
                    }
                }
                current = current.parent;
            }
            return null;
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
        public void SetMovementLocked(Object owner, bool isLocked)
        {
            if (owner == null) return;
            if (isLocked) _movementOwners.Add(owner); else _movementOwners.Remove(owner);
            if (MovementLocked) StopMotion();
        }
        public void BindJoystick(VirtualJoystick joystick) => _virtualJoystick = joystick;
        public void StopMotion()
        {
            _moveInput = _smoothVelocity = _adventureDirection = Vector2.zero;
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector2.zero;
        }
        private void OnDestroy() { if (_runtimeMaterial != null) Destroy(_runtimeMaterial); }
        public void SetMovementLocked(bool isLocked)
        {
            _movementLocked = isLocked;
            if (isLocked)
            {
                StopMotion();
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
